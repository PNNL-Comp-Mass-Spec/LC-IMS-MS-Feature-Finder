using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureFinder.Utilities;
using UIMFLibrary;
using FeatureFinder.Data.Maps;
using FeatureFinder.Algorithms;

namespace FeatureFinder.Data
{
	public class LCIMSMSFeature
	{
		public byte Charge { get; set; }

		public List<IMSMSFeature> IMSMSFeatureList { get; set; }

		public float IMSScore { get; set; }
		public float LCScore { get; set; }
		public int OriginalIndex { get; set; }
		public int MaxMemberCount { get; set; }
		public int AbundanceMaxRaw { get; set; }
		public int AbundanceSumRaw { get; set; }

		public LCIMSMSFeature(byte charge)
		{
			IMSMSFeatureList = new List<IMSMSFeature>();
			Charge = charge;
			IMSScore = 0;
			LCScore = 0;
			MaxMemberCount = 0;
			AbundanceMaxRaw = 0;
			AbundanceSumRaw = 0;
		}

		public void AddIMSMSFeature(IMSMSFeature imsmsFeature)
		{
			int lcScan = imsmsFeature.ScanLC;

			foreach (IMSMSFeature otherIMSMSFeature in IMSMSFeatureList)
			{
				if (otherIMSMSFeature.ScanLC == lcScan)
				{
					FeatureUtil.MergeIMSMSFeatures(imsmsFeature, otherIMSMSFeature);
					IMSMSFeatureList.Add(imsmsFeature);
					IMSMSFeatureList.Remove(otherIMSMSFeature);
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

			scanLCMinimum = sortByScanLCQuery.First().ScanLC;
			scanLCMaximum = sortByScanLCQuery.Last().ScanLC;
		}

		public void GetMinAndMaxScanLCAndScanIMSAndMSFeatureRep(out int scanLCMinimum, out int scanLCMaximum, out int scanIMSMinimum, out int scanIMSMaximum, out MSFeature msFeatureRep)
		{
			List<MSFeature> msFeatureList = new List<MSFeature>();

			foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
			{
				msFeatureList.AddRange(imsmsFeature.MSFeatureList);
			}

			var sortByScanLCQuery = from msFeature in msFeatureList
									orderby msFeature.ScanLC ascending
									select msFeature;

			scanLCMinimum = sortByScanLCQuery.First().ScanLC;
			scanLCMaximum = sortByScanLCQuery.Last().ScanLC;

			var sortByScanIMSQuery = from msFeature in msFeatureList
									 orderby msFeature.ScanIMS ascending
									 select msFeature;

			scanIMSMinimum = sortByScanIMSQuery.First().ScanIMS;
			scanIMSMaximum = sortByScanIMSQuery.Last().ScanIMS;

			var sortByAbundanceQuery = from msFeature in msFeatureList
									   orderby msFeature.Abundance descending
									   select msFeature;

			msFeatureRep = sortByAbundanceQuery.First();
		}

		public MSFeature GetMSFeatureRep()
		{
			List<MSFeature> msFeatureList = new List<MSFeature>();

			foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
			{
				msFeatureList.AddRange(imsmsFeature.MSFeatureList);
			}

			var sortByAbundanceQuery = from msFeature in msFeatureList
									   orderby msFeature.Abundance descending
									   select msFeature;

			MSFeature msFeatureRep = sortByAbundanceQuery.First();
			return msFeatureRep;
		}

		public List<XYPair> GetIMSScanProfileFromRawData(DataReader uimfReader, int frameType, double binWidth, double calibrationSlope, double calibrationIntercept)
		{
			int scanLCMinimum = 0;
			int scanLCMaximum = 0;
			int scanIMSMinimum = 0;
			int scanIMSMaximum = 0;

			MSFeature msFeatureRep = null;

			GetMinAndMaxScanLCAndScanIMSAndMSFeatureRep(out scanLCMinimum, out scanLCMaximum, out scanIMSMinimum, out scanIMSMaximum, out msFeatureRep);

			double currentFWHM = msFeatureRep.Fwhm;
			double currentMonoMZ = msFeatureRep.MassMonoisotopic / msFeatureRep.Charge + 1.00727649;

			List<double> startMZ = new List<double>();
			List<double> endMZ = new List<double>();

			// Set ranges over which to look for the original data in the UIMF.
			double charge = Convert.ToDouble(this.Charge);
			for (int i = 0; i < 3; i++)
			{
				startMZ.Add(currentMonoMZ + (1.003 * i / charge) - (0.5 * currentFWHM));
				endMZ.Add(currentMonoMZ + (1.003 * i / charge) + (0.5 * currentFWHM));
			}

			double minMZ = startMZ[0];
			double maxMZ = endMZ[endMZ.Count - 1];



            //int globalStartBin = (int)((Math.Sqrt(minMZ) / calibrationSlope + calibrationIntercept) * 1000 / binWidth);
            //int globalEndBin = (int)Math.Ceiling(((Math.Sqrt(maxMZ) / calibrationSlope + calibrationIntercept) * 1000 / binWidth));


            List<XYPair> imsScanProfile = new List<XYPair>();

            //[gord] added May 11 2011
            int frameIndexMinimum = uimfReader.get_FrameIndex(ScanLCMap.Mapping[scanLCMinimum]);
            int frameIndexMaximum = uimfReader.get_FrameIndex(ScanLCMap.Mapping[scanLCMaximum]);
            int[] scanValues = null;
            int[] intensityVals = null;


            double midPointMZ = (maxMZ + minMZ) / 2;
            double toleranceInMZ = midPointMZ - minMZ;

            uimfReader.GetDriftTimeProfile(frameIndexMinimum, frameIndexMaximum, frameType, scanIMSMinimum, scanIMSMaximum, midPointMZ, toleranceInMZ, ref scanValues, ref intensityVals);

            for (int i = 0; i < intensityVals.Length; i++)
            {
                XYPair xyPair = new XYPair(scanIMSMinimum + i, intensityVals[i]);

                imsScanProfile.Add(xyPair);

            }



            //int[][] intensityValues = uimfReader.GetIntensityBlock(ScanLCMap.Mapping[scanLCMinimum], ScanLCMap.Mapping[scanLCMaximum], frameType, scanIMSMinimum, scanIMSMaximum, globalStartBin, globalEndBin);


            //for (int i = 0; i < intensityValues.Length; i++)
            //{
            //    int[] intensityArray = intensityValues[i];
            //    int intensitySum = 0;

            //    foreach (int intensity in intensityArray)
            //    {
            //        intensitySum += intensity;
            //    }

            //    XYPair xyPair = new XYPair(scanIMSMinimum + i, intensitySum);
            //    imsScanProfile.Add(xyPair);
            //}

			// Add "0" intensity values to the left and right of the Peak
			imsScanProfile = ConformationDetection.PadXYPairsWithZeros(imsScanProfile, 5);

			return imsScanProfile;
		}

		public void PrintLCAndDriftTimeMap()
		{
			List<MSFeature> msFeatureList = new List<MSFeature>();

			foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
			{
				msFeatureList.AddRange(imsmsFeature.MSFeatureList);
			}

			var groupByScanLCQuery = from msFeature in msFeatureList
									 group msFeature by new { msFeature.ScanLC } into newGroup
									 select newGroup;

			foreach (IEnumerable<MSFeature> msFeatureGroup in groupByScanLCQuery)
			{
				Console.Write("LC Scan = " + msFeatureGroup.First().ScanLC + ": ");

				var orderByDriftTimeQuery = from msFeature in msFeatureGroup
											orderby msFeature.ScanLC, msFeature.DriftTime ascending
											select msFeature;

				foreach (MSFeature msFeature in orderByDriftTimeQuery)
				{
					Console.Write(msFeature.DriftTime + ",");
				}

				Console.Write("\n");
			}
		}
	}
}
