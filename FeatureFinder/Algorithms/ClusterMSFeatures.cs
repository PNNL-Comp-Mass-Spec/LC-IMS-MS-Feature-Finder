using System.Collections.Generic;
using System.Linq;
using FeatureFinder.Data;
using FeatureFinder.Control;

namespace FeatureFinder.Algorithms
{
    public static class ClusterMSFeatures
    {
        public static IEnumerable<IMSMSFeature> ClusterByMass(IEnumerable<MSFeature> msFeatureEnumerable)
        {
            var imsmsFeatureList = new List<IMSMSFeature>();
            
            var massToleranceBase = Settings.MassMonoisotopicConstraint;

            var sortByMassQuery = from msFeature in msFeatureEnumerable
                                  orderby msFeature.MassMonoisotopic
                                  select msFeature;

            IMSMSFeature imsmsFeature = null;
            var massReference = double.MinValue;

            foreach (var msFeature in sortByMassQuery)
            {
                var mass = msFeature.MassMonoisotopic;

                var massTolerance = massToleranceBase * massReference / 1000000;
                var massToleranceHigh = massReference + massTolerance;
                var massToleranceLow = massReference - massTolerance;

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

        public static IEnumerable<IMSMSFeature> SplitByIMSScan(IEnumerable<IMSMSFeature> imsmsFeatureEnumerable, int maxGap)
        {
            var newIMSMSFeatureList = new List<IMSMSFeature>();
            foreach (var imsmsFeature in imsmsFeatureEnumerable)
            {
                IEnumerable<MSFeature> msFeatureList = imsmsFeature.MSFeatureList.OrderBy(x => x.ScanIMS);
                IMSMSFeature newIMSMSFeature = null;
                var scanIMSReference = -99999;

                foreach (var msFeature in msFeatureList)
                {
                    if (msFeature.ScanIMS - scanIMSReference > maxGap)
                    {
                        newIMSMSFeature = new IMSMSFeature(imsmsFeature.ScanLC, imsmsFeature.Charge);
                        newIMSMSFeature.AddMSFeature(msFeature);
                        newIMSMSFeatureList.Add(newIMSMSFeature);
                    }
                    else
                    {
                        newIMSMSFeature.AddMSFeature(msFeature);
                    }

                    scanIMSReference = msFeature.ScanIMS;
                }
            }

            return newIMSMSFeatureList;
        }
    }
}
