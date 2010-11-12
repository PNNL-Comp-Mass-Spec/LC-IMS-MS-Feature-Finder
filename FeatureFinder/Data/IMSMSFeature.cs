using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FeatureFinder.Data
{
	public class IMSMSFeature
	{
		public byte Charge { get; set; }
		public int ScanLC { get; set; }

		public List<MSFeature> MSFeatureList { get; set; }

		public IMSMSFeature(int scanLC, byte charge)
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

		public double CalculateAverageMass()
		{
			double totalMass = 0;

			foreach (MSFeature msFeature in MSFeatureList)
			{
				totalMass += msFeature.MassMonoisotopic;
			}

			double averageMass = totalMass / MSFeatureList.Count;
			return averageMass;
		}

		public double GetIntensity()
		{
			double totalIntensity = 0;

			foreach (MSFeature msFeature in MSFeatureList)
			{
				totalIntensity += msFeature.Abundance;
			}

			return totalIntensity;
		}

		public Dictionary<double, double> GetIntensityValues()
		{
			Dictionary<double, double> intensityDictionary = new Dictionary<double, double>();

			foreach (MSFeature msFeature in MSFeatureList)
			{
				double driftTime = msFeature.DriftTime;
				double intensity = msFeature.Abundance; // TODO: Use Original Intensity if available

				double currentIntensity = 0.0;
				if (intensityDictionary.TryGetValue(driftTime, out currentIntensity))
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

		public void GetMinAndMaxDriftTimes(out double driftTimeMinimum, out double driftTimeMaximum)
		{
			var sortByScanIMSQuery = from msFeature in MSFeatureList
									 orderby msFeature.DriftTime
									 select msFeature;

			driftTimeMinimum = sortByScanIMSQuery.First().DriftTime;
			driftTimeMaximum = sortByScanIMSQuery.Last().DriftTime;
		}

		public IEnumerable<MSFeature> FindMSFeaturesInDriftTimeRange(double lowDriftTime, double highDriftTime)
		{
			var findByDriftTimeQuery = from msFeature in MSFeatureList
									   where msFeature.DriftTime >= lowDriftTime && msFeature.DriftTime <= highDriftTime
									   select msFeature;

			return findByDriftTimeQuery.AsEnumerable();
		}
	}
}
