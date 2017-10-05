using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FeatureFinder.Control;
using FeatureFinder.Data;
using FeatureFinder.Data.Maps;
using FeatureFinder.Utilities;
using UIMFLibrary;

namespace FeatureFinder.Algorithms
{
    public static class ConformationDetection
    {
        private const double DRIFT_TIME_WINDOW_WIDTH = 0.6;
        private const double DRIFT_TIME_SLICE_WIDTH = 0.1;
        private const double FRAME_PRESSURE_STANDARD = 4.0;

        public static IEnumerable<LCIMSMSFeature> DetectConformationsUsingRawData(IReadOnlyList<LCIMSMSFeature> lcImsMSFeatures)
        {
            var newLCIMSMSFeatureList = new List<LCIMSMSFeature>();
            var uimfFile = new FileInfo(Path.Combine(Settings.InputDirectory, FileUtil.GetUimfFileForIsosFile(Settings.InputFileName)));

            if (!uimfFile.Exists)
            {
                Logger.Log("Uimf file not found at " + uimfFile.FullName + "; skipping conformer detection");
                return newLCIMSMSFeatureList;
            }

            using (var uimfReader = new DataReader(uimfFile.FullName))
            {
                Logger.Log("UIMF file has been opened.");

                var globalParams = uimfReader.GetGlobalParams();

                var binWidth = globalParams.BinWidth;
                var featureCount = lcImsMSFeatures.Count;
                var featuresProcessed = 0;
                var lastProgressUpdate = DateTime.UtcNow;

                foreach (var lcimsmsFeature in lcImsMSFeatures)
                {
                    var scanLC = ScanLCMap.Mapping[lcimsmsFeature.IMSMSFeatureList[0].ScanLC];

                    var frameParams= uimfReader.GetFrameParams(scanLC);

                    var calibrationSlope = frameParams.CalibrationSlope;
                    var calibrationIntercept = frameParams.CalibrationIntercept;
                    var averageTOFLength = frameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength);
                    var framePressure = uimfReader.GetFramePressureForCalculationOfDriftTime(scanLC);
                    var frameType = frameParams.FrameType;


                    //For saturated Features, will extract the imsScan profile from the MSFeature data (which contains the adjusted intensities)
                    //For non-saturated Features, extract the imsScan profile from the raw data. Then normalize it and scale the intensities to match that of MSFeature data
                    //We need to do the normalization and scaling so the two approaches give comparable intensity outputs
                    var containsSaturatedFeatures = lcimsmsFeature.GetSaturatedMemberCount() > 0;

                    var msfeature = lcimsmsFeature.GetMSFeatureRep();
                    var maxIntensity = msfeature.Abundance;

                    //Some features are barely saturated. We want to go to the raw data for those. (We miss these if we don't!)
                    var isAboveIntensityThreshold = msfeature.IntensityUnSummed > 25000;     //note that Unsummed MS is considered saturated at 50000;

                    List<XYPair> imsScanProfile;
                    if (containsSaturatedFeatures && isAboveIntensityThreshold)
                    {
                        imsScanProfile = lcimsmsFeature.GetIMSScanProfileFromMSFeatures();

                    }
                    else
                    {
                        imsScanProfile = lcimsmsFeature.GetIMSScanProfileFromRawData(uimfReader, frameType, binWidth,
                                                                                     calibrationSlope,
                                                                                     calibrationIntercept);
                    }

                    var maxIntensityFromProfile = imsScanProfile.Select(p => p.YValue).Max();

                    //normalize and scale the intensity values based on the max intensity of the ims profile from MSFeature data
                    //this is needed so that intensities from both IMSScanProfile extraction algorithms are comparable
                    foreach (var xyPair in imsScanProfile)
                    {
                        //double uncorrectedYValue = xyPair.YValue;

                        xyPair.YValue = xyPair.YValue/maxIntensityFromProfile*
                                        maxIntensity;

                        //Console.WriteLine(xyPair.XValue + "\t" + uncorrectedYValue + "\t" + xyPair.YValue);
                    }


                    var driftProfilePeak = new Peak(imsScanProfile);

                    //DisplayPeakXYData(driftProfilePeak);

                    var lcimsmsFeaturesWithDriftTimes = FindDriftTimePeaks(driftProfilePeak,
                                                                           lcimsmsFeature,
                                                                           averageTOFLength,
                                                                           framePressure);
                    newLCIMSMSFeatureList.AddRange(lcimsmsFeaturesWithDriftTimes);

                    featuresProcessed++;

                    if (DateTime.UtcNow.Subtract(lastProgressUpdate).TotalSeconds >= 30)
                    {
                        lastProgressUpdate = DateTime.UtcNow;
                        var percentComplete = (double)featuresProcessed / featureCount * 100;
                        Logger.Log("  " + percentComplete.ToString("0.0") + "% complete");
                    }


                }

            }
            return newLCIMSMSFeatureList;
        }

