using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using UIMFLibrary;

namespace FeatureFinder.Control
{
    public class IniReader
    {
        private readonly string m_path;

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public IniReader(string path)
        {
            if (File.Exists(path))
            {
                m_path = path;
            }
            else
            {
                Logger.Log("Could not find file '" + path + "'.");
                throw new FileNotFoundException("Could not find file '" + path + "'.");
            }
        }

        public void CreateSettings()
        {
            var value = "";

            /*
             * Files Settings
             */
            value = IniReadValue("Files", "InputFileName");
            if (!string.IsNullOrWhiteSpace(value))
            {
                var directoryName = Path.GetDirectoryName(value);
                if (directoryName != null && !directoryName.Equals(string.Empty))
                {
                    Settings.InputDirectory = Path.GetDirectoryName(value) + "\\";
                }

                Settings.InputFileName = Path.GetFileName(value);
            }

            value = IniReadValue("Files", "OutputDirectory");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.OutputDirectory = value + "\\";
            }
            else
            {
                Settings.OutputDirectory = Settings.InputDirectory;
            }

            value = IniReadValue("Files", "DeconToolsFilterFileName");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.DeconToolsFilterFileName = value;
                var loader = new DeconToolsFilterLoader(value);
                Settings.DeconToolsFilterList = loader.DeconToolsFilterList;
            }
            else
            {
                Settings.DeconToolsFilterList = new System.Collections.Generic.List<DeconToolsFilter>();

            }

            /*
             * DataFilters Settings
             */
            value = IniReadValue("DataFilters", "MaxIsotopicFit");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.FitMax = float.Parse(value);
            }

            value = IniReadValue("DataFilters", "MaxIScore");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.InterferenceScoreMax = float.Parse(value);
            }

            value = IniReadValue("DataFilters", "MinimumIntensity");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.IntensityMin = float.Parse(value);
            }

            value = IniReadValue("DataFilters", "UseHardCodedFilters");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.FilterUsingHardCodedFilters = bool.Parse(value);
            }

            value = IniReadValue("DataFilters", "FilterFlaggedData");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.FilterFlaggedData = bool.Parse(value);
            }

            value = IniReadValue("DataFilters", "IMSMinScan");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.ScanIMSMin = int.Parse(value);
            }

            value = IniReadValue("DataFilters", "IMSMaxScan");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.ScanIMSMax = int.Parse(value);
                if (Settings.ScanIMSMax <= 0) Settings.ScanIMSMax = int.MaxValue;
            }

            value = IniReadValue("DataFilters", "LCMinScan");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.ScanLCMin = int.Parse(value);
            }

            value = IniReadValue("DataFilters", "LCMaxScan");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.ScanLCMax = int.Parse(value);
                if (Settings.ScanLCMax <= 0) Settings.ScanLCMax = int.MaxValue;
            }

            value = IniReadValue("DataFilters", "MonoMassStart");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.MassMonoisotopicStart = float.Parse(value);
            }

            value = IniReadValue("DataFilters", "MonoMassEnd");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.MassMonoisotopicEnd = float.Parse(value);
            }

            value = IniReadValue("DataFilters", "FrameType");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.FrameTypeFilter = (DataReader.FrameType)short.Parse(value);
            }

            /*
             * UMCCreationOptions Settings
             */
            value = IniReadValue("UMCCreationOptions", "IgnoreIMSDriftTime");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.IgnoreIMSDriftTime = bool.Parse(value);
            }

            value = IniReadValue("UMCCreationOptions", "MonoMassConstraint");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.MassMonoisotopicConstraint = float.Parse(value);
            }

            value = IniReadValue("UMCCreationOptions", "MonoMassConstraintIsPPM");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.MassMonoisotopicConstraintIsPPM = bool.Parse(value);
            }

            value = IniReadValue("UMCCreationOptions", "UseGenericNET");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.UseGenericNET = bool.Parse(value);
            }

            value = IniReadValue("UMCCreationOptions", "UseCharge");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.UseCharge = bool.Parse(value);
            }

            value = IniReadValue("UMCCreationOptions", "MinFeatureLengthPoints");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.FeatureLengthMin = short.Parse(value);
            }

            value = IniReadValue("UMCCreationOptions", "LCGapMaxSize");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.LCGapSizeMax = short.Parse(value);
            }

            value = IniReadValue("UMCCreationOptions", "IMSMaxDaCorrection");
            if (!string.IsNullOrWhiteSpace(value))
            {
                int readValue = short.Parse(value);

                Settings.IMSDaCorrectionMax = readValue < 0 ? 0 : readValue;
            }

            value = IniReadValue("UMCCreationOptions", "UMCFitScoreMinimum");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.UMCFitScoreMinimum = float.Parse(value);
            }

            /*
             * UMCSplittingOptions Settings
             */
            value = IniReadValue("UMCSplittingOptions", "Split");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.Split = bool.Parse(value);
            }

            value = IniReadValue("UMCSplittingOptions", "MinimumDifferenceInMedianPpmMassToSplit");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.MinimumDifferenceInMedianPpmMassToSplit = short.Parse(value);
            }

            /*
             * DriftProfile Settings
             */
            value = IniReadValue("DriftProfileOptions", "UseConformationDetection");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.UseConformationDetection = bool.Parse(value);
            }

            value = IniReadValue("DriftProfileOptions", "SmoothingStDev");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.SmoothingStDev = float.Parse(value);
            }

            /*
             * PostCreationFiltering Settings
             */
            value = IniReadValue("PostCreationFilteringOptions", "FilterIsosToSinglePoint");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Settings.FilterIsosToSinglePoint = bool.Parse(value);
            }
        }

        private string IniReadValue(string Section, string Key)
        {
            var stringBuilder = new StringBuilder(255);
            GetPrivateProfileString(Section, Key, "", stringBuilder, 255, this.m_path);
            return stringBuilder.ToString();
        }
    }
}
