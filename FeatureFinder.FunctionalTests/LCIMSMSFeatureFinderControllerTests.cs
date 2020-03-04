using FeatureFinder.Control;
using NUnit.Framework;

namespace FeatureFinder.FunctionalTests
{
    [TestFixture]
    public class LCIMSMSFeatureFinderControllerTests
    {
        [Test]
        [Ignore("Missing_File")]
        public void standardFile_no_conformationDetection_test1()
        {

            var testfile = @"\\protoapps\UserData\Slysz\Standard_Testing\LCMSFeatureFinder\UIMF\Parameter_Files\FF_IMS_UseHardCodedFilters_NoFlags_20ppm_Min3Pts_4MaxLCGap_NoDaCorr_NoConfDtn_2011-03-21.ini";

            var iniReader = new IniReader(testfile);
            iniReader.UpdateSettings();

            var isosReader = new IsosReader(Settings.InputFileName, Settings.OutputDirectory);

            var controller = new LCIMSMSFeatureFinderController(isosReader);
            controller.Execute();

        }

        [Test]
        [Ignore("Missing_File")]
        public void standardFile_conformationDetection_test1()
        {

            var testfile = @"\\protoapps\UserData\Slysz\Standard_Testing\LCMSFeatureFinder\UIMF\Parameter_Files\FF_IMS_UseHardCodedFilters_NoFlags_20ppm_Min3Pts_4MaxLCGap_NoDaCorr_ConfDtn_2011-03-21.ini";

            var iniReader = new IniReader(testfile);
            iniReader.UpdateSettings();

            var isosReader = new IsosReader(Settings.InputFileName, Settings.OutputDirectory);

            var controller = new LCIMSMSFeatureFinderController(isosReader);
            controller.Execute();

        }

    }
}
