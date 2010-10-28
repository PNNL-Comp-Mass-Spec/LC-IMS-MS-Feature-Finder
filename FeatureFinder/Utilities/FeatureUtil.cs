using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureFinder.Data;

namespace FeatureFinder.Utilities
{
	public static class FeatureUtil
	{
		public static IEnumerable<IMSMSFeature> FilterByMemberCount(IEnumerable<IMSMSFeature> imsmsFeatureEnumerable)
		{
			var filterQuery = from imsmsFeature in imsmsFeatureEnumerable
							  where imsmsFeature.m_msFeatureList.Count > 3
							  select imsmsFeature;

			return filterQuery.AsEnumerable();
		}
	}
}
