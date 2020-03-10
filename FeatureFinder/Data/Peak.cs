using System;
using System.Collections.Generic;
using System.Linq;
using FeatureFinder.Control;

namespace FeatureFinder.Data
{
    public class Peak
    {
        public List<XYPair> XYPairList { get; set; }

        public Peak(IEnumerable<XYPair> xyPairList)
        {
            XYPairList = new List<XYPair>();
            XYPairList.AddRange(xyPairList);
        }

        public Peak(IList<double> xValues, IList<double> yValues)
        {
            if (xValues.Count != yValues.Count)
            {
                var errorMessage = "The xValues and yValues Lists must be the same size to create a Peak";
                Logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var xyPairList = new List<XYPair>();

            for (var i = 0; i < xValues.Count; i++)
            {
                var xValue = xValues[i];
                var yValue = yValues[i];

                var xyPair = new XYPair(xValue, yValue);
                xyPairList.Add(xyPair);
            }

            XYPairList = xyPairList;
        }

        public void GetXAndYValuesAsLists(out List<double> xValues, out List<double> yValues)
        {
            xValues = new List<double>();
            yValues = new List<double>();

            foreach (var xyPair in XYPairList)
            {
                xValues.Add(xyPair.XValue);
                yValues.Add(xyPair.YValue);
            }
        }

        public void GetMinAndMaxXValues(out double xValueMinimum, out double xValueMaximum)
        {
            var sortByXValue = (from xyPair in XYPairList
                               where xyPair.YValue > 0
                               orderby xyPair.XValue
                               select xyPair).ToList();

            xValueMinimum = sortByXValue.First().XValue;
            xValueMaximum = sortByXValue.Last().XValue;
        }

        [Obsolete("Unused")]
        public double GetXValueOfMaximumYValue()
        {
            var sortByYValue = from xyPair in XYPairList
                               orderby xyPair.YValue descending
                               select xyPair;

            return sortByYValue.First().XValue;
        }

        public double GetMaximumYValue()
        {
            var sortByYValue = from xyPair in XYPairList
                               orderby xyPair.YValue descending
                               select xyPair;

            return sortByYValue.First().YValue;
        }

        public double GetMaximumXValue()
        {
            var sortByXValue = from xyPair in XYPairList
                               orderby xyPair.XValue descending
                               select xyPair;

            return sortByXValue.First().XValue;
        }

        [Obsolete("Unused")]
        public void PrintPeakToConsole()
        {
            var sortByXValue = from xyPair in XYPairList
                               orderby xyPair.XValue
                               select xyPair;


            foreach (var xyPair in sortByXValue)
            {
                Console.WriteLine("[" + xyPair.XValue + ", " + xyPair.YValue + "]\t");
            }
        }

        [Obsolete("Unused")]
        public double GetWeightedApex()
        {
            double totalY = 0;
            double totalXTimesY = 0;

            foreach (var xyPair in XYPairList)
            {
                totalY += xyPair.YValue;
                totalXTimesY += (xyPair.XValue * xyPair.YValue);
            }

            return totalXTimesY / totalY;
        }

        public double GetQuadraticFit()
        {
            if(XYPairList.Count < 3)
            {
                return double.NaN;
            }

            var indexOfMaxIntensity = 0;
            double maxIntensity = 0;
            for (var i = 0; i < XYPairList.Count; i++)
            {
                var xyPair = XYPairList[i];
                if (xyPair.YValue > maxIntensity)
                {
                    indexOfMaxIntensity = i;
                    maxIntensity = xyPair.YValue;
                }
            }

            var x1 = XYPairList[indexOfMaxIntensity - 1].XValue;
            var x2 = XYPairList[indexOfMaxIntensity].XValue;
            var x3 = XYPairList[indexOfMaxIntensity + 1].XValue;
            var y1 = XYPairList[indexOfMaxIntensity - 1].YValue;
            var y2 = XYPairList[indexOfMaxIntensity].YValue;
            var y3 = XYPairList[indexOfMaxIntensity + 1].YValue;

            var quadratic = (y2 - y1) * (x3 - x2) - (y3 - y2) * (x2 - x1);

            // no good.  Just return the known peak
            if (Math.Abs(quadratic - 0) < double.Epsilon)
            {
                return x2;
            }

            quadratic = ((x1 + x2) - ((y2 - y1) * (x3 - x2) * (x1 - x3)) / quadratic) / 2.0;
            return quadratic;   // Calculated new peak.  Return it.
        }
    }
}
