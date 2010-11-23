using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureFinder.Data;
using FeatureFinder.Control;
using System.IO;
using System.Text.RegularExpressions;
using FeatureFinder.Data.Maps;
using UIMFLibrary;
using FeatureFinder.Algorithms;

namespace FeatureFinder.Utilities
{
	public static class FeatureUtil
	{
		public static void WriteLCIMSMSFeatureToFile(IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable)
		{
			String baseFileName = Regex.Split(Settings.InputFileName, "_isos")[0];
			String outputDirectory = "";

			if (!Settings.OutputDirectory.Equals(String.Empty))
			{
				outputDirectory = Settings.OutputDirectory + "\\";
			}

			TextWriter featureWriter = new StreamWriter(outputDirectory + baseFileName + "_LCMSFeatures.txt");
			TextWriter mapWriter = new StreamWriter(outputDirectory + baseFileName + "_LCMSFeatureToPeakMap.txt");

			DataReader uimfReader = new UIMFLibrary.DataReader();
			if (!uimfReader.OpenUIMF(Settings.InputDirectory + Settings.InputFileName.Replace("_isos.csv", ".uimf")))
			{
				throw new FileNotFoundException("Could not find file '" + Settings.InputDirectory + Settings.InputFileName.Replace("_isos.csv", ".uimf") + "'.");
			}

			StringBuilder labelStringBuilder = new StringBuilder();
			labelStringBuilder.Append("Feature_Index" + "\t");
			labelStringBuilder.Append("Original_Index" + "\t");
			labelStringBuilder.Append("Monoisotopic_Mass" + "\t");
			labelStringBuilder.Append("Average_Mono_Mass" + "\t");
			labelStringBuilder.Append("UMC_MW_Min" + "\t");
			labelStringBuilder.Append("UMC_MW_Max" + "\t");
			labelStringBuilder.Append("Scan_Start" + "\t");
			labelStringBuilder.Append("Scan_End" + "\t");
			labelStringBuilder.Append("Scan" + "\t");
			labelStringBuilder.Append("UMC_Member_Count" + "\t");
			labelStringBuilder.Append("Max_Abundance" + "\t");
			labelStringBuilder.Append("Abundance" + "\t");
			labelStringBuilder.Append("Class_Rep_MZ" + "\t");
			labelStringBuilder.Append("Class_Rep_Charge" + "\t");
			labelStringBuilder.Append("Charge_Max" + "\t");
			labelStringBuilder.Append("Old_Drift_Time" + "\t");
			labelStringBuilder.Append("Drift_Time" + "\t");
			labelStringBuilder.Append("Conformation_Fit_Score" + "\t");
			labelStringBuilder.Append("LC_Fit_Score" + "\t");
			labelStringBuilder.Append("Average_Isotopic_Fit" + "\t");
			labelStringBuilder.Append("Members_Percentage" + "\t");
			labelStringBuilder.Append("Combined_Score");

			featureWriter.WriteLine(labelStringBuilder.ToString());

			mapWriter.WriteLine("Feature_Index\tPeak_Index\tFiltered_Peak_Index");

			int index = 0;

			foreach (LCIMSMSFeature lcimsmsFeature in lcimsmsFeatureEnumerable)
			{
				IMSMSFeature imsmsFeatureRep = null;
				MSFeature msFeatureRep = null;

				int maxAbundance = int.MinValue;
				int msFeatureCount = 0;
				long totalAbundance = 0;
				float minMass = float.MaxValue;
				float maxMass = float.MinValue;
				double totalMass = 0;
				double totalFit = 0;
				double totalAbundanceTimesDriftTime = 0;

				var sortByScanLCQuery = from imsmsFeature in lcimsmsFeature.IMSMSFeatureList
										orderby imsmsFeature.ScanLC ascending
										select imsmsFeature;

				int scanLCStart = sortByScanLCQuery.First().ScanLC;
				int scanLCEnd = sortByScanLCQuery.Last().ScanLC;

				// TODO: Use a moving average frame pressure instead
				// First find the average Pressure for the Feature to use for Drift Time calculation
				double totalFramePressure = 0;
				foreach (IMSMSFeature imsmsFeature in sortByScanLCQuery)
				{
					int scanLC = ScanLCMap.Mapping[imsmsFeature.ScanLC];
					FrameParameters frameParameters = uimfReader.GetFrameParameters(scanLC);

					double framePressure = frameParameters.PressureBack;

					totalFramePressure += framePressure;
				}
				double averageFramePressure = totalFramePressure / (double)sortByScanLCQuery.Count();

				foreach (IMSMSFeature imsmsFeature in sortByScanLCQuery)
				{
					int scanLC = ScanLCMap.Mapping[imsmsFeature.ScanLC];
					FrameParameters frameParameters = uimfReader.GetFrameParameters(scanLC);

					double averageTOFLength = frameParameters.AverageTOFLength;

					foreach (MSFeature msFeature in imsmsFeature.MSFeatureList)
					{
						String filteredFeatureId = msFeature.FilteredIndex >= 0 ? msFeature.FilteredIndex.ToString() : "";
						mapWriter.WriteLine(index + "\t" + msFeature.IndexInFile + "\t" + filteredFeatureId);

						if (msFeature.Abundance > maxAbundance)
						{
							imsmsFeatureRep = imsmsFeature;
							msFeatureRep = msFeature;
							maxAbundance = msFeature.Abundance;
						}

						if (msFeature.MassMonoisotopic < minMass) minMass = msFeature.MassMonoisotopic;
						if (msFeature.MassMonoisotopic > maxMass) maxMass = msFeature.MassMonoisotopic;

						double driftTime = ConformationDetection.ConvertIMSScanToDriftTime((int)msFeature.ScanIMS, averageTOFLength, averageFramePressure);

						totalAbundance += msFeature.Abundance;
						totalAbundanceTimesDriftTime += ((double)msFeature.Abundance * (double)driftTime);
						totalMass += msFeature.MassMonoisotopic;
						totalFit += msFeature.Fit;
						msFeatureCount++;
					}
				}

				double averageMass = totalMass / msFeatureCount;
				double averageFit = 1.0 - ((totalFit / msFeatureCount) / Settings.FitMax);
				double memberPercentage = (double)msFeatureCount / (double)lcimsmsFeature.MaxMemberCount;
				double combinedScore = (lcimsmsFeature.IMSScore + averageFit + memberPercentage) / 3.0;
				double driftTimeWeightedAverage = totalAbundanceTimesDriftTime / (double)totalAbundance;

				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(index + "\t");
				stringBuilder.Append(lcimsmsFeature.OriginalIndex + "\t");
				stringBuilder.Append(averageMass.ToString("0.00000") + "\t");
				stringBuilder.Append(averageMass.ToString("0.00000") + "\t");
				stringBuilder.Append(minMass.ToString("0.00000") + "\t");
				stringBuilder.Append(maxMass.ToString("0.00000") + "\t");
				stringBuilder.Append(ScanLCMap.Mapping[scanLCStart] + "\t");
				stringBuilder.Append(ScanLCMap.Mapping[scanLCEnd] + "\t");
				stringBuilder.Append(ScanLCMap.Mapping[msFeatureRep.ScanLC] + "\t");
				stringBuilder.Append(msFeatureCount + "\t");
				stringBuilder.Append(maxAbundance + "\t");
				stringBuilder.Append(totalAbundance + "\t");
				stringBuilder.Append(msFeatureRep.Mz + "\t");
				stringBuilder.Append(lcimsmsFeature.Charge + "\t");
				stringBuilder.Append(lcimsmsFeature.Charge + "\t");
				stringBuilder.Append(msFeatureRep.DriftTime + "\t");
				stringBuilder.Append(driftTimeWeightedAverage.ToString("0.00000") + "\t");
				stringBuilder.Append(lcimsmsFeature.IMSScore.ToString("0.00000") + "\t");
				stringBuilder.Append(lcimsmsFeature.LCScore.ToString("0.00000") + "\t");
				stringBuilder.Append(averageFit.ToString("0.00000") + "\t");
				stringBuilder.Append(memberPercentage.ToString("0.00000") + "\t"); // Mem Percent
				stringBuilder.Append(combinedScore.ToString("0.00000")); // Combined

				featureWriter.WriteLine(stringBuilder.ToString());

				index++;
			}

			featureWriter.Close();
			mapWriter.Close();
		}

