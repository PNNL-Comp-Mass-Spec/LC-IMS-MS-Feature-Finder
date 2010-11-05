using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.Interpolation;

namespace FeatureFinder.Data.Maps
{
	public static class ScanIMSToDriftTimeMap
	{
		public static Dictionary<int, float> Mapping { get; set; }

		static ScanIMSToDriftTimeMap()
		{
			Mapping = new Dictionary<int, float>();
		}

		public static IInterpolationMethod GetInterpolation()
		{
			List<double> xValues = new List<double>();
			List<double> yValues = new List<double>();

			foreach (KeyValuePair<int, float> kvp in Mapping)
			{
				xValues.Add(kvp.Key);
				yValues.Add(kvp.Value);
			}

			IInterpolationMethod interpolation = Interpolation.CreateLinearSpline(xValues, yValues);

			return interpolation;
		}
	}
}
