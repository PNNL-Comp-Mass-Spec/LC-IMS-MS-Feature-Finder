using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureFinder.Data;
using FeatureFinder.Utilities;
using FeatureFinder.Data.Maps;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.Distributions;

namespace FeatureFinder.Algorithms
{
	public static class ConformationDetection
	{
		private const int IMS_SCAN_WINDOW_WIDTH = 5;

		public static void DetectConformationsForLCIMSMSFeature(LCIMSMSFeature lcimsmsFeature)
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

			// Calculate the IMS half window. i.e. If the window size is 5, the half window is 2 in each direction.
			int imsHalfWindow = (int)Math.Floor(IMS_SCAN_WINDOW_WIDTH / 2.0);

			double maxIntensity = 0.0;
			int indexOfMaxIntensity = 0;

			IInterpolationMethod scanIMSToDriftTimeInterpolation = ScanIMSToDriftTimeMap.GetInterpolation();
			
			List<XYPair> driftProfileXYPairList = new List<XYPair>();

			// Add "0" intensity values to the left and right of the Peak
			driftProfileXYPairList = PadXYPairsWithZeros(driftProfileXYPairList, scanIMSToDriftTimeInterpolation, globalScanIMSMinimum, globalScanIMSMaximum, imsHalfWindow);

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

				double driftTime = scanIMSToDriftTimeInterpolation.Interpolate(i);

				XYPair xyPair = new XYPair(driftTime, totalIntensity);
				driftProfileXYPairList.Add(xyPair);

				if (totalIntensity > maxIntensity)
				{
					maxIntensity = totalIntensity;
					indexOfMaxIntensity = i;
				}
			}

			Peak driftProfilePeak = new Peak(driftProfileXYPairList);
			Peak smoothedDriftProfilePeak = PeakUtil.KDESmooth(driftProfilePeak, 0.35);

			//driftProfilePeak.PrintPeakToConsole();
			//smoothedDriftProfilePeak.PrintPeakToConsole();
			//Console.WriteLine("================================================");
		
			IInterpolationMethod driftProfileInterpolation = PeakUtil.GetLinearInterpolationMethod(driftProfilePeak);
			IInterpolationMethod smoothedDriftProfileInterpolation = PeakUtil.GetLinearInterpolationMethod(smoothedDriftProfilePeak);

			List<XYPair> xyPairList = new List<XYPair>();
			List<Peak> peakList = new List<Peak>();
			int scanIMSMinimum = globalScanIMSMinimum;
			double previousIntensity = 0;
			bool movingUp = true;

			for (int i = globalScanIMSMinimum; i <= globalScanIMSMaximum; i++)
			{
				double driftTime = scanIMSToDriftTimeInterpolation.Interpolate(i);
				double intensity = smoothedDriftProfileInterpolation.Interpolate(driftTime);

				if (intensity > previousIntensity)
				{
					// End of Peak
					if (!movingUp)
					{
						xyPairList = PadXYPairsWithZeros(xyPairList, scanIMSToDriftTimeInterpolation, scanIMSMinimum, i - 1, imsHalfWindow);
						Peak peak = new Peak(xyPairList);
						peakList.Add(peak);
						
						// Start over with a new Peak
						xyPairList.Clear();
						scanIMSMinimum = i;
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
			xyPairList = PadXYPairsWithZeros(xyPairList, scanIMSToDriftTimeInterpolation, scanIMSMinimum, globalScanIMSMaximum, imsHalfWindow);
			Peak lastPeak = new Peak(xyPairList);
			peakList.Add(lastPeak);

			double resolvingPower = GetResolvingPower(lcimsmsFeature.Charge);

			foreach (Peak peak in peakList)
			{
				Peak smoothedPeak = PeakUtil.KDESmooth(peak, 0.35);
				double driftTime = smoothedPeak.GetXValueOfMaximumYValue();
				double theoreticalFWHM = driftTime / resolvingPower;

				double minimumXValue = 0;
				double maximumXValue = 0;
				smoothedPeak.GetMinAndMaxXValues(out minimumXValue, out maximumXValue);

				double peakWidth = (maximumXValue - minimumXValue) / 3; // Dividing by 3 to make compatible with Normal Distribution Creator??
				int numPoints = 100;

				List<XYPair> normalDistributionXYPairList = PeakUtil.CreateTheoreticalGaussianPeak(driftTime, theoreticalFWHM, numPoints);
				normalDistributionXYPairList = PadXYPairsWithZeros(normalDistributionXYPairList, scanIMSToDriftTimeInterpolation);
				Peak normalDistributionPeak = new Peak(normalDistributionXYPairList);

				IInterpolationMethod smoothedPeakInterpolation = PeakUtil.GetLinearInterpolationMethod(smoothedPeak);
				IInterpolationMethod normalDistribution = PeakUtil.GetLinearInterpolationMethod(normalDistributionPeak);

				//NormalDistribution normalDistribution = PeakUtil.CreateNormalDistribution(driftTime, theoreticalFWHM);

				double fitScore = PeakUtil.CalculatePeakFit(smoothedPeakInterpolation, normalDistribution, minimumXValue, maximumXValue, driftTime, 0.05);

				if (fitScore > lcimsmsFeature.IMSScore)
				{
					lcimsmsFeature.IMSScore = (float)fitScore;
				}
			}

		}

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

		private static List<XYPair> PadXYPairsWithZeros(List<XYPair> driftProfileXYPairList, IInterpolationMethod scanIMSToDriftTimeInterpolation, int globalScanIMSMinimum, int globalScanIMSMaximum, int imsHalfWindow)
		{
			for (int i = 1; i <= 5; i++)
			{
				double lowDriftTime = scanIMSToDriftTimeInterpolation.Interpolate(globalScanIMSMinimum - imsHalfWindow - i);
				double highDriftTime = scanIMSToDriftTimeInterpolation.Interpolate(globalScanIMSMaximum + imsHalfWindow + i);

				XYPair lowXYPair = new XYPair(lowDriftTime, 0);
				XYPair highXYPair = new XYPair(highDriftTime, 0);

				driftProfileXYPairList.Add(lowXYPair);
				driftProfileXYPairList.Add(highXYPair);
			}

			return driftProfileXYPairList;
		}

		private static List<XYPair> PadXYPairsWithZeros(List<XYPair> driftProfileXYPairList, IInterpolationMethod scanIMSToDriftTimeInterpolation)
		{
			var sortByXValue = from xyPair in driftProfileXYPairList
							   orderby xyPair.XValue ascending
							   select xyPair;

			double minXValue = sortByXValue.First().XValue;
			double maxXValue = sortByXValue.Last().XValue;

			for (int i = 1; i <= 5; i++)
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
	}
}
