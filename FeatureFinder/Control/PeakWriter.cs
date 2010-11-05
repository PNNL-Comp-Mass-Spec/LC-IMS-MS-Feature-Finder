using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace FeatureFinder.Control
{
	public static class PeakWriter
	{
		private static TextWriter m_textWriter;

		static PeakWriter()
		{
			String baseFileName = Regex.Split(Settings.InputFileName, "_isos")[0];

			StreamWriter streamWriter = new StreamWriter(Settings.OutputDirectory + baseFileName + "_Peaks.txt");
			streamWriter.AutoFlush = true;
			m_textWriter = streamWriter;
		}

		public static void Write(List<double> xValues, List<double> yValues1, List<double> yValues2)
		{
			StringBuilder stringBuilder = new StringBuilder();

			for(int i = 0; i < xValues.Count; i++)
			{
				stringBuilder.Append(xValues[i] + "\t" + yValues1[i] + "\t" + yValues2 + "\n");
			}

			m_textWriter.WriteLine(stringBuilder.ToString());
		}

		public static void CloseWriter()
		{
			m_textWriter.Close();
		}
	}
}
