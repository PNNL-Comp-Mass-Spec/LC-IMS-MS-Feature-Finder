using System;
using System.Collections.Generic;
using System.Linq;
using FeatureFinder.Data;
using FeatureFinder.Control;
using System.IO;
using System.Text.RegularExpressions;
using FeatureFinder.Data.Maps;

namespace FeatureFinder.Utilities
{
    public static class FeatureUtil
    {
        public static void WriteLCIMSMSFeatureToFile(IEnumerable<LCIMSMSFeature> lcImsMsFeatureEnumerable)
        {
            var baseFileName = Regex.Split(Settings.InputFileName, "_isos")[0];
            var outputDirectory = "";

            if (!string.IsNullOrWhiteSpace(Settings.OutputDirectory))
            {
                outputDirectory = Settings.OutputDirectory;
            }

            var featuresFilePath = Path.Combine(outputDirectory, baseFileName + "_LCMSFeatures.txt");
            var featureToPeakMapPath = Path.Combine(outputDirectory, baseFileName + "_LCMSFeatureToPeakMap.txt");

            using (var featureWriter = new StreamWriter(featuresFilePath))
            using (var mapWriter = new StreamWriter(featureToPeakMapPath))
            {

                var headerCols = new List<string>
                {
                    "Feature_Index",
                    "Original_Index",
                    "Monoisotopic_Mass",
                    "Average_Mono_Mass",
                    "UMC_MW_Min",
                    "UMC_MW_Max",
                    "Scan_Start",
                    "Scan_End",
                    "Scan",
                    "IMS_Scan",
                    "IMS_Scan_Start",
                    "IMS_Scan_End",
                    "Avg_Interference_Score",
                    "Decon2ls_Fit_Score",
                    "UMC_Member_Count",
                    "Saturated_Member_Count",
                    "Max_Abundance",
                    "Abundance",
                    "Class_Rep_MZ",
                    "Class_Rep_Charge",
                    "Charge_Max",
                    "Drift_Time",
                    "Conformation_Fit_Score",
                    "LC_Fit_Score",
                    "Average_Isotopic_Fit",
                    "Members_Percentage",
                    "Combined_Score"
                };

                featureWriter.WriteLine(string.Join("\t", headerCols));

                mapWriter.WriteLine("Feature_Index\tPeak_Index\tFiltered_Peak_Index");

                var index = 0;

                foreach (var lcimsmsFeature in lcImsMsFeatureEnumerable)
                {
                    MSFeature msFeatureRep = null;

                    var maxAbundance = int.MinValue;
                    var msFeatureCount = 0;
                    var saturatedMSFeatureCount = 0;
                    var repMinIMSScan = 0;
                    var repMaxIMSScan = 0;
                    long totalAbundance = 0;
                    var minMass = double.MaxValue;
                    var maxMass = double.MinValue;
                    double totalMass = 0;
                    double totalFit = 0;
                    double totalInterferenceScore = 0;
                    double totalAbundanceTimesDriftTime = 0;

                    var sortByScanLCQuery = (from imsMsFeature in lcimsmsFeature.imsMsFeatureList
                                             orderby imsMsFeature.ScanLC
                                             select imsMsFeature).ToList();

                    var scanLCStart = sortByScanLCQuery.First().ScanLC;
                    var scanLCEnd = sortByScanLCQuery.Last().ScanLC;

                    foreach (var imsMsFeature in sortByScanLCQuery)
                    {
                        var minIMSScan = int.MaxValue;
                        var maxIMSScan = int.MinValue;

                        var isFeatureRep = false;

                        foreach (var msFeature in imsMsFeature.MSFeatureList)
                        {
                            var filteredFeatureId = msFeature.FilteredIndex >= 0 ? msFeature.FilteredIndex.ToString() : "";
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
                            totalAbundanceTimesDriftTime += (double)msFeature.Abundance * msFeature.DriftTime;
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

                    var averageMass = totalMass / msFeatureCount;
                    var averageFit = 1.0 - ((totalFit / msFeatureCount) / Settings.FitMax);
                    var averageInterferenceScore = (totalInterferenceScore / msFeatureCount);
                    var averageDecon2lsFit = (totalFit / msFeatureCount);

                    if (float.IsInfinity(lcimsmsFeature.IMSScore) || float.IsNaN(lcimsmsFeature.IMSScore)) lcimsmsFeature.IMSScore = 0;
                    if (float.IsInfinity(lcimsmsFeature.LCScore) || float.IsNaN(lcimsmsFeature.LCScore)) lcimsmsFeature.IMSScore = 0;

                    var memberPercentage = msFeatureCount / (double)lcimsmsFeature.MaxMemberCount;
                    if (double.IsInfinity(memberPercentage) || double.IsNaN(memberPercentage)) memberPercentage = 0.0;

                    var combinedScore = (lcimsmsFeature.IMSScore + averageFit + memberPercentage) / 3.0;
                    if (double.IsInfinity(combinedScore) || double.IsNaN(combinedScore)) combinedScore = 0.0;

                    var driftTimeWeightedAverage = totalAbundanceTimesDriftTime / totalAbundance;

                    var outLine = new List<string>
                {
                    index.ToString(),
                    lcimsmsFeature.OriginalIndex.ToString(),
                    averageMass.ToString("0.0####"),
                    averageMass.ToString("0.0####"),
                    minMass.ToString("0.0####"),
                    maxMass.ToString("0.0####"),
                    ScanLCMap.Mapping[scanLCStart].ToString(),
                    ScanLCMap.Mapping[scanLCEnd].ToString()
                };

                    if (msFeatureRep != null)
                    {
                        outLine.Add(ScanLCMap.Mapping[msFeatureRep.ScanLC].ToString());
                        outLine.Add(msFeatureRep.ScanIMS.ToString());
                    }
                    else
                    {
                        outLine.Add("");
                        outLine.Add("");
                    }

                    outLine.Add(repMinIMSScan.ToString());
                    outLine.Add(repMaxIMSScan.ToString());
                    outLine.Add(PRISM.StringUtilities.ValueToString(averageInterferenceScore, 5));
                    outLine.Add(PRISM.StringUtilities.ValueToString(averageDecon2lsFit, 5));
                    outLine.Add(msFeatureCount.ToString());
                    outLine.Add(saturatedMSFeatureCount.ToString());
                    outLine.Add(maxAbundance.ToString());


                    if (Settings.UseConformationDetection)
                    {
                        outLine.Add(PRISM.StringUtilities.ValueToString(lcimsmsFeature.AbundanceSumRaw, 5));
                    }
                    else
                    {
                        outLine.Add(totalAbundance.ToString());
                    }

                    if (msFeatureRep != null)
                    {
                        outLine.Add(msFeatureRep.Mz.ToString("0.0####"));
                    }
                    else
                    {
                        outLine.Add("");
                    }

                    outLine.Add(lcimsmsFeature.Charge.ToString());      // ClassRepCharge
                    outLine.Add(lcimsmsFeature.Charge.ToString());      // ChargeMax

                    if (Settings.UseConformationDetection)
                    {
                        outLine.Add(PRISM.StringUtilities.ValueToString(lcimsmsFeature.DriftTime, 5));
                    }
                    else
                    {
                        outLine.Add(PRISM.StringUtilities.ValueToString(driftTimeWeightedAverage, 5));
                    }


                    outLine.Add(PRISM.StringUtilities.ValueToString(lcimsmsFeature.IMSScore, 5));   // Conformation_Fit_Score
                    outLine.Add(PRISM.StringUtilities.ValueToString(lcimsmsFeature.LCScore, 5));    // LC_Fit_Score
                    outLine.Add(PRISM.StringUtilities.ValueToString(averageFit, 5));                // Average_Isotopic_Fit
                    outLine.Add(PRISM.StringUtilities.ValueToString(memberPercentage, 5));          // Members_Percentage
                    outLine.Add(PRISM.StringUtilities.ValueToString(combinedScore, 5));             // Combined_Score

                    featureWriter.WriteLine(string.Join("\t", outLine));

                    index++;
                }

            }

        }

        [Obsolete("Unused")]
        public static IEnumerable<imsMsFeature> FilterByMemberCount(IEnumerable<imsMsFeature> imsMsFeatureEnumerable)
        {
            var filterQuery = from imsMsFeature in imsMsFeatureEnumerable
                              where imsMsFeature.MSFeatureList.Count >= Settings.FeatureLengthMin
                              select imsMsFeature;

            return filterQuery.AsEnumerable();
        }

        public static IEnumerable<LCIMSMSFeature> FilterByMemberCount(IEnumerable<LCIMSMSFeature> lcImsMsFeatureEnumerable)
        {
            var filterQuery = from lcimsmsFeature in lcImsMsFeatureEnumerable
                              where lcimsmsFeature.GetMemberCount() >= Settings.FeatureLengthMin
                              select lcimsmsFeature;

            return filterQuery.AsEnumerable();
        }

        public static IEnumerable<LCIMSMSFeature> FilterSingleLCScan(IEnumerable<LCIMSMSFeature> lcImsMsFeatureEnumerable)
        {
            var lcimsmsFeatureList = new List<LCIMSMSFeature>();

            foreach (var lcimsmsFeature in lcImsMsFeatureEnumerable)
            {
                var referenceScanLC = lcimsmsFeature.imsMsFeatureList[0].ScanLC;

                if (lcimsmsFeature.imsMsFeatureList.Any(imsMsFeature => imsMsFeature.ScanLC != referenceScanLC))
                {
                    lcimsmsFeatureList.Add(lcimsmsFeature);
                }
            }

            return lcimsmsFeatureList;
        }

        [Obsolete("Unused")]
        public static bool DoLcImsMsFeaturesFitTogether(LCIMSMSFeature feature1, LCIMSMSFeature feature2)
        {
            if (feature1.Charge != feature2.Charge)
            {
                return false;
            }

            var minLCScan1 = int.MaxValue;
            var minLCScan2 = int.MaxValue;
            var maxLCScan1 = int.MinValue;
            var maxLCScan2 = int.MaxValue;

            var lcScanToImsMsFeatureMap1 = new Dictionary<int, imsMsFeature>();

            foreach (var imsMsFeature1 in feature1.imsMsFeatureList)
            {
                var lcScan = imsMsFeature1.ScanLC;

                if (lcScan < minLCScan1) minLCScan1 = lcScan;
                if (lcScan > maxLCScan1) maxLCScan1 = lcScan;

                lcScanToImsMsFeatureMap1.Add(imsMsFeature1.ScanLC, imsMsFeature1);
            }

            foreach (var imsMsFeature2 in feature2.imsMsFeatureList)
            {
                var lcScan = imsMsFeature2.ScanLC;

                if (lcScan < minLCScan2) minLCScan2 = lcScan;
                if (lcScan > maxLCScan2) maxLCScan2 = lcScan;

                if (!lcScanToImsMsFeatureMap1.TryGetValue(lcScan, out var imsMsFeature1)) continue;
                if (DoImsMsFeaturesFitTogether(imsMsFeature1, imsMsFeature2)) continue;
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

        public static bool DoImsMsFeaturesFitTogether(imsMsFeature feature1, imsMsFeature feature2)
        {
            var imsScanList1 = feature1.MSFeatureList.Select(msFeature1 => msFeature1.ScanIMS).ToList();

            return feature2.MSFeatureList.All(msFeature2 => !imsScanList1.Contains(msFeature2.ScanIMS));
        }

        public static void MergeImsMsFeatures(imsMsFeature dominantFeature, imsMsFeature recessiveFeature)
        {
            dominantFeature.AddMSFeatureList(recessiveFeature.MSFeatureList);
            recessiveFeature.MSFeatureList.Clear();
            recessiveFeature.ScanLC = int.MinValue;
        }

        public static void MergeLCIMSMSFeatures(LCIMSMSFeature dominantFeature, LCIMSMSFeature recessiveFeature)
        {
            var referenceMass = dominantFeature.imsMsFeatureList[0].MSFeatureList[0].MassMonoisotopic;
            var massToChange = recessiveFeature.imsMsFeatureList[0].MSFeatureList[0].MassMonoisotopic;

            var massChange = (int)Math.Round(referenceMass - massToChange);

            var scanLcToImsMsFeatureMap = dominantFeature.imsMsFeatureList.ToDictionary(dominantImsMsFeature => dominantImsMsFeature.ScanLC);

            foreach (var recessiveImsMsFeature in recessiveFeature.imsMsFeatureList)
            {
                // First correct the Mass of the recessive imsMsFeature
                foreach (var msFeature in recessiveImsMsFeature.MSFeatureList)
                {
                    msFeature.MassMonoisotopic += massChange;

                    // TODO: Keep this??
                    //msFeature.Mz = (msFeature.MassMonoisotopic / msFeature.Charge) + (float)1.00727849;
                }

                if (scanLcToImsMsFeatureMap.TryGetValue(recessiveImsMsFeature.ScanLC, out var dominantImsMsFeature))
                {
                    MergeImsMsFeatures(dominantImsMsFeature, recessiveImsMsFeature);
                }
                else
                {
                    dominantFeature.AddImsMsFeature(recessiveImsMsFeature);
                }
            }

            recessiveFeature.imsMsFeatureList.Clear();
        }

        public static List<List<LCIMSMSFeature>> PartitionFeaturesByMass(IEnumerable<LCIMSMSFeature> lcImsMsFeatureEnumerable)
        {
            var returnList = new List<List<LCIMSMSFeature>>();

            var sortByMassQuery = from lcimsmsFeature in lcImsMsFeatureEnumerable
                                  orderby lcimsmsFeature.CalculateAverageMonoisotopicMass()
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

        public static IEnumerable<LCIMSMSFeature> SplitLCIMSMSFeaturesByScanLC(IEnumerable<LCIMSMSFeature> lcImsMsFeatureEnumerable)
        {
            var lcimsmsFeatureList = new List<LCIMSMSFeature>();

            var lcImsMsFeatureIndex = 0;

            foreach (var lcimsmsFeature in lcImsMsFeatureEnumerable)
            {
                var sortByScanLC = from imsMsFeature in lcimsmsFeature.imsMsFeatureList
                                   orderby imsMsFeature.ScanLC
                                   select imsMsFeature;

                LCIMSMSFeature newLcImsMsFeature = null;
                var referenceScanLC = -99999;

                foreach (var imsMsFeature in sortByScanLC)
                {
                    var scanLC = imsMsFeature.ScanLC;

                    if (scanLC - referenceScanLC > Settings.LCGapSizeMax)
                    {
                        newLcImsMsFeature = new LCIMSMSFeature(imsMsFeature.Charge);
                        newLcImsMsFeature.AddImsMsFeature(imsMsFeature);
                        newLcImsMsFeature.OriginalIndex = lcImsMsFeatureIndex;
                        lcImsMsFeatureIndex++;
                        lcimsmsFeatureList.Add(newLcImsMsFeature);
                    }
                    else
                    {
                        newLcImsMsFeature?.AddImsMsFeature(imsMsFeature);
                    }

                    referenceScanLC = scanLC;
                }
            }

            return lcimsmsFeatureList;
        }

        public static IEnumerable<LCIMSMSFeature> SortByMass(IEnumerable<LCIMSMSFeature> lcImsMsFeatureEnumerable)
        {
            var sortByMassQuery = from lcimsmsFeature in lcImsMsFeatureEnumerable
                                  orderby lcimsmsFeature.Charge, lcimsmsFeature.CalculateAverageMonoisotopicMass()
                                  select lcimsmsFeature;

            return sortByMassQuery.AsEnumerable();
        }
    }
}
