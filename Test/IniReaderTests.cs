using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using FeatureFinder.Control;

namespace Test
{
    [TestFixture]
    public class IniReaderTests
    {
        [Test]
        [TestCase("FF_IMS4Filters_NoFlags_20ppm_Min3Pts_4MaxLCGap_NoDaCorr_ConfDtn_2011-05-16.ini")]
        public void deconToolsFiltersAreBeingReadIn_test1(string settingsFileName)
        {
            var methodName = MethodBase.GetCurrentMethod().Name;

            var iniFile = Test.GetTestFile(methodName, settingsFileName);

            if (!iniFile.Exists)
                Assert.Ignore("Skipping test " + methodName + " since file not found: " + iniFile.FullName);

            Console.WriteLine("Reading settings in " + iniFile.FullName);
            var iniReader = new IniReader(iniFile.FullName);
            iniReader.CreateSettings();

            Assert.AreEqual(12, Settings.DeconToolsFilterList.Count);

        }

        [Test]
        [TestCase("FF_IMS4Filters_NoFlags_20ppm_Min3Pts_4MaxLCGap_NoDaCorr_ConfDtn_2011-05-16.ini")]
        [Category("Long_Running")]
        public void deconToolsfiltersAreBeingAppliedTest(string settingsFileName)
        {
            var methodName = MethodBase.GetCurrentMethod().Name;

            // FF_IMS4Filters_NoFlags_20ppm_Min3Pts_4MaxLCGap_NoDaCorr_ConfDtn_2011-05-16.ini references
            // Sarc_MS2_90_6Apr11_Cheetah_11-02-19_inverse_isos.csv and DeconToolsIsosFilters_IMS4_2011-04-28.txt in folder
            // \\proto-2\UnitTest_Files\DeconTools_TestFiles\LCMSFeatureFinder

            var iniFile = Test.GetTestFile(methodName, settingsFileName);

            if (!iniFile.Exists)
                Assert.Ignore("Skipping test " + methodName + " since file not found: " + iniFile.FullName);

            Console.WriteLine("Reading settings in " + iniFile.FullName);
            var iniReader = new IniReader(iniFile.FullName);
            iniReader.CreateSettings();

            if (string.IsNullOrWhiteSpace(Settings.InputDirectory) && iniFile.Directory != null)
                Settings.InputDirectory = iniFile.Directory.FullName;

            var isosFilePath = Path.Combine(Settings.InputDirectory, Settings.InputFileName);

            var isosFile = new FileInfo(isosFilePath);
            if (!isosFile.Exists)
                Assert.Ignore("Skipping test " + methodName + " since file not found: " + isosFile.FullName);

            var isosReader = new IsosReader(isosFilePath, Settings.OutputDirectory);

            var controller = new LCIMSMSFeatureFinderController(isosReader);
            controller.Execute();

        }

    }
}
