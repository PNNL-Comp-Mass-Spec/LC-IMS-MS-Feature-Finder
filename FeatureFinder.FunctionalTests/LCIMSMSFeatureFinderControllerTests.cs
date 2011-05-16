using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FeatureFinder.Control;

namespace Test
{
    [TestFixture]
    public class LCIMSMSFeatureFinderControllerTests
    {
        [Test]
        public void standardFile_no_conformationDetection_test1()
        {

            string testfile = @"C:\Users\d3x720\Documents\PNNL\My_DataAnalysis\Standard_Testing\LCMSFeatureFinder\UIMF\Parameter_Files\FF_IMS_UseHardCodedFilters_NoFlags_20ppm_Min3Pts_4MaxLCGap_NoDaCorr_NoConfDtn_2011-03-21.ini";

            IniReader iniReader = new IniReader(testfile);
            iniReader.CreateSettings();

            IsosReader isosReader = new IsosReader();

            LCIMSMSFeatureFinderController controller = new LCIMSMSFeatureFinderController(isosReader);
            controller.Execute();
	
        }

        [Test]
        public void standardFile_conformationDetection_test1()
        {

            string testfile = @"\\protoapps\UserData\Slysz\Standard_Testing\LCMSFeatureFinder\UIMF\Parameter_Files\FF_IMS_UseHardCodedFilters_NoFlags_20ppm_Min3Pts_4MaxLCGap_NoDaCorr_ConfDtn_2011-03-21.ini";

            IniReader iniReader = new IniReader(testfile);
            iniReader.CreateSettings();

            IsosReader isosReader = new IsosReader();

            LCIMSMSFeatureFinderController controller = new LCIMSMSFeatureFinderController(isosReader);
            controller.Execute();

        }

    }
}
