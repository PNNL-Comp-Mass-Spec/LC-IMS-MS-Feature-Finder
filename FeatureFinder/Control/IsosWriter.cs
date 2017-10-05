using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FeatureFinder.Data;

namespace FeatureFinder.Control
{
    public class IsosWriter
    {
        private readonly StreamReader m_isosFileReader;
        private readonly TextWriter m_isosFileWriter;
        private readonly Dictionary<string, int> m_columnMap;
        private readonly List<MSFeature> m_msFeatureList;

        public IsosWriter(List<MSFeature> msFeatureList, Dictionary<string, int> columnMap)
        {
            var baseFileName = Regex.Split(Settings.InputFileName, "_isos")[0];
            m_isosFileReader = new StreamReader(Path.Combine(Settings.OutputDirectory, baseFileName + "_Filtered_isos.csv"));
            m_isosFileWriter = new StreamWriter(Path.Combine(Settings.OutputDirectory, baseFileName + "_Filtered_New_isos.csv"));
            m_columnMap = columnMap;
            m_msFeatureList = msFeatureList;

            WriteIsosFile();

            File.Delete(Path.Combine(Settings.OutputDirectory, baseFileName + "_Filtered_isos.csv"));

            File.Move(
                Path.Combine(Settings.OutputDirectory, baseFileName + "_Filtered_New_isos.csv"),
                Path.Combine(Settings.OutputDirectory, baseFileName + "_Filtered_isos.csv"));
        }

        private void WriteIsosFile()
        {
            var line = "";
            var offset = 0;
            var index = 0;
            var previousFeatureId = int.MaxValue;

            m_msFeatureList.Sort(MSFeature.IDComparison);

            m_isosFileWriter.WriteLine(m_isosFileReader.ReadLine());

            // Read the rest of the Stream, 1 line at a time, and save the write the appropriate data into the new Isos file
            for (var i = 0; (line = m_isosFileReader.ReadLine()) != null && i + offset < m_msFeatureList.Count; i++)
            {
                var msFeature = m_msFeatureList[i + offset];
                while (msFeature.Id == previousFeatureId && i + offset < m_msFeatureList.Count - 1)
                {
                    offset++;
                    msFeature = m_msFeatureList[i + offset];
                }

                if (msFeature.Id == i)
                {
                    msFeature.FilteredIndex = index;
                    index++;
                    var columns = line.Split(',', '\t', '\n');

                    if (m_columnMap.ContainsKey("MSFeature.Mz")) columns[m_columnMap["MSFeature.Mz"]] = msFeature.Mz.ToString();
                    if (m_columnMap.ContainsKey("MSFeature.MassMonoisotopic")) columns[m_columnMap["MSFeature.MassMonoisotopic"]] = msFeature.MassMonoisotopic.ToString();
                    if (m_columnMap.ContainsKey("MSFeature.MassMostAbundant")) columns[m_columnMap["MSFeature.MassMostAbundant"]] = msFeature.MassMostAbundantIsotope.ToString();

                    var newLine = "";

                    foreach (var column in columns)
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
