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

				// Convert IMS Scan # to Drift Time values
				foreach (XYPair xyPair in imsScanProfile)
				{
					double imsScan = xyPair.XValue;
					double driftTime = ConvertIMSScanToDriftTime((int)imsScan, averageTOFLength);
					xyPair.XValue = driftTime;
				}

				Peak driftProfilePeak = new Peak(imsScanProfile);

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

			Peak smoothedDriftProfilePeak = PeakUtil.KDESmooth(driftProfilePeak, Settings.SmoothingStDev); // TODO: Find a good value. 0.15? Less smooth = more conformations!

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

			// When you get to the end, end the last Peak, but only if it has a non-zero value
			foreach (XYPair xyPair in xyPairList)
			{
				if (xyPair.YValue > 0)
				{
					xyPairList = PadXYPairsWithZeros(xyPairList, driftTimeMinimum, globalDriftTimeMaximum, 1);
					Peak lastPeak = new Peak(xyPairList);

					if (lastPeak.XYPairList.Count >= 3)
					{
						peakList.Add(lastPeak);
					}

					break;
				}
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

				newLCIMSMSFeatureList.Add(newLCIMSMSFeature);
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

		public static double ConvertIMSScanToDriftTime(int imsScan, double averageTOFLength)
		{
			double driftTime = (averageTOFLength * imsScan / 1e6);
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
