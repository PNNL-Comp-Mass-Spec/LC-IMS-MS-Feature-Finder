using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using FeatureFinder.Utilities;
using FeatureFinder.Control;

namespace Test
{
    [TestFixture]
    public class DeconToolsFilterLoaderTests
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

            var testFilter1 = loader.DeconToolsFilterList[6];
            Assert.AreEqual(2, testFilter1.ChargeMinimum);
            Assert.AreEqual(2147483647, testFilter1.ChargeMaximum);
            Assert.AreEqual(500, testFilter1.AbundanceMinimum);
            Assert.AreEqual(1000, testFilter1.AbundanceMaximum);
            Assert.AreEqual(0.3, testFilter1.FitScoreMaximum);
            Assert.AreEqual(0, testFilter1.InterferenceScoreMaximum);

            //2 2147483647  500 1000    0.3 0

        }

    }
}
