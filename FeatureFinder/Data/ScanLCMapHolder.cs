using System;
using System.Collections.Generic;

namespace FeatureFinder.Data
{
	public static class ScanLCMapHolder
	{
		public static int ScanLCIndex { get; set; }
		public static Dictionary<int, int> ScanLCMap { get; set; }

		static ScanLCMapHolder()
		{
			Reset();
		}

		public static void Reset()
		{
			ScanLCIndex = 1;
			ScanLCMap = new Dictionary<int, int>();
		}
	}
}
