using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FeatureFinder.Data;
using FeatureFinder.Control;

namespace FeatureFinder.Algorithms
{
    public static class ClusterMSFeatures
    {
        public static IEnumerable<IMSMSFeature> ClusterByMass(IEnumerable<MSFeature> msFeatureEnumerable)
        {
            List<IMSMSFeature> imsmsFeatureList = new List<IMSMSFeature>();
            
            float massToleranceBase = Settings.MassMonoisotopicConstraint;

            var sortByMassQuery = from msFeature in msFeatureEnumerable
                                  orderby msFeature.MassMonoisotopic
                                  select msFeature;

            IMSMSFeature imsmsFeature = null;
            double massReference = double.MinValue;

            foreach (MSFeature msFeature in sortByMassQuery)
            {
                double mass = msFeature.MassMonoisotopic;

                double massTolerance = massToleranceBase * massReference / 1000000;
                double massToleranceHigh = massReference + massTolerance;
                double massToleranceLow = massReference - massTolerance;

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
            List<IMSMSFeature> newIMSMSFeatureList = new List<IMSMSFeature>();
            foreach (IMSMSFeature imsmsFeature in imsmsFeatureEnumerable)
            {
                IEnumerable<MSFeature> msFeatureList = imsmsFeature.MSFeatureList.OrderBy(x => x.ScanIMS);
                IMSMSFeature newIMSMSFeature = null;
                int scanIMSReference = -99999;

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
