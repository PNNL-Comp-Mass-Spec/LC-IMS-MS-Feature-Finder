using System;
using System.Collections.Generic;
using UIMFLibrary;

namespace FeatureFinder.Control
{
    public static class Settings
    {
        public enum FrameType { NoFilter = -1, Prescan = 0, MS, MSMS }

        public static string InputDirectory { get; set; }
        public static string InputFileName { get; set; }
        public static string OutputDirectory { get; set; }
        public static string DeconToolsFilterFileName { get; set; }

        public static short FeatureLengthMin { get; set; }
        public static short MinimumDifferenceInMedianPpmMassToSplit { get; set; }
        public static short LCGapSizeMax { get; set; }

        public static int ScanIMSMin { get; set; }
        public static int ScanIMSMax { get; set; }
        public static int ScanLCMin { get; set; }
        public static int ScanLCMax { get; set; }
        public static int IMSDaCorrectionMax { get; set; }

        public static float FitMax { get; set; }
        public static float InterferenceScoreMax { get; set; }
        public static float IntensityMin { get; set; }
        public static float MassMonoisotopicConstraint { get; set; }
        public static float UMCFitScoreMinimum { get; set; }
        public static float MassMonoisotopicStart { get; set; }
        public static float MassMonoisotopicEnd { get; set; }
        public static float SmoothingStDev { get; set; }

        public static bool MassMonoisotopicConstraintIsPPM { get; set; }
        public static bool UseGenericNET { get; set; }
        public static bool UseCharge { get; set; }
        public static bool Split { get; set; }
        public static bool UseConformationDetection { get; set; }
        public static bool IgnoreIMSDriftTime { get; set; }
        public static bool FilterIsosToSinglePoint { get; set; }
        public static bool FilterUsingHardCodedFilters { get; set; }
        public static bool FilterFlaggedData { get; set; }

        public static DataReader.FrameType FrameTypeFilter { get; set; }
        public static List<DeconToolsFilter> DeconToolsFilterList { get; set; }

        static Settings()
        {
            // Default Settings
            InputDirectory = "";
            InputFileName = "";
            OutputDirectory = "";
            FitMax = 0.15f;
            InterferenceScoreMax = 0.3f;
            IntensityMin = 500;
            ScanIMSMin = 0;
            ScanIMSMax = int.MaxValue;
            ScanLCMin = 0;
            ScanLCMax = int.MaxValue;
            MassMonoisotopicStart = 0;
            MassMonoisotopicEnd = 15000;
            MassMonoisotopicConstraint = 20f;
            MassMonoisotopicConstraintIsPPM = true;
            FeatureLengthMin = 3;
            UseGenericNET = true;
            UseCharge = false;
            LCGapSizeMax = 5;
            MinimumDifferenceInMedianPpmMassToSplit = 4;
            Split = true;
            IMSDaCorrectionMax = 0;
            SmoothingStDev = 2f;
            UMCFitScoreMinimum = 0f;
            UseConformationDetection = true;
            IgnoreIMSDriftTime = false;
            FilterIsosToSinglePoint = true;
            FrameTypeFilter = DataReader.FrameType.Calibration;
            FilterUsingHardCodedFilters = false;
            FilterFlaggedData = false;
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
            Console.WriteLine("FilterFlaggedData=False");
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
            Console.WriteLine("SmoothingStDev=1.5");
            Console.WriteLine("[PostCreationFilteringOptions]");
            Console.WriteLine("FilterIsosToSinglePoint=True");
        }
    }
}
