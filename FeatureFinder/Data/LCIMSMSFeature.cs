using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FeatureFinder.Data
{
	public class LCIMSMSFeature
	{
		public byte Charge { get; set; }

		public List<IMSMSFeature> IMSMSFeatureList { get; set; }

		public LCIMSMSFeature(byte charge)
		{
			IMSMSFeatureList = new List<IMSMSFeature>();
			Charge = charge;
		}

		public void AddIMSMSFeature(IMSMSFeature imsmsFeature)
		{
			IMSMSFeatureList.Add(imsmsFeature);
		}
	}
}
