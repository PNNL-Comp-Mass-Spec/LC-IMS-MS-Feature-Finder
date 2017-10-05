using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FeatureFinder.Control
{
    public static class PeakWriter
    {
        private static readonly TextWriter m_writer;

        static PeakWriter()
        {
            var baseFileName = Regex.Split(Settings.InputFileName, "_isos")[0];

            var streamWriter = new StreamWriter(Path.Combine(Settings.OutputDirectory, baseFileName + "_Peaks.txt")) {AutoFlush = true};
            m_writer = streamWriter;
        }

        public static void Write(List<double> xValues, List<double> yValues1, List<double> yValues2)
        {
            for(var i = 0; i < xValues.Count; i++)
            {
                m_writer.WriteLine(xValues[i] + "\t" + yValues1[i] + "\t" + yValues2[i]);
            }

        }

        public static void CloseWriter()
        {
            m_writer.Close();
        }
    }
}
