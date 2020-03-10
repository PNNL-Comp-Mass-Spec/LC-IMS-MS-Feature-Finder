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
            var baseFileName = Regex.Split(Settings.InputFileName, "_isos")[0];

            var streamWriter = new StreamWriter(Path.Combine(Settings.OutputDirectory, baseFileName + "_FeatureFinder_Log.txt")) { AutoFlush = true };
            m_textWriter = streamWriter;
        }

        public static void Log(string textToLog)
        {
            var currentTime = DateTime.Now;
            var textWithTimestamp = string.Format("{0:yyyy-MM-dd HH:mm:ss}", currentTime) + "\t" + textToLog;

            Console.WriteLine(textWithTimestamp);
            m_textWriter.WriteLine(textWithTimestamp);
        }

        public static void LogError(string errorMessage, Exception ex = null)
        {
            string textToLog;
            if (ex == null)
                textToLog = errorMessage;
            else
                textToLog = errorMessage + ": " + ex.Message;

            var currentTime = DateTime.Now;
            var textWithTimestamp = string.Format("{0:yyyy-MM-dd HH:mm:ss}", currentTime) + "\t" + textToLog;

            PRISM.ConsoleMsgUtils.ShowError(textWithTimestamp, ex);
            m_textWriter.WriteLine(textWithTimestamp);
        }

        [Obsolete("Unused")]
        public static void ChangeLogFileLocation(string fileLocation)
        {
            CloseLog();
            var streamWriter = new StreamWriter(fileLocation) { AutoFlush = true };
            m_textWriter = streamWriter;
        }

        public static void CloseLog()
        {
            m_textWriter?.Close();
        }
    }
}
