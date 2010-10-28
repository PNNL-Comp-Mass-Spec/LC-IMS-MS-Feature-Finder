using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FeatureFinder.Data
{
	public class LCIMSMSFeature
	{
		public byte Charge { get; set; }

		public List<IMSMSFeature> m_imsmsFeatureList;

		public LCIMSMSFeature(byte charge)
		{
			m_imsmsFeatureList = new List<IMSMSFeature>();
			Charge = charge;
		}

		public void AddIMSMSFeature(IMSMSFeature imsmsFeature)
		{
			m_imsmsFeatureList.Add(imsmsFeature);
		}
	}
}
