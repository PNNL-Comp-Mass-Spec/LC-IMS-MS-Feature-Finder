using System;
using System.Collections.Generic;
using System.Linq;
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
        public void test1()
        {
            string testFilterFile1 = @"\\protoapps\UserData\Slysz\DeconTools_TestFiles\LCMSFeatureFinder\DeconToolsFilterFiles\testFilterFile1.txt";
            DeconToolsFilterLoader loader = new DeconToolsFilterLoader(testFilterFile1);
            loader.DisplayFilters();

            DeconToolsFilter testFilter1 = loader.DeconToolsFilterList[6];
            Assert.AreEqual(2, testFilter1.ChargeMinimum);
            Assert.AreEqual(2147483647, testFilter1.ChargeMaximum);
            Assert.AreEqual(500, testFilter1.AbundanceMinimum);
            Assert.AreEqual(1000, testFilter1.AbundanceMaximum);
            Assert.AreEqual(0.3, testFilter1.FitScoreMaximum);
            Assert.AreEqual(0, testFilter1.InterferenceScoreMaximum);

            //2	2147483647	500	1000	0.3	0

        }

    }
}
