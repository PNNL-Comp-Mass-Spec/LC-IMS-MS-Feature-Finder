using System;

namespace FeatureFinder.Control
{
	public static class Settings
	{
		private static string inputDirectory;
		private static string inputFileName;
		private static string outputDirectory;

		private static short featureLengthMin;
		private static short minimumDifferenceInMedianPpmMassToSplit;
		private static short lcGapSizeMax;

		private static int imsDaCorrectionMax;
		private static int scanIMSMin;
		private static int scanIMSMax;
		private static int scanLCMin;
		private static int scanLCMax;

		private static float fitMax;
		private static float interferenceScoreMax;
		private static float intensityMin;
		private static float massMonoisotopicConstraint;
		private static float umcFitScoreMinimum;
		private static float massMonoisotopicStart;
		private static float massMonoisotopicEnd;
		private static float smoothingStDev;

		private static bool massMonoisotopicConstraintIsPPM;
		private static bool useGenericNET;
		private static bool useCharge;
		private static bool split;
		private static bool useConformationDetection;
		private static bool useConformationIndex;
		private static bool ignoreIMSDriftTime;
		private static bool filterIsosToSinglePoint;
		private static bool filterUsingHardCodedFilters;

		private static FrameType frameTypeFilter;

		public enum FrameType { NoFilter = -1, Prescan = 0, MS, MSMS }

		static Settings()
		{
			// Default Settings
			inputDirectory = "";
			inputFileName = "";
			outputDirectory = "";
			fitMax = 0.15f;
			interferenceScoreMax = 0.3f;
			intensityMin = 2500;
			scanIMSMin = 0;
			scanIMSMax = int.MaxValue;
			scanLCMin = 0;
			scanLCMax = int.MaxValue;
			massMonoisotopicStart = 0;
			massMonoisotopicEnd = 15000;
			massMonoisotopicConstraint = 20f;
			massMonoisotopicConstraintIsPPM = true;
			featureLengthMin = 3;
			useGenericNET = true;
			useCharge = false;
			lcGapSizeMax = 5;
			minimumDifferenceInMedianPpmMassToSplit = 4;
			split = true;
			imsDaCorrectionMax = 1;
			smoothingStDev = 0.25f;
			umcFitScoreMinimum = 0f;
			useConformationDetection = true;
			useConformationIndex = false;
			ignoreIMSDriftTime = false;
			filterIsosToSinglePoint = true;
			frameTypeFilter = FrameType.NoFilter;
			filterUsingHardCodedFilters = false;
		}

		public static void PrintExampleSettings()
		{
			Console.WriteLine("");
			Console.WriteLine("[Files]");
			Console.WriteLine("InputFileName=InputFile_isos.csv");
			Console.WriteLine("OutputDirectory=C:\\");
			Console.WriteLine("[DataFilters]");
			Console.WriteLine("MaxIsotopicFit=0.15");
			Console.WriteLine("MaxIScore=0.3");
			Console.WriteLine("MinimumIntensity=0");
			Console.WriteLine("UseHardCodedFilters=False");
			Console.WriteLine("IMSMinScan=0");
			Console.WriteLine("IMSMaxScan=0");
			Console.WriteLine("LCMinScan=0");
			Console.WriteLine("LCMaxScan=0");
			Console.WriteLine("MonoMassStart=0");
			Console.WriteLine("MonoMassEnd=15000");
			Console.WriteLine("FrameType=-1");
			Console.WriteLine("[UMCCreationOptions]");
			Console.WriteLine("IgnoreIMSDriftTime=False");
			Console.WriteLine("MonoMassConstraint=12");
			Console.WriteLine("MonoMassConstraintIsPPM=True");
			Console.WriteLine("UsegenericNET=True");
			Console.WriteLine("UseCharge=True");
			Console.WriteLine("MinFeatureLengthPoints=3");
			Console.WriteLine("LCGapMaxSize=4");
			Console.WriteLine("IMSMaxDaCorrection=1");
			Console.WriteLine("UMCFitScoreMinimum=0.9");
			Console.WriteLine("[UMCSplittingOptions]");
			Console.WriteLine("Split=True");
			Console.WriteLine("MinimumDifferenceInMedianPpmMassToSplit=4");
			Console.WriteLine("[DriftProfileOptions]");
			Console.WriteLine("UseConformationDetection=True");
			Console.WriteLine("UseConformationIndex=False");
			Console.WriteLine("SmoothingStDev=0.25");
			Console.WriteLine("[PostCreationFilteringOptions]");
			Console.WriteLine("FilterIsosToSinglePoint=True");
		}

