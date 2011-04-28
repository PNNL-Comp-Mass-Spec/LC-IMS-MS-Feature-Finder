using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using FeatureFinder.Control;
using FeatureFinder.Algorithms;
using FeatureFinder.Data;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FeatureFinder.Utilities;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace LCMSFeatureFinder
{
	class ConsoleApplication
	{
		[DllImport("kernel32.dll")]
		public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

		private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

		private const int LC_DATA = 0;
		private const int IMS_DATA = 1;

		static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				PrintUsage();
				return;
			}

			if (args[0].ToUpper().Equals("/X"))
			{
				Settings.PrintExampleSettings();
				return;
			}

			IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
			SetConsoleMode(handle, ENABLE_EXTENDED_FLAGS);

			Assembly assembly = Assembly.GetExecutingAssembly();
			string assemblyVersion = assembly.GetName().Version.ToString();

			String iniFile = ProcessFileLocation(args[0]);

			IniReader iniReader = new IniReader(iniFile);
			iniReader.CreateSettings();

			Logger.Log("LCMSFeatureFinder Version " + assemblyVersion);
			Logger.Log("Loading settings from INI file: " + iniFile);
			Logger.Log("Data Filters - ");
			Logger.Log(" Minimum LC scan = " + Settings.ScanLCMin);
			Logger.Log(" Maximum LC scan = " + Settings.ScanLCMax);
			Logger.Log(" Minimum IMS scan = " + Settings.ScanIMSMin);
			Logger.Log(" Maximum IMS scan = " + Settings.ScanIMSMax);
			if (Settings.FilterUsingHardCodedFilters)
			{
				Logger.Log(" Filtering using charge/abundance/fitScore/i_score table from file:" + Settings.DeconToolsFilterFileName);
                
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

			int dataType = PeekAtIsosFile();

			if (dataType < 0)
			{
				Logger.Log("Unknown type of Isos file. Exiting.");
				return;
			}
			else
			{
				IsosReader isosReader = new IsosReader();
				Logger.Log("Total number of MS Features in _isos.csv file = " + isosReader.NumOfUnfilteredMSFeatures);
				Logger.Log("Total number of MS Features we'll consider = " + isosReader.MSFeatureList.Count);

				if (dataType == LC_DATA || Settings.IgnoreIMSDriftTime)
				{
					Logger.Log("Processing LC-MS Data...");
					Logger.Log("Currently not implemented. Exiting...");
					//RunLCMSFeatureFinder(isosReader);
				}
				else if (dataType == IMS_DATA)
				{
					Logger.Log("Processing LC-IMS-MS Data...");

					//DataReader uimfReader = null;

					/*
					if (settings.UseConformationDetection)
					{
						uimfReader = new UIMFLibrary.DataReader();
						if (!uimfReader.OpenUIMF(Settings.InputDirectory + Settings.InputFileName.Replace("_isos.csv", ".uimf")))
						{
							throw new FileNotFoundException("Could not find file '" + Settings.InputDirectory + Settings.InputFileName.Replace("_isos.csv", ".uimf") + "'.");
						}
						Logger.Log("UIMF file has been opened.");
					}
					*/ 

					//RunLCIMSMSFeatureFinder(isosReader, uimfReader);
					RunLCIMSMSFeatureFinder(isosReader);
				}

				Logger.Log("Finished!");
				Logger.CloseLog();
			}
		}

		/// <summary>
		/// Runs the necessary steps for LC-IMS-MS Feature Finding.
		/// </summary>
		/// <param name="isosReader">The IsosReader object</param>
		private static void RunLCIMSMSFeatureFinder(IsosReader isosReader)
		{
            LCIMSMSFeatureFinderController controller = new LCIMSMSFeatureFinderController(isosReader);
            controller.Execute();
		}

		/// <summary>
		/// Checks to see what type of data is being processed.
		/// </summary>
		/// <returns>LC_DATA if LC Data is being processed, IMS_DATA if IMS Data is being processed, -1 if error</returns>
		private static int PeekAtIsosFile()
		{
			String baseFileName = Regex.Split(Settings.InputFileName, "_isos")[0];
			StreamReader isosFileReader = new StreamReader(Settings.InputDirectory + Settings.InputFileName);

			String firstLine = isosFileReader.ReadLine();

			if (firstLine == null)
			{
				return -1;
			}

			String[] columnTitles = firstLine.Split('\t', ',', '\n');
			foreach (String column in columnTitles)
			{
				if (column.Equals("ims_scan_num"))
				{
					isosFileReader.Close();
					return IMS_DATA;
				}
			}

			isosFileReader.Close();
			return LC_DATA;
		}

		/// <summary>
		/// Formats the file location into a usable format.
		/// </summary>
		/// <param name="filename">Original file location</param>
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
				else
				{
					return ".\\" + fileLocation;
				}
			}

			// filename is in the form of "C:\blabla" or "\\blabla"
			return fileLocation;
		}

		/// <summary>
		/// Prints the correct usage of the application.
		/// </summary>
		private static void PrintUsage()
		{
			Console.WriteLine("");
			Console.WriteLine("Syntax: LCMSFeatureFinder.exe SettingsFile.ini\n");
			Console.WriteLine("The settings file defines the input file path and the outputdirectory.");
			Console.WriteLine("It also defines a series of settings used to aid the Feature Finder.\n");
			Console.WriteLine("To see an example settings file, use LCMSFeatureFinder.exe /X");
		}
	}
}