        public static IEnumerable<LCIMSMSFeature> FindDriftTimePeaks(Peak driftProfilePeak, LCIMSMSFeature lcimsmsFeature, double averageTOFLength, double framePressure)
        {
            var imsmsFeatureList = lcimsmsFeature.IMSMSFeatureList;

            var sortByScanLCQuery = from imsmsFeature in imsmsFeatureList
                                    orderby imsmsFeature.ScanLC
                                    select imsmsFeature;

            var globalIMSScanMinimum = double.MaxValue;
            var globalIMSScanMaximum = double.MinValue;

            // Grab all of the intensity values for each IMS-MS Feature and find the global minimum and maximum Drift Times
            foreach (var imsmsFeature in sortByScanLCQuery)
            {
                imsmsFeature.GetMinAndMaxIMSScan(out var localIMSScanMinimum, out var localIMSScanMaximum);

                if (localIMSScanMinimum < globalIMSScanMinimum) globalIMSScanMinimum = localIMSScanMinimum;
                if (localIMSScanMaximum > globalIMSScanMaximum) globalIMSScanMaximum = localIMSScanMaximum;
            }

            var smoothedDriftProfilePeak = PeakUtil.KDESmooth(driftProfilePeak, Settings.SmoothingStDev); // TODO: Find a good value. 0.15? Less smooth = more conformations!

            var smoothedDriftProfileInterpolation = PeakUtil.GetLinearInterpolationMethod(smoothedDriftProfilePeak);

            var xyPairList = new List<XYPair>();
            var peakList = new List<Peak>();
            var previousIntensity = double.MinValue;
            var movingUp = true;

            // lcimsmsFeature.GetMinAndMaxScanLC(out var minScanLC, out var maxScanLC);

            var minimumIntensityToConsider = smoothedDriftProfilePeak.GetMaximumYValue() * 0.05;

            //DisplayPeakXYData(smoothedDriftProfilePeak);

            //Console.WriteLine("Global IMS Scan Min = " + globalIMSScanMinimum + "\tGlobal IMS Scan Max = " + globalIMSScanMaximum);

            for (var i = globalIMSScanMinimum; i <= globalIMSScanMaximum; i += 1)
            {
                var imsScan = i;
                var intensity = smoothedDriftProfileInterpolation.Interpolate(imsScan);

                if (intensity > minimumIntensityToConsider)
                {
                    //Console.WriteLine(imsScan + "\t" + intensity + "\t" + movingUp);

                    if (intensity > previousIntensity)
                    {
                        // End of Peak
                        if (!movingUp && xyPairList.Count > 0)
                        {
                            PadXYPairsWithZeros(ref xyPairList, 2);
                            //xyPairList = PadXYPairsWithZeros(xyPairList, imsScanMinimum, i - DRIFT_TIME_SLICE_WIDTH, 1);
                            var peak = new Peak(xyPairList);

                            if (peak.XYPairList.Count >= 7)
                            {
                                peakList.Add(peak);
                            }

                            // Start over with a new Peak
                            xyPairList.Clear();
                            movingUp = true;
                        }
                    }
                    else
                    {
                        movingUp = false;
                    }

                    var xyPair = new XYPair(imsScan, intensity);
                    xyPairList.Add(xyPair);

                    previousIntensity = intensity;
                }
                else
                {
                    movingUp = false;
                    previousIntensity = 0;
                }
            }

            // When you get to the end, end the last Peak, but only if it has a non-zero value
            if (xyPairList.Any(xyPair => xyPair.YValue > minimumIntensityToConsider))
            {
                PadXYPairsWithZeros(ref xyPairList, 2);
                //xyPairList = PadXYPairsWithZeros(xyPairList, imsScanMinimum, globalIMSScanMaximum, 1);
                var lastPeak = new Peak(xyPairList);

                if (lastPeak.XYPairList.Count >= 7)
                {
                    peakList.Add(lastPeak);
                }
            }

            var resolvingPower = GetResolvingPower(lcimsmsFeature.Charge);

            var newLCIMSMSFeatureList = new List<LCIMSMSFeature>();

            foreach (var peak in peakList)
            {
                var repIMSScan = peak.GetQuadraticFit();

                // TODO: Fix this
                //double theoreticalFWHM = driftTime / resolvingPower;
                double theoreticalFWHM = 3;

                peak.GetMinAndMaxXValues(out var minimumXValue, out var maximumXValue);

                const int numPoints = 100;

                var normalDistributionXYPairList = PeakUtil.CreateTheoreticalGaussianPeak(repIMSScan, theoreticalFWHM, numPoints);
                PadXYPairsWithZeros(ref normalDistributionXYPairList, 5);
                var normalDistributionPeak = new Peak(normalDistributionXYPairList);

                var peakInterpolation = PeakUtil.GetLinearInterpolationMethod(peak);

                var fitScore = PeakUtil.CalculatePeakFit(peak, normalDistributionPeak, 0);

                // Create a new LC-IMS-MS Feature
                var newLCIMSMSFeature = new LCIMSMSFeature(lcimsmsFeature.Charge)
                                                    {
                                                        OriginalIndex = lcimsmsFeature.OriginalIndex,
                                                        IMSScore = (float) fitScore,
                                                        AbundanceMaxRaw = Math.Round(peak.GetMaximumYValue()),
                                                        // Using Math.Floor instaed of Math.Round because I used to cast this to an int which is esentially Math.Floor.
                                                        // The difference is negligible, but OHSU would complain if results were the slightest bit different if the app was re-run on the same dataset.
                                                        AbundanceSumRaw = Math.Floor(peakInterpolation.Integrate(peak.GetMaximumXValue())),
                                                        DriftTime = ConvertIMSScanToDriftTime(repIMSScan, averageTOFLength, framePressure)
                                                    };

                // Create new IMS-MS Features by grabbing MS Features in each LC Scan that are in the defined window of the detected drift time
                foreach (var imsmsFeature in lcimsmsFeature.IMSMSFeatureList)
                {
                    var msFeatures = imsmsFeature.FindMSFeaturesInScanIMSRange(minimumXValue, maximumXValue).ToList();

                    if (!msFeatures.Any()) continue;

                    var newIMSMSFeature = new IMSMSFeature(imsmsFeature.ScanLC, imsmsFeature.Charge);
                    newIMSMSFeature.AddMSFeatureList(msFeatures);
                    newLCIMSMSFeature.AddIMSMSFeature(newIMSMSFeature);
                }

                if (newLCIMSMSFeature.IMSMSFeatureList.Count > 0)
                {
                    newLCIMSMSFeatureList.Add(newLCIMSMSFeature);
                    /*
                    // TODO: Find LC Peaks
                    var sortByScanLC = from imsmsFeature in newLCIMSMSFeature.IMSMSFeatureList
                                       orderby imsmsFeature.ScanLC ascending
                                       select imsmsFeature;

                    Console.WriteLine("*************************************************");
                    Console.WriteLine("Index = " + index + "\tMass = " + newLCIMSMSFeature.CalculateAverageMass() + "\tDrift = " + driftTime + "\tLC Range = " + sortByScanLC.First().ScanLC + "\t" + sortByScanLC.Last().ScanLC);

                    List<XYPair> lcXYPairList = new List<XYPair>();
                    int scanLC = sortByScanLC.First().ScanLC - 1;

                    foreach (IMSMSFeature imsmsFeature in sortByScanLC)
                    {
                        int currentScanLC = imsmsFeature.ScanLC;

                        for (int i = scanLC + 1; i < currentScanLC; i++)
                        {
                            XYPair zeroValue = new XYPair(i, 0);
                            lcXYPairList.Add(zeroValue);
                            Console.Write("0\t");
                        }

                        XYPair xyPair = new XYPair(currentScanLC, imsmsFeature.GetIntensity());
                        lcXYPairList.Add(xyPair);

                        scanLC = currentScanLC;

                        Console.Write(imsmsFeature.GetIntensity() + "\t");
                    }
                    Console.WriteLine("");
                    Console.WriteLine("*************************************************");
                    */
                    // TODO: Calculate LC Score
                }
                else
                {
                    //Console.WriteLine("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$ FOUND EMPTY $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
                    // TODO: Figure out why this actually happens. I believe that this SHOULD NOT happen. Below is a hack to return a conformation even if this happens
                    // It actually looks like most of these occurences are due to large gaps in the drift time, which cause a small peak to be found in the gap which has no members.

                    //Console.WriteLine("**********************************************************************");
                    //Console.WriteLine("Detected Drift Time = " + driftTime + "\tLow = " + lowDriftTime + "\tHigh = " + highDriftTime);
                    //lcimsmsFeature.PrintLCAndDriftTimeMap();
                    //Console.WriteLine("**********************************************************************");

                    //Console.WriteLine("===============================================================");
                    //Console.WriteLine("DT = " + driftTime + "\tLow DT = " + lowDriftTime + "\tHigh DT = " + highDriftTime);
                    //Console.WriteLine("Global Min = " + globalDriftTimeMinimum + "\tGlobal Max = " + globalDriftTimeMaximum);
                    //peak.PrintPeakToConsole();
                    //Console.WriteLine("===============================================================");
                }
            }

            // Find the Conformation that has the highest member count and store the value into all conformations of this LC-IMS-MS Feature
            if (newLCIMSMSFeatureList.Count > 0)
            {
                var maxMemberCount = newLCIMSMSFeatureList.Select(feature => feature.GetMemberCount()).Max();

                foreach (var feature in newLCIMSMSFeatureList)
                {
                    feature.MaxMemberCount = maxMemberCount;
                }
            }

            return newLCIMSMSFeatureList;
        }

