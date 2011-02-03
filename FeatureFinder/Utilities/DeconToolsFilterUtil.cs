using System;
using System.Collections.Generic;
using System.Linq;
using FeatureFinder.Control;
using FeatureFinder.Data;

namespace FeatureFinder.Utilities
{
	public static class DeconToolsFilterUtil
	{
		private static List<DeconToolsFilter> m_deconToolsFilterList;

		static DeconToolsFilterUtil()
		{
			CreateDeconToolsFilters();
		}

		public static bool IsValidMSFeature(MSFeature msFeature)
		{
			var searchQuery = from filter in m_deconToolsFilterList
							  where msFeature.Charge >= filter.ChargeMinimum &&
									msFeature.Charge <= filter.ChargeMaximum &&
									msFeature.Abundance >= filter.AbundanceMinimum &&
									msFeature.Abundance <= filter.AbundanceMaximum &&
									msFeature.Fit <= filter.FitScoreMaximum &&
									msFeature.InterferenceScore <= filter.InterferenceScoreMaximum
							  select filter;

			if (searchQuery.Count() > 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		private static void CreateDeconToolsFilters()
		{
			m_deconToolsFilterList = new List<DeconToolsFilter>();

			DeconToolsFilter deconsToolsFilter1 = new DeconToolsFilter();
			DeconToolsFilter deconsToolsFilter2 = new DeconToolsFilter();
			DeconToolsFilter deconsToolsFilter3 = new DeconToolsFilter();
			DeconToolsFilter deconsToolsFilter4 = new DeconToolsFilter();
			DeconToolsFilter deconsToolsFilter5 = new DeconToolsFilter();
			DeconToolsFilter deconsToolsFilter6 = new DeconToolsFilter();

			for (int i = 1; i < 6; i++)
			{
				if (i == 1)
				{
					deconsToolsFilter1 = new DeconToolsFilter(i, i, 500, 1000, 0.15, 0);
					deconsToolsFilter2 = new DeconToolsFilter(i, i, 1000, 2000, 0.15, 0);
					deconsToolsFilter3 = new DeconToolsFilter(i, i, 2000, 5000, 0.15, 0.2);
					deconsToolsFilter4 = new DeconToolsFilter(i, i, 5000, 10000, 0.15, 0.3);
					deconsToolsFilter5 = new DeconToolsFilter(i, i, 10000, 25000, 0.15, 0.3);
					deconsToolsFilter6 = new DeconToolsFilter(i, i, 25000, int.MaxValue, 0.15, 0.3);
				}
				else if (i == 2)
				{
					deconsToolsFilter1 = new DeconToolsFilter(i, i, 500, 1000, 0.15, 0);
					deconsToolsFilter2 = new DeconToolsFilter(i, i, 1000, 2000, 0.15, 0);
					deconsToolsFilter3 = new DeconToolsFilter(i, i, 2000, 5000, 0.15, 0.45);
					deconsToolsFilter4 = new DeconToolsFilter(i, i, 5000, 10000, 0.15, 0.8);
					deconsToolsFilter5 = new DeconToolsFilter(i, i, 10000, 25000, 0.2, 0.8);
					deconsToolsFilter6 = new DeconToolsFilter(i, i, 25000, int.MaxValue, 0.3, 1);
				}
				else if (i == 3)
				{
					deconsToolsFilter1 = new DeconToolsFilter(i, i, 500, 1000, 0.15, 0);
					deconsToolsFilter2 = new DeconToolsFilter(i, i, 1000, 2000, 0.15, 0);
					deconsToolsFilter3 = new DeconToolsFilter(i, i, 2000, 5000, 0.15, 0.45);
					deconsToolsFilter4 = new DeconToolsFilter(i, i, 5000, 10000, 0.15, 0.8);
					deconsToolsFilter5 = new DeconToolsFilter(i, i, 10000, 25000, 0.2, 0.8);
					deconsToolsFilter6 = new DeconToolsFilter(i, i, 25000, int.MaxValue, 0.3, 1);
				}
				else if (i == 4)
				{
					deconsToolsFilter1 = new DeconToolsFilter(i, i, 500, 1000, 0.15, 0);
					deconsToolsFilter2 = new DeconToolsFilter(i, i, 1000, 2000, 0.15, 0);
					deconsToolsFilter3 = new DeconToolsFilter(i, i, 2000, 5000, 0.15, 0.45);
					deconsToolsFilter4 = new DeconToolsFilter(i, i, 5000, 10000, 0.15, 0.8);
					deconsToolsFilter5 = new DeconToolsFilter(i, i, 10000, 25000, 0.15, 0.8);
					deconsToolsFilter6 = new DeconToolsFilter(i, i, 25000, int.MaxValue, 0.15, 1);
				}
				else if (i == 5)
				{
					deconsToolsFilter1 = new DeconToolsFilter(i, int.MaxValue, 500, 1000, 0.15, 0);
					deconsToolsFilter2 = new DeconToolsFilter(i, int.MaxValue, 1000, 2000, 0.15, 0);
					deconsToolsFilter3 = new DeconToolsFilter(i, int.MaxValue, 2000, 5000, 0.15, 0.45);
					deconsToolsFilter4 = new DeconToolsFilter(i, int.MaxValue, 5000, 10000, 0.15, 0.8);
					deconsToolsFilter5 = new DeconToolsFilter(i, int.MaxValue, 10000, 25000, 0.15, 0.8);
					deconsToolsFilter6 = new DeconToolsFilter(i, int.MaxValue, 25000, int.MaxValue, 0.3, 1);
				}

				m_deconToolsFilterList.Add(deconsToolsFilter1);
				m_deconToolsFilterList.Add(deconsToolsFilter2);
				m_deconToolsFilterList.Add(deconsToolsFilter3);
				m_deconToolsFilterList.Add(deconsToolsFilter4);
				m_deconToolsFilterList.Add(deconsToolsFilter5);
				m_deconToolsFilterList.Add(deconsToolsFilter6);
			}
		}
	}
}
