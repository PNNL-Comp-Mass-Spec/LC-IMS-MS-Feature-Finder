using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FeatureFinder.Data;
using FeatureFinder.Data.Maps;
using UIMFLibrary;
using System.Linq;
using FeatureFinder.Algorithms;
using FeatureFinder.Utilities;

namespace FeatureFinder.Control
{
	public class IsosReader
	{
		private StreamReader m_isosFileReader;
		private TextWriter m_isosFileWriter;
		private Dictionary<String, int> m_columnMap;
		private List<MSFeature> m_msFeatureList;
		private int m_numOfUnfilteredMSFeatures;
		private Dictionary<int, Settings.FrameType> m_lcScanToFrameTypeMap;

		#region Constructors
		/// <summary>
		/// Constructor for passing in a String containing the location of the ISOS csv file
		/// </summary>
		/// <param name="Settings">Reference to the Settings object</param>
		public IsosReader()
		{
			String baseFileName = Regex.Split(Settings.InputFileName, "_isos")[0];

			m_isosFileReader = new StreamReader(Settings.InputDirectory + Settings.InputFileName);
			m_isosFileWriter = new StreamWriter(Settings.OutputDirectory + baseFileName + "_Filtered_isos.csv");
			m_lcScanToFrameTypeMap = CreateLCScanToFrameTypeMapping(baseFileName);
			m_columnMap = CreateColumnMapping();
			m_msFeatureList = SaveDataToMSFeatureList();

			// Calculate the drift time for each MS Feature. We are choosing to not use the Decon2ls output.
			//DataReader uimfReader = new UIMFLibrary.DataReader();
			//if (uimfReader.OpenUIMF(Settings.InputDirectory + Settings.InputFileName.Replace("_isos.csv", ".uimf")))
			//{
			//    FixDriftTimeValues(uimfReader);
			//}
			//uimfReader.CloseUIMF();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Returns the MSFeatureList contained in this class
		/// </summary>
		public List<MSFeature> MSFeatureList
		{
			get { return m_msFeatureList; }
		}

		/// <summary>
		/// Returns the Column Map contained in this class
		/// </summary>
		public Dictionary<String, int> ColumnMap
		{
			get { return m_columnMap; }
		}

		/// <summary>
		/// Returns the number of unfiltered MSFeatures that were read from the isos file
		/// </summary>
		public int NumOfUnfilteredMSFeatures
		{
			get { return m_numOfUnfilteredMSFeatures; }
		}
		#endregion

		#region Private Methods
		private Dictionary<int, Settings.FrameType> CreateLCScanToFrameTypeMapping(String baseFileName)
		{
			Dictionary<int, Settings.FrameType> lcScanToFrameTypeMap = new Dictionary<int, Settings.FrameType>();
			StreamReader scansFileReader = null;

			try
			{
				scansFileReader = new StreamReader(Settings.OutputDirectory + baseFileName + "_scans.csv");
				String firstLine = scansFileReader.ReadLine();

				if (firstLine == null)
				{
					return null;
				}

				String[] columnTitles = firstLine.Split('\t', ',', '\n');
				int frameNumColumn = -1;
				int frameTypeColumn = -1;

				// Find the Frame Type column
				for (int i = 0; i < columnTitles.Length; i++)
				{
					switch (columnTitles[i].Trim().ToLower())
					{
						case "frame_num":
							frameNumColumn = i;
							break;
						case "type":
							frameTypeColumn = i;
							break;
						default:
							break;
					}
				}

				// If the frame Number or Frame Type column was not found, return an empty dictionary
				if (frameTypeColumn == -1 || frameNumColumn == -1)
				{
					return new Dictionary<int, Settings.FrameType>();
				}

				// Add each Frame Number and its corresponding Frame Type to the Map
				String line = "";
				for (int i = 0; (line = scansFileReader.ReadLine()) != null; i++)
				{
					String[] columns = line.Split(',', '\t', '\n');

					int frameNum = int.Parse(columns[frameNumColumn]);
					Settings.FrameType frameType = (Settings.FrameType)short.Parse(columns[frameTypeColumn]);

					lcScanToFrameTypeMap.Add(frameNum, frameType);
				}
			}
			catch (FileNotFoundException)
			{
				// If the Scans file is not found, return an empty dictionary
				return new Dictionary<int, Settings.FrameType>();
			}

			return lcScanToFrameTypeMap;
		}

		/// <summary>
		/// Fills in the Column Map with the appropriate values.
		/// The Map will have a Column Property (e.g. MSFeature.Frame) mapped to a Column Number.
		/// </summary>
		/// <returns>The column map as a Dictionary object</returns>
		private Dictionary<String, int> CreateColumnMapping()
		{
			Dictionary<String, int> columnMap = new Dictionary<String, int>();

			String firstLine = m_isosFileReader.ReadLine();

			if (firstLine == null)
			{
				return null;
			}

			String[] columnTitles = firstLine.Split('\t', ',', '\n');
			m_isosFileWriter.WriteLine(firstLine);

			for (int i = 0; i < columnTitles.Length; i++)
			{
				switch (columnTitles[i].Trim().ToLower())
				{
					case "frame_num":
						columnMap.Add("MSFeature.Frame", i);
						break;
					case "scan_num":
						columnMap.Add("MSFeature.Frame", i);
						break;
					case "lc_scan_num":
						columnMap.Add("MSFeature.Frame", i);
						break;
					case "ims_scan_num":
						columnMap.Add("MSFeature.ScanIMS", i);
						break;
					case "charge":
						columnMap.Add("MSFeature.Charge", i);
						break;
					case "abundance":
						columnMap.Add("MSFeature.Abundance", i);
						break;
					case "mz":
						columnMap.Add("MSFeature.Mz", i);
						break;
					case "fit":
						columnMap.Add("MSFeature.Fit", i);
						break;
					case "interference_score":
						columnMap.Add("MSFeature.InterferenceScore", i);
						break;
					case "average_mw":
						columnMap.Add("MSFeature.MassAverage", i);
						break;
					case "monoisotopic_mw":
						columnMap.Add("MSFeature.MassMonoisotopic", i);
						break;
					case "mostabundant_mw":
						columnMap.Add("MSFeature.MassMostAbundant", i);
						break;
					case "fwhm":
						columnMap.Add("MSFeature.Fwhm", i);
						break;
					case "signal_noise":
						columnMap.Add("MSFeature.SignalNoise", i);
						break;
					case "mono_abundance":
						columnMap.Add("MSFeature.AbundanceMono", i);
						break;
					case "mono_plus2_abundance":
						columnMap.Add("MSFeature.AbundancePlus2", i);
						break;
					case "orig_intensity":
						columnMap.Add("MSFeature.IntensityOriginal", i);
						break;
					case "tia_orig_intensity":
						columnMap.Add("MSFeature.IntensityOriginalTIA", i);
						break;
					case "drift_time":
						columnMap.Add("MSFeature.DriftTimeIMS", i);
						break;
					case "cumulative_drift_time":
						columnMap.Add("MSFeature.DriftTimeCumulative", i);
						break;
					case "flag":
						columnMap.Add("MSFeature.ErrorFlag", i);
						break;
					default:
						//Title not found.
						break;
				}
			}

			if (columnMap.Count == 0)
			{
				//TODO: Create default mapping?
				Logger.Log("Isos file does not contain column headers. Cannot continue.");
				throw new ApplicationException("Isos file does not contain column headers. Cannot continue.");
			}

			return columnMap;
		}

		/// <summary>
		/// Saves the data from a ISOS csv file to an List of MSFeature Objects.
		/// </summary>
		private List<MSFeature> SaveDataToMSFeatureList()
		{
			List<MSFeature> msFeatureList = new List<MSFeature>();
			String line;
			MSFeature msFeature;
			m_numOfUnfilteredMSFeatures = 0;
			int msFeatureIndex = 0;
			int currentFrame = 0;

			// Read the rest of the Stream, 1 line at a time, and save the appropriate data into new Objects
			for (int i = 0; (line = m_isosFileReader.ReadLine()) != null; i++)
			{
				try
				{
					String[] columns = line.Split(',', '\t', '\n');
					
					msFeature = new MSFeature();
					msFeature.IndexInFile = i;

					if (m_columnMap.ContainsKey("MSFeature.Frame"))
					{
						int frame = Int32.Parse(columns[m_columnMap["MSFeature.Frame"]]);

						Settings.FrameType frameType;
						m_lcScanToFrameTypeMap.TryGetValue((int)frame, out frameType);

						// Ignore this MS Feature if it belongsm to a Frame Type that is not correct
						if (Settings.FrameTypeFilter != Settings.FrameType.NoFilter && frameType != Settings.FrameTypeFilter)
						{
							m_numOfUnfilteredMSFeatures++;
							continue;
						}

						if (i == 0)
						{
							currentFrame = frame;
							ScanLCMap.Mapping.Add(ScanLCMap.ScanLCIndex, frame);
						}
						if (frame != currentFrame)
						{
							currentFrame = frame;
							ScanLCMap.ScanLCIndex++;
							ScanLCMap.Mapping.Add(ScanLCMap.ScanLCIndex, frame);
						}

						msFeature.ScanLC = ScanLCMap.ScanLCIndex;
					}

					if (m_columnMap.ContainsKey("MSFeature.ScanIMS")) msFeature.ScanIMS = Int32.Parse(columns[m_columnMap["MSFeature.ScanIMS"]], System.Globalization.NumberStyles.Any);
					if (m_columnMap.ContainsKey("MSFeature.Charge")) msFeature.Charge = (byte)Int16.Parse(columns[m_columnMap["MSFeature.Charge"]], System.Globalization.NumberStyles.Any);
					if (m_columnMap.ContainsKey("MSFeature.Abundance")) msFeature.Abundance = Int32.Parse(columns[m_columnMap["MSFeature.Abundance"]], System.Globalization.NumberStyles.Any);
					if (m_columnMap.ContainsKey("MSFeature.Mz")) msFeature.Mz = float.Parse(columns[m_columnMap["MSFeature.Mz"]], System.Globalization.NumberStyles.Any);
					if (m_columnMap.ContainsKey("MSFeature.Fit")) msFeature.Fit = float.Parse(columns[m_columnMap["MSFeature.Fit"]], System.Globalization.NumberStyles.Any);
					if (m_columnMap.ContainsKey("MSFeature.InterferenceScore")) msFeature.InterferenceScore = float.Parse(columns[m_columnMap["MSFeature.InterferenceScore"]], System.Globalization.NumberStyles.Any);
					if (m_columnMap.ContainsKey("MSFeature.MassMonoisotopic")) msFeature.MassMonoisotopic = float.Parse(columns[m_columnMap["MSFeature.MassMonoisotopic"]], System.Globalization.NumberStyles.Any);
					if (m_columnMap.ContainsKey("MSFeature.Fwhm")) msFeature.Fwhm = float.Parse(columns[m_columnMap["MSFeature.Fwhm"]], System.Globalization.NumberStyles.Any);
					if (m_columnMap.ContainsKey("MSFeature.DriftTimeIMS")) msFeature.DriftTime = float.Parse(columns[m_columnMap["MSFeature.DriftTimeIMS"]], System.Globalization.NumberStyles.Any);
					if (m_columnMap.ContainsKey("MSFeature.ErrorFlag")) msFeature.ErrorFlag = (byte)(columns[m_columnMap["MSFeature.ErrorFlag"]].Equals("") ? 0 : Int16.Parse(columns[m_columnMap["MSFeature.ErrorFlag"]], System.Globalization.NumberStyles.Any));

					if (PassesFilters(msFeature))
					{
						msFeature.Id = msFeatureIndex;
						msFeatureList.Add(msFeature);
						m_isosFileWriter.WriteLine(line);
						msFeatureIndex++;
					}

					m_numOfUnfilteredMSFeatures++;
				}
				catch (Exception e)
				{
					Logger.Log("Error while reading line in isos file. Skipping Line #" + (i + 2));
					Console.WriteLine(e.StackTrace);
				}
			}

			m_isosFileReader.Close();
			m_isosFileWriter.Close();

			return msFeatureList;
		}

		private bool PassesFilters(MSFeature msFeature)
		{
			if (m_columnMap.ContainsKey("MSFeature.Frame"))
			{
				if (msFeature.ScanLC < Settings.ScanLCMin || msFeature.ScanLC > Settings.ScanLCMax) return false;
			}

			if (m_columnMap.ContainsKey("MSFeature.ScanIMS"))
			{
				if (msFeature.ScanIMS < Settings.ScanIMSMin || msFeature.ScanIMS > Settings.ScanIMSMax) return false;
			}

			if (m_columnMap.ContainsKey("MSFeature.MassMonoisotopic"))
			{
				if (msFeature.MassMonoisotopic < Settings.MassMonoisotopicStart || msFeature.MassMonoisotopic > Settings.MassMonoisotopicEnd) return false;
			}

			if (Settings.FilterUsingHardCodedFilters)
			{
				if (!DeconToolsFilterUtil.IsValidMSFeature(msFeature))
				{
					return false;
				}
			}
			else
			{
				if (m_columnMap.ContainsKey("MSFeature.Fit"))
				{
					if (msFeature.Fit > Settings.FitMax) return false;
				}

				if (m_columnMap.ContainsKey("MSFeature.InterferenceScore"))
				{
					if (msFeature.InterferenceScore > Settings.InterferenceScoreMax) return false;
				}

				if (m_columnMap.ContainsKey("MSFeature.Abundance"))
				{
					if (msFeature.Abundance < Settings.IntensityMin) return false;
				}
			}

			return true;
		}

		/// <summary>
		/// This method will alter the Drift Time values of the MS Features.
		/// The drift time value will be calculated assuming that frame pressure is normal and not varying.
		/// </summary>
		/// <param name="uimfReader">The UIMF file DataReader object</param>
		private void FixDriftTimeValues(DataReader uimfReader)
		{
			var groupByScanLCQuery = from msFeature in m_msFeatureList
									 group msFeature by msFeature.ScanLC into newGroup
									 select newGroup;

			foreach (IEnumerable<MSFeature> msFeatureGroup in groupByScanLCQuery)
			{
				int lcScan = ScanLCMap.Mapping[msFeatureGroup.First().ScanLC];

				FrameParameters frameParameters = uimfReader.GetFrameParameters(lcScan);
				double averageTOFLength = frameParameters.AverageTOFLength;
				double framePressure = frameParameters.PressureBack;

				foreach(MSFeature msFeature in msFeatureGroup)
				{
					double driftTime = ConformationDetection.ConvertIMSScanToDriftTime(msFeature.ScanIMS, averageTOFLength);
					msFeature.DriftTime = (float)driftTime;
				}
			}
		}
		#endregion
	}
}
