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

			

			
		}
	}
}
