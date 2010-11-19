using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureFinder.Data;
using FeatureFinder.Utilities;
using FeatureFinder.Data.Maps;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.Distributions;
using UIMFLibrary;
using System.IO;
using FeatureFinder.Control;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FeatureFinder.Algorithms
{
	public static class ConformationDetection
	{
		private const double DRIFT_TIME_WINDOW_WIDTH = 0.6;
		private const double DRIFT_TIME_SLICE_WIDTH = 0.1;
		private const double FRAME_PRESSURE_STANDARD = 4.0;

		public static IEnumerable<LCIMSMSFeature> DetectConformationsUsingRawData(IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable)
		{
			List<LCIMSMSFeature> newLCIMSMSFeatureList = new List<LCIMSMSFeature>();

			DataReader uimfReader = new UIMFLibrary.DataReader();
			if (!uimfReader.OpenUIMF(Settings.InputDirectory + Settings.InputFileName.Replace("_isos.csv", ".uimf")))
			{
				throw new FileNotFoundException("Could not find file '" + Settings.InputDirectory + Settings.InputFileName.Replace("_isos.csv", ".uimf") + "'.");
			}
			Logger.Log("UIMF file has been opened.");

			GlobalParameters globalParameters = uimfReader.GetGlobalParameters();

			double binWidth = globalParameters.BinWidth;

			foreach (LCIMSMSFeature lcimsmsFeature in lcimsmsFeatureEnumerable)
			{
				int scanLC = ScanLCMap.Mapping[lcimsmsFeature.IMSMSFeatureList[0].ScanLC];
				FrameParameters frameParameters = uimfReader.GetFrameParameters(scanLC);

				double calibrationSlope = frameParameters.CalibrationSlope;
				double calibrationIntercept = frameParameters.CalibrationIntercept;
				double averageTOFLength = frameParameters.AverageTOFLength;
				double framePressure = frameParameters.PressureBack;
				int frameType = frameParameters.FrameType;

				List<XYPair> imsScanProfile = lcimsmsFeature.GetIMSScanProfileFromRawData(uimfReader, frameType, binWidth, calibrationSlope, calibrationIntercept);

				//Console.WriteLine("*************************************************************************");
				// Convert IMS Scan # to Drift Time values
				foreach (XYPair xyPair in imsScanProfile)
				{
					double imsScan = xyPair.XValue;
					double driftTime = ConvertIMSScanToDriftTime((int)imsScan, averageTOFLength, framePressure);
					//Console.WriteLine("Old = [" + xyPair.XValue + ", " + xyPair.YValue + "]");
					xyPair.XValue = driftTime;
					//Console.WriteLine("New = [" + xyPair.XValue + ", " + xyPair.YValue + "]");
				}

				Peak driftProfilePeak = new Peak(imsScanProfile);

				Console.WriteLine("============================================================================");
				driftProfilePeak.PrintPeakToConsole();
				Console.WriteLine("============================================================================");

				IEnumerable<LCIMSMSFeature> lcimsmsFeaturesWithDriftTimes = FindDriftTimePeaks(driftProfilePeak, lcimsmsFeature);
				newLCIMSMSFeatureList.AddRange(lcimsmsFeaturesWithDriftTimes);
			}

			uimfReader.CloseUIMF();

			return newLCIMSMSFeatureList;
		}

		public static IEnumerable<LCIMSMSFeature> FindDriftTimePeaks(Peak driftProfilePeak, LCIMSMSFeature lcimsmsFeature)
		{
			List<IMSMSFeature> imsmsFeatureList = lcimsmsFeature.IMSMSFeatureList;

			var sortByScanLCQuery = from imsmsFeature in imsmsFeatureList
									orderby imsmsFeature.ScanLC
									select imsmsFeature;

			double globalDriftTimeMinimum = double.MaxValue;
			double globalDriftTimeMaximum = double.MinValue;
			double localDriftTimeMinimum = 0;
			double localDriftTimeMaximum = 0;

			// Grab all of the intensity values for each IMS-MS Feature and find the global minimum and maximum Drift Times
			foreach (IMSMSFeature imsmsFeature in sortByScanLCQuery)
			{
				imsmsFeature.GetMinAndMaxDriftTimes(out localDriftTimeMinimum, out localDriftTimeMaximum);

				if (localDriftTimeMinimum < globalDriftTimeMinimum) globalDriftTimeMinimum = localDriftTimeMinimum;
				if (localDriftTimeMaximum > globalDriftTimeMaximum) globalDriftTimeMaximum = localDriftTimeMaximum;
			}

			double driftTimeHalfWindow = DRIFT_TIME_WINDOW_WIDTH / 2.0;

			Peak smoothedDriftProfilePeak = PeakUtil.KDESmooth(driftProfilePeak, 0.35); // TODO: Find a good value. 0.15? Less smooth = more conformations!

			//driftProfilePeak.PrintPeakToConsole();
			smoothedDriftProfilePeak.PrintPeakToConsole();
			//Console.WriteLine("================================================");

			IInterpolationMethod smoothedDriftProfileInterpolation = PeakUtil.GetLinearInterpolationMethod(smoothedDriftProfilePeak);

			List<XYPair> xyPairList = new List<XYPair>();
			List<Peak> peakList = new List<Peak>();
			double driftTimeMinimum = globalDriftTimeMinimum;
			double previousIntensity = 0;
			bool movingUp = true;

			int minScanLC = 0;
			int maxScanLC = 0;
			lcimsmsFeature.GetMinAndMaxScanLC(out minScanLC, out maxScanLC);

			for (double i = globalDriftTimeMinimum; i <= globalDriftTimeMaximum; i += DRIFT_TIME_SLICE_WIDTH)
			{
				double driftTime = i;
				double intensity = smoothedDriftProfileInterpolation.Interpolate(driftTime);

				if (intensity > previousIntensity)
				{
					// End of Peak
					if (!movingUp)
					{
						xyPairList = PadXYPairsWithZeros(xyPairList, driftTimeMinimum, i - DRIFT_TIME_SLICE_WIDTH, 1);
						Peak peak = new Peak(xyPairList);

						if (peak.XYPairList.Count >= 3)
						{
							peakList.Add(peak);
						}

						// Start over with a new Peak
						xyPairList.Clear();
						driftTimeMinimum = i;
						movingUp = true;
					}
				}
				else
				{
					movingUp = false;
				}

				XYPair xyPair = new XYPair(driftTime, intensity);
				xyPairList.Add(xyPair);

				previousIntensity = intensity;
			}

			// When you get to the end, end the last Peak
			xyPairList = PadXYPairsWithZeros(xyPairList, driftTimeMinimum, globalDriftTimeMaximum, 1);
			Peak lastPeak = new Peak(xyPairList);

			if (lastPeak.XYPairList.Count >= 3)
			{
				peakList.Add(lastPeak);
			}

			double resolvingPower = GetResolvingPower(lcimsmsFeature.Charge);

			List<LCIMSMSFeature> newLCIMSMSFeatureList = new List<LCIMSMSFeature>();

			int index = 0;
			int conformationIndex = 0;

			foreach (Peak peak in peakList)
			{
				//Peak smoothedPeak = PeakUtil.KDESmooth(peak, 0.10); // TODO: To smooth this peak or not
				double driftTime = peak.GetXValueOfMaximumYValue();
				double theoreticalFWHM = driftTime / resolvingPower;

				double minimumXValue = 0;
				double maximumXValue = 0;
				peak.GetMinAndMaxXValues(out minimumXValue, out maximumXValue);

				int numPoints = 100;

				List<XYPair> normalDistributionXYPairList = PeakUtil.CreateTheoreticalGaussianPeak(driftTime, theoreticalFWHM, numPoints);
				normalDistributionXYPairList = PadXYPairsWithZeros(normalDistributionXYPairList, 5);
				Peak normalDistributionPeak = new Peak(normalDistributionXYPairList);

				IInterpolationMethod peakInterpolation = PeakUtil.GetLinearInterpolationMethod(peak);
				IInterpolationMethod normalDistribution = PeakUtil.GetLinearInterpolationMethod(normalDistributionPeak);

				//NormalDistribution normalDistribution = PeakUtil.CreateNormalDistribution(driftTime, theoreticalFWHM);

				double fitScore = PeakUtil.CalculatePeakFit(peakInterpolation, normalDistribution, minimumXValue, maximumXValue, driftTime, 0);

				// Create a new LC-IMS-MS Feature
				LCIMSMSFeature newLCIMSMSFeature = new LCIMSMSFeature(lcimsmsFeature.Charge);
				newLCIMSMSFeature.OriginalIndex = lcimsmsFeature.OriginalIndex;
				newLCIMSMSFeature.IMSScore = (float)fitScore;

				double lowDriftTime = driftTime - driftTimeHalfWindow;
				double highDriftTime = driftTime + driftTimeHalfWindow;

				//Console.WriteLine("**************************************************************************");
				//Console.WriteLine("DT = " + driftTime + "\tLow DT = " + lowDriftTime + "\tHigh DT = " + highDriftTime);

				// Create new IMS-MS Features by grabbing MS Features in each LC Scan that are in the defined window of the detected drift time
				foreach (IMSMSFeature imsmsFeature in lcimsmsFeature.IMSMSFeatureList)
				{
					IEnumerable<MSFeature> msFeatureEnumerable = imsmsFeature.FindMSFeaturesInDriftTimeRange(lowDriftTime, highDriftTime);

					if (msFeatureEnumerable.Count() > 0)
					{
						IMSMSFeature newIMSMSFeature = new IMSMSFeature(imsmsFeature.ScanLC, imsmsFeature.Charge);
						newIMSMSFeature.AddMSFeatureList(msFeatureEnumerable);
						newLCIMSMSFeature.AddIMSMSFeature(newIMSMSFeature);
					}
				}

				if (newLCIMSMSFeature.IMSMSFeatureList.Count > 0)
				{
					//Console.WriteLine("*****************************************************************");
					//Console.WriteLine("DT = " + driftTime + "\tLow DT = " + lowDriftTime + "\tHigh DT = " + highDriftTime);
					//smoothedPeak.PrintPeakToConsole();
					//Console.WriteLine("*****************************************************************");

					newLCIMSMSFeatureList.Add(newLCIMSMSFeature);
					conformationIndex++;
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
					// TODO: Figure out why this actually happens. I believe that this SHOULD NOT happen. Below is a hack to return a conformation even if this happens

					Console.WriteLine("**********************************************************************");
					Console.WriteLine("Detected Drift Time = " + driftTime + "\tLow = " + lowDriftTime + "\tHigh = " + highDriftTime);
					lcimsmsFeature.PrintLCAndDriftTimeMap();
					Console.WriteLine("**********************************************************************");

					//Console.WriteLine("===============================================================");
					//Console.WriteLine("DT = " + driftTime + "\tLow DT = " + lowDriftTime + "\tHigh DT = " + highDriftTime);
					//Console.WriteLine("Global Min = " + globalDriftTimeMinimum + "\tGlobal Max = " + globalDriftTimeMaximum);
					//smoothedPeak.PrintPeakToConsole();
					//Console.WriteLine("***************************************************************");
					//peak.PrintPeakToConsole();
					//Console.WriteLine("===============================================================");
				}

				index++;
			}

			// If no conformation was detected, use the most abundant drift time
			if (newLCIMSMSFeatureList.Count == 0)
			{
				LCIMSMSFeature newLCIMSMSFeature = new LCIMSMSFeature(lcimsmsFeature.Charge);
				newLCIMSMSFeature.IMSScore = 0; // TODO: What to do about the score?

				MSFeature msFeatureRep = lcimsmsFeature.GetMSFeatureRep();
				double driftTime = msFeatureRep.DriftTime;

				double lowDriftTime = driftTime - driftTimeHalfWindow;
				double highDriftTime = driftTime + driftTimeHalfWindow;

				int totalCount = 0;

				// Create new IMS-MS Features by grabbing MS Features in each LC Scan that are in the defined window of the detected drift time
				foreach (IMSMSFeature imsmsFeature in lcimsmsFeature.IMSMSFeatureList)
				{
					IEnumerable<MSFeature> msFeatureEnumerable = imsmsFeature.FindMSFeaturesInDriftTimeRange(lowDriftTime, highDriftTime);

					if (msFeatureEnumerable.Count() > 0)
					{
						totalCount += msFeatureEnumerable.Count();
						IMSMSFeature newIMSMSFeature = new IMSMSFeature(imsmsFeature.ScanLC, imsmsFeature.Charge);
						newIMSMSFeature.AddMSFeatureList(msFeatureEnumerable);
						newLCIMSMSFeature.AddIMSMSFeature(newIMSMSFeature);
					}
				}
			}

			// Find the Conformation that has the highest member count and store the value into all conformations of this LC-IMS-MS Feature
			int maxMemberCount = int.MinValue;

			foreach (LCIMSMSFeature feature in newLCIMSMSFeatureList)
			{
				int memberCount = feature.GetMemberCount();

				if (memberCount > maxMemberCount)
				{
					maxMemberCount = memberCount;
				}
			}

			foreach (LCIMSMSFeature feature in newLCIMSMSFeatureList)
			{
				feature.MaxMemberCount = maxMemberCount;
			}

			return newLCIMSMSFeatureList;
		}

		//public static IEnumerable<LCIMSMSFeature> DetectConformationsForLCIMSMSFeature(LCIMSMSFeature lcimsmsFeature)
		//{
		//    List<IMSMSFeature> imsmsFeatureList = lcimsmsFeature.IMSMSFeatureList;

		//    var sortByScanLCQuery = from imsmsFeature in imsmsFeatureList
		//                            orderby imsmsFeature.ScanLC
		//                            select imsmsFeature;

		//    List<Dictionary<double, double>> intensityDictionaries = new List<Dictionary<double, double>>();

		//    double globalDriftTimeMinimum = double.MaxValue;
		//    double globalDriftTimeMaximum = double.MinValue;
		//    double localDriftTimeMinimum = 0;
		//    double localDriftTimeMaximum = 0;

		//    // Grab all of the intensity values for each IMS-MS Feature and find the global minimum and maximum Drift Times
		//    foreach (IMSMSFeature imsmsFeature in sortByScanLCQuery)
		//    {
		//        intensityDictionaries.Add(imsmsFeature.GetIntensityValues());

		//        imsmsFeature.GetMinAndMaxDriftTimes(out localDriftTimeMinimum, out localDriftTimeMaximum);

		//        if (localDriftTimeMinimum < globalDriftTimeMinimum) globalDriftTimeMinimum = localDriftTimeMinimum;
		//        if (localDriftTimeMaximum > globalDriftTimeMaximum) globalDriftTimeMaximum = localDriftTimeMaximum;
		//    }

		//    double maxIntensity = 0.0;

		//    double driftTimeHalfWindow = DRIFT_TIME_WINDOW_WIDTH / 2.0;

		//    List<XYPair> driftProfileXYPairList = new List<XYPair>();

		//    // Add "0" intensity values to the left and right of the Peak
		//    driftProfileXYPairList = PadXYPairsWithZeros(driftProfileXYPairList, globalDriftTimeMinimum, globalDriftTimeMaximum, 5);

		//    // Find the drift profile
		//    for (double i = globalDriftTimeMinimum - driftTimeHalfWindow; i < globalDriftTimeMaximum + driftTimeHalfWindow; i += DRIFT_TIME_SLICE_WIDTH)
		//    {
		//        double totalIntensity = 0.0;

		//        foreach (Dictionary<double, double> intensityDictionary in intensityDictionaries)
		//        {
		//            var getIntensitiesByDriftTimeRange = from entry in intensityDictionary
		//                                                 where entry.Key >= i - driftTimeHalfWindow && entry.Key <= i + driftTimeHalfWindow
		//                                                 select entry.Value;

		//            foreach (double intensity in getIntensitiesByDriftTimeRange)
		//            {
		//                totalIntensity += intensity;
		//            }
		//        }

		//        double driftTime = i;

		//        XYPair xyPair = new XYPair(driftTime, totalIntensity);
		//        driftProfileXYPairList.Add(xyPair);

		//        if (totalIntensity > maxIntensity)
		//        {
		//            maxIntensity = totalIntensity;
		//        }
		//    }

		//    Peak driftProfilePeak = new Peak(driftProfileXYPairList);
		//    Peak smoothedDriftProfilePeak = PeakUtil.KDESmooth(driftProfilePeak, 0.25); // TODO: Find a good value. 0.15? Less smooth = more conformations!

		//    //driftProfilePeak.PrintPeakToConsole();
		//    //smoothedDriftProfilePeak.PrintPeakToConsole();
		//    //Console.WriteLine("================================================");
		
		//    IInterpolationMethod driftProfileInterpolation = PeakUtil.GetLinearInterpolationMethod(driftProfilePeak);
		//    IInterpolationMethod smoothedDriftProfileInterpolation = PeakUtil.GetLinearInterpolationMethod(smoothedDriftProfilePeak);

		//    List<XYPair> xyPairList = new List<XYPair>();
		//    List<Peak> peakList = new List<Peak>();
		//    double driftTimeMinimum = globalDriftTimeMinimum;
		//    double previousIntensity = 0;
		//    bool movingUp = true;

		//    // TODO: Found an example in my small dataset where I detected same DT twice
		//    // Mass = 2511.2872, LC Scans = 1335 - 1344, Charge = 2, DT = 50.155, Score1 = 0.8803248, Score2 = 0.7762359, Conf Idx = 5 and 6
		//    // Different scores means the peak was created differently

		//    int minScanLC = 0;
		//    int maxScanLC = 0;
		//    lcimsmsFeature.GetMinAndMaxScanLC(out minScanLC, out maxScanLC);

		//    if (lcimsmsFeature.CalculateAverageMass() > 2526.1 && lcimsmsFeature.CalculateAverageMass() < 2526.5 && lcimsmsFeature.Charge == 2)
		//    {
		//        //Console.WriteLine("here");
		//    }

		//    for (double i = globalDriftTimeMinimum; i <= globalDriftTimeMaximum; i += DRIFT_TIME_SLICE_WIDTH)
		//    {
		//        double driftTime = i;
		//        double intensity = smoothedDriftProfileInterpolation.Interpolate(driftTime);

		//        if (intensity > previousIntensity)
		//        {
		//            // End of Peak
		//            if (!movingUp)
		//            {
		//                xyPairList = PadXYPairsWithZeros(xyPairList, driftTimeMinimum, i - DRIFT_TIME_SLICE_WIDTH, 2);
		//                Peak peak = new Peak(xyPairList);
		//                peakList.Add(peak);
						
		//                // Start over with a new Peak
		//                xyPairList.Clear();
		//                driftTimeMinimum = i;
		//                movingUp = true;
		//            }
		//        }
		//        else
		//        {
		//            movingUp = false;
		//        }

		//        XYPair xyPair = new XYPair(driftTime, intensity);
		//        xyPairList.Add(xyPair);

		//        previousIntensity = intensity;
		//    }

		//    // When you get to the end, end the last Peak
		//    xyPairList = PadXYPairsWithZeros(xyPairList, driftTimeMinimum, globalDriftTimeMaximum, 2);
		//    Peak lastPeak = new Peak(xyPairList);
		//    peakList.Add(lastPeak);

		//    double resolvingPower = GetResolvingPower(lcimsmsFeature.Charge);

		//    List<LCIMSMSFeature> newLCIMSMSFeatureList = new List<LCIMSMSFeature>();

		//    int index = 0;
		//    int conformationIndex = 0;

		//    foreach (Peak peak in peakList)
		//    {
		//        Peak smoothedPeak = PeakUtil.KDESmooth(peak, 0.35);
		//        double driftTime = smoothedPeak.GetXValueOfMaximumYValue();
		//        double theoreticalFWHM = driftTime / resolvingPower;

		//        double minimumXValue = 0;
		//        double maximumXValue = 0;
		//        smoothedPeak.GetMinAndMaxXValues(out minimumXValue, out maximumXValue);

		//        int numPoints = 100;

		//        List<XYPair> normalDistributionXYPairList = PeakUtil.CreateTheoreticalGaussianPeak(driftTime, theoreticalFWHM, numPoints);
		//        normalDistributionXYPairList = PadXYPairsWithZeros(normalDistributionXYPairList, 5);
		//        Peak normalDistributionPeak = new Peak(normalDistributionXYPairList);

		//        IInterpolationMethod smoothedPeakInterpolation = PeakUtil.GetLinearInterpolationMethod(smoothedPeak);
		//        IInterpolationMethod normalDistribution = PeakUtil.GetLinearInterpolationMethod(normalDistributionPeak);

		//        //NormalDistribution normalDistribution = PeakUtil.CreateNormalDistribution(driftTime, theoreticalFWHM);

		//        double fitScore = PeakUtil.CalculatePeakFit(smoothedPeakInterpolation, normalDistribution, minimumXValue, maximumXValue, driftTime, 0.05);

		//        // Create a new LC-IMS-MS Feature
		//        LCIMSMSFeature newLCIMSMSFeature = new LCIMSMSFeature(lcimsmsFeature.Charge);
		//        newLCIMSMSFeature.IMSScore = (float)fitScore;

		//        double lowDriftTime = driftTime - driftTimeHalfWindow;
		//        double highDriftTime = driftTime + driftTimeHalfWindow;

		//        //Console.WriteLine("**************************************************************************");
		//        //Console.WriteLine("DT = " + driftTime + "\tLow DT = " + lowDriftTime + "\tHigh DT = " + highDriftTime);

		//        // Create new IMS-MS Features by grabbing MS Features in each LC Scan that are in the defined window of the detected drift time
		//        foreach (IMSMSFeature imsmsFeature in lcimsmsFeature.IMSMSFeatureList)
		//        {
		//            IEnumerable<MSFeature> msFeatureEnumerable = imsmsFeature.FindMSFeaturesInDriftTimeRange(lowDriftTime, highDriftTime);

		//            if (msFeatureEnumerable.Count() > 0)
		//            {
		//                IMSMSFeature newIMSMSFeature = new IMSMSFeature(imsmsFeature.ScanLC, imsmsFeature.Charge);
		//                newIMSMSFeature.AddMSFeatureList(msFeatureEnumerable);
		//                newLCIMSMSFeature.AddIMSMSFeature(newIMSMSFeature);
		//            }
		//        }

		//        if (newLCIMSMSFeature.IMSMSFeatureList.Count > 0)
		//        {
		//            //Console.WriteLine("*****************************************************************");
		//            //Console.WriteLine("DT = " + driftTime + "\tLow DT = " + lowDriftTime + "\tHigh DT = " + highDriftTime);
		//            //smoothedPeak.PrintPeakToConsole();
		//            //Console.WriteLine("*****************************************************************");

		//            newLCIMSMSFeatureList.Add(newLCIMSMSFeature);
		//            conformationIndex++;
		//            /*
		//            // TODO: Find LC Peaks
		//            var sortByScanLC = from imsmsFeature in newLCIMSMSFeature.IMSMSFeatureList
		//                               orderby imsmsFeature.ScanLC ascending
		//                               select imsmsFeature;

		//            Console.WriteLine("*************************************************");
		//            Console.WriteLine("Index = " + index + "\tMass = " + newLCIMSMSFeature.CalculateAverageMass() + "\tDrift = " + driftTime + "\tLC Range = " + sortByScanLC.First().ScanLC + "\t" + sortByScanLC.Last().ScanLC);

		//            List<XYPair> lcXYPairList = new List<XYPair>();
		//            int scanLC = sortByScanLC.First().ScanLC - 1;

		//            foreach (IMSMSFeature imsmsFeature in sortByScanLC)
		//            {
		//                int currentScanLC = imsmsFeature.ScanLC;

		//                for (int i = scanLC + 1; i < currentScanLC; i++)
		//                {
		//                    XYPair zeroValue = new XYPair(i, 0);
		//                    lcXYPairList.Add(zeroValue);
		//                    Console.Write("0\t");
		//                }

		//                XYPair xyPair = new XYPair(currentScanLC, imsmsFeature.GetIntensity());
		//                lcXYPairList.Add(xyPair);

		//                scanLC = currentScanLC;

		//                Console.Write(imsmsFeature.GetIntensity() + "\t");
		//            }
		//            Console.WriteLine("");
		//            Console.WriteLine("*************************************************");
		//            */
		//            // TODO: Calculate LC Score
		//        }
		//        else
		//        {
		//            Console.WriteLine("===============================================================");
		//            Console.WriteLine("DT = " + driftTime + "\tLow DT = " + lowDriftTime + "\tHigh DT = " + highDriftTime);
		//            smoothedPeak.PrintPeakToConsole();
		//            Console.WriteLine("***************************************************************");
		//            peak.PrintPeakToConsole();
		//            Console.WriteLine("===============================================================");
		//        }

		//        index++;
		//    }

		//    return newLCIMSMSFeatureList;
		//}

		/*
		public static void DetectConformationsForLCIMSMSFeatureOld(LCIMSMSFeature lcimsmsFeature)
		{
			List<IMSMSFeature> imsmsFeatureList = lcimsmsFeature.IMSMSFeatureList;

			var sortByScanLCQuery = from imsmsFeature in imsmsFeatureList
									orderby imsmsFeature.ScanLC
									select imsmsFeature;

			List<Dictionary<int, double>> intensityDictionaries = new List<Dictionary<int, double>>();

			int globalScanIMSMinimum = int.MaxValue;
			int globalScanIMSMaximum = int.MinValue;
			int localScanIMSMinimum = 0;
			int localScanIMSMaximum = 0;

			// Grab all of the intensity values for each IMS-MS Feature and find the global minimum and maximum IMS Scan
			foreach (IMSMSFeature imsmsFeature in sortByScanLCQuery)
			{
				intensityDictionaries.Add(imsmsFeature.GetIntensityValues());

				imsmsFeature.GetMinAndMaxScanIMS(out localScanIMSMinimum, out localScanIMSMaximum);

				if (localScanIMSMinimum < globalScanIMSMinimum) globalScanIMSMinimum = localScanIMSMinimum;
				if (localScanIMSMaximum > globalScanIMSMaximum) globalScanIMSMaximum = localScanIMSMaximum;
			}

			List<double> driftProfile = new List<double>();
			List<XYPair> driftProfileXYPairList = new List<XYPair>();

			// Pad the beginning of the drift profile with zeros
			driftProfile.AddRange(new double[] { 0, 0, 0, 0, 0 });

			// Calculate the IMS half window. i.e. If the window size is 5, the half window is 2 in each direction.
			int imsHalfWindow = (int)Math.Floor(IMS_SCAN_WINDOW_WIDTH / 2.0);

			double maxIntensity = 0.0;
			int indexOfMaxIntensity = 0;

			// Find the drift profile
			for (int i = globalScanIMSMinimum - imsHalfWindow; i <= globalScanIMSMaximum + imsHalfWindow; i++)
			{
				double totalIntensity = 0.0;

				foreach (Dictionary<int, double> intensityDictionary in intensityDictionaries)
				{
					for (int j = -imsHalfWindow; j <= imsHalfWindow; j++)
					{
						double currentIntensity = 0.0;
						intensityDictionary.TryGetValue(i + j, out currentIntensity);
						totalIntensity += currentIntensity;
					}
				}

				float driftTime = ScanIMSToDriftTimeMap.Mapping[i];

				XYPair xyPair = new XYPair(driftTime, totalIntensity);
				driftProfileXYPairList.Add(xyPair);

				driftProfile.Add(totalIntensity);

				if (totalIntensity > maxIntensity)
				{
					maxIntensity = totalIntensity;
					indexOfMaxIntensity = i;
				}
			}

			// Pad the end of the drift profile with zeros
			driftProfile.AddRange(new double[] { 0, 0, 0, 0, 0 });
			Peak driftProfilePeak = new Peak(driftProfileXYPairList);
			IInterpolationMethod driftprofileInterpolation = PeakUtil.GetLinearInterpolationMethod(driftProfilePeak);

			//foreach (double intensity in driftProfile)
			//{
			//    Console.Write(intensity + "\t");
			//}
			//Console.Write("\n");
			//Console.Write("*************************************************************\n");

			// TODO: Smooth Drift Profile

			// TODO: Detect Peaks for Drift Profile
			PeakUtil.DetectGaussianPeaks(driftProfile);

			// Create theoretical Gaussian Peak
			int peakWidth = driftProfile.Count - 10;
			List<double> theoreticalGaussianPeak = PeakUtil.CreateTheoreticalGaussianPeak(peakWidth / 2.0, peakWidth, peakWidth);

			// Calculate Fit Score for Drift Profile
			double fitScore = PeakUtil.CalculatePeakFit(driftProfile.GetRange(5, peakWidth), theoreticalGaussianPeak, 0.05);

			//Console.WriteLine("IMS Fit Score = " + fitScore);
			//Console.WriteLine("==========================================================");

			List<double> lcProfile = new List<double>();

			// Pad the beginning of the LC profile with zeros
			lcProfile.AddRange(new double[] { 0, 0, 0, 0, 0 });

			// TODO: Grab LC Profile for each detected drift time
			// Calculate the half window. i.e. If the window size is 5, the half window is 2 in each direction.
			foreach (Dictionary<int, double> intensityDictionary in intensityDictionaries)
			{
				double totalIntensity = 0.0;

				for (int i = indexOfMaxIntensity - imsHalfWindow; i <= indexOfMaxIntensity + imsHalfWindow; i++)
				{
					double currentIntensity = 0.0;
					intensityDictionary.TryGetValue(i, out currentIntensity);
					totalIntensity += currentIntensity;
				}

				lcProfile.Add(totalIntensity);
			}

			// Pad the end of the LC profile with zeros
			lcProfile.AddRange(new double[] { 0, 0, 0, 0, 0 });

			//foreach (double intensity in lcProfile)
			//{
			//    Console.Write(intensity + "\t");
			//}
			//Console.Write("\n");
			//Console.Write("----------------------------------------------------------------\n");

			// TODO: Smooth LC Profile

			// TODO: Detect Peaks for LC Profile

			// TODO: Calculate fit score for LC Profile
			int lcPeakWidth = lcProfile.Count - 10;
			List<double> lcTheoreticalGaussianPeak = PeakUtil.CreateTheoreticalGaussianPeak(lcPeakWidth / 2.0, lcPeakWidth, lcPeakWidth);

			double lcFitScore = PeakUtil.CalculatePeakFit(lcProfile.GetRange(5, lcPeakWidth), lcTheoreticalGaussianPeak, 0.05);
			//Console.WriteLine("LC Fit Score = " + lcFitScore);

			lcimsmsFeature.IMSScore = (float)fitScore;
			lcimsmsFeature.LCScore = (float)lcFitScore;
		}
		*/ 

		public static List<XYPair> PadXYPairsWithZeros(List<XYPair> driftProfileXYPairList, double globalDriftTimeMinimum, double globalDriftTimeMaximum, int numZeros)
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

				driftProfileXYPairList.Add(lowXYPair);
				driftProfileXYPairList.Add(highXYPair);
			}

			return driftProfileXYPairList;
		}

		public static List<XYPair> PadXYPairsWithZeros(List<XYPair> driftProfileXYPairList, int numZeros)
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

				driftProfileXYPairList.Add(lowXYPair);
				driftProfileXYPairList.Add(highXYPair);
			}

			return driftProfileXYPairList;
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

		public static double ConvertIMSScanToDriftTime(int imsScan, double averageTOFLength, double framePressure)
		{
			double driftTime = (averageTOFLength * imsScan / 1e6) * (FRAME_PRESSURE_STANDARD / framePressure);
			return driftTime;
		}

		public static void TestDriftTimeTheory(IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable)
		{
			DataReader uimfReader = new UIMFLibrary.DataReader();
			if (!uimfReader.OpenUIMF(Settings.InputDirectory + Settings.InputFileName.Replace("_isos.csv", ".uimf")))
			{
				throw new FileNotFoundException("Could not find file '" + Settings.InputDirectory + Settings.InputFileName.Replace("_isos.csv", ".uimf") + "'.");
			}

			foreach (LCIMSMSFeature lcimsmsFeature in lcimsmsFeatureEnumerable)
			{
				Console.WriteLine("**************************************************************");

				var sortByScanLCQuery = from imsmsFeature in lcimsmsFeature.IMSMSFeatureList
										orderby imsmsFeature.ScanLC ascending
										select imsmsFeature;

				foreach (IMSMSFeature imsmsFeature in sortByScanLCQuery)
				{
					int scanLC = ScanLCMap.Mapping[imsmsFeature.ScanLC];
					FrameParameters frameParameters = uimfReader.GetFrameParameters(scanLC);
					double averageTOFLength = frameParameters.AverageTOFLength;
					double framePressure = frameParameters.PressureBack;

					MSFeature msFeatureRep = imsmsFeature.FindRepMSFeature();

					double driftTime = (averageTOFLength * msFeatureRep.ScanIMS / 1e6);
					double correctedDriftTime = ConvertIMSScanToDriftTime(msFeatureRep.ScanIMS, averageTOFLength, framePressure);

					Console.WriteLine("Drift Time = " + driftTime + "\tCorrected = " + correctedDriftTime);
				}


				Console.WriteLine("**************************************************************");
			}
		}
	}
}
