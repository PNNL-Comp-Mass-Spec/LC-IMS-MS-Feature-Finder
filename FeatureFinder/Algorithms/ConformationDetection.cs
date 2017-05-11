using System;
using System.Collections.Generic;
using System.Linq;
using FeatureFinder.Control;
using FeatureFinder.Data;
using FeatureFinder.Data.Maps;
using FeatureFinder.Utilities;
using MathNet.Numerics.Interpolation;
using UIMFLibrary;

namespace FeatureFinder.Algorithms
{
    public static class ConformationDetection
    {
        private const double DRIFT_TIME_WINDOW_WIDTH = 0.6;
        private const double DRIFT_TIME_SLICE_WIDTH = 0.1;
        private const double FRAME_PRESSURE_STANDARD = 4.0;

        public static IEnumerable<LCIMSMSFeature> DetectConformationsUsingRawData(IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable)
        {
            var newLCIMSMSFeatureList = new List<LCIMSMSFeature>();

            using (var uimfReader = new DataReader(Settings.InputDirectory + Settings.InputFileName.Replace("_isos.csv", ".uimf")))
            {
                Logger.Log("UIMF file has been opened.");

                var globalParams = uimfReader.GetGlobalParams();

                var binWidth = globalParams.BinWidth;
                var featureCount = lcimsmsFeatureEnumerable.Count();
                var featuresProcessed = 0;
                var lastProgressUpdate = DateTime.UtcNow;

                foreach (var lcimsmsFeature in lcimsmsFeatureEnumerable)
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
                    foreach (XYPair xyPair in imsScanProfile)
                    {
                        //double uncorrectedYValue = xyPair.YValue;

                        xyPair.YValue = xyPair.YValue/maxIntensityFromProfile*
                                        maxIntensity;

                        //Console.WriteLine(xyPair.XValue + "\t" + uncorrectedYValue + "\t" + xyPair.YValue);
                    }


                    Peak driftProfilePeak = new Peak(imsScanProfile);

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
                        double percentComplete = (double)featuresProcessed / featureCount * 100;
                        Logger.Log("  " + percentComplete.ToString("0.0") + "% complete");
                    }
                    

                }

            }
            return newLCIMSMSFeatureList;
        }

        public static IEnumerable<LCIMSMSFeature> FindDriftTimePeaks(Peak driftProfilePeak, LCIMSMSFeature lcimsmsFeature, double averageTOFLength, double framePressure)
        {
            List<IMSMSFeature> imsmsFeatureList = lcimsmsFeature.IMSMSFeatureList;

            var sortByScanLCQuery = from imsmsFeature in imsmsFeatureList
                                    orderby imsmsFeature.ScanLC
                                    select imsmsFeature;

            double globalIMSScanMinimum = double.MaxValue;
            double globalIMSScanMaximum = double.MinValue;
            double localIMSScanMinimum = 0;
            double localIMSScanMaximum = 0;

            // Grab all of the intensity values for each IMS-MS Feature and find the global minimum and maximum Drift Times
            foreach (IMSMSFeature imsmsFeature in sortByScanLCQuery)
            {
                imsmsFeature.GetMinAndMaxIMSScan(out localIMSScanMinimum, out localIMSScanMaximum);

                if (localIMSScanMinimum < globalIMSScanMinimum) globalIMSScanMinimum = localIMSScanMinimum;
                if (localIMSScanMaximum > globalIMSScanMaximum) globalIMSScanMaximum = localIMSScanMaximum;
            }

            Peak smoothedDriftProfilePeak = PeakUtil.KDESmooth(driftProfilePeak, Settings.SmoothingStDev); // TODO: Find a good value. 0.15? Less smooth = more conformations!

            IInterpolation smoothedDriftProfileInterpolation = PeakUtil.GetLinearInterpolationMethod(smoothedDriftProfilePeak);

            List<XYPair> xyPairList = new List<XYPair>();
            List<Peak> peakList = new List<Peak>();
            double previousIntensity = double.MinValue;
            bool movingUp = true;

            int minScanLC = 0;
            int maxScanLC = 0;
            lcimsmsFeature.GetMinAndMaxScanLC(out minScanLC, out maxScanLC);

            double minimumIntensityToConsider = smoothedDriftProfilePeak.GetMaximumYValue() * 0.05;

            //DisplayPeakXYData(smoothedDriftProfilePeak);

            //Console.WriteLine("Global IMS Scan Min = " + globalIMSScanMinimum + "\tGlobal IMS Scan Max = " + globalIMSScanMaximum);

            for (double i = globalIMSScanMinimum; i <= globalIMSScanMaximum; i += 1)
            {
                double imsScan = i;
                double intensity = smoothedDriftProfileInterpolation.Interpolate(imsScan);

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
                            Peak peak = new Peak(xyPairList);

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

                    XYPair xyPair = new XYPair(imsScan, intensity);
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
                Peak lastPeak = new Peak(xyPairList);

                if (lastPeak.XYPairList.Count >= 7)
                {
                    peakList.Add(lastPeak);
                }
            }

            double resolvingPower = GetResolvingPower(lcimsmsFeature.Charge);

            List<LCIMSMSFeature> newLCIMSMSFeatureList = new List<LCIMSMSFeature>();

            foreach (Peak peak in peakList)
            {
                double repIMSScan = peak.GetQuadraticFit();

                // TODO: Fix this
                //double theoreticalFWHM = driftTime / resolvingPower;
                double theoreticalFWHM = 3;

                double minimumXValue = 0;
                double maximumXValue = 0;
                peak.GetMinAndMaxXValues(out minimumXValue, out maximumXValue);

                const int numPoints = 100;

                List<XYPair> normalDistributionXYPairList = PeakUtil.CreateTheoreticalGaussianPeak(repIMSScan, theoreticalFWHM, numPoints);
                PadXYPairsWithZeros(ref normalDistributionXYPairList, 5);
                Peak normalDistributionPeak = new Peak(normalDistributionXYPairList);

                IInterpolation peakInterpolation = PeakUtil.GetLinearInterpolationMethod(peak);

                double fitScore = PeakUtil.CalculatePeakFit(peak, normalDistributionPeak, 0);

                // Create a new LC-IMS-MS Feature
                LCIMSMSFeature newLCIMSMSFeature = new LCIMSMSFeature(lcimsmsFeature.Charge)
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
                foreach (IMSMSFeature imsmsFeature in lcimsmsFeature.IMSMSFeatureList)
                {
                    IEnumerable<MSFeature> msFeatureEnumerable = imsmsFeature.FindMSFeaturesInScanIMSRange(minimumXValue, maximumXValue);

                    if (msFeatureEnumerable.Count() <= 0) continue;
                    IMSMSFeature newIMSMSFeature = new IMSMSFeature(imsmsFeature.ScanLC, imsmsFeature.Charge);
                    newIMSMSFeature.AddMSFeatureList(msFeatureEnumerable);
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
                int maxMemberCount = newLCIMSMSFeatureList.Select(feature => feature.GetMemberCount()).Max();

                foreach (LCIMSMSFeature feature in newLCIMSMSFeatureList)
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
            double lowDriftTime = globalDriftTimeMinimum - (DRIFT_TIME_SLICE_WIDTH / 1000);
            double highDriftTime = globalDriftTimeMaximum + (DRIFT_TIME_SLICE_WIDTH / 1000);

            XYPair lowXYPair = new XYPair(lowDriftTime, 0);
            XYPair highXYPair = new XYPair(highDriftTime, 0);

            driftProfileXYPairList.Add(lowXYPair);
            driftProfileXYPairList.Add(highXYPair);

            for (int i = 1; i <= numZeros; i++)
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
            var sortByXValue = from xyPair in driftProfileXYPairList
                               orderby xyPair.XValue ascending
                               select xyPair;

            double minXValue = sortByXValue.First().XValue;
            double maxXValue = sortByXValue.Last().XValue;

            for (int i = 1; i <= numZeros; i++)
            {
                double lowDriftTime = minXValue - i;
                double highDriftTime = maxXValue + i;

                XYPair lowXYPair = new XYPair(lowDriftTime, 0);
                XYPair highXYPair = new XYPair(highDriftTime, 0);

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

            double driftTime = (averageTOFLength * imsScan / 1e6) * (FRAME_PRESSURE_STANDARD / framePressure);
            return driftTime;
        }

        public static double ConvertIMSScanToDriftTime(double imsScan, double averageTOFLength)
        {
            double driftTime = (averageTOFLength * imsScan / 1e6);
            return driftTime;
        }

        public static void TestDriftTimeTheory(IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable)
        {
            var expectedFilename = Settings.InputDirectory + Settings.InputFileName.Replace("_isos.csv", ".uimf");


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
