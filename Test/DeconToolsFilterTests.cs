using System;
using System.Reflection;
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
        [TestCase("DeconToolsIsosFilters_IMS4_2011-04-28.txt")]
        public void test1(string settingsFileName)
        {
            var methodName = MethodBase.GetCurrentMethod().Name;

            var deconToolsParamFile = Test.GetTestFile(methodName, settingsFileName);

            Console.WriteLine("Reading " + deconToolsParamFile.FullName);

            var loader = new DeconToolsFilterLoader(deconToolsParamFile.FullName);
            loader.DisplayFilters();

            var testFeature1 = new MSFeature
            {
                Abundance = 16117,
                DriftTime = 20.584f,
                Fit = 0.0264f,
                Fwhm = 0.0689f,
                InterferenceScore = 0,
                MassMonoisotopic = 2902.229,
                Mz = 726.5646,
                ScanIMS = 126,
                ScanLC = 384,
                Charge = 4
            };


            var isValid = DeconToolsFilterUtil.IsValidMSFeature(testFeature1, loader.DeconToolsFilterList);

            Assert.IsTrue(isValid);

            //384,126,4,16117,726.5646,0.0264,2904.08709,2902.229,2903.23217,0.0689,55.2,9527,14254,0,0,20.584,,0

        }

    }
}
