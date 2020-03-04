using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PRISM;
using UIMFLibrary;

namespace FeatureFinder.Control
{
    public class IniReader
    {
        private readonly Dictionary<string, Dictionary<string, string>> mSettingsBySection;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settingsFilePath"></param>
        public IniReader(string settingsFilePath)
        {
            if (!File.Exists(settingsFilePath))
            {
                var errorMessage = "Could not find the settings file: " + settingsFilePath;
                Logger.LogError(errorMessage);
                throw new FileNotFoundException(errorMessage);
            }

            mSettingsBySection = LoadIniFile(settingsFilePath);
        }

        /// <summary>
        /// Updates the program's settings using the .ini settings file passed to the constructor
        /// </summary>
        public void UpdateSettings()
        {

            /*
             * Files Settings
             */
            var inputFilePath = GetValueForKey("Files", "InputFileName");
            if (!string.IsNullOrWhiteSpace(inputFilePath))
            {
                var directoryName = Path.GetDirectoryName(inputFilePath);
                if (directoryName != null && !directoryName.Equals(string.Empty))
                {
                    Settings.InputDirectory = Path.GetDirectoryName(inputFilePath) + "\\";
                }

                Settings.InputFileName = Path.GetFileName(inputFilePath);
            }

            var outputDirectory = GetValueForKey("Files", "OutputDirectory");
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Settings.OutputDirectory = outputDirectory + "\\";
            }
            else
            {
                Settings.OutputDirectory = Settings.InputDirectory;
            }

            var deconToolsFilterFile = GetValueForKey("Files", "DeconToolsFilterFileName");
            if (!string.IsNullOrWhiteSpace(deconToolsFilterFile))
            {
                Settings.DeconToolsFilterFileName = deconToolsFilterFile;

                Console.WriteLine("Reading " + deconToolsFilterFile);
                var loader = new DeconToolsFilterLoader(deconToolsFilterFile);

                loader.DisplayFilters();

                Settings.DeconToolsFilterList = loader.DeconToolsFilterList;
            }
            else
            {
                Settings.DeconToolsFilterList = new List<DeconToolsFilter>();
            }

            /*
             * DataFilters Settings
             */
            var  maxIsotopicFit = GetValueForKey("DataFilters", "MaxIsotopicFit");
            if (!string.IsNullOrWhiteSpace(maxIsotopicFit))
            {
                Settings.FitMax = float.Parse(maxIsotopicFit);
            }

            var maxIScore = GetValueForKey("DataFilters", "MaxIScore");
            if (!string.IsNullOrWhiteSpace(maxIScore))
            {
                Settings.InterferenceScoreMax = float.Parse(maxIScore);
            }

            var minimumIntensity = GetValueForKey("DataFilters", "MinimumIntensity");
            if (!string.IsNullOrWhiteSpace(minimumIntensity))
            {
                Settings.IntensityMin = float.Parse(minimumIntensity);
            }

            var useHardCodedFilters = GetValueForKey("DataFilters", "UseHardCodedFilters");
            if (!string.IsNullOrWhiteSpace(useHardCodedFilters))
            {
                Settings.FilterUsingHardCodedFilters = bool.Parse(useHardCodedFilters);
            }

            var filterFlaggedData = GetValueForKey("DataFilters", "FilterFlaggedData");
            if (!string.IsNullOrWhiteSpace(filterFlaggedData))
            {
                Settings.FilterFlaggedData = bool.Parse(filterFlaggedData);
            }

            var imsMinScan = GetValueForKey("DataFilters", "IMSMinScan");
            if (!string.IsNullOrWhiteSpace(imsMinScan))
            {
                Settings.ScanIMSMin = int.Parse(imsMinScan);
            }

            var imsMaxScan = GetValueForKey("DataFilters", "IMSMaxScan");
            if (!string.IsNullOrWhiteSpace(imsMaxScan))
            {
                Settings.ScanIMSMax = int.Parse(imsMaxScan);
                if (Settings.ScanIMSMax <= 0) Settings.ScanIMSMax = int.MaxValue;
            }

            var lcMinScan = GetValueForKey("DataFilters", "LCMinScan");
            if (!string.IsNullOrWhiteSpace(lcMinScan))
            {
                Settings.ScanLCMin = int.Parse(lcMinScan);
            }

            var lcMaxScan = GetValueForKey("DataFilters", "LCMaxScan");
            if (!string.IsNullOrWhiteSpace(lcMaxScan))
            {
                Settings.ScanLCMax = int.Parse(lcMaxScan);
                if (Settings.ScanLCMax <= 0) Settings.ScanLCMax = int.MaxValue;
            }

            var monoisotopicMassMin = GetValueForKey("DataFilters", "MonoMassStart");
            if (!string.IsNullOrWhiteSpace(monoisotopicMassMin))
            {
                Settings.MassMonoisotopicStart = float.Parse(monoisotopicMassMin);
            }

            var monoisotopicMassMax = GetValueForKey("DataFilters", "MonoMassEnd");
            if (!string.IsNullOrWhiteSpace(monoisotopicMassMax))
            {
                Settings.MassMonoisotopicEnd = float.Parse(monoisotopicMassMax);
            }

            var frameTypeFilter = GetValueForKey("DataFilters", "FrameType");
            if (!string.IsNullOrWhiteSpace(frameTypeFilter))
            {
                Settings.FrameTypeFilter = (UIMFData.FrameType)short.Parse(frameTypeFilter);
            }

            /*
             * UMCCreationOptions Settings
             */
            var ignoreImsDriftTime = GetValueForKey("UMCCreationOptions", "IgnoreIMSDriftTime");
            if (!string.IsNullOrWhiteSpace(ignoreImsDriftTime))
            {
                Settings.IgnoreIMSDriftTime = bool.Parse(ignoreImsDriftTime);
            }