        private static void DisplayPeakXYData(Peak smoothedDriftProfilePeak)
        {
            foreach (var xypair in smoothedDriftProfilePeak.XYPairList)
            {
                Console.WriteLine(xypair.XValue + "\t" + xypair.YValue);

            }
        }

        public static void PadXYPairsWithZeros(ref List<XYPair> driftProfileXYPairList, double globalDriftTimeMinimum, double globalDriftTimeMaximum, int numZeros)
        {
            var lowDriftTime = globalDriftTimeMinimum - (DRIFT_TIME_SLICE_WIDTH / 1000);
            var highDriftTime = globalDriftTimeMaximum + (DRIFT_TIME_SLICE_WIDTH / 1000);

            var lowXYPair = new XYPair(lowDriftTime, 0);
            var highXYPair = new XYPair(highDriftTime, 0);

            driftProfileXYPairList.Add(lowXYPair);
            driftProfileXYPairList.Add(highXYPair);

            for (var i = 1; i <= numZeros; i++)
            {
                lowDriftTime = globalDriftTimeMinimum - (DRIFT_TIME_SLICE_WIDTH * i);
                highDriftTime = globalDriftTimeMaximum + (DRIFT_TIME_SLICE_WIDTH * i);

                lowXYPair = new XYPair(lowDriftTime, 0);
                highXYPair = new XYPair(highDriftTime, 0);

                driftProfileXYPairList.Insert(0, lowXYPair);
                driftProfileXYPairList.Insert(driftProfileXYPairList.Count, highXYPair);
            }
        }

