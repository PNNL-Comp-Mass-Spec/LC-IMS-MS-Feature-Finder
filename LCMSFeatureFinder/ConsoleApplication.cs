using System;
using System.Linq;
using System.Reflection;
using System.IO;
using FeatureFinder.Control;
using System.Runtime.InteropServices;
using System.Diagnostics;
using FeatureFinder.Utilities;

namespace LCMSFeatureFinder
{
    /// <summary>
    /// This program finds LC-IMS-MS features using deisotoped features from DeconTools.
    /// Required files are a DeconTools _isos.csv file plus the corresponding .UIMF file if UseConformationDetection is True
    /// </summary>
    /// <remarks>
    /// Written by Kevin Crowell for the Department of Energy (PNNL, Richland, WA)
    /// Program started in October, 2010
    ///
    /// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
    /// Website: http://omics.pnl.gov/ or http://panomics.pnnl.gov/
    /// </remarks>
    class ConsoleApplication
    {
        private const string PROGRAM_DATE = "May 10, 2017";

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

        private const int LC_DATA = 0;
        private const int IMS_DATA = 1;

        static int Main(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    PrintUsage();
                    return -1;
                }

                if (args[0].ToUpper().Equals("/X"))
                {
                    Settings.PrintExampleSettings();
                    return 0;
                }

                if (args[0].ToUpper().Equals("/Y"))
                {
                    Settings.PrintExampleDeconToolsFilterFile();
                    return 0;
                }

                var handle = Process.GetCurrentProcess().MainWindowHandle;
                SetConsoleMode(handle, ENABLE_EXTENDED_FLAGS);

                var assembly = Assembly.GetExecutingAssembly();
                var assemblyVersion = assembly.GetName().Version.ToString();

                var iniFilePath = ProcessFileLocation(args[0]);

                var iniFile = new FileInfo(iniFilePath);
                if (!iniFile.Exists)
                {
                    Logger.Log("Error: Ini file not found at " + iniFilePath);
                    return -2;
                }

                var iniReader = new IniReader(iniFile.FullName);
                iniReader.CreateSettings();

                Logger.Log("LCMSFeatureFinder Version " + assemblyVersion);
                Logger.Log("Loading settings from INI file: " + iniFile.FullName);
                Logger.Log("Data Filters - ");
                Logger.Log(" Minimum LC scan = " + Settings.ScanLCMin);
                Logger.Log(" Maximum LC scan = " + Settings.ScanLCMax);
                Logger.Log(" Minimum IMS scan = " + Settings.ScanIMSMin);
                Logger.Log(" Maximum IMS scan = " + Settings.ScanIMSMax);
                if (Settings.FilterUsingHardCodedFilters)
                {
                    Logger.Log(" Filtering using charge/abundance/fitScore/i_score table from file: " + Settings.DeconToolsFilterFileName);
                }
                else
                {
                    Logger.Log(" Maximum fit = " + Settings.FitMax);
                    Logger.Log(" Maximum i_score = " + Settings.InterferenceScoreMax);
                    Logger.Log(" Minimum intensity = " + Settings.IntensityMin);
                }
                if (Settings.FilterFlaggedData)
                {
                    Logger.Log(" Filtering out flagged data");
                }
                Logger.Log(" Mono mass start = " + Settings.MassMonoisotopicStart);
                Logger.Log(" Mono mass end = " + Settings.MassMonoisotopicEnd);

                var isosFile = GetSourceFile(Settings.InputDirectory, Settings.InputFileName);

                if (!isosFile.Exists)
                {
                    Logger.Log("Error: Isos file not found at " + isosFile.FullName);
                    return -3;
                }

                var dataType = PeekAtIsosFile(isosFile.FullName);

                if (dataType < 0)
                {
                    Logger.Log("Unknown type of Isos file. Exiting.");
                    return -4;
                }

                var uimfFile = GetSourceFile(Settings.InputDirectory, FileUtil.GetUimfFileForIsosFile(Settings.InputFileName));

                if (Settings.UseConformationDetection && !uimfFile.Exists && dataType != LC_DATA)
                {
                    Logger.Log("Error: UIMF file not found at " + uimfFile.FullName);
                    return -7;
                }

                var isosReader = new IsosReader(isosFile.FullName, Settings.OutputDirectory);

                if (dataType == LC_DATA || Settings.IgnoreIMSDriftTime)
                {
                    Logger.Log("Total number of MS Features in _isos.csv file = " + isosReader.NumOfUnfilteredMSFeatures);
                    Logger.Log("Total number of MS Features we'll consider = " + isosReader.MSFeatureList.Count);

                    Logger.Log("LC data processing is not currently implemented. Exiting...");
                    //RunLCMSFeatureFinder(isosReader);

                    Logger.Log("Aborted!");
                    return -5;

                }

