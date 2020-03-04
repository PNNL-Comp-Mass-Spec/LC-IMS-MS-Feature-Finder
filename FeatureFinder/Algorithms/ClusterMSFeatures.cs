using System.Collections.Generic;
using System.Linq;
using FeatureFinder.Data;
using FeatureFinder.Control;

namespace FeatureFinder.Algorithms
{
    public static class ClusterMSFeatures
    {
        public static IEnumerable<imsMsFeature> ClusterByMass(IEnumerable<MSFeature> msFeatureEnumerable)
        {
            var imsMsFeatureList = new List<imsMsFeature>();

            var massToleranceBase = Settings.MassMonoisotopicConstraint;

            var sortByMassQuery = from msFeature in msFeatureEnumerable
                                  orderby msFeature.MassMonoisotopic
                                  select msFeature;

            imsMsFeature imsMsFeature = null;
            var massReference = double.MinValue;

            foreach (var msFeature in sortByMassQuery)
            {
                var mass = msFeature.MassMonoisotopic;

                var massTolerance = massToleranceBase * massReference / 1000000;
                var massToleranceHigh = massReference + massTolerance;
                var massToleranceLow = massReference - massTolerance;

                if (mass >= massToleranceLow && mass <= massToleranceHigh && imsMsFeature != null)
                {
                    imsMsFeature.AddMSFeature(msFeature);
                }
                else
                {
                    imsMsFeature = new imsMsFeature(msFeature.ScanLC, msFeature.Charge);
                    imsMsFeature.AddMSFeature(msFeature);
                    imsMsFeatureList.Add(imsMsFeature);
                }

                massReference = mass;
            }

            return imsMsFeatureList;
        }

        public static IEnumerable<imsMsFeature> SplitByIMSScan(IEnumerable<imsMsFeature> imsMsFeatureEnumerable, int maxGap)
        {
            var newImsMsFeatureList = new List<imsMsFeature>();
            foreach (var imsMsFeature in imsMsFeatureEnumerable)
            {
                IEnumerable<MSFeature> msFeatureList = imsMsFeature.MSFeatureList.OrderBy(x => x.ScanIMS);
                imsMsFeature newImsMsFeature = null;
                var scanIMSReference = -99999;

                foreach (var msFeature in msFeatureList)
                {
                    if (msFeature.ScanIMS - scanIMSReference > maxGap)
                    {
                        newImsMsFeature = new imsMsFeature(imsMsFeature.ScanLC, imsMsFeature.Charge);
                        newImsMsFeature.AddMSFeature(msFeature);
                        newImsMsFeatureList.Add(newImsMsFeature);
                    }
                    else
                    {
                        newImsMsFeature?.AddMSFeature(msFeature);
                    }

                    scanIMSReference = msFeature.ScanIMS;
                }
            }

            return newImsMsFeatureList;
        }
    }
}