        public static void PadXYPairsWithZeros(ref List<XYPair> driftProfileXYPairList, int numZeros)
        {
            var sortByXValue = (from xyPair in driftProfileXYPairList
                               orderby xyPair.XValue
                               select xyPair).ToList();

            var minXValue = sortByXValue.First().XValue;
            var maxXValue = sortByXValue.Last().XValue;

            for (var i = 1; i <= numZeros; i++)
            {
                var lowDriftTime = minXValue - i;
                var highDriftTime = maxXValue + i;

                var lowXYPair = new XYPair(lowDriftTime, 0);
                var highXYPair = new XYPair(highDriftTime, 0);

                driftProfileXYPairList.Insert(0, lowXYPair);
                driftProfileXYPairList.Insert(driftProfileXYPairList.Count, highXYPair);
            }
        }

        private static double GetResolvingPower(int chargeState)
        {
            if (chargeState == 1)
            {
                return 50;
            }
            if (chargeState == 2)
            {
                return 60;
            }
            else
            {
                return 70;
            }
        }

        public static double ConvertIMSScanToDriftTime(double imsScan, double averageTOFLength, double framePressure)
        {
            if (double.IsNaN(framePressure) || Math.Abs(framePressure - 0) < double.Epsilon)
            {
                return ConvertIMSScanToDriftTime(imsScan, averageTOFLength);
            }

            var driftTime = (averageTOFLength * imsScan / 1e6) * (FRAME_PRESSURE_STANDARD / framePressure);
            return driftTime;
        }

