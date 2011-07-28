using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.Distributions;
using FeatureFinder.Data;
using FeatureFinder.Control;
using MathNet.Numerics.Interpolation.Algorithms;

namespace FeatureFinder.Utilities
{
	public static class PeakUtil
	{
		private const double ONE_OVER_SQRT_OF_2_PI = 0.3989423;

		// TODO: Sometimes, a normalized value of 1 is never seen. This happens when the # of points is an even number.
		public static List<XYPair> CreateTheoreticalGaussianPeak(double centerOfPeak, double peakFWHM, int numOfPoints)
		{
			double sigma = peakFWHM / 2.35482;
			double sixSigma = 3 * peakFWHM;
			double pointSize = sixSigma / (double)(numOfPoints - 1);

			int startPoint = 0 - (int)Math.Floor(((numOfPoints - 1) / 2.0));
			int stopPoint = 0 + (int)Math.Ceiling(((numOfPoints - 1) / 2.0));

			List<XYPair> xyPairList = new List<XYPair>();

			for (int i = startPoint; i <= stopPoint; i++)
			{
				double xValue = centerOfPeak + (pointSize * i);
				double yValue = (1 / sigma) * ONE_OVER_SQRT_OF_2_PI * Math.Exp(-1 * (Math.Pow(xValue - centerOfPeak, 2)) / (2 * Math.Pow(sigma, 2)));

				XYPair xyPair = new XYPair(xValue, yValue);
				xyPairList.Add(xyPair);
			}

			return xyPairList;
		}

		public static double CalculatePeakFit(IInterpolation observedPeak, IInterpolation theoreticalPeak, double minimumXValue, double maximumXValue, double xValueOfMaximumYValue, double minYValueFactor)
		{
			List<double> xValues = new List<double>();
			List<double> yValues1 = new List<double>();
			List<double> yValues2 = new List<double>();

			double maxObservedPeakValue = observedPeak.Interpolate(xValueOfMaximumYValue);
			double maxTheoreticalPeakValue = theoreticalPeak.Interpolate(xValueOfMaximumYValue);

			double totalWidth = maximumXValue - minimumXValue;
			double pointWidth = totalWidth / 1000.0;

			double minValueToTest = maxObservedPeakValue * minYValueFactor;
			int numPointsTested = 0;

			double sumOfSquaredResiduals = 0.0;

			for (double i = 0; i < totalWidth; i += pointWidth)
			{
				double observedPeakValue = observedPeak.Interpolate(minimumXValue + i);

				if (observedPeakValue >= minValueToTest)
				{
					double normalizedObservedPeakValue = observedPeakValue / maxObservedPeakValue;
					double normalizedTheoreticalPeakValue = theoreticalPeak.Interpolate(minimumXValue + i) / maxTheoreticalPeakValue;

					xValues.Add(minimumXValue + i);
					yValues1.Add(normalizedObservedPeakValue);
					yValues2.Add(normalizedTheoreticalPeakValue);

					double residualDifference = normalizedObservedPeakValue - normalizedTheoreticalPeakValue;

					//sumOfSquaredResiduals += Math.Pow(residualDifference, 2);
					sumOfSquaredResiduals += Math.Abs(residualDifference);
					numPointsTested++;
				}
			}

			double fitScore = 1 - (sumOfSquaredResiduals / (double)numPointsTested);
			//Console.WriteLine(fitScore);

			//PeakWriter.Write(xValues, yValues1, yValues2);

			return fitScore;
		}

		public static double CalculatePeakFit(List<double> observedPeak, List<double> modelPeak, double minYValueFactor)
		{
			// If the Peaks are a different size, then transform the Test Peak to be the same size as the Fit Peak
			if (modelPeak.Count != modelPeak.Count)
			{
				observedPeak = TransformPeakToNewWidth(observedPeak, modelPeak.Count);
			}

			double maxObservedPeakValue = double.MinValue;
			double maxModelPeakValue = double.MinValue;
			double sumOfObservedPeakValues = 0.0;

			// First find the max values for normalization purposes. Also find the sum of the Y values for the Fit Peak.
			for (int i = 0; i < modelPeak.Count; i++)
			{
				if (observedPeak[i] > maxObservedPeakValue) maxObservedPeakValue = observedPeak[i];
				if (modelPeak[i] > maxModelPeakValue) maxModelPeakValue = modelPeak[i];

				sumOfObservedPeakValues += observedPeak[i];
			}

			double minValueToTest = maxObservedPeakValue * minYValueFactor;

			double sumOfSquaredResiduals = 0.0;

			for (int i = 0; i < observedPeak.Count; i++)
			{
				double observedPeakValue = observedPeak[i];

				if (observedPeakValue >= minValueToTest)
				{
					double normalizedObservedPeakValue = observedPeakValue / maxObservedPeakValue;
					double normalizedModelPeakValue = modelPeak[i] / maxModelPeakValue;

					double residualDifference = normalizedObservedPeakValue - normalizedModelPeakValue;

					sumOfSquaredResiduals += Math.Pow(residualDifference, 2);
				}
			}

			double fitScore = 1 - (sumOfSquaredResiduals / observedPeak.Count);

			return fitScore;
		}

		public static List<double> TransformPeakToNewWidth(List<double> peak, int newWidth)
		{
			if (peak.Count == newWidth)
			{
				return peak;
			}

			List<double> newPeak = new List<double>();

			// TODO: Create the new peak

			return newPeak;
		}

