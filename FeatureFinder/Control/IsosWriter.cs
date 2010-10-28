﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FeatureFinder.Data;

namespace FeatureFinder.Control
{
	public class IsosWriter
	{
		private StreamReader m_isosFileReader;
		private TextWriter m_isosFileWriter;
		private Dictionary<String, int> m_columnMap;
		private List<MSFeature> m_msFeatureList;

		public IsosWriter(List<MSFeature> msFeatureList, Dictionary<String, int> columnMap)
		{
			String baseFileName = Regex.Split(Settings.InputFileName, "_isos")[0];
			m_isosFileReader = new StreamReader(Settings.OutputDirectory + baseFileName + "_Filtered_isos.csv");
			m_isosFileWriter = new StreamWriter(Settings.OutputDirectory + baseFileName + "_Filtered_New_isos.csv");
			m_columnMap = columnMap;
			m_msFeatureList = msFeatureList;

			WriteIsosFile();

			File.Delete(Settings.OutputDirectory + baseFileName + "_Filtered_isos.csv");
			File.Move(Settings.OutputDirectory + baseFileName + "_Filtered_New_isos.csv", Settings.OutputDirectory + baseFileName + "_Filtered_isos.csv");
		}

		private void WriteIsosFile()
		{
			String line = "";
			int offset = 0;
			int index = 0;
			int previousFeatureId = int.MaxValue;

			m_msFeatureList.Sort(MSFeature.IDComparison);

			m_isosFileWriter.WriteLine(m_isosFileReader.ReadLine());

			// Read the rest of the Stream, 1 line at a time, and save the write the appropriate data into the new Isos file
			for (int i = 0; (line = m_isosFileReader.ReadLine()) != null && i + offset < m_msFeatureList.Count; i++)
			{
				MSFeature msFeature = m_msFeatureList[i + offset];
				while (msFeature.Id == previousFeatureId && i + offset < m_msFeatureList.Count - 1)
				{
					offset++;
					msFeature = m_msFeatureList[i + offset];
				}

				if (msFeature.Id == i)
				{
					msFeature.FilteredIndex = index;
					index++;
					String[] columns = line.Split(',', '\t', '\n');

					if (m_columnMap.ContainsKey("MSFeature.Mz")) columns[m_columnMap["MSFeature.Mz"]] = msFeature.Mz.ToString();
					if (m_columnMap.ContainsKey("MSFeature.MassMonoisotopic")) columns[m_columnMap["MSFeature.MassMonoisotopic"]] = msFeature.MassMonoisotopic.ToString();

					string newLine = "";

					foreach (String column in columns)
					{
						newLine = newLine + column + ",";
					}

					newLine = newLine.Remove(newLine.Length - 1);

					m_isosFileWriter.WriteLine(newLine);
					previousFeatureId = msFeature.Id;
				}
				else
				{
					offset--;
				}
			}

			m_isosFileWriter.Close();
			m_isosFileReader.Close();
		}
	}
}