            // Monoisotopic mass constraint, in ppm
            var monoMassConstraint = GetValueForKey("UMCCreationOptions", "MonoMassConstraint");
            if (!string.IsNullOrWhiteSpace(monoMassConstraint))
            {
                Settings.MassMonoisotopicConstraint = float.Parse(monoMassConstraint);
            }

            // Obsolete:
            // var monoMassConstraintIsPPM = GetValueForKey("UMCCreationOptions", "MonoMassConstraintIsPPM");

            // Obsolete:
            // var useGenericNET = GetValueForKey("UMCCreationOptions", "UseGenericNET");

            var useCharge = GetValueForKey("UMCCreationOptions", "UseCharge");
            if (!string.IsNullOrWhiteSpace(useCharge))
            {
                Settings.UseCharge = bool.Parse(useCharge);
            }

            var minFeatureLengthPoints = GetValueForKey("UMCCreationOptions", "MinFeatureLengthPoints");
            if (!string.IsNullOrWhiteSpace(minFeatureLengthPoints))
            {
                Settings.FeatureLengthMin = short.Parse(minFeatureLengthPoints);
            }

            var lcGapMaxSize = GetValueForKey("UMCCreationOptions", "LCGapMaxSize");
            if (!string.IsNullOrWhiteSpace(lcGapMaxSize))
            {
                Settings.LCGapSizeMax = short.Parse(lcGapMaxSize);
            }

            var imsMaxDaCorrection = GetValueForKey("UMCCreationOptions", "IMSMaxDaCorrection");
            if (!string.IsNullOrWhiteSpace(imsMaxDaCorrection))
            {
                int readValue = short.Parse(imsMaxDaCorrection);

                Settings.IMSDaCorrectionMax = readValue < 0 ? 0 : readValue;
            }

            // Obsolete:
            // var umcFitScoreMinimum = GetValueForKey("UMCCreationOptions", "UMCFitScoreMinimum");

            // Obsolete:
            // umcSplittingEnabled = GetValueForKey("UMCSplittingOptions", "Split");
            // minimumDifferenceInMedianPpmMass = GetValueForKey("UMCSplittingOptions", "MinimumDifferenceInMedianPpmMassToSplit");

            /*
             * DriftProfile Settings
             */
            var useConformationDetection = GetValueForKey("DriftProfileOptions", "UseConformationDetection");
            if (!string.IsNullOrWhiteSpace(useConformationDetection))
            {
                Settings.UseConformationDetection = bool.Parse(useConformationDetection);
            }

            var smoothingStDev = GetValueForKey("DriftProfileOptions", "SmoothingStDev");
            if (!string.IsNullOrWhiteSpace(smoothingStDev))
            {
                Settings.SmoothingStDev = float.Parse(smoothingStDev);
            }

            /*
             * PostCreationFiltering Settings
             */
            var filterIsosToSinglePoint = GetValueForKey("PostCreationFilteringOptions", "FilterIsosToSinglePoint");
            if (!string.IsNullOrWhiteSpace(filterIsosToSinglePoint))
            {
                Settings.FilterIsosToSinglePoint = bool.Parse(filterIsosToSinglePoint);
            }
        }

        private string GetValueForKey(string sectionName, string keyName, string valueIfMissing = "")
        {
            // ReSharper disable once InvertIf
            if (mSettingsBySection.TryGetValue(sectionName, out var sectionInfo))
            {
                if (sectionInfo.TryGetValue(keyName, out var keyValue))
                    return keyValue;
            }

            return valueIfMissing;
        }

        private Dictionary<string, Dictionary<string, string>> LoadIniFile(string settingsFilePath)
        {
            var settings = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var splitChar = new[] {'='};

            var sectionMatcher = new Regex(@"^\[(?<SectionName>.+)\]", RegexOptions.Compiled);

            var currentSection = string.Empty;
            var linesRead = 0;

            try
            {
                using (var reader = new StreamReader(new FileStream(settingsFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        linesRead++;

                        if (string.IsNullOrWhiteSpace(dataLine))
                            continue;

                        var trimmedLine = dataLine.Trim();
                        if (trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                        {
                            // Comment line
                            continue;
                        }

                        var sectionMatch = sectionMatcher.Match(trimmedLine);
                        if (sectionMatch.Success)
                        {
                            currentSection = sectionMatch.Groups["SectionName"].Value;
                            continue;
                        }

                        var splitLine = trimmedLine.Split(splitChar, 2);

                        if (splitLine.Length < 2)
                        {
                            ConsoleMsgUtils.ShowWarning("Ignoring line {0} since no equals sign: {1}", linesRead, dataLine);
                        }

                        var key = splitLine[0];
                        var value = splitLine[1];

                        if (settings.TryGetValue(currentSection, out var sectionInfo))
                        {
                            StoreKey(linesRead, currentSection, sectionInfo, key, value);
                            continue;
                        }

                        var newSectionInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        settings.Add(currentSection, newSectionInfo);

                        StoreKey(linesRead, currentSection, newSectionInfo, key, value);
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = "Exception reading data from settings file " + settingsFilePath;
                Logger.LogError(errorMessage, ex);
                throw new Exception(errorMessage + ": " + ex.Message, ex);
            }

            return settings;
        }

        private void StoreKey(int linesRead, string sectionName, IDictionary<string, string> sectionInfo, string key, string value)
        {
            if (sectionInfo.ContainsKey(key))
            {
                ConsoleMsgUtils.ShowWarning(
                    "Line {0} has a duplicate key named {1} in section {2}; ignoring duplicate value {3}",
                    linesRead, key, sectionName, value);
            }

            sectionInfo.Add(key, value);
        }
    }
}