		public static IEnumerable<IMSMSFeature> FilterByMemberCount(IEnumerable<IMSMSFeature> imsmsFeatureEnumerable)
		{
			var filterQuery = from imsmsFeature in imsmsFeatureEnumerable
							  where imsmsFeature.MSFeatureList.Count > 3
							  select imsmsFeature;

			return filterQuery.AsEnumerable();
		}

		public static IEnumerable<LCIMSMSFeature> FilterByMemberCount(IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable)
		{
			var filterQuery = from lcimsmsFeature in lcimsmsFeatureEnumerable
							  where lcimsmsFeature.GetMemberCount() > 3
							  select lcimsmsFeature;

			return filterQuery.AsEnumerable();
		}

		public static IEnumerable<LCIMSMSFeature> FilterSingleLCScan(IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable)
		{
			List<LCIMSMSFeature> lcimsmsFeatureList = new List<LCIMSMSFeature>();

			foreach (LCIMSMSFeature lcimsmsFeature in lcimsmsFeatureEnumerable)
			{
				int referenceScanLC = lcimsmsFeature.IMSMSFeatureList[0].ScanLC;

				foreach (IMSMSFeature imsmsFeature in lcimsmsFeature.IMSMSFeatureList)
				{
					if (imsmsFeature.ScanLC != referenceScanLC)
					{
						lcimsmsFeatureList.Add(lcimsmsFeature);
						break;
					}
				}
			}

			return lcimsmsFeatureList;
		}

