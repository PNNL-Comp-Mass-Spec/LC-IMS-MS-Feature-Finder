using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FeatureFinder.Data;
using FeatureFinder.Data.Maps;
//using UIMFLibrary;
using FeatureFinder.Utilities;

namespace FeatureFinder.Control
{
	public class IsosReader
	{
		private StreamReader m_isosFileReader;
		private TextWriter m_isosFileWriter;
		private Dictionary<int, Settings.FrameType> m_lcScanToFrameTypeMap;

		#region Constructors
		/// <summary>
		/// Constructor for passing in a String containing the location of the ISOS csv file
		/// </summary>
		public IsosReader()
		{
			String baseFileName = Regex.Split(Settings.InputFileName, "_isos")[0];

			m_isosFileReader = new StreamReader(Settings.InputDirectory + Settings.InputFileName);
			m_isosFileWriter = new StreamWriter(Settings.OutputDirectory + baseFileName + "_Filtered_isos.csv");
			m_lcScanToFrameTypeMap = CreateLCScanToFrameTypeMapping(baseFileName);
			ColumnMap = CreateColumnMapping();
			MSFeatureList = SaveDataToMSFeatureList();

			// Calculate the drift time for each MS Feature. We are choosing to not use the Decon2ls output.
			//DataReader uimfReader = new UIMFLibrary.DataReader();

			//string uimfRawdataFile = Settings.InputDirectory + Settings.InputFileName.Replace("_isos.csv", ".uimf");
			//if (!File.Exists(uimfRawdataFile))
			//{
			//    Logger.Log("File not found error. Could not find the file: " + uimfRawdataFile);
			//    throw new FileNotFoundException("File not found error. Could not find the file: " + uimfRawdataFile);
			//}

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
		public List<MSFeature> MSFeatureList { get; private set; }

		/// <summary>
		/// Returns the Column Map contained in this class
		/// </summary>
		public Dictionary<string, int> ColumnMap { get; private set; }

		/// <summary>
		/// Returns the number of unfiltered MSFeatures that were read from the isos file
		/// </summary>
		public int NumOfUnfilteredMSFeatures { get; private set; }

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

					if (!lcScanToFrameTypeMap.ContainsKey(frameNum))
					{
						lcScanToFrameTypeMap.Add(frameNum, frameType);
					}
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
		/// <exception cref="ApplicationException">Thrown when isos file does not contain column headers</exception>
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
						columnMap.Add("MSFeature.IntensityUnSummed", i);
						break;
					case "unsummed_intensity":
						columnMap.Add("MSFeature.IntensityUnSummed", i);
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
					case "saturation_flag":
						columnMap.Add("MSFeature.IsSaturated", i);
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
			NumOfUnfilteredMSFeatures = 0;
			int msFeatureIndex = 0;
			int currentFrame = 0;

			// Read the rest of the Stream, 1 line at a time, and save the appropriate data into new Objects
			for (int i = 0; (line = m_isosFileReader.ReadLine()) != null; i++)
			{
				try
				{
					String[] columns = line.Split(',', '\t', '\n');

					MSFeature msFeature = new MSFeature {IndexInFile = i};

					if (ColumnMap.ContainsKey("MSFeature.Frame"))
					{
						int frame = Int32.Parse(columns[ColumnMap["MSFeature.Frame"]]);

						Settings.FrameType frameType;
						m_lcScanToFrameTypeMap.TryGetValue(frame, out frameType);

						// Ignore this MS Feature if it belongsm to a Frame Type that is not correct
						if (Settings.FrameTypeFilter != Settings.FrameType.NoFilter && frameType != Settings.FrameTypeFilter)
						{
							NumOfUnfilteredMSFeatures++;
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

					if (ColumnMap.ContainsKey("MSFeature.ScanIMS")) msFeature.ScanIMS = Int32.Parse(columns[ColumnMap["MSFeature.ScanIMS"]], System.Globalization.NumberStyles.Any);
					if (ColumnMap.ContainsKey("MSFeature.Charge")) msFeature.Charge = (byte)Int16.Parse(columns[ColumnMap["MSFeature.Charge"]], System.Globalization.NumberStyles.Any);
					if (ColumnMap.ContainsKey("MSFeature.Abundance")) msFeature.Abundance =(int)float.Parse(columns[ColumnMap["MSFeature.Abundance"]], System.Globalization.NumberStyles.Any);
					if (ColumnMap.ContainsKey("MSFeature.IntensityUnSummed")) msFeature.IntensityUnSummed = (int)float.Parse(columns[ColumnMap["MSFeature.IntensityUnSummed"]], System.Globalization.NumberStyles.Any);
					if (ColumnMap.ContainsKey("MSFeature.Mz")) msFeature.Mz = double.Parse(columns[ColumnMap["MSFeature.Mz"]], System.Globalization.NumberStyles.Any);
					if (ColumnMap.ContainsKey("MSFeature.Fit")) msFeature.Fit = float.Parse(columns[ColumnMap["MSFeature.Fit"]], System.Globalization.NumberStyles.Any);
					if (ColumnMap.ContainsKey("MSFeature.InterferenceScore")) msFeature.InterferenceScore = float.Parse(columns[ColumnMap["MSFeature.InterferenceScore"]], System.Globalization.NumberStyles.Any);
					if (ColumnMap.ContainsKey("MSFeature.MassMonoisotopic")) msFeature.MassMonoisotopic = double.Parse(columns[ColumnMap["MSFeature.MassMonoisotopic"]], System.Globalization.NumberStyles.Any);
                    if (ColumnMap.ContainsKey("MSFeature.MassMostAbundant")) msFeature.MassMostAbundantIsotope = double.Parse(columns[ColumnMap["MSFeature.MassMostAbundant"]], System.Globalization.NumberStyles.Any);
                    if (ColumnMap.ContainsKey("MSFeature.Fwhm")) msFeature.Fwhm = float.Parse(columns[ColumnMap["MSFeature.Fwhm"]], System.Globalization.NumberStyles.Any);
					if (ColumnMap.ContainsKey("MSFeature.DriftTimeIMS")) msFeature.DriftTime = float.Parse(columns[ColumnMap["MSFeature.DriftTimeIMS"]], System.Globalization.NumberStyles.Any);
					if (ColumnMap.ContainsKey("MSFeature.ErrorFlag")) msFeature.ErrorFlag = (byte)(columns[ColumnMap["MSFeature.ErrorFlag"]].Equals("") ? 0 : Int16.Parse(columns[ColumnMap["MSFeature.ErrorFlag"]], System.Globalization.NumberStyles.Any));
					if (ColumnMap.ContainsKey("MSFeature.IsSaturated")) msFeature.IsSaturated = Convert.ToBoolean(Int16.Parse(columns[ColumnMap["MSFeature.IsSaturated"]], System.Globalization.NumberStyles.Any));

					if (PassesFilters(msFeature))
					{
						msFeature.Id = msFeatureIndex;
						msFeatureList.Add(msFeature);
						m_isosFileWriter.WriteLine(line);
						msFeatureIndex++;
					}

					NumOfUnfilteredMSFeatures++;
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
			if (ColumnMap.ContainsKey("MSFeature.Frame"))
			{
				if (msFeature.ScanLC < Settings.ScanLCMin || msFeature.ScanLC > Settings.ScanLCMax) return false;
			}

			if (ColumnMap.ContainsKey("MSFeature.ScanIMS"))
			{
				if (msFeature.ScanIMS < Settings.ScanIMSMin || msFeature.ScanIMS > Settings.ScanIMSMax) return false;
			}

			if (ColumnMap.ContainsKey("MSFeature.MassMonoisotopic"))
			{
				if (msFeature.MassMonoisotopic < Settings.MassMonoisotopicStart || msFeature.MassMonoisotopic > Settings.MassMonoisotopicEnd) return false;
			}

			if (ColumnMap.ContainsKey("MSFeature.ErrorFlag"))
			{
				if (msFeature.ErrorFlag == 1) return false;
			}


            bool deconToolsFilterTableIsBeingUsed = (Settings.FilterUsingHardCodedFilters &&  Settings.DeconToolsFilterList != null && Settings.DeconToolsFilterList.Count > 0);
            if (deconToolsFilterTableIsBeingUsed)
            {
                if (!DeconToolsFilterUtil.IsValidMSFeature(msFeature,Settings.DeconToolsFilterList))
                {
                    return false;
                }
            }
			else
			{
				if (ColumnMap.ContainsKey("MSFeature.Fit"))
				{
					if (msFeature.Fit > Settings.FitMax) return false;
				}

				if (ColumnMap.ContainsKey("MSFeature.InterferenceScore"))
				{
					if (msFeature.InterferenceScore > Settings.InterferenceScoreMax) return false;
				}

				if (ColumnMap.ContainsKey("MSFeature.Abundance"))
				{
					if (msFeature.Abundance < Settings.IntensityMin) return false;
				}
			}

			return true;
		}
		#endregion
	}
}
