using System;
using System.Collections.Generic;
using System.Linq;
using FeatureFinder.Data;
using FeatureFinder.Control;

namespace FeatureFinder.Algorithms
{
    public static class ClusterImsMsFeatures
    {
        public static IEnumerable<LCIMSMSFeature> ClusterByMassAndScanLC(IEnumerable<imsMsFeature> imsMsFeatureEnumerable)
        {
            var lcimsmsFeatureList = new List<LCIMSMSFeature>();

            var massToleranceBase = Settings.MassMonoisotopicConstraint;

            var sortByMassQuery = from imsMsFeature in imsMsFeatureEnumerable
                                  orderby imsMsFeature.CalculateAverageMonoisotopicMass()
                                  select imsMsFeature;

            LCIMSMSFeature lcimsmsFeature = null;
            double massReference = -99;

            foreach (var imsMsFeature in sortByMassQuery)
            {
                var mass = imsMsFeature.CalculateAverageMonoisotopicMass();

                var massTolerance = massToleranceBase * massReference / 1000000;
                var massToleranceHigh = massReference + massTolerance;
                var massToleranceLow = massReference - massTolerance;

                if (mass >= massToleranceLow && mass <= massToleranceHigh && lcimsmsFeature != null)
                {
                    lcimsmsFeature.AddImsMsFeature(imsMsFeature);
                }
                else
                {
                    lcimsmsFeature = new LCIMSMSFeature(imsMsFeature.Charge);
                    lcimsmsFeature.AddImsMsFeature(imsMsFeature);
                    lcimsmsFeatureList.Add(lcimsmsFeature);
                }

                massReference = mass;
            }

            return lcimsmsFeatureList;

            //IEnumerable<LCIMSMSFeature> splitLCIMSMSFeatureEnumerable = SplitByScanLCGap(lcimsmsFeatureList);

            //return splitLCIMSMSFeatureEnumerable;
        }

        [Obsolete("Unused")]
        private static IEnumerable<LCIMSMSFeature> SplitByScanLCGap(IEnumerable<LCIMSMSFeature> lcImsMsFeatureEnumerable)
        {
            var lcimsmsFeatureList = new List<LCIMSMSFeature>();

            int gapSizeMax = Settings.LCGapSizeMax;

            foreach (var lcimsmsFeature in lcImsMsFeatureEnumerable)
            {
                var sortByScanLCQuery = from imsMsFeature in lcimsmsFeature.imsMsFeatureList
                                        orderby imsMsFeature.ScanLC
                                        select imsMsFeature;

                LCIMSMSFeature newLCImsMsFeature = null;
                var scanLCReference = -99;

                foreach (var imsMsFeature in sortByScanLCQuery)
                {
                    if (imsMsFeature.ScanLC - scanLCReference - 1 <= gapSizeMax && newLCImsMsFeature != null)
                    {
                        newLCImsMsFeature.AddImsMsFeature(imsMsFeature);
                    }
                    else
                    {
                        newLCImsMsFeature = new LCIMSMSFeature(imsMsFeature.Charge);
                        newLCImsMsFeature.AddImsMsFeature(imsMsFeature);
                        lcimsmsFeatureList.Add(newLCImsMsFeature);
                    }

                    scanLCReference = imsMsFeature.ScanLC;
                }
            }

            return lcimsmsFeatureList;
        }
    }
}
