using System.Collections.Generic;
using System.Linq;
using FeatureFinder.Control;
using FeatureFinder.Data;

namespace FeatureFinder.Utilities
{
	public static class DeconToolsFilterUtil
	{
		public static bool IsValidMSFeature(MSFeature msFeature, List<DeconToolsFilter> deconToolsFilterList)
		{
            var searchQuery = from filter in deconToolsFilterList
							  where msFeature.Charge >= filter.ChargeMinimum &&
									msFeature.Charge <= filter.ChargeMaximum &&
									msFeature.Abundance >= filter.AbundanceMinimum &&
									msFeature.Abundance <= filter.AbundanceMaximum &&
									msFeature.Fit <= filter.FitScoreMaximum &&
									msFeature.InterferenceScore <= filter.InterferenceScoreMaximum
							  select filter;

			return searchQuery.Count() > 0;
		}
	}
}
