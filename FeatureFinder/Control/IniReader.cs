using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace FeatureFinder.Control
{
	public class IniReader
	{
		private String m_path;

		[DllImport("kernel32")]
		private static extern int GetPrivateProfileString(String section, String key, String def, StringBuilder retVal, int size, String filePath);

		public IniReader(String path)
		{
			if (File.Exists(path))
			{
				m_path = path;
			}
			else
			{
				throw new FileNotFoundException("Could not find file '" + path + "'.");
			}
		}

		public void CreateSettings()
		{
			String value = "";

			/*
			 * Files Settings
			 */
			value = IniReadValue("Files", "InputFileName");
			if (!value.Equals(String.Empty))
			{
				if (!Path.GetDirectoryName(value).Equals(string.Empty))
				{
					Settings.InputDirectory = Path.GetDirectoryName(value) + "\\";
				}

				Settings.InputFileName = Path.GetFileName(value);
			}

			value = IniReadValue("Files", "OutputDirectory");
			if (!value.Equals(String.Empty))
			{
				Settings.OutputDirectory = value + "\\";
			}
			else
			{
				Settings.OutputDirectory = Settings.InputDirectory;
			}

			/*
			 * DataFilters Settings
			 */
			value = IniReadValue("DataFilters", "MaxIsotopicFit");
			if (!value.Equals(String.Empty))
			{
				Settings.FitMax = float.Parse(value);
			}

			value = IniReadValue("DataFilters", "MinimumIntensity");
			if (!value.Equals(String.Empty))
			{
				Settings.IntensityMin = float.Parse(value);
			}

			value = IniReadValue("DataFilters", "IMSMinScan");
			if (!value.Equals(String.Empty))
			{
				Settings.ScanIMSMin = int.Parse(value);
			}

			value = IniReadValue("DataFilters", "IMSMaxScan");
			if (!value.Equals(String.Empty))
			{
				Settings.ScanIMSMax = int.Parse(value);
				if (Settings.ScanIMSMax <= 0) Settings.ScanIMSMax = int.MaxValue;
			}

			value = IniReadValue("DataFilters", "LCMinScan");
			if (!value.Equals(String.Empty))
			{
				Settings.ScanLCMin = int.Parse(value);
			}

			value = IniReadValue("DataFilters", "LCMaxScan");
			if (!value.Equals(String.Empty))
			{
				Settings.ScanLCMax = int.Parse(value);
				if (Settings.ScanLCMax <= 0) Settings.ScanLCMax = int.MaxValue;
			}

			value = IniReadValue("DataFilters", "MonoMassStart");
			if (!value.Equals(String.Empty))
			{
				Settings.MassMonoisotopicStart = float.Parse(value);
			}

			value = IniReadValue("DataFilters", "MonoMassEnd");
			if (!value.Equals(String.Empty))
			{
				Settings.MassMonoisotopicEnd = float.Parse(value);
			}

			value = IniReadValue("DataFilters", "FrameType");
			if (!value.Equals(String.Empty))
			{
				Settings.FrameTypeFilter = (Settings.FrameType)short.Parse(value);
			}

			/*
			 * UMCCreationOptions Settings
			 */
			value = IniReadValue("UMCCreationOptions", "IgnoreIMSDriftTime");
			if (!value.Equals(String.Empty))
			{
				Settings.IgnoreIMSDriftTime = bool.Parse(value);
			}

			value = IniReadValue("UMCCreationOptions", "MonoMassConstraint");
			if (!value.Equals(String.Empty))
			{
				Settings.MassMonoisotopicConstraint = float.Parse(value);
			}

			value = IniReadValue("UMCCreationOptions", "MonoMassConstraintIsPPM");
			if (!value.Equals(String.Empty))
			{
				Settings.MassMonoisotopicConstraintIsPPM = bool.Parse(value);
			}

			value = IniReadValue("UMCCreationOptions", "UseGenericNET");
			if (!value.Equals(String.Empty))
			{
				Settings.UseGenericNET = bool.Parse(value);
			}

			value = IniReadValue("UMCCreationOptions", "UseCharge");
			if (!value.Equals(String.Empty))
			{
				Settings.UseCharge = bool.Parse(value);
			}

			value = IniReadValue("UMCCreationOptions", "MinFeatureLengthPoints");
			if (!value.Equals(String.Empty))
			{
				Settings.FeatureLengthMin = short.Parse(value);
			}

			value = IniReadValue("UMCCreationOptions", "LCGapMaxSize");
			if (!value.Equals(String.Empty))
			{
				Settings.LCGapSizeMax = short.Parse(value);
			}

			value = IniReadValue("UMCCreationOptions", "IMSGapMaxSize");
			if (!value.Equals(String.Empty))
			{
				Settings.IMSGapSizeMax = short.Parse(value);
			}

			value = IniReadValue("UMCCreationOptions", "IMSMaxDaCorrection");
			if (!value.Equals(String.Empty))
			{
				int readValue = short.Parse(value);

				if (readValue < 0)
				{
					Settings.IMSDaCorrectionMax = 0;
				}
				else
				{
					Settings.IMSDaCorrectionMax = readValue;
				}
			}

			value = IniReadValue("UMCCreationOptions", "UMCFitScoreMinimum");
			if (!value.Equals(String.Empty))
			{
				Settings.UMCFitScoreMinimum = float.Parse(value);
			}

			/*
			 * UMCSplittingOptions Settings
			 */
			value = IniReadValue("UMCSplittingOptions", "Split");
			if (!value.Equals(String.Empty))
			{
				Settings.Split = bool.Parse(value);
			}

			value = IniReadValue("UMCSplittingOptions", "MinimumDifferenceInMedianPpmMassToSplit");
			if (!value.Equals(String.Empty))
			{
				Settings.MinimumDifferenceInMedianPpmMassToSplit = short.Parse(value);
			}

			/*
			 * DriftProfile Settings
			 */
			value = IniReadValue("DriftProfileOptions", "UseConformationDetection");
			if (!value.Equals(String.Empty))
			{
				Settings.UseConformationDetection = bool.Parse(value);
			}

			value = IniReadValue("DriftProfileOptions", "UseConformationIndex");
			if (!value.Equals(String.Empty))
			{
				Settings.UseConformationIndex = bool.Parse(value);
			}

			value = IniReadValue("DriftProfileOptions", "SmoothingStDev");
			if (!value.Equals(String.Empty))
			{
				Settings.SmoothingStDev = float.Parse(value);
			}

			/*
			 * PostCreationFiltering Settings
			 */
			value = IniReadValue("PostCreationFilteringOptions", "FilterIsosToSinglePoint");
			if (!value.Equals(String.Empty))
			{
				Settings.FilterIsosToSinglePoint = bool.Parse(value);
			}
		}

		private String IniReadValue(String Section, String Key)
		{
			StringBuilder stringBuilder = new StringBuilder(255);
			int i = GetPrivateProfileString(Section, Key, "", stringBuilder, 255, this.m_path);
			return stringBuilder.ToString();
		}
	}
}
