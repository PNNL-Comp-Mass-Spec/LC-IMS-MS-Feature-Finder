using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureFinder.Control;

namespace FeatureFinder.Data
{
	public class Peak
	{
		public List<XYPair> XYPairList { get; set; }

		public Peak(List<XYPair> xyPairList)
		{
			this.XYPairList = new List<XYPair>();
			this.XYPairList.AddRange(xyPairList);
		}

		public Peak(List<double> xValues, List<double> yValues)
		{
			if (xValues.Count != yValues.Count)
			{
				Logger.Log("The xValues and yValues Lists must be the same size to create a Peak");
				throw new InvalidOperationException("The xValues and yValues Lists must be the same size to create a Peak");
			}

			List<XYPair> xyPairList = new List<XYPair>();

			for (int i = 0; i < xValues.Count; i++)
			{
				double xValue = xValues[i];
				double yValue = yValues[i];

				XYPair xyPair = new XYPair(xValue, yValue);
				xyPairList.Add(xyPair);
			}

			this.XYPairList = xyPairList;
		}

		public void GetXAndYValuesAsLists(out List<double> xValues, out List<double> yValues)
		{
			xValues = new List<double>();
			yValues = new List<double>();

			foreach (XYPair xyPair in this.XYPairList)
			{
				xValues.Add(xyPair.XValue);
				yValues.Add(xyPair.YValue);
			}
		}

		public void GetMinAndMaxXValues(out double xValueMinimum, out double xValueMaximum)
		{
			var sortByXValue = from xyPair in XYPairList
							   where xyPair.YValue > 0
							   orderby xyPair.XValue ascending
							   select xyPair;

			xValueMinimum = sortByXValue.First().XValue;
			xValueMaximum = sortByXValue.Last().XValue;
		}

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

		public void PrintPeakToConsole()
		{
			var sortByXValue = from xyPair in XYPairList
							   orderby xyPair.XValue ascending
							   select xyPair;

			
			foreach (XYPair xyPair in sortByXValue)
			{
				Console.WriteLine("[" + xyPair.XValue + ", " + xyPair.YValue + "]\t");
			}
		}

		public double GetWeightedApex()
		{
			double totalY = 0;
			double totalXTimesY = 0;

			foreach (XYPair xyPair in this.XYPairList)
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

			int indexOfMaxIntensity = 0;
			double maxIntensity = 0;
			for (int i = 0; i < XYPairList.Count; i++)
			{
				XYPair xyPair = XYPairList[i];
				if (xyPair.YValue > maxIntensity)
				{
					indexOfMaxIntensity = i;
					maxIntensity = xyPair.YValue;
				}
			}

			double x1 = XYPairList[indexOfMaxIntensity - 1].XValue;
			double x2 = XYPairList[indexOfMaxIntensity].XValue;
			double x3 = XYPairList[indexOfMaxIntensity + 1].XValue;
			double y1 = XYPairList[indexOfMaxIntensity - 1].YValue;
			double y2 = XYPairList[indexOfMaxIntensity].YValue;
			double y3 = XYPairList[indexOfMaxIntensity + 1].YValue;

			double quadratic = (y2 - y1) * (x3 - x2) - (y3 - y2) * (x2 - x1);

			// no good.  Just return the known peak
			if (quadratic == 0)
			{
				return x2;  
			}

			quadratic = ((x1 + x2) - ((y2 - y1) * (x3 - x2) * (x1 - x3)) / quadratic) / 2.0;
			return quadratic;	// Calculated new peak.  Return it.
		}
	}
}
