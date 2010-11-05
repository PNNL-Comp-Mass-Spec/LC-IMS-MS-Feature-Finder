using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureFinder.Utilities;

namespace FeatureFinder.Data
{
	public class LCIMSMSFeature
	{
		public byte Charge { get; set; }

		public List<IMSMSFeature> IMSMSFeatureList { get; set; }

		public float IMSScore { get; set; }
		public float LCScore { get; set; }

		public LCIMSMSFeature(byte charge)
		{
			IMSMSFeatureList = new List<IMSMSFeature>();
			Charge = charge;
			IMSScore = 0;
			LCScore = 0;
		}

		public void AddIMSMSFeature(IMSMSFeature imsmsFeature)
		{
			int lcScan = imsmsFeature.ScanLC;

			foreach (IMSMSFeature otherIMSMSFeature in IMSMSFeatureList)
			{
				if (otherIMSMSFeature.ScanLC == lcScan)
				{
					FeatureUtil.MergeIMSMSFeatures(imsmsFeature, otherIMSMSFeature);
					return;
				}
			}

			IMSMSFeatureList.Add(imsmsFeature);
		}

		public int GetMemberCount()
		{
			int count = 0;

			foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
			{
				count += imsmsFeature.MSFeatureList.Count;
			}

			return count;
		}

		public double CalculateAverageMass()
		{
			int totalMemberCount = 0;
			double massTotal = 0.0;

			foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
			{
				int memberCount = imsmsFeature.MSFeatureList.Count;
				massTotal += imsmsFeature.CalculateAverageMass() * memberCount;
				totalMemberCount += memberCount;
			}

			double averageMass = massTotal / totalMemberCount;

			return averageMass;
		}

		public double GetFlaggedPercentage()
		{
			int numFlagged = 0;
			int numTotal = 0;

			foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
			{
				foreach (MSFeature msFeature in imsmsFeature.MSFeatureList)
				{
					if (msFeature.ErrorFlag == 1)
					{
						numFlagged++;
					}

					numTotal++;
				}
			}

			double percentage = (double)numFlagged / (double)numTotal;
			return percentage;
		}

		public void GetMinAndMaxScanLC(out int scanLCMinimum, out int scanLCMaximum)
		{
			List<MSFeature> msFeatureList = new List<MSFeature>();

			foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
			{
				msFeatureList.AddRange(imsmsFeature.MSFeatureList);
			}

			var sortByScanLCQuery = from msFeature in msFeatureList
									orderby msFeature.ScanLC
									select msFeature;

			scanLCMinimum = sortByScanLCQuery.First().ScanIMS;
			scanLCMaximum = sortByScanLCQuery.Last().ScanIMS;
		}
	}
}