                if (dataType != IMS_DATA)
                {
                    Logger.Log("Unsupported data type. Exiting...");
                    Logger.Log("Aborted!");
                    return -6;
                }

                Logger.Log("Processing LC-IMS-MS Data...");

                var success = RunLCIMSMSFeatureFinder(isosReader);

                if (success)
                {
                    Logger.Log("Finished!");
                    return 0;
                }

                Logger.Log("Processing error; check the log messages");

                return -10;
            }
            catch (Exception ex)
            {
                Logger.Log("Exception while processing: " + ex.Message);
                System.Threading.Thread.Sleep(2000);
                return -11;
            }
            finally
            {
                Logger.CloseLog();
            }
        }

        private static string GetAppVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";
        }

        private static string GetExeName()
        {
            var assemblyPath = Assembly.GetEntryAssembly().Location;
            return System.IO.Path.GetFileName(assemblyPath);
        }

        private static FileInfo GetSourceFile(string inputDirectory, string inputFileName)
        {
            if (string.IsNullOrEmpty(inputDirectory))
            {
                return new FileInfo(inputFileName);
            }

            return new FileInfo(Path.Combine(inputDirectory, inputFileName));

        }

        /// <summary>
        /// Runs the necessary steps for LC-IMS-MS Feature Finding.
        /// </summary>
        /// <param name="isosReader">The IsosReader object</param>
        private static bool RunLCIMSMSFeatureFinder(IsosReader isosReader)
        {
            try
            {
                var controller = new LCIMSMSFeatureFinderController(isosReader);
                controller.Execute();
                return true;
            }
            catch (Exception e)
            {
                Logger.Log(e.Message);
                Logger.Log(e.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Checks to see what type of data is being processed.
        /// </summary>
        /// <returns>LC_DATA if LC Data is being processed, IMS_DATA if IMS Data is being processed, -1 if error</returns>
        private static int PeekAtIsosFile(string isosFilePath)
        {
            var isosFileReader = new StreamReader(isosFilePath);

            var firstLine = isosFileReader.ReadLine();

            if (firstLine == null)
            {
                return -1;
            }

            var columnTitles = firstLine.Split('\t', ',', '\n');
            if (columnTitles.Any(column => column.Equals("ims_scan_num")))
            {
                isosFileReader.Close();
                return IMS_DATA;
            }

            isosFileReader.Close();
            return LC_DATA;
        }

        /// <summary>
        /// Formats the file location into a usable format.
        /// </summary>
        /// <param name="fileLocation">Original file location</param>
        /// <returns>Processed file location</returns>
        private static string ProcessFileLocation(string fileLocation)
        {
            // Replace all slashes to backslashes since we are working with a Windows directory
            fileLocation = fileLocation.Replace("/", "\\");

            // If the string does not contain ":\" or "\\", move on.
            if (!fileLocation.Contains(":\\") && !fileLocation.StartsWith("\\\\"))
            {
                // Append "." to the front of the string if in the form of "\blabla"
                if (fileLocation.StartsWith("\\"))
                {
                    return "." + fileLocation;
                }

                // Append ".\" to the front of the string if in the form of "blabla"
                return ".\\" + fileLocation;
            }

            // filename is in the form of "C:\blabla" or "\\blabla"
            return fileLocation;
        }

        /// <summary>
        /// Prints the correct usage of the application.
        /// </summary>
        private static void PrintUsage()
        {
            var exeName = GetExeName();

            Console.WriteLine("");
            Console.WriteLine("This program finds LC-IMS-MS features using deisotoped features from DeconTools.");
            Console.WriteLine("Required files are a DeconTools _isos.csv file plus the corresponding .UIMF file if UseConformationDetection is True");
            Console.WriteLine("");
            Console.WriteLine("Syntax: " + exeName + " SettingsFile.ini");
            Console.WriteLine();
            Console.WriteLine("The settings file defines the input file path and the output directory.");
            Console.WriteLine("It also defines a series of settings used to aid the Feature Finder.");
            Console.WriteLine();
            Console.WriteLine("To see an example settings file, use " + exeName + " /X");
            Console.WriteLine("To see an example file for parameter DeconToolsFilterFileName, use " + exeName + " /Y");
            Console.WriteLine();
            Console.WriteLine("Program written by Kevin Crowell for the Department of Energy (PNNL, Richland, WA) in 2010");
            Console.WriteLine("Version: " + GetAppVersion());
            Console.WriteLine();
            Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
            Console.WriteLine("Website: http://omics.pnl.gov/ or http://panomics.pnnl.gov/");
            Console.WriteLine();

            System.Threading.Thread.Sleep(1500);
        }
    }
}