        public static double ConvertIMSScanToDriftTime(double imsScan, double averageTOFLength)
        {
            var driftTime = (averageTOFLength * imsScan / 1e6);
            return driftTime;
        }

        public static void TestDriftTimeTheory(IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable)
        {
            var expectedFilename = Path.Combine(Settings.InputDirectory, FileUtil.GetUimfFileForIsosFile(Settings.InputFileName));

            var uimfReader = new DataReader(expectedFilename);

            foreach (var lcimsmsFeature in lcimsmsFeatureEnumerable)
            {
                Console.WriteLine("**************************************************************");

                var sortByScanLCQuery = from imsmsFeature in lcimsmsFeature.IMSMSFeatureList
                                        orderby imsmsFeature.ScanLC ascending
                                        select imsmsFeature;

                foreach (var imsmsFeature in sortByScanLCQuery)
                {
                    var scanLC = ScanLCMap.Mapping[imsmsFeature.ScanLC];
                    var frameParams = uimfReader.GetFrameParams(scanLC);
                    var averageTOFLength = frameParams.GetValueDouble(FrameParamKeyType.AverageTOFLength);
                    var framePressure = frameParams.GetValueDouble(FrameParamKeyType.PressureBack);

                    var msFeatureRep = imsmsFeature.FindRepMSFeature();

                    var driftTime = (averageTOFLength * msFeatureRep.ScanIMS / 1e6);
                    var correctedDriftTime = ConvertIMSScanToDriftTime(msFeatureRep.ScanIMS, averageTOFLength, framePressure);

                    Console.WriteLine("Drift Time = " + driftTime + "\tCorrected = " + correctedDriftTime);
                }

                Console.WriteLine("**************************************************************");
            }
        }
    }
}