		/*
		public static List<int> DetectGaussianPeaks(List<double> peak)
		{
			int peakWindow = 5;
			int halfWindow = (int)Math.Floor(peakWindow / 2.0);

			List<double> gaussianPeak = CreateTheoreticalGaussianPeak(peakWindow / 2.0, peakWindow, peakWindow);

			Dictionary<int, double> peakIndexToScoreMap = new Dictionary<int, double>();

			for (int i = peakWindow - halfWindow - 1; i + peakWindow < peak.Count; i++)
			{
				List<double> currentPeakPoints = peak.GetRange(i, peakWindow);

				//double maximumValue = double.NaN;
				//FindPositionOfMaximum(currentPeakPoints, out maximumValue);

				double fitScore = CalculatePeakFit(currentPeakPoints, gaussianPeak, 0.05);
				peakIndexToScoreMap.Add(i, fitScore);
			}

			// TODO: Use the scores to find peaks??
			int referenceIndex = -99;
			double referenceScore = -99;
			bool movingUp = false;
			List<int> peakIndices = new List<int>();

			foreach (KeyValuePair<int, double> kvp in peakIndexToScoreMap)
			{
				Console.WriteLine(kvp.Key + ": " + kvp.Value);

				int index = kvp.Key;
				double score = kvp.Value;

				if (score > referenceScore)
				{
					movingUp = true;
				}
				else
				{
					if (movingUp)
					{
						peakIndices.Add(referenceIndex);
					}

					movingUp = false;
				}

				referenceScore = score;
				referenceIndex = index;
			}

			Console.WriteLine("*******************************************************************");

			Console.Write("Peaks found at: ");
			foreach (int index in peakIndices)
			{
				Console.Write(index + "\t");
			}
			Console.Write("\n");

			Console.WriteLine("===================================================================");

			return null;
		}
		*/ 

		// TODO: Verify this actually works --- Da's code, slightly modified by me
		public static Peak KDESmooth(Peak peak, double bandwidth)
		{
			List<double> xValueList = new List<double>();
			List<double> yValueList = new List<double>();

			peak.GetXAndYValuesAsLists(out xValueList, out yValueList);

			int numPoints = xValueList.Count;
			int numBins = numPoints;

			List<double> newYValueList = new List<double>();

			foreach (double point in xValueList)
			{
				double sumWInv = 0;
				double sumXoW = 0;
				double sumX2oW = 0;
				double sumYoW = 0;
				double sumXYoW = 0;

				for (int j = 0; j < numPoints; j++)
				{
					double x = xValueList[j];
					double y = yValueList[j];
					double standardized = Math.Abs(x - point) / bandwidth;
					double w = 0;
					if (standardized < 6)
					{
						w = (2 * Math.Sqrt(2 * Math.PI) * Math.Exp(-2 * standardized * standardized));
						sumWInv += 1 / w;
					}
					sumXoW += x * w;
					sumX2oW += x * x * w;
					sumYoW += y * w;
					sumXYoW += x * y * w;
				}

				double intercept = 1 / (sumWInv * sumX2oW - sumXoW * sumXoW) * (sumX2oW * sumYoW - sumXoW * sumXYoW);
				double slope = 1 / (sumWInv * sumX2oW - sumXoW * sumXoW) * (sumWInv * sumXYoW - sumXoW * sumYoW);
				newYValueList.Add(intercept + slope * point);
			}

			Peak newPeak = new Peak(xValueList, newYValueList);

			return newPeak;
		}

		// TODO: Verify this actually works --- Da's code, slightly modified by me
		public static List<double> KDESmooth(List<double> yValueList, double bandwidth)
		{
			int numPoints = yValueList.Count;

			List<double> newYValueList = new List<double>();

			for (int i = 0; i < numPoints; i++)
			{
				double sumWInv = 0;
				double sumXoW = 0;
				double sumX2oW = 0;
				double sumYoW = 0;
				double sumXYoW = 0;

				for (int j = 0; j < numPoints; j++)
				{
					double x = j;
					double y = yValueList[j];
					double standardized = Math.Abs(x - i) / bandwidth;
					double w = 0;
					if (standardized < 6)
					{
						w = (2 * Math.Sqrt(2 * Math.PI) * Math.Exp(-2 * standardized * standardized));
						sumWInv += 1 / w;
					}
					sumXoW += x * w;
					sumX2oW += x * x * w;
					sumYoW += y * w;
					sumXYoW += x * y * w;
				}

				double intercept = 1 / (sumWInv * sumX2oW - sumXoW * sumXoW) * (sumX2oW * sumYoW - sumXoW * sumXYoW);
				double slope = 1 / (sumWInv * sumX2oW - sumXoW * sumXoW) * (sumWInv * sumXYoW - sumXoW * sumYoW);
				newYValueList.Add(intercept + slope * i);
			}

			return newYValueList;
		}

		public static IInterpolation GetLinearInterpolationMethod(Peak peak)
		{
			List<double> xValues = new List<double>();
			List<double> yValues = new List<double>();

			peak.GetXAndYValuesAsLists(out xValues, out yValues);

			IInterpolation interpolation = new LinearSplineInterpolation(xValues, yValues);

			return interpolation;
		}

		private static int FindPositionOfMaximum(List<double> peak, out double maximum)
		{
			maximum = double.MinValue;
			int position = 0;

			for (int i = 0; i < peak.Count; i++)
			{
				double point = peak[i];

				if (point > maximum)
				{
					maximum = point;
					position = i;
				}
			}

			return position;
		}
	}
}
