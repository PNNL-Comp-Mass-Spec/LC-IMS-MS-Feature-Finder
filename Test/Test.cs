using System;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using FeatureFinder.Control;

namespace Test
{
    public class Test
    {
        public const string UNIT_TEST_FOLDER = @"\\proto-2\UnitTest_Files\DeconTools_TestFiles\LCMSFeatureFinder";

        public static FileInfo GetTestFile(string methodName, string fileToFind)
        {
            var testFolderPath = "";
            if (!File.Exists(fileToFind) &&
                Directory.Exists(UNIT_TEST_FOLDER))
                testFolderPath = UNIT_TEST_FOLDER;

            var fileInfo = new FileInfo(Path.Combine(testFolderPath, fileToFind));
            if (fileInfo.Exists)
                return fileInfo;

            Assert.Ignore("Ignoring test {0} since file not found: {1}", methodName, fileInfo.FullName);

            return null;

        }

        [Test]
        public void PrintExampleSettings()
        {
            Settings.PrintExampleSettings();
        }

        [Test]
        [TestCase("FF_IMS4Filters_NoFlags_20ppm_Min3Pts_4MaxLCGap_NoDaCorr_ConfDtn.ini")]
        public void RunFeatureFinder(string settingsFileName)
        {
            var methodName = MethodBase.GetCurrentMethod().Name;

            // This Ini file references file Sarc_MS2_90_6Apr11_Cheetah_11-02-19_Excerpt_isos.csv

            var iniFile = GetTestFile(methodName, settingsFileName);

            if (!iniFile.Exists)
                Assert.Ignore("Skipping test " + methodName + " since file not found: " + iniFile.FullName);

            Console.WriteLine("Reading settings in " + iniFile.FullName);
            var iniReader = new IniReader(iniFile.FullName);
            iniReader.CreateSettings();

            if (string.IsNullOrWhiteSpace(Settings.InputDirectory) && iniFile.Directory != null)
                Settings.InputDirectory = iniFile.Directory.FullName;

            if (!string.IsNullOrWhiteSpace(Settings.DeconToolsFilterFileName) && !File.Exists(Settings.DeconToolsFilterFileName) && iniFile.Directory != null)
            {
                var updatedPath = Path.Combine(iniFile.Directory.FullName, Path.GetFileName(Settings.DeconToolsFilterFileName));
                Settings.DeconToolsFilterFileName = updatedPath;
            }

            var isosFilePath = Path.Combine(Settings.InputDirectory, Settings.InputFileName);

            var isosFile = new FileInfo(isosFilePath);
            if (!isosFile.Exists)
                Assert.Ignore("Skipping test " + methodName + " since file not found: " + isosFile.FullName);

            if (isosFile.Directory == null)
                Assert.Ignore("Skipping test " + methodName + " since cannot determine the parent folder of: " + isosFile.FullName);

            Console.WriteLine("Processing " + isosFile.FullName);

            if (string.IsNullOrWhiteSpace(Settings.OutputDirectory))
                Settings.OutputDirectory = isosFile.Directory.FullName;

            Console.WriteLine("Writing results to " + Settings.OutputDirectory);

            var isosReader = new IsosReader(isosFile.FullName, Settings.OutputDirectory);

            var featuresFilePath = Path.Combine(Settings.OutputDirectory, isosFile.Name.Replace("_isos.csv", "_LCMSFeatures.txt"));
            var filteredIsosFilePath = Path.Combine(Settings.OutputDirectory, isosFile.Name.Replace("_isos.csv", "_Filtered_isos.csv"));

            var featuresFile = new FileInfo(featuresFilePath);
            var filteredIsos = new FileInfo(filteredIsosFilePath);

            if (!featuresFile.Exists)
                featuresFile.Delete();

            if (!filteredIsos.Exists)
                filteredIsos.Delete();

            var controller = new LCIMSMSFeatureFinderController(isosReader);
            controller.Execute();

            featuresFile.Refresh();
            filteredIsos.Refresh();

            if (!featuresFile.Exists)
                Assert.Fail("LCMSFeatures file not found at " + featuresFile.FullName);

            if (!filteredIsos.Exists)
                Assert.Fail("_Filtered_isos.csv file not found at " + filteredIsos.FullName);

            var lcmsFeatureCount = 0;
            var filteredIsosCount = 0;

            var lcmsFeaturesHeaderLine = string.Empty;
            var filteredIsosHeaderLine = string.Empty;

            using (var featuresFileReader = new StreamReader(new FileStream(featuresFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                if (!featuresFileReader.EndOfStream)
                    lcmsFeaturesHeaderLine = featuresFileReader.ReadLine();

                while (!featuresFileReader.EndOfStream)
                {
                    var dataLine = featuresFileReader.ReadLine();
                    if (string.IsNullOrWhiteSpace(dataLine))
                        continue;

                    lcmsFeatureCount++;
                }

                Console.WriteLine("{0} data lines in {1}", lcmsFeatureCount, featuresFile.Name);
            }

            using (var filteredIsosReader = new StreamReader(new FileStream(filteredIsos.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                if (!filteredIsosReader.EndOfStream)
                    filteredIsosHeaderLine = filteredIsosReader.ReadLine();

                while (!filteredIsosReader.EndOfStream)
                {
                    var dataLine = filteredIsosReader.ReadLine();

                    if (string.IsNullOrWhiteSpace(dataLine))
                        continue;

                    filteredIsosCount++;
                }

                Console.WriteLine("{0} data lines in {1}", filteredIsosCount, filteredIsos.Name);
            }


            Assert.AreEqual(lcmsFeatureCount, filteredIsosCount, "Mismatch between number of LC-IMS-MS features and Filtered_Isos data points");

            Assert.AreEqual(2046, lcmsFeatureCount, "Unexpected number of LC-IMS-features");

            if (lcmsFeaturesHeaderLine == null || !lcmsFeaturesHeaderLine.StartsWith("Feature_Index"))
                Assert.Fail("LCMSFeatures file header line does not start with Feature_Index");

            if (filteredIsosHeaderLine == null || !filteredIsosHeaderLine.StartsWith("msfeature_id"))
                Assert.Fail("LCMSFeatures file header line does not start with msfeature_id");

        }
    }
}
