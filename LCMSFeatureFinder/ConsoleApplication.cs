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
			Logger.Log(" Maximum fit = " + Settings.FitMax);
			Logger.Log(" Maximum i_score = " + Settings.InterferenceScoreMax);
			Logger.Log(" Minimum intensity = " + Settings.IntensityMin);
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
			Logger.Log("Creating IMS-MS Features...");

			List<MSFeature> filteredMSFeatureList = isosReader.MSFeatureList;

			ConcurrentBag<IMSMSFeature> imsmsfeatureBag = new ConcurrentBag<IMSMSFeature>();

			if (Settings.UseCharge)
			{
				var groupByScanLCAndChargeQuery = from msFeature in filteredMSFeatureList
												  group msFeature by new { msFeature.ScanLC, msFeature.Charge } into newGroup
												  select newGroup;

				Parallel.ForEach(groupByScanLCAndChargeQuery, msFeatureGroup =>
				{
					IEnumerable<IMSMSFeature> imsmsFeatureList = ClusterMSFeatures.ClusterByMass(msFeatureGroup);

					foreach (IMSMSFeature imsmsFeature in imsmsFeatureList)
					{
						imsmsfeatureBag.Add(imsmsFeature);
					}
				});
			}
			else
			{
				var groupByScanLCQuery = from msFeature in filteredMSFeatureList
										 group msFeature by msFeature.ScanLC into newGroup
										 select newGroup;

				Parallel.ForEach(groupByScanLCQuery, msFeatureGroup =>
				{
					IEnumerable<IMSMSFeature> imsmsFeatureList = ClusterMSFeatures.ClusterByMass(msFeatureGroup);

					foreach (IMSMSFeature imsmsFeature in imsmsFeatureList)
					{
						imsmsfeatureBag.Add(imsmsFeature);
					}
				});
			}

			Logger.Log("Total Number of Unfiltered IMS-MS Features = " + imsmsfeatureBag.Count);
			//Logger.Log("Filtering out short IMS-MS Features...");

			//IEnumerable<IMSMSFeature> imsmsFeatureEnumerable = FeatureUtil.FilterByMemberCount(imsmsfeatureBag);
			//imsmsfeatureBag = null;

			//Logger.Log("Total Number of Filtered IMS-MS Features = " + imsmsFeatureEnumerable.Count());
			Logger.Log("Creating LC-IMS-MS Features...");

			ConcurrentBag<LCIMSMSFeature> lcimsmsFeatureBag = new ConcurrentBag<LCIMSMSFeature>();

			if (Settings.UseCharge)
			{
				var groupByChargeQuery = from imsmsFeature in imsmsfeatureBag
										 group imsmsFeature by imsmsFeature.Charge into newGroup
										 select newGroup;

				Parallel.ForEach(groupByChargeQuery, imsmsFeatureGroup =>
				{
					IEnumerable<LCIMSMSFeature> lcimsmsFeatureList = ClusterIMSMSFeatures.ClusterByMassAndScanLC(imsmsFeatureGroup);

					foreach (LCIMSMSFeature lcimsmsFeature in lcimsmsFeatureList)
					{
						lcimsmsFeatureBag.Add(lcimsmsFeature);
					}
				});
			}
			else
			{
				IEnumerable<LCIMSMSFeature> lcimsmsFeatureList = ClusterIMSMSFeatures.ClusterByMassAndScanLC(imsmsfeatureBag);

				foreach (LCIMSMSFeature lcimsmsFeature in lcimsmsFeatureList)
				{
					lcimsmsFeatureBag.Add(lcimsmsFeature);
				}
			}

			Logger.Log("Total Number of LC-IMS-MS Features = " + lcimsmsFeatureBag.Count);

			IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable = null;

			if (Settings.IMSDaCorrectionMax > 0)
			{
				Logger.Log("Executing Dalton Correction Algorithm on LC-IMS-MS Features...");

				ConcurrentBag<LCIMSMSFeature> daCorrectedLCIMSMSFeatureBag = new ConcurrentBag<LCIMSMSFeature>();
				ConcurrentBag<IEnumerable<LCIMSMSFeature>> lcimsmsFeatureListBag = new ConcurrentBag<IEnumerable<LCIMSMSFeature>>();

				if (Settings.UseCharge)
				{
					var groupByChargeQuery2 = from lcimsmsFeature in lcimsmsFeatureBag
											  group lcimsmsFeature by lcimsmsFeature.Charge into newGroup
											  select newGroup;

					Parallel.ForEach(groupByChargeQuery2, lcimsmsFeatureGroup =>
					{
						IEnumerable<IEnumerable<LCIMSMSFeature>> returnList = FeatureUtil.PartitionFeaturesByMass(lcimsmsFeatureGroup);

						foreach (IEnumerable<LCIMSMSFeature> lcimsmsFeatureList in returnList)
						{
							lcimsmsFeatureListBag.Add(lcimsmsFeatureList);
						}
					});
				}
				else
				{
					IEnumerable<IEnumerable<LCIMSMSFeature>> returnList = FeatureUtil.PartitionFeaturesByMass(lcimsmsFeatureBag);

					foreach (IEnumerable<LCIMSMSFeature> lcimsmsFeatureList in returnList)
					{
						lcimsmsFeatureListBag.Add(lcimsmsFeatureList);
					}
				}

				lcimsmsFeatureBag = null;

				Parallel.ForEach(lcimsmsFeatureListBag, lcimsmsFeatureGroup =>
				{
					IEnumerable<LCIMSMSFeature> lcimsmsFeatureList = DaltonCorrection.CorrectLCIMSMSFeatures(lcimsmsFeatureGroup);

					foreach (LCIMSMSFeature lcimsmsFeature in lcimsmsFeatureList)
					{
						daCorrectedLCIMSMSFeatureBag.Add(lcimsmsFeature);
					}
				});

				lcimsmsFeatureEnumerable = daCorrectedLCIMSMSFeatureBag;
				daCorrectedLCIMSMSFeatureBag = null;

				Logger.Log("Total Number of Dalton Corrected LC-IMS-MS Features = " + lcimsmsFeatureEnumerable.Count());
			}
			else
			{
				lcimsmsFeatureEnumerable = lcimsmsFeatureBag;
			}

			Logger.Log("Filtering LC-IMS-MS features based on Member Count...");
			lcimsmsFeatureEnumerable = FeatureUtil.FilterByMemberCount(lcimsmsFeatureEnumerable);
			Logger.Log("Total Number of Filtered LC-IMS-MS Features = " + lcimsmsFeatureEnumerable.Count());

			Logger.Log("Splitting LC-IMS-MS Features by LC Scan...");
			lcimsmsFeatureEnumerable = FeatureUtil.SplitLCIMSMSFeaturesByScanLC(lcimsmsFeatureEnumerable);
			lcimsmsFeatureEnumerable = FeatureUtil.FilterSingleLCScan(lcimsmsFeatureEnumerable);
			Logger.Log("New Total Number of Filtered LC-IMS-MS Features = " + lcimsmsFeatureEnumerable.Count());
			
			if (Settings.UseConformationDetection)
			{
				Logger.Log("Conformation Detection...");
				lcimsmsFeatureEnumerable = ConformationDetection.DetectConformationsUsingRawData(lcimsmsFeatureEnumerable);
				Logger.Log("New Total Number of LC-IMS-MS Features = " + lcimsmsFeatureEnumerable.Count());
			}
				
			lcimsmsFeatureEnumerable = FeatureUtil.SortByMass(lcimsmsFeatureEnumerable);

			Logger.Log("Creating filtered Isos file...");

			List<MSFeature> msFeatureListOutput = new List<MSFeature>();
			foreach (LCIMSMSFeature lcimsmsFeature in lcimsmsFeatureEnumerable)
			{
				if (Settings.FilterIsosToSinglePoint)
				{
					MSFeature msFeatureRep = lcimsmsFeature.GetMSFeatureRep();
					msFeatureListOutput.Add(msFeatureRep);
				}
				else
				{
					foreach (IMSMSFeature imsmsFeature in lcimsmsFeature.IMSMSFeatureList)
					{
						msFeatureListOutput.AddRange(imsmsFeature.MSFeatureList);
					}
				}
			}

			IsosWriter isosWriter = new IsosWriter(msFeatureListOutput, isosReader.ColumnMap);

			Logger.Log("Writing output files...");
			FeatureUtil.WriteLCIMSMSFeatureToFile(lcimsmsFeatureEnumerable);
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
