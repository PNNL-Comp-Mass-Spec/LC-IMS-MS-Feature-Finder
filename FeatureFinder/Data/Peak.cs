using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FeatureFinder.Data
{
	public class Peak
	{
		public List<XYPair> XYPairList { get; set; }

		public Peak(List<XYPair> xyPairList)
		{
			this.XYPairList = xyPairList;
		}

		public Peak(List<double> xValues, List<double> yValues)
		{
			if (xValues.Count != yValues.Count)
			{
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
							   orderby xyPair.XValue ascending
							   select xyPair;

			xValueMinimum = sortByXValue.First().XValue;
			xValueMaximum = sortByXValue.Last().XValue;
		}

		public void PrintPeakToConsole()
		{
			var sortByXValue = from xyPair in XYPairList
							   orderby xyPair.XValue ascending
							   select xyPair;

			Console.WriteLine("*********************************************");
			foreach (XYPair xyPair in sortByXValue)
			{
				Console.Write(xyPair.YValue + "\t");
			}
		}
	}
}
