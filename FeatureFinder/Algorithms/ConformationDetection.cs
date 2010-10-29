using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureFinder.Data;

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

			List<Dictionary<int, int>> intensityDictionaries = new List<Dictionary<int, int>>();

			int globalScanIMSMinimum = int.MaxValue;
			int globalScanIMSMaximum = int.MinValue;
			int localScanIMSMinimum = 0;
			int localScanIMSMaximum = 0;

			foreach (IMSMSFeature imsmsFeature in sortByScanLCQuery)
			{
				intensityDictionaries.Add(imsmsFeature.GetIntensityValues());

				imsmsFeature.GetMinAndMaxScanIMS(out localScanIMSMinimum, out localScanIMSMaximum);

				if (localScanIMSMinimum < globalScanIMSMinimum) globalScanIMSMinimum = localScanIMSMinimum;
				if (localScanIMSMaximum > globalScanIMSMaximum) globalScanIMSMaximum = localScanIMSMaximum;
			}

			for (int i = globalScanIMSMinimum; i <= globalScanIMSMaximum; i++)
			{

			}
		}
	}
}
