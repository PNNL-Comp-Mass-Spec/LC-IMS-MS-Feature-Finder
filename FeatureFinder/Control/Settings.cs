using System;
using System.Collections.Generic;
using UIMFLibrary;

namespace FeatureFinder.Control
{
    public static class Settings
    {
        public enum FrameType
        {
            NoFilter = -1,
            Prescan = 0,
            MS,
            MSMS
        }

        public static string InputDirectory { get; set; }
        public static string InputFileName { get; set; }
        public static string OutputDirectory { get; set; }

        /// <summary>
        /// Tab-delimited text file with filter specs
        /// Columns are: chargeMin	chargeMax	abundanceMin	abundanceMax	iscoreCutoff	fitScoreCutoff
        /// </summary>
        public static string DeconToolsFilterFileName { get; set; }

        /// <summary>
        /// Minimum number of points that a feature must contain
        /// </summary>
        public static short FeatureLengthMin { get; set; }

        // Obsolete (not implemented)
        // public static bool Split { get; set; }
        // public static short MinimumDifferenceInMedianPpmMassToSplit { get; set; }

        public static short LCGapSizeMax { get; set; }

        public static int ScanIMSMin { get; set; }
        public static int ScanIMSMax { get; set; }
        public static int ScanLCMin { get; set; }
        public static int ScanLCMax { get; set; }
        public static int IMSDaCorrectionMax { get; set; }

        public static float FitMax { get; set; }
        public static float InterferenceScoreMax { get; set; }
        public static float IntensityMin { get; set; }

        /// <summary>
        /// Mass tolerance for grouping data into features, in ppm
        /// </summary>
        public static float MassMonoisotopicConstraint { get; set; }

        public static float MassMonoisotopicStart { get; set; }
        public static float MassMonoisotopicEnd { get; set; }
        public static float SmoothingStDev { get; set; }

        public static bool UseCharge { get; set; }
        public static bool UseConformationDetection { get; set; }
        public static bool IgnoreIMSDriftTime { get; set; }

        /// <summary>
        /// When true, make a filtered isos.csv file with one point per LC-IMS-MS feature
        /// </summary>
        public static bool FilterIsosToSinglePoint { get; set; }

        /// <summary>
        /// When true, load filters from the file defined by DeconToolsFilterFileName
        /// </summary>
        public static bool FilterUsingHardCodedFilters { get; set; }

        /// <summary>
        /// When true, could theoretically skip data points with 1 in the "Flag" column of the _isos.csv file
        /// However, this option is not implemented
        /// </summary>
        public static bool FilterFlaggedData { get; set; }

        public static UIMFData.FrameType FrameTypeFilter { get; set; }
        public static List<DeconToolsFilter> DeconToolsFilterList { get; set; }

        static Settings()
        {
            // Default Settings
            InputDirectory = string.Empty;
            InputFileName = string.Empty;
            OutputDirectory = string.Empty;
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
            FeatureLengthMin = 3;
            UseCharge = false;
            LCGapSizeMax = 5;

            // Obsolete (not implemented)
            // MinimumDifferenceInMedianPpmMassToSplit = 4;
            // Split = true;
            IMSDaCorrectionMax = 0;
            SmoothingStDev = 2f;
            UseConformationDetection = true;
            IgnoreIMSDriftTime = false;
            FilterIsosToSinglePoint = true;
            FrameTypeFilter = UIMFData.FrameType.Calibration;
            FilterUsingHardCodedFilters = false;
            FilterFlaggedData = false;
        }

        public static void PrintExampleSettings()
        {
            Console.WriteLine();
            Console.WriteLine("[Files]");
            Console.WriteLine("InputFileName=InputFile_isos.csv");
            Console.WriteLine("OutputDirectory=C:\\");
            Console.WriteLine("; The following defines a file with custom IScore and IsotopicFit score cutoffs for various combos of charge and abundance");
            Console.WriteLine("DeconToolsFilterFileName=");

            Console.WriteLine("[DataFilters]");
            Console.WriteLine("; If UseHardCodedFilters is True, IsotopicFit and IScore filters in the file");
            Console.WriteLine("; specified by DeconToolsFilterFileName will override MaxIsotopicFit and MaxIScore");
            Console.WriteLine("; based on charge and intensity of the given data point");
            Console.WriteLine(";");
            Console.WriteLine("; Maximum isotopic fit");
            Console.WriteLine("MaxIsotopicFit=0.15");
            Console.WriteLine("; Maximum interference score");
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
            Console.WriteLine("; Monoisotopic mass tolerance for grouping data, in ppm");
            Console.WriteLine("MonoMassConstraint=20");
            Console.WriteLine("UseCharge=True");
            Console.WriteLine("MinFeatureLengthPoints=3");
            Console.WriteLine("LCGapMaxSize=4");
            Console.WriteLine("IMSMaxDaCorrection=1");

            // Obsolete (not implemented)
            // Console.WriteLine("[UMCSplittingOptions]");
            // Console.WriteLine("Split=True");
            // Console.WriteLine("MinimumDifferenceInMedianPpmMassToSplit=4");

            Console.WriteLine("[DriftProfileOptions]");
            Console.WriteLine("UseConformationDetection=True");
            Console.WriteLine("SmoothingStDev=1.5");

            Console.WriteLine("[PostCreationFilteringOptions]");
            Console.WriteLine("FilterIsosToSinglePoint=True");
        }

        public static void PrintExampleDeconToolsFilterFile()
        {
            Console.WriteLine();
            Console.WriteLine("Data in this file must be tab-delimited");
            Console.WriteLine();
            Console.WriteLine("chargeMin	chargeMax	abundanceMin	abundanceMax	iscoreCutoff	fitScoreCutoff");
            Console.WriteLine("1	1	500	1000	0	0.3");
            Console.WriteLine("1	1	1000	2000	0	0.3");
            Console.WriteLine("1	1	2000	8000	0	0.2");
            Console.WriteLine("1	1	8000	20000	0	0.15");
            Console.WriteLine("1	1	20000	300000	0.4	0.1");
            Console.WriteLine("1	1	300000	2147483647	0.4	0.35");
            Console.WriteLine("2	1000	500	1000	0	0.3");
            Console.WriteLine("2	1000	1000	2000	0.4	0.3");
            Console.WriteLine("2	1000	2000	8000	0.4	0.2");
            Console.WriteLine("2	1000	8000	20000	0.4	0.15");
            Console.WriteLine("2	1000	20000	300000	0.4	0.1");
            Console.WriteLine("2	1000	300000	2147483647	0.4	0.35");
        }
    }
}