		public static string InputDirectory
		{
			get { return inputDirectory; }
			set { inputDirectory = value; }
		}

		public static string InputFileName
		{
			get { return inputFileName; }
			set { inputFileName = value; }
		}

		public static string OutputDirectory
		{
			get { return outputDirectory; }
			set { outputDirectory = value; }
		}

		public static short FeatureLengthMin
		{
			get { return featureLengthMin; }
			set { featureLengthMin = value; }
		}

		public static short MinimumDifferenceInMedianPpmMassToSplit
		{
			get { return minimumDifferenceInMedianPpmMassToSplit; }
			set { minimumDifferenceInMedianPpmMassToSplit = value; }
		}

		public static short LCGapSizeMax
		{
			get { return lcGapSizeMax; }
			set { lcGapSizeMax = value; }
		}

		public static int ScanIMSMin
		{
			get { return scanIMSMin; }
			set { scanIMSMin = value; }
		}

		public static int ScanIMSMax
		{
			get { return scanIMSMax; }
			set { scanIMSMax = value; }
		}

		public static int ScanLCMin
		{
			get { return scanLCMin; }
			set { scanLCMin = value; }
		}

		public static int ScanLCMax
		{
			get { return scanLCMax; }
			set { scanLCMax = value; }
		}

		public static int IMSDaCorrectionMax
		{
			get { return imsDaCorrectionMax; }
			set { imsDaCorrectionMax = value; }
		}

		public static float FitMax
		{
			get { return fitMax; }
			set { fitMax = value; }
		}

		public static float InterferenceScoreMax
		{
			get { return interferenceScoreMax; }
			set { interferenceScoreMax = value; }
		}

		public static float IntensityMin
		{
			get { return intensityMin; }
			set { intensityMin = value; }
		}

		public static float MassMonoisotopicConstraint
		{
			get { return massMonoisotopicConstraint; }
			set { massMonoisotopicConstraint = value; }
		}

		public static float UMCFitScoreMinimum
		{
			get { return umcFitScoreMinimum; }
			set { umcFitScoreMinimum = value; }
		}

		public static float MassMonoisotopicStart
		{
			get { return massMonoisotopicStart; }
			set { massMonoisotopicStart = value; }
		}

		public static float MassMonoisotopicEnd
		{
			get { return massMonoisotopicEnd; }
			set { massMonoisotopicEnd = value; }
		}

		public static float SmoothingStDev
        {
            get { return smoothingStDev; }
            set { smoothingStDev = value; }
        }

		public static bool MassMonoisotopicConstraintIsPPM
		{
			get { return massMonoisotopicConstraintIsPPM; }
			set { massMonoisotopicConstraintIsPPM = value; }
		}

		public static bool UseGenericNET
		{
			get { return useGenericNET; }
			set { useGenericNET = value; }
		}

		public static bool UseCharge
		{
			get { return useCharge; }
			set { useCharge = value; }
		}

		public static bool Split
		{
			get { return split; }
			set { split = value; }
		}

		public static bool UseConformationDetection
		{
			get { return useConformationDetection; }
			set { useConformationDetection = value; }
		}

		public static bool UseConformationIndex
		{
			get { return useConformationIndex; }
			set { useConformationIndex = value; }
		}

		public static bool IgnoreIMSDriftTime
		{
			get { return ignoreIMSDriftTime; }
			set { ignoreIMSDriftTime = value; }
		}

		public static bool FilterIsosToSinglePoint
		{
			get { return filterIsosToSinglePoint; }
			set { filterIsosToSinglePoint = value; }
		}

		public static bool FilterUsingHardCodedFilters
		{
			get { return filterUsingHardCodedFilters; }
			set { filterUsingHardCodedFilters = value; }
		}

		public static FrameType FrameTypeFilter
		{
			get { return frameTypeFilter; }
			set { frameTypeFilter = value; }
		}
	}
}
