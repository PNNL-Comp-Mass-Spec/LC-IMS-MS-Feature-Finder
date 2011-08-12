using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FeatureFinder.Data;
using FeatureFinder.Control;

namespace FeatureFinder.Algorithms
{
	public static class ClusterIMSMSFeatures
	{
		public static IEnumerable<LCIMSMSFeature> ClusterByMassAndScanLC(IEnumerable<IMSMSFeature> imsmsFeatureEnumerable)
		{
			List<LCIMSMSFeature> lcimsmsFeatureList = new List<LCIMSMSFeature>();
			
			float massToleranceBase = Settings.MassMonoisotopicConstraint;

			var sortByMassQuery = from imsmsFeature in imsmsFeatureEnumerable
								  orderby imsmsFeature.CalculateAverageMass()
								  select imsmsFeature;

			LCIMSMSFeature lcimsmsFeature = null;
			double massReference = -99;

			foreach (IMSMSFeature imsmsFeature in sortByMassQuery)
			{
				double mass = imsmsFeature.CalculateAverageMass();

				double massTolerance = massToleranceBase * massReference / 1000000;
				double massToleranceHigh = massReference + massTolerance;
				double massToleranceLow = massReference - massTolerance;

				if (mass >= massToleranceLow && mass <= massToleranceHigh)
				{
					lcimsmsFeature.AddIMSMSFeature(imsmsFeature);
				}
				else
				{
					lcimsmsFeature = new LCIMSMSFeature(imsmsFeature.Charge);
					lcimsmsFeature.AddIMSMSFeature(imsmsFeature);
					lcimsmsFeatureList.Add(lcimsmsFeature);
				}

				massReference = mass;
			}

			return lcimsmsFeatureList;

			//IEnumerable<LCIMSMSFeature> splitLCIMSMSFeatureEnumerable = SplitByScanLCGap(lcimsmsFeatureList);

			//return splitLCIMSMSFeatureEnumerable;
		}

		private static IEnumerable<LCIMSMSFeature> SplitByScanLCGap(IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable)
		{
			List<LCIMSMSFeature> lcimsmsFeatureList = new List<LCIMSMSFeature>();

			int gapSizeMax = Settings.LCGapSizeMax;

			foreach (LCIMSMSFeature lcimsmsFeature in lcimsmsFeatureEnumerable)
			{
				var sortByScanLCQuery = from imsmsFeature in lcimsmsFeature.IMSMSFeatureList
										orderby imsmsFeature.ScanLC
										select imsmsFeature;

				LCIMSMSFeature newLCIMSMSFeature = null;
				int scanLCReference = -99;

				foreach (IMSMSFeature imsmsFeature in sortByScanLCQuery)
				{
					if (imsmsFeature.ScanLC - scanLCReference - 1 <= gapSizeMax)
					{
						newLCIMSMSFeature.AddIMSMSFeature(imsmsFeature);
					}
					else
					{
						newLCIMSMSFeature = new LCIMSMSFeature(imsmsFeature.Charge);
						newLCIMSMSFeature.AddIMSMSFeature(imsmsFeature);
						lcimsmsFeatureList.Add(newLCIMSMSFeature);
					}

					scanLCReference = imsmsFeature.ScanLC;
				}
			}

			return lcimsmsFeatureList;
		}
	}
}
