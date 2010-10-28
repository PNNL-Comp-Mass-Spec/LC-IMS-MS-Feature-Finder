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

		public float CalculateAverageMass()
		{
			float totalMass = 0;

			foreach (MSFeature msFeature in MSFeatureList)
			{
				totalMass += msFeature.MassMonoisotopic;
			}

			float averageMass = totalMass / MSFeatureList.Count;
			return averageMass;
		}

		public Dictionary<int, int> GetIntensityValues()
		{
			Dictionary<int, int> intensityDictionary = new Dictionary<int, int>();

			foreach (MSFeature msFeature in MSFeatureList)
			{
				int scanIMS = msFeature.ScanIMS;
				int intensity = msFeature.Abundance;


			}

			return intensityDictionary;
		}
	}
}
