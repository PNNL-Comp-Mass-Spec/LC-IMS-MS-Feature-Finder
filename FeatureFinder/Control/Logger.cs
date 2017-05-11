using System;
using System.IO;
using System.Text.RegularExpressions;

namespace FeatureFinder.Control
{
    public static class Logger
    {
        private static TextWriter m_textWriter;

        static Logger()
        {
            String baseFileName = Regex.Split(Settings.InputFileName, "_isos")[0];

            StreamWriter streamWriter = new StreamWriter(Settings.OutputDirectory + baseFileName + "_FeatureFinder_Log.txt") {AutoFlush = true};
            m_textWriter = streamWriter;
        }

        public static void Log(String textToLog)
        {
            DateTime currentTime = DateTime.Now;
            String logText = String.Format("{0:MM/dd/yyyy HH:mm:ss}", currentTime) + "\t" + textToLog;
            m_textWriter.WriteLine(logText);
            Console.WriteLine(logText);
        }

        public static void ChangeLogFileLocation(string fileLocation)
        {
            CloseLog();
            StreamWriter streamWriter = new StreamWriter(fileLocation) {AutoFlush = true};
            m_textWriter = streamWriter;
        }

        public static void CloseLog()
        {
            m_textWriter.Close();
        }
    }
}
