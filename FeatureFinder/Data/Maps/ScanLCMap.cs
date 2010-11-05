using System;
using System.Collections.Generic;

namespace FeatureFinder.Data.Maps
{
	public static class ScanLCMap
	{
		public static int ScanLCIndex { get; set; }
		public static Dictionary<int, int> Mapping { get; set; }

		static ScanLCMap()
		{
			Reset();
		}

		public static void Reset()
		{
			ScanLCIndex = 1;
			Mapping = new Dictionary<int, int>();
		}
	}
}
