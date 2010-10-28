using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureFinder.Data;
using FeatureFinder.Control;

namespace FeatureFinder.Algorithms
{
	public static class ClusterIMSMSFeatures
	{
		// TODO: Finish this implementation. Should I just sort by mass, cluster everything across NET, and then split at the end?
		public static IEnumerable<LCIMSMSFeature> ClusterByMass(IEnumerable<IMSMSFeature> imsmsFeatureEnumerable)
		{
			List<LCIMSMSFeature> lcimsmsFeatureList = new List<LCIMSMSFeature>();
			
			float massToleranceBase = Settings.MassMonoisotopicConstraint;

			var sortByMassQuery = from imsmsFeature in imsmsFeatureEnumerable
								  orderby imsmsFeature.CalculateAverageMass()
								  select imsmsFeature;

			LCIMSMSFeature lcimsmsFeature = null;
			float massReference = -99;

			foreach (IMSMSFeature imsmsFeature in sortByMassQuery)
			{
				float mass = imsmsFeature.CalculateAverageMass();

				float massTolerance = massToleranceBase * massReference / 1000000;
				float massToleranceHigh = massReference + massTolerance;
				float massToleranceLow = massReference - massTolerance;

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
		}
	}
}
