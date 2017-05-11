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
            var lcimsmsFeatureList = new List<LCIMSMSFeature>();
            
            var massToleranceBase = Settings.MassMonoisotopicConstraint;

            var sortByMassQuery = from imsmsFeature in imsmsFeatureEnumerable
                                  orderby imsmsFeature.CalculateAverageMonoisotopicMass()
                                  select imsmsFeature;

            LCIMSMSFeature lcimsmsFeature = null;
            double massReference = -99;

            foreach (var imsmsFeature in sortByMassQuery)
            {
                var mass = imsmsFeature.CalculateAverageMonoisotopicMass();

                var massTolerance = massToleranceBase * massReference / 1000000;
                var massToleranceHigh = massReference + massTolerance;
                var massToleranceLow = massReference - massTolerance;

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
            var lcimsmsFeatureList = new List<LCIMSMSFeature>();

            int gapSizeMax = Settings.LCGapSizeMax;

            foreach (var lcimsmsFeature in lcimsmsFeatureEnumerable)
            {
                var sortByScanLCQuery = from imsmsFeature in lcimsmsFeature.IMSMSFeatureList
                                        orderby imsmsFeature.ScanLC
                                        select imsmsFeature;

                LCIMSMSFeature newLCIMSMSFeature = null;
                var scanLCReference = -99;

                foreach (var imsmsFeature in sortByScanLCQuery)
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
