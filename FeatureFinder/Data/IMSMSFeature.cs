using System;
using System.Collections.Generic;
using System.Linq;

namespace FeatureFinder.Data
{
    public class imsMsFeature
    {
        public byte Charge { get; set; }
        public int ScanLC { get; set; }

        public List<MSFeature> MSFeatureList { get; set; }

        public imsMsFeature(int scanLC, byte charge)
        {
            MSFeatureList = new List<MSFeature>();
            ScanLC = scanLC;
            Charge = charge;
        }

        public void AddMSFeature(MSFeature msFeature)
        {
            MSFeatureList.Add(msFeature);
        }

        public void AddMSFeatureList(IEnumerable<MSFeature> msFeatureEnumerable)
        {
            MSFeatureList.AddRange(msFeatureEnumerable);
        }


        public double CalculateAverageMonoisotopicMass()
        {
            return MSFeatureList.Average(msFeature => msFeature.MassMonoisotopic);
        }

        public double CalculateAverageMz()
        {
            return MSFeatureList.Average(msFeature => msFeature.Mz);
        }

        public double GetIntensity()
        {
            return MSFeatureList.Sum(msFeature => msFeature.Abundance);
        }

        [Obsolete("Unused")]
        public Dictionary<double, double> GetIntensityValues()
        {
            var intensityDictionary = new Dictionary<double, double>();

            foreach (var msFeature in MSFeatureList)
            {
                double driftTime = msFeature.DriftTime;
                double intensity = msFeature.Abundance; // TODO: Use Original Intensity if available

                if (intensityDictionary.TryGetValue(driftTime, out _))
                {
                    intensityDictionary[driftTime] += intensity;
                }
                else
                {
                    intensityDictionary.Add(driftTime, intensity);
                }
            }

            return intensityDictionary;
        }

        [Obsolete("Unused")]
        public void GetMinAndMaxDriftTimes(out double driftTimeMinimum, out double driftTimeMaximum)
        {
            var sortByDriftTimeQuery = (from msFeature in MSFeatureList
                                       orderby msFeature.DriftTime
                                       select msFeature).ToList();

            driftTimeMinimum = sortByDriftTimeQuery.First().DriftTime;
            driftTimeMaximum = sortByDriftTimeQuery.Last().DriftTime;
        }

        public void GetMinAndMaxIMSScan(out double scanIMSMinimum, out double scanIMSMaximum)
        {
            var sortByScanIMSQuery = (from msFeature in MSFeatureList
                                     orderby msFeature.ScanIMS
                                     select msFeature).ToList();

            scanIMSMinimum = sortByScanIMSQuery.First().ScanIMS;
            scanIMSMaximum = sortByScanIMSQuery.Last().ScanIMS;
        }

        [Obsolete("Unused")]
        public IEnumerable<MSFeature> FindMSFeaturesInDriftTimeRange(double lowDriftTime, double highDriftTime)
        {
            var findByDriftTimeQuery = from msFeature in MSFeatureList
                                       where msFeature.DriftTime >= lowDriftTime && msFeature.DriftTime <= highDriftTime
                                       select msFeature;

            return findByDriftTimeQuery.AsEnumerable();
        }

        public IEnumerable<MSFeature> FindMSFeaturesInScanIMSRange(double lowScanIMS, double highScanIMS)
        {
            var findByScanIMSQuery = from msFeature in MSFeatureList
                                     where msFeature.ScanIMS >= lowScanIMS && msFeature.ScanIMS <= highScanIMS
                                     select msFeature;

            return findByScanIMSQuery.AsEnumerable();
        }

        public MSFeature FindRepMSFeature()
        {
            var sortByAbudnanceQuery = from msFeature in MSFeatureList
                                       orderby msFeature.Abundance descending
                                       select msFeature;

            return sortByAbudnanceQuery.First();
        }
    }
}
