using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureFinder.Data;
using FeatureFinder.Control;

namespace FeatureFinder.Algorithms
{
	public static class ClusterMSFeatures
	{
		public static List<IMSMSFeature> ClusterByMass(IEnumerable<MSFeature> msFeatureEnumerable)
		{
			List<IMSMSFeature> imsmsFeatureList = new List<IMSMSFeature>();
			
			float massToleranceBase = Settings.MassMonoisotopicConstraint;

			var sortByMassQuery = from msFeature in msFeatureEnumerable
								  orderby msFeature.MassMonoisotopic
								  select msFeature;

			IMSMSFeature imsmsFeature = null;
			float massReference = -99;

			foreach (MSFeature msFeature in sortByMassQuery)
			{
				float mass = msFeature.MassMonoisotopic;

				float massTolerance = massToleranceBase * massReference / 1000000;
				float massToleranceHigh = massReference + massTolerance;
				float massToleranceLow = massReference - massTolerance;

				if (mass >= massToleranceLow && mass <= massToleranceHigh)
				{
					imsmsFeature.AddMSFeature(msFeature);
				}
				else
				{
					imsmsFeature = new IMSMSFeature(msFeature.ScanLC, msFeature.Charge);
					imsmsFeature.AddMSFeature(msFeature);
					imsmsFeatureList.Add(imsmsFeature);
				}

				massReference = mass;
			}

			return imsmsFeatureList;
		}
	}
}
