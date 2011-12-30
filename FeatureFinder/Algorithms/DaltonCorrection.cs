using System;
using System.Collections.Generic;
using System.Linq;
using FeatureFinder.Data;
using FeatureFinder.Control;
using FeatureFinder.Utilities;

namespace FeatureFinder.Algorithms
{
	public static class DaltonCorrection
	{
		public static IEnumerable<LCIMSMSFeature> CorrectLCIMSMSFeatures(IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable)
		{
			int daCorrectionMax = Settings.IMSDaCorrectionMax;
			float massToleranceBase = Settings.MassMonoisotopicConstraint;
			int totalFound = 0;

			foreach (LCIMSMSFeature lcimsmsFeature in lcimsmsFeatureEnumerable)
			{
				if (lcimsmsFeature.IMSMSFeatureList.Count == 0)
				{
					continue;
				}

				double averageMass = lcimsmsFeature.CalculateMonoIsotopicMass();
				double massTolerance = massToleranceBase * averageMass / 1000000.0;

				double errorFlagPercentage = lcimsmsFeature.GetFlaggedPercentage();

				var searchForDaErrorQuery = from otherLCIMSMSFeature in lcimsmsFeatureEnumerable
											where 
												Math.Abs(averageMass - otherLCIMSMSFeature.CalculateMonoIsotopicMass()) >= (1 - massTolerance) 
												&& Math.Abs(averageMass - otherLCIMSMSFeature.CalculateMonoIsotopicMass()) <= (1 + massTolerance)
												&& otherLCIMSMSFeature.IMSMSFeatureList.Count > 0
											orderby Math.Abs(errorFlagPercentage - otherLCIMSMSFeature.GetFlaggedPercentage()) descending
											select otherLCIMSMSFeature;

				foreach (LCIMSMSFeature lcimsmsFeatureToCheck in searchForDaErrorQuery.AsParallel().Where(lcimsmsFeatureToCheck => Math.Abs(errorFlagPercentage - lcimsmsFeatureToCheck.GetFlaggedPercentage()) > 0.3))
				{
					//bool featuresFitTogether = FeatureUtil.DoLCIMSMSFeaturesFitTogether(lcimsmsFeature, lcimsmsFeatureToCheck);

					//if (featuresFitTogether)
					//{
					if (errorFlagPercentage > lcimsmsFeatureToCheck.GetFlaggedPercentage())
					{
						FeatureUtil.MergeLCIMSMSFeatures(lcimsmsFeatureToCheck, lcimsmsFeature);
					}
					else
					{
						FeatureUtil.MergeLCIMSMSFeatures(lcimsmsFeature, lcimsmsFeatureToCheck);
					}
							
					totalFound++;
					break;
					//}
				}
			}

			var newFeatureListQuery = lcimsmsFeatureEnumerable.Where(lcimsmsFeature => lcimsmsFeature.IMSMSFeatureList.Count > 0);

			return newFeatureListQuery.AsEnumerable();
		}
	}
}
