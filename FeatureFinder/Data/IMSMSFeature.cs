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

		public List<MSFeature> m_msFeatureList;

		public IMSMSFeature(int scanLC, byte charge)
		{
			m_msFeatureList = new List<MSFeature>();
			ScanLC = scanLC;
			Charge = charge;
		}

		public void AddMSFeature(MSFeature msFeature)
		{
			m_msFeatureList.Add(msFeature);
		}

		public float CalculateAverageMass()
		{
			float totalMass = 0;

			foreach (MSFeature msFeature in m_msFeatureList)
			{
				totalMass += msFeature.MassMonoisotopic;
			}

			float averageMass = totalMass / m_msFeatureList.Count;
			return averageMass;
		}
	}
}
