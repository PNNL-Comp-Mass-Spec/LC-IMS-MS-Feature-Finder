using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FeatureFinder.Data;
using FeatureFinder.Utilities;
using FeatureFinder.Control;

namespace Test
{
    [TestFixture]
    public class DeconToolsFilterTests
    {
        [Test]
        public void Test1()
        {
             string testFilterFile1 = @"\\protoapps\UserData\Slysz\DeconTools_TestFiles\LCMSFeatureFinder\DeconToolsFilterFiles\testFilterFile1.txt";
            DeconToolsFilterLoader loader = new DeconToolsFilterLoader(testFilterFile1);
            loader.DisplayFilters();

            MSFeature testfeature1 = new MSFeature();
            testfeature1.Abundance = 16117;
            testfeature1.DriftTime = 20.584f;
            testfeature1.DriftTimeUncorrected = 20.584f;
            testfeature1.Fit = 0.0264f;
            testfeature1.Fwhm = 0.0689f;
            testfeature1.InterferenceScore = 0;
            testfeature1.MassMonoisotopic = 2902.229;
            testfeature1.Mz = 726.5646;
            testfeature1.ScanIMS = 126;
            testfeature1.ScanLC = 384;
            testfeature1.Charge = 4;


            bool isValid =   DeconToolsFilterUtil.IsValidMSFeature(testfeature1, loader.DeconToolsFilterList);

            Assert.IsTrue(isValid);

            //384,126,4,16117,726.5646,0.0264,2904.08709,2902.229,2903.23217,0.0689,55.2,9527,14254,0,0,20.584,,0

        }

    }
}
