using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FeatureFinder.Data;

namespace FeatureFinder.Control
{
    public class IsosWriter
    {
        private readonly string mBaseFileName;

        private readonly StreamReader m_isosFileReader;
        private readonly TextWriter m_isosFileWriter;
        private readonly Dictionary<string, int> m_columnMap;

        public IsosWriter(Dictionary<string, int> columnMap)
        {
            mBaseFileName = Regex.Split(Settings.InputFileName, "_isos")[0];
            m_isosFileReader = new StreamReader(Path.Combine(Settings.OutputDirectory, mBaseFileName + "_Filtered_isos.csv"));
            m_isosFileWriter = new StreamWriter(Path.Combine(Settings.OutputDirectory, mBaseFileName + "_Filtered_New_isos.csv"));
            m_columnMap = columnMap;
        }

        public void CreateFilteredIsosFile(List<MSFeature> msFeatureList)
        {
            WriteIsosFile(msFeatureList);

            File.Delete(Path.Combine(Settings.OutputDirectory, mBaseFileName + "_Filtered_isos.csv"));

            File.Move(
                Path.Combine(Settings.OutputDirectory, mBaseFileName + "_Filtered_New_isos.csv"),
                Path.Combine(Settings.OutputDirectory, mBaseFileName + "_Filtered_isos.csv"));
        }

        private void WriteIsosFile(List<MSFeature> msFeatureList)
        {
            string line;
            var offset = 0;
            var index = 0;
            var previousFeatureId = int.MaxValue;

            msFeatureList.Sort(MSFeature.IDComparison);

            m_isosFileWriter.WriteLine(m_isosFileReader.ReadLine());

            // Read the rest of the Stream, 1 line at a time, and save the write the appropriate data into the new Isos file
            for (var i = 0; (line = m_isosFileReader.ReadLine()) != null && i + offset < msFeatureList.Count; i++)
            {
                var msFeature = msFeatureList[i + offset];
                while (msFeature.Id == previousFeatureId && i + offset < msFeatureList.Count - 1)
                {
                    offset++;
                    msFeature = msFeatureList[i + offset];
                }

                if (msFeature.Id == i)
                {
                    msFeature.FilteredIndex = index;
                    index++;
                    var columns = line.Split(',', '\t', '\n');

                    if (m_columnMap.ContainsKey("MSFeature.Mz"))
                        columns[m_columnMap["MSFeature.Mz"]] = PRISM.StringUtilities.DblToString(msFeature.Mz, 5);

                    if (m_columnMap.ContainsKey("MSFeature.MassMonoisotopic"))
                        columns[m_columnMap["MSFeature.MassMonoisotopic"]] = PRISM.StringUtilities.DblToString(msFeature.MassMonoisotopic, 5);

                    if (m_columnMap.ContainsKey("MSFeature.MassMostAbundant"))
                        columns[m_columnMap["MSFeature.MassMostAbundant"]] = PRISM.StringUtilities.DblToString(msFeature.MassMostAbundantIsotope, 5);

                    var outLine = string.Join(",", columns);
                    m_isosFileWriter.WriteLine(outLine);

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