		public static bool DoLCIMSMSFeaturesFitTogether(LCIMSMSFeature feature1, LCIMSMSFeature feature2)
		{
			if (feature1.Charge != feature2.Charge)
			{
				return false;
			}

			int minLCScan1 = int.MaxValue;
			int minLCScan2 = int.MaxValue;
			int maxLCScan1 = int.MinValue;
			int maxLCScan2 = int.MaxValue;

			Dictionary<int, IMSMSFeature> lcScanToIMSMSFeatureMap1 = new Dictionary<int, IMSMSFeature>();

			foreach (IMSMSFeature imsmsFeature1 in feature1.IMSMSFeatureList)
			{
				int lcScan = imsmsFeature1.ScanLC;

				if (lcScan < minLCScan1) minLCScan1 = lcScan;
				if (lcScan > maxLCScan1) maxLCScan1 = lcScan;

				lcScanToIMSMSFeatureMap1.Add(imsmsFeature1.ScanLC, imsmsFeature1);
			}

			foreach (IMSMSFeature imsmsFeature2 in feature2.IMSMSFeatureList)
			{
				int lcScan = imsmsFeature2.ScanLC;

				if (lcScan < minLCScan2) minLCScan2 = lcScan;
				if (lcScan > maxLCScan2) maxLCScan2 = lcScan;

				IMSMSFeature imsmsFeature1 = null;

				if (lcScanToIMSMSFeatureMap1.TryGetValue(lcScan, out imsmsFeature1))
				{
					if (!DoIMSMSFeaturesFitTogether(imsmsFeature1, imsmsFeature2))
					{
						return false;
					}
				}
			}

			if (minLCScan1 - maxLCScan2 - 1 > Settings.LCGapSizeMax)
			{
				return false;
			}

			if (minLCScan2 - maxLCScan1 - 1 > Settings.LCGapSizeMax)
			{
				return false;
			}

			return true;
		}

		public static bool DoIMSMSFeaturesFitTogether(IMSMSFeature feature1, IMSMSFeature feature2)
		{
			List<int> imsScanList1 = new List<int>();

			foreach (MSFeature msFeature1 in feature1.MSFeatureList)
			{
				imsScanList1.Add(msFeature1.ScanIMS);
			}

			foreach (MSFeature msFeature2 in feature2.MSFeatureList)
			{
				if (imsScanList1.Contains(msFeature2.ScanIMS))
				{
					return false;
				}
			}

			return true;
		}

		public static void MergeIMSMSFeatures(IMSMSFeature dominantFeature, IMSMSFeature recessiveFeature)
		{
			dominantFeature.AddMSFeatureList(recessiveFeature.MSFeatureList);
			recessiveFeature.MSFeatureList.Clear();
			recessiveFeature.ScanLC = int.MinValue;
		}

