using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FeatureFinder.Data
{
	public class XYPair
	{
		public double XValue { get; set; }
		public double YValue { get; set; }

		public XYPair(double xValue, double yValue)
		{
			this.XValue = xValue;
			this.YValue = yValue;
		}
	}
}
