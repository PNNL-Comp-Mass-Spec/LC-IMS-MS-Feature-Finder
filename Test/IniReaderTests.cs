using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FeatureFinder.Control;

namespace Test
{
    [TestFixture]
    public class IniReaderTests
    {
        [Test]
        public void deconToolsFiltersAreBeingReadIn_test1()
        {
            string testfile = @"\\protoapps\UserData\Slysz\DeconTools_TestFiles\LCMSFeatureFinder\DeconToolsFilterFiles\FF_IMS_UseHardCodedFilters_NoFlags_20ppm_Min3Pts_12MaxLCGap_NoDaCorr_NoConfDtn_2011-03-21.ini";

            IniReader iniReader = new IniReader(testfile);
            iniReader.CreateSettings();

            Assert.AreEqual(12, Settings.DeconToolsFilterList.Count);
            

        }

        [Test]
        public void deconToolsfiltersAreBeingAppliedTest()
        {
            string testfile = @"\\protoapps\UserData\Slysz\DeconTools_TestFiles\LCMSFeatureFinder\DeconToolsFilterFiles\FF_IMS_UseHardCodedFilters_NoFlags_20ppm_Min3Pts_12MaxLCGap_NoDaCorr_NoConfDtn_2011-03-21.ini";

            IniReader iniReader = new IniReader(testfile);
            iniReader.CreateSettings();

            IsosReader isosReader = new IsosReader();

            LCIMSMSFeatureFinderController controller = new LCIMSMSFeatureFinderController(isosReader);
            controller.Execute();
	


        }




    }
}
