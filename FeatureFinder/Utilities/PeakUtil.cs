using System;
using System.Collections.Generic;
using MathNet.Numerics.Interpolation;
using FeatureFinder.Data;

namespace FeatureFinder.Utilities
{
    public static class PeakUtil
    {
        private const double ONE_OVER_SQRT_OF_2_PI = 0.3989423;

        // TODO: Sometimes, a normalized value of 1 is never seen. This happens when the # of points is an even number.
        public static List<XYPair> CreateTheoreticalGaussianPeak(double centerOfPeak, double peakFWHM, int numOfPoints)
        {
            var sigma = peakFWHM / 2.35482;
            var sixSigma = 3 * peakFWHM;
            var pointSize = sixSigma / (double)(numOfPoints - 1);

            var startPoint = 0 - (int)Math.Floor(((numOfPoints - 1) / 2.0));
            var stopPoint = 0 + (int)Math.Ceiling(((numOfPoints - 1) / 2.0));

            var xyPairList = new List<XYPair>();

            for (var i = startPoint; i <= stopPoint; i++)
            {
                var xValue = centerOfPeak + (pointSize * i);
                var yValue = (1 / sigma) * ONE_OVER_SQRT_OF_2_PI * Math.Exp(-1 * (Math.Pow(xValue - centerOfPeak, 2)) / (2 * Math.Pow(sigma, 2)));

                var xyPair = new XYPair(xValue, yValue);
                xyPairList.Add(xyPair);
            }

            return xyPairList;
        }

        public static double CalculatePeakFit(Peak observedPeak, Peak theoreticalPeak, double minYValueFactor)
        {
            var xValues = new List<double>();
            var yValues1 = new List<double>();
            var yValues2 = new List<double>();

            double minimumXValue = 0;
            double maximumXValue = 0;
            observedPeak.GetMinAndMaxXValues(out minimumXValue, out maximumXValue);

            var observedInterpolation = PeakUtil.GetLinearInterpolationMethod(observedPeak);
            var theoreticalInterpolation = PeakUtil.GetLinearInterpolationMethod(theoreticalPeak);

            var maxObservedPeakValue = observedInterpolation.Interpolate(observedPeak.GetQuadraticFit());
            var maxTheoreticalPeakValue = theoreticalInterpolation.Interpolate(theoreticalPeak.GetQuadraticFit());

            var totalWidth = maximumXValue - minimumXValue;
            var pointWidth = totalWidth / 1000.0;

            var minValueToTest = maxObservedPeakValue * minYValueFactor;
            var numPointsTested = 0;

            var sumOfSquaredResiduals = 0.0;

            for (double i = 0; i < totalWidth; i += pointWidth)
            {
                var observedPeakValue = observedInterpolation.Interpolate(minimumXValue + i);

                if (observedPeakValue < minValueToTest) continue;
                var normalizedObservedPeakValue = observedPeakValue / maxObservedPeakValue;
                var normalizedTheoreticalPeakValue = theoreticalInterpolation.Interpolate(minimumXValue + i) / maxTheoreticalPeakValue;

                xValues.Add(minimumXValue + i);
                yValues1.Add(normalizedObservedPeakValue);
                yValues2.Add(normalizedTheoreticalPeakValue);

                var residualDifference = normalizedObservedPeakValue - normalizedTheoreticalPeakValue;

                //sumOfSquaredResiduals += Math.Pow(residualDifference, 2);
                sumOfSquaredResiduals += Math.Abs(residualDifference);
                numPointsTested++;
            }

            var fitScore = 1 - (sumOfSquaredResiduals / (double)numPointsTested);
            //Console.WriteLine(fitScore);

            //PeakWriter.Write(xValues, yValues1, yValues2);

            return fitScore;
        }

