﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FeatureFinder.Data;
using FeatureFinder.Control;
using System.IO;
using System.Text.RegularExpressions;
using FeatureFinder.Data.Maps;

namespace FeatureFinder.Utilities
{
    public static class FeatureUtil
    {
        public static void WriteLCIMSMSFeatureToFile(IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable)
        {
            var baseFileName = Regex.Split(Settings.InputFileName, "_isos")[0];
            var outputDirectory = "";

            if (!Settings.OutputDirectory.Equals(String.Empty))
            {
                outputDirectory = Settings.OutputDirectory + "\\";
            }

            TextWriter featureWriter = new StreamWriter(outputDirectory + baseFileName + "_LCMSFeatures.txt");
            TextWriter mapWriter = new StreamWriter(outputDirectory + baseFileName + "_LCMSFeatureToPeakMap.txt");

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
            labelStringBuilder.Append("IMS_Scan" + "\t");
            labelStringBuilder.Append("IMS_Scan_Start" + "\t");
            labelStringBuilder.Append("IMS_Scan_End" + "\t");
            labelStringBuilder.Append("Avg_Interference_Score" + "\t");
            labelStringBuilder.Append("Decon2ls_Fit_Score" + "\t");
            labelStringBuilder.Append("UMC_Member_Count" + "\t");
            labelStringBuilder.Append("Saturated_Member_Count" + "\t");
            labelStringBuilder.Append("Max_Abundance" + "\t");
            labelStringBuilder.Append("Abundance" + "\t");
            labelStringBuilder.Append("Class_Rep_MZ" + "\t");
            labelStringBuilder.Append("Class_Rep_Charge" + "\t");
            labelStringBuilder.Append("Charge_Max" + "\t");
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
                MSFeature msFeatureRep = null;

                int maxAbundance = int.MinValue;
                int msFeatureCount = 0;
                int saturatedMSFeatureCount = 0;
                int repMinIMSScan = 0;
                int repMaxIMSScan = 0;
                long totalAbundance = 0;
                double minMass = double.MaxValue;
                double maxMass = double.MinValue;
                double totalMass = 0;
                double totalFit = 0;
                double totalInterferenceScore = 0;
                double totalAbundanceTimesDriftTime = 0;

                var sortByScanLCQuery = from imsmsFeature in lcimsmsFeature.IMSMSFeatureList
                                        orderby imsmsFeature.ScanLC ascending
                                        select imsmsFeature;

                int scanLCStart = sortByScanLCQuery.First().ScanLC;
                int scanLCEnd = sortByScanLCQuery.Last().ScanLC;

                foreach (IMSMSFeature imsmsFeature in sortByScanLCQuery)
                {
                    int minIMSScan = int.MaxValue;
                    int maxIMSScan = int.MinValue;

                    bool isFeatureRep = false;

                    foreach (MSFeature msFeature in imsmsFeature.MSFeatureList)
                    {
                        String filteredFeatureId = msFeature.FilteredIndex >= 0 ? msFeature.FilteredIndex.ToString() : "";
                        mapWriter.WriteLine(index + "\t" + msFeature.IndexInFile + "\t" + filteredFeatureId);

                        if (msFeature.Abundance > maxAbundance)
                        {
                            msFeatureRep = msFeature;
                            maxAbundance = msFeature.Abundance;
                            isFeatureRep = true;
                        }

                        if (msFeature.MassMonoisotopic < minMass) minMass = msFeature.MassMonoisotopic;
                        if (msFeature.MassMonoisotopic > maxMass) maxMass = msFeature.MassMonoisotopic;

                        if (msFeature.ScanIMS < minIMSScan) minIMSScan = msFeature.ScanIMS;
                        if (msFeature.ScanIMS > maxIMSScan) maxIMSScan = msFeature.ScanIMS;

                        if (msFeature.IsSaturated) saturatedMSFeatureCount++;

                        totalAbundance += msFeature.Abundance;
                        totalAbundanceTimesDriftTime += ((double)msFeature.Abundance * msFeature.DriftTime);
                        totalMass += msFeature.MassMonoisotopic;
                        totalFit += msFeature.Fit;
                        totalInterferenceScore += msFeature.InterferenceScore;
                        msFeatureCount++;
                    }

                    if (isFeatureRep)
                    {
                        repMinIMSScan = minIMSScan;
                        repMaxIMSScan = maxIMSScan;
                    }
                }

                double averageMass = totalMass / msFeatureCount;
                double averageFit = 1.0 - ((totalFit / msFeatureCount) / Settings.FitMax);
                double averageInterferenceScore = (totalInterferenceScore / msFeatureCount);
                double averageDecon2lsFit = (totalFit / msFeatureCount);

                if (float.IsInfinity(lcimsmsFeature.IMSScore) || float.IsNaN(lcimsmsFeature.IMSScore)) lcimsmsFeature.IMSScore = 0;
                if (float.IsInfinity(lcimsmsFeature.LCScore) || float.IsNaN(lcimsmsFeature.LCScore)) lcimsmsFeature.IMSScore = 0;

                double memberPercentage = (double)msFeatureCount / (double)lcimsmsFeature.MaxMemberCount;
                if (double.IsInfinity(memberPercentage) || double.IsNaN(memberPercentage)) memberPercentage = 0.0;

                double combinedScore = (lcimsmsFeature.IMSScore + averageFit + memberPercentage) / 3.0;
                if (double.IsInfinity(combinedScore) || double.IsNaN(combinedScore)) combinedScore = 0.0;

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
                stringBuilder.Append(msFeatureRep.ScanIMS + "\t");
                stringBuilder.Append(repMinIMSScan + "\t");
                stringBuilder.Append(repMaxIMSScan + "\t");
                stringBuilder.Append(averageInterferenceScore.ToString("0.00000") + "\t");
                stringBuilder.Append(averageDecon2lsFit.ToString("0.00000") + "\t");
                stringBuilder.Append(msFeatureCount + "\t");
                stringBuilder.Append(saturatedMSFeatureCount + "\t");
                stringBuilder.Append(maxAbundance + "\t");
                if (Settings.UseConformationDetection)
                {
                    stringBuilder.Append(lcimsmsFeature.AbundanceSumRaw + "\t");
                }
                else
                {
                    stringBuilder.Append(totalAbundance + "\t");
                }
                stringBuilder.Append(msFeatureRep.Mz + "\t");
                stringBuilder.Append(lcimsmsFeature.Charge + "\t");
                stringBuilder.Append(lcimsmsFeature.Charge + "\t");
                if (Settings.UseConformationDetection)
                {
                    stringBuilder.Append(lcimsmsFeature.DriftTime.ToString("0.00000") + "\t");
                }
                else
                {
                    stringBuilder.Append(driftTimeWeightedAverage.ToString("0.00000") + "\t");
                }
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
            var lcimsmsFeatureList = new List<LCIMSMSFeature>();

            foreach (var lcimsmsFeature in lcimsmsFeatureEnumerable)
            {
                var referenceScanLC = lcimsmsFeature.IMSMSFeatureList[0].ScanLC;

                if (lcimsmsFeature.IMSMSFeatureList.Any(imsmsFeature => imsmsFeature.ScanLC != referenceScanLC))
                {
                    lcimsmsFeatureList.Add(lcimsmsFeature);
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

            var minLCScan1 = int.MaxValue;
            var minLCScan2 = int.MaxValue;
            var maxLCScan1 = int.MinValue;
            var maxLCScan2 = int.MaxValue;

            var lcScanToIMSMSFeatureMap1 = new Dictionary<int, IMSMSFeature>();

            foreach (var imsmsFeature1 in feature1.IMSMSFeatureList)
            {
                var lcScan = imsmsFeature1.ScanLC;

                if (lcScan < minLCScan1) minLCScan1 = lcScan;
                if (lcScan > maxLCScan1) maxLCScan1 = lcScan;

                lcScanToIMSMSFeatureMap1.Add(imsmsFeature1.ScanLC, imsmsFeature1);
            }

            foreach (var imsmsFeature2 in feature2.IMSMSFeatureList)
            {
                var lcScan = imsmsFeature2.ScanLC;

                if (lcScan < minLCScan2) minLCScan2 = lcScan;
                if (lcScan > maxLCScan2) maxLCScan2 = lcScan;

                if (!lcScanToIMSMSFeatureMap1.TryGetValue(lcScan, out var imsmsFeature1)) continue;
                if (DoIMSMSFeaturesFitTogether(imsmsFeature1, imsmsFeature2)) continue;
                return false;
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
            var imsScanList1 = feature1.MSFeatureList.Select(msFeature1 => msFeature1.ScanIMS).ToList();

            return feature2.MSFeatureList.All(msFeature2 => !imsScanList1.Contains(msFeature2.ScanIMS));
        }

        public static void MergeIMSMSFeatures(IMSMSFeature dominantFeature, IMSMSFeature recessiveFeature)
        {
            dominantFeature.AddMSFeatureList(recessiveFeature.MSFeatureList);
            recessiveFeature.MSFeatureList.Clear();
            recessiveFeature.ScanLC = int.MinValue;
        }

        public static void MergeLCIMSMSFeatures(LCIMSMSFeature dominantFeature, LCIMSMSFeature recessiveFeature)
        {
            var referenceMass = dominantFeature.IMSMSFeatureList[0].MSFeatureList[0].MassMonoisotopic;
            var massToChange = recessiveFeature.IMSMSFeatureList[0].MSFeatureList[0].MassMonoisotopic;

            var massChange = (int)Math.Round(referenceMass - massToChange);

            var scanLCToIMSMSFeatureMap = dominantFeature.IMSMSFeatureList.ToDictionary(dominantIMSMSFeature => dominantIMSMSFeature.ScanLC);

            foreach (var recessiveIMSMSFeature in recessiveFeature.IMSMSFeatureList)
            {
                // First correct the Mass of the recessive IMSMSFeature
                foreach (var msFeature in recessiveIMSMSFeature.MSFeatureList)
                {
                    msFeature.MassMonoisotopic += massChange;

                    // TODO: Keep this??
                    //msFeature.Mz = (msFeature.MassMonoisotopic / msFeature.Charge) + (float)1.00727849;
                }

                if (scanLCToIMSMSFeatureMap.TryGetValue(recessiveIMSMSFeature.ScanLC, out var dominantIMSMSFeature))
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
            var returnList = new List<List<LCIMSMSFeature>>();

            var sortByMassQuery = from lcimsmsFeature in lcimsmsFeatureEnumerable
                                  orderby lcimsmsFeature.CalculateAverageMonoisotopicMass() ascending
                                  select lcimsmsFeature;

            var lcimsmsFeatureList = new List<LCIMSMSFeature>();
            var referenceMass = double.MinValue;

            foreach (var lcimsmsFeature in sortByMassQuery)
            {
                var currentMass = lcimsmsFeature.CalculateAverageMonoisotopicMass();

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
            var lcimsmsFeatureList = new List<LCIMSMSFeature>();

            var lcimcmsFeatureIndex = 0;

            foreach (var lcimsmsFeature in lcimsmsFeatureEnumerable)
            {
                var sortByScanLC = from imsmsFeature in lcimsmsFeature.IMSMSFeatureList
                                   orderby imsmsFeature.ScanLC
                                   select imsmsFeature;

                LCIMSMSFeature newLCIMSMSFeature = null;
                var referenceScanLC = -99999;

                foreach (var imsmsFeature in sortByScanLC)
                {
                    var scanLC = imsmsFeature.ScanLC;

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
                        newLCIMSMSFeature?.AddIMSMSFeature(imsmsFeature);
                    }

                    referenceScanLC = scanLC;
                }
            }

            return lcimsmsFeatureList;
        }

        public static IEnumerable<LCIMSMSFeature> SortByMass(IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable)
        {
            var sortByMassQuery = from lcimsmsFeature in lcimsmsFeatureEnumerable
                                  orderby lcimsmsFeature.Charge ascending, lcimsmsFeature.CalculateAverageMonoisotopicMass() ascending
                                  select lcimsmsFeature;

            return sortByMassQuery.AsEnumerable();
        }
    }
}
