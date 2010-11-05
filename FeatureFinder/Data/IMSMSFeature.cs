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

		public void AddMSFeatureList(List<MSFeature> msFeatureList)
		{
			MSFeatureList.AddRange(msFeatureList);
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

		public Dictionary<int, double> GetIntensityValues()
		{
			Dictionary<int, double> intensityDictionary = new Dictionary<int, double>();

			foreach (MSFeature msFeature in MSFeatureList)
			{
				int scanIMS = msFeature.ScanIMS;
				double intensity = msFeature.Abundance; // TODO: Use Original Intensity if available

				double currentIntensity = 0.0;
				if (intensityDictionary.TryGetValue(scanIMS, out currentIntensity))
				{
					intensityDictionary[scanIMS] += intensity;
				}
				else
				{
					intensityDictionary.Add(scanIMS, intensity);
				}
			}

			return intensityDictionary;
		}

		public void GetMinAndMaxScanIMS(out int scanIMSMinimum, out int scanIMSMaximum)
		{
			var sortByScanIMSQuery = from msFeature in MSFeatureList
									 orderby msFeature.ScanIMS
									 select msFeature;

			scanIMSMinimum = sortByScanIMSQuery.First().ScanIMS;
			scanIMSMaximum = sortByScanIMSQuery.Last().ScanIMS;
		}
	}
}