        public static double CalculatePeakFit(List<double> observedPeak, List<double> modelPeak, double minYValueFactor)
        {
            // If the Peaks are a different size, then transform the Test Peak to be the same size as the Fit Peak
            if (observedPeak.Count != modelPeak.Count)
            {
                observedPeak = TransformPeakToNewWidth(observedPeak, modelPeak.Count);
            }

            var maxObservedPeakValue = double.MinValue;
            var maxModelPeakValue = double.MinValue;
            var sumOfObservedPeakValues = 0.0;

            // First find the max values for normalization purposes. Also find the sum of the Y values for the Fit Peak.
            for (var i = 0; i < modelPeak.Count; i++)
            {
                if (observedPeak[i] > maxObservedPeakValue) maxObservedPeakValue = observedPeak[i];
                if (modelPeak[i] > maxModelPeakValue) maxModelPeakValue = modelPeak[i];

                sumOfObservedPeakValues += observedPeak[i];
            }

            var minValueToTest = maxObservedPeakValue * minYValueFactor;

            var sumOfSquaredResiduals = 0.0;

            for (var i = 0; i < observedPeak.Count; i++)
            {
                var observedPeakValue = observedPeak[i];

                if (observedPeakValue >= minValueToTest)
                {
                    var normalizedObservedPeakValue = observedPeakValue / maxObservedPeakValue;
                    var normalizedModelPeakValue = modelPeak[i] / maxModelPeakValue;

                    var residualDifference = normalizedObservedPeakValue - normalizedModelPeakValue;

                    sumOfSquaredResiduals += Math.Pow(residualDifference, 2);
                }
            }

            var fitScore = 1 - (sumOfSquaredResiduals / observedPeak.Count);

            return fitScore;
        }

        public static List<double> TransformPeakToNewWidth(List<double> peak, int newWidth)
        {
            if (peak.Count == newWidth)
            {
                return peak;
            }

            var newPeak = new List<double>();

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
            var xValueList = new List<double>();
            var yValueList = new List<double>();

            peak.GetXAndYValuesAsLists(out xValueList, out yValueList);

            var numPoints = xValueList.Count;
            var numBins = numPoints;

            var newYValueList = new List<double>();

            foreach (var point in xValueList)
            {
                double sumWInv = 0;
                double sumXoW = 0;
                double sumX2oW = 0;
                double sumYoW = 0;
                double sumXYoW = 0;

                for (var j = 0; j < numPoints; j++)
                {
                    var x = xValueList[j];
                    var y = yValueList[j];
                    var standardized = Math.Abs(x - point) / bandwidth;
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

                var intercept = 1 / (sumWInv * sumX2oW - sumXoW * sumXoW) * (sumX2oW * sumYoW - sumXoW * sumXYoW);
                var slope = 1 / (sumWInv * sumX2oW - sumXoW * sumXoW) * (sumWInv * sumXYoW - sumXoW * sumYoW);
                newYValueList.Add(intercept + slope * point);
            }

            var newPeak = new Peak(xValueList, newYValueList);

            return newPeak;
        }

        // TODO: Verify this actually works --- Da's code, slightly modified by me
        public static List<double> KDESmooth(List<double> yValueList, double bandwidth)
        {
            var numPoints = yValueList.Count;

            var newYValueList = new List<double>();

            for (var i = 0; i < numPoints; i++)
            {
                double sumWInv = 0;
                double sumXoW = 0;
                double sumX2oW = 0;
                double sumYoW = 0;
                double sumXYoW = 0;

                for (var j = 0; j < numPoints; j++)
                {
                    double x = j;
                    var y = yValueList[j];
                    var standardized = Math.Abs(x - i) / bandwidth;
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

                var intercept = 1 / (sumWInv * sumX2oW - sumXoW * sumXoW) * (sumX2oW * sumYoW - sumXoW * sumXYoW);
                var slope = 1 / (sumWInv * sumX2oW - sumXoW * sumXoW) * (sumWInv * sumXYoW - sumXoW * sumYoW);
                newYValueList.Add(intercept + slope * i);
            }

            return newYValueList;
        }

        public static IInterpolation GetLinearInterpolationMethod(Peak peak)
        {
            var xValues = new List<double>();
            var yValues = new List<double>();

            peak.GetXAndYValuesAsLists(out xValues, out yValues);

            IInterpolation interpolation = LinearSpline.Interpolate(xValues, yValues);

            return interpolation;
        }

        private static int FindPositionOfMaximum(List<double> peak, out double maximum)
        {
            maximum = double.MinValue;
            var position = 0;

            for (var i = 0; i < peak.Count; i++)
            {
                var point = peak[i];

                if (point <= maximum) continue;
                maximum = point;
                position = i;
            }

            return position;
        }
    }
}