		public static void MergeLCIMSMSFeatures(LCIMSMSFeature dominantFeature, LCIMSMSFeature recessiveFeature)
		{
			double referenceMass = dominantFeature.IMSMSFeatureList[0].MSFeatureList[0].MassMonoisotopic;
			double massToChange = recessiveFeature.IMSMSFeatureList[0].MSFeatureList[0].MassMonoisotopic;

			int massChange = (int)Math.Round(referenceMass - massToChange);

			Dictionary<int, IMSMSFeature> scanLCToIMSMSFeatureMap = new Dictionary<int, IMSMSFeature>();

			foreach (IMSMSFeature dominantIMSMSFeature in dominantFeature.IMSMSFeatureList)
			{
				scanLCToIMSMSFeatureMap.Add(dominantIMSMSFeature.ScanLC, dominantIMSMSFeature);
			}

			foreach (IMSMSFeature recessiveIMSMSFeature in recessiveFeature.IMSMSFeatureList)
			{
				// First correct the Mass of the recessive IMSMSFeature
				foreach (MSFeature msFeature in recessiveIMSMSFeature.MSFeatureList)
				{
					msFeature.MassMonoisotopic += massChange;

					// TODO: Keep this??
					//msFeature.Mz = (msFeature.MassMonoisotopic / msFeature.Charge) + (float)1.00727849;
				}

				IMSMSFeature dominantIMSMSFeature = null;

				if (scanLCToIMSMSFeatureMap.TryGetValue(recessiveIMSMSFeature.ScanLC, out dominantIMSMSFeature))
				{
					MergeIMSMSFeatures(dominantIMSMSFeature, recessiveIMSMSFeature);
				}
				else
				{
					dominantFeature.AddIMSMSFeature(recessiveIMSMSFeature);
				}
			}

			recessiveFeature.IMSMSFeatureList.Clear();
		}

		public static List<List<LCIMSMSFeature>> PartitionFeaturesByMass(IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable)
		{
			List<List<LCIMSMSFeature>> returnList = new List<List<LCIMSMSFeature>>();

			var sortByMassQuery = from lcimsmsFeature in lcimsmsFeatureEnumerable
								  orderby lcimsmsFeature.CalculateAverageMass() ascending
								  select lcimsmsFeature;

			List<LCIMSMSFeature> lcimsmsFeatureList = new List<LCIMSMSFeature>();
			double referenceMass = double.MinValue;

			foreach (LCIMSMSFeature lcimsmsFeature in sortByMassQuery)
			{
				double currentMass = lcimsmsFeature.CalculateAverageMass();

				if (currentMass > referenceMass + Settings.IMSDaCorrectionMax + 0.25)
				{
					lcimsmsFeatureList = new List<LCIMSMSFeature>();
					returnList.Add(lcimsmsFeatureList);
				}

				lcimsmsFeatureList.Add(lcimsmsFeature);
				referenceMass = currentMass;
			}

			return returnList;
		}

		public static IEnumerable<LCIMSMSFeature> SplitLCIMSMSFeaturesByScanLC(IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable)
		{
			List<LCIMSMSFeature> lcimsmsFeatureList = new List<LCIMSMSFeature>();

			int lcimcmsFeatureIndex = 0;

			foreach (LCIMSMSFeature lcimsmsFeature in lcimsmsFeatureEnumerable)
			{
				var sortByScanLC = from imsmsFeature in lcimsmsFeature.IMSMSFeatureList
								   orderby imsmsFeature.ScanLC
								   select imsmsFeature;

				LCIMSMSFeature newLCIMSMSFeature = null;
				int referenceScanLC = -99;

				foreach (IMSMSFeature imsmsFeature in sortByScanLC)
				{
					int scanLC = imsmsFeature.ScanLC;

					if (scanLC - referenceScanLC > Settings.LCGapSizeMax)
					{
						newLCIMSMSFeature = new LCIMSMSFeature(imsmsFeature.Charge);
						newLCIMSMSFeature.AddIMSMSFeature(imsmsFeature);
						newLCIMSMSFeature.OriginalIndex = lcimcmsFeatureIndex;
						lcimcmsFeatureIndex++;
						lcimsmsFeatureList.Add(newLCIMSMSFeature);
					}
					else
					{
						newLCIMSMSFeature.AddIMSMSFeature(imsmsFeature);
					}

					referenceScanLC = scanLC;
				}
			}

			return lcimsmsFeatureList;
		}
	}
}
