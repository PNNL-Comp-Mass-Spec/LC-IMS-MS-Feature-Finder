using System;
using System.IO;
using System.Linq;
using System.Text;
using FeatureFinder.Control;
using FeatureFinder.Data;
using NUnit.Framework;

namespace FeatureFinder.FunctionalTests
{
    [TestFixture]
    public class SaturationRepairTesting
    {
        // see Redmine issue: http://redmine.pnl.gov/issues/947

        [Test]
        public void saturatedMSFeatureDataProcessingTest1()
        {
            string sourceIsosFile =
                @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_mass860_repaired_isos.csv";

            string copiedIsosFileName = @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_isos.csv";

            File.Copy(sourceIsosFile, copiedIsosFileName, true);


            string testfile =
                @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\FF_IMS4Filters_NoFlags_20ppm_Min3Pts_4MaxLCGap_NoDaCorr_ConfDtn_2011-05-16_issue947.ini";

            var iniReader = new IniReader(testfile);
            iniReader.CreateSettings();

            var isosReader = new IsosReader();

            var controller = new LCIMSMSFeatureFinderController(isosReader);
            controller.Execute();

            Assert.AreEqual(2, controller.LCimsmsFeatures.Count());

            var testFeature1 = (from n in controller.LCimsmsFeatures where n.Charge == 2 select n).FirstOrDefault();

            Assert.IsNotNull(testFeature1);

            DisplayFeatureStats(testFeature1);
        }

        [Test]
        public void traditionalMSFeatureDataProcessingTest1()
        {
            string sourceIsosFile =
                @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_mass860_noRepair_isos.csv";

            string copiedIsosFileName = @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_isos.csv";

            File.Copy(sourceIsosFile, copiedIsosFileName, true);


            string testfile =
                @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\FF_IMS4Filters_NoFlags_20ppm_Min3Pts_4MaxLCGap_NoDaCorr_ConfDtn_2011-05-16_issue947.ini";

            var iniReader = new IniReader(testfile);
            iniReader.CreateSettings();

            var isosReader = new IsosReader();

            var controller = new LCIMSMSFeatureFinderController(isosReader);
            controller.Execute();

            Assert.AreEqual(2, controller.LCimsmsFeatures.Count());

            var testFeature1 = (from n in controller.LCimsmsFeatures where n.Charge == 2 select n).FirstOrDefault();

            Assert.IsNotNull(testFeature1);

            DisplayFeatureStats(testFeature1);
        }


        [Test]
        public void nonSaturatedMSFeatureProcessingTest1()
        {
            string sourceIsosFile =
                @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_mass1064_repaired_isos.csv";

            string copiedIsosFileName = @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_isos.csv";

            File.Copy(sourceIsosFile, copiedIsosFileName, true);


            string testfile =
                @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\FF_IMS4Filters_NoFlags_20ppm_Min3Pts_4MaxLCGap_NoDaCorr_ConfDtn_2011-05-16_issue947.ini";

            var iniReader = new IniReader(testfile);
            iniReader.CreateSettings();

            var isosReader = new IsosReader();

            var controller = new LCIMSMSFeatureFinderController(isosReader);
            controller.Execute();

            Assert.AreEqual(1, controller.LCimsmsFeatures.Count());

            var testFeature1 = (from n in controller.LCimsmsFeatures where n.Charge == 2 select n).FirstOrDefault();

            Assert.IsNotNull(testFeature1);

            DisplayFeatureStats(testFeature1);
        }

        [Test]
        public void nonSaturatedLowIntensityProcessingTest1()
        {
            string sourceIsosFile =
                @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_lowIntensityCase860_repaired_isos.csv";

            string copiedIsosFileName = @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_isos.csv";

            File.Copy(sourceIsosFile, copiedIsosFileName, true);


            string testfile =
                @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\FF_IMS4Filters_NoFlags_20ppm_Min3Pts_4MaxLCGap_NoDaCorr_ConfDtn_2011-05-16_issue947.ini";

            var iniReader = new IniReader(testfile);
            iniReader.CreateSettings();

            var isosReader = new IsosReader();

            var controller = new LCIMSMSFeatureFinderController(isosReader);
            controller.Execute();

            Assert.AreEqual(1, controller.LCimsmsFeatures.Count());

            var testFeature1 = (from n in controller.LCimsmsFeatures where n.Charge == 2 select n).FirstOrDefault();

            Assert.IsNotNull(testFeature1);

            DisplayFeatureStats(testFeature1);

        }



        [Test]
        public void processEntireIsosTraditionalTest1()
        {
            string sourceIsosFile =
                @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_DMSVersion_isos.csv";

            sourceIsosFile =
                @"C:\Users\d3x720\Documents\PNNL\My_DataAnalysis\2012\IMS_related\2012_01_27_Saturation_threshold_analysis\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_Sat90000_isos.csv";

            sourceIsosFile =
                @"C:\Users\d3x720\Documents\PNNL\My_DataAnalysis\2012\IMS_related\2012_01_27_Saturation_threshold_analysis\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_Original_isos.csv";

            //sourceIsosFile =
            //    @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\temp\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_mass1064_repaired_isos.csv";



            string copiedIsosFileName = @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_isos.csv";

            File.Copy(sourceIsosFile, copiedIsosFileName, true);


            string testfile =
                @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\FF_IMS4Filters_NoFlags_20ppm_Min3Pts_4MaxLCGap_NoDaCorr_ConfDtn_2011-05-16_issue947.ini";
            var iniReader = new IniReader(testfile);
            iniReader.CreateSettings();

            var isosReader = new IsosReader();

            var controller = new LCIMSMSFeatureFinderController(isosReader);
            controller.Execute();

            FileInfo fileInfo = new FileInfo(sourceIsosFile);
            var directoryInfo = fileInfo.Directory;

            
            string sourceFeaturesFile = copiedIsosFileName.Replace("_isos.csv", "_LCMSFeatures.txt");
            string sourceLogFile = copiedIsosFileName.Replace("_isos.csv", "_FeatureFinder_Log.txt");

            string copiedFeaturesFile = directoryInfo.FullName + "\\" +
                                        fileInfo.Name.Replace("_isos.csv", "_LCMSFeatures.txt");
            string copiedFeaturesLogFile = directoryInfo.FullName + "\\" +
                                        fileInfo.Name.Replace("_isos.csv", "_FeatureFinder_Log.txt");

            //copy the LCMSFeatures file back to my working folder
            File.Copy(sourceFeaturesFile, copiedFeaturesFile);
            File.Copy(sourceLogFile, copiedFeaturesLogFile);


        }


        //[Test]
        //public void processEntireIsosSaturationRepairedTest1()
        //{
        //    string sourceIsosFile =
        //        @"D:\Data\UIMF\Sarc_Main_Study_Controls\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_saturationFixed_all_2011_12_30_isos.csv";

        //    string copiedIsosFileName = @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\Sarc_P09_B06_0786_20Jul11_Cheetah_11-05-31_isos.csv";

        //    File.Copy(sourceIsosFile, copiedIsosFileName, true);


        //    string testfile =
        //        @"\\protoapps\UserData\Slysz\Data\Redmine_Issues\Issue947_saturationTesting\FF_IMS4Filters_NoFlags_20ppm_Min3Pts_4MaxLCGap_NoDaCorr_ConfDtn_2011-05-16_issue947.ini";

        //    var iniReader = new IniReader(testfile);
        //    iniReader.CreateSettings();

        //    var isosReader = new IsosReader();

        //    var controller = new LCIMSMSFeatureFinderController(isosReader);
        //    controller.Execute();
        //}



        private void DisplayFeatureStats(LCIMSMSFeature testFeature1)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("OrigIndex= \t" + testFeature1.OriginalIndex);
            sb.Append(Environment.NewLine);
            sb.Append("monoMass = \t" + testFeature1.CalculateAverageMonoisotopicMass().ToString("0.0000"));
            sb.Append(Environment.NewLine);
            sb.Append("maxAbundance = \t" + testFeature1.AbundanceMaxRaw);
            sb.Append(Environment.NewLine);
            sb.Append("summedAbundance = \t" + testFeature1.AbundanceSumRaw);
            sb.Append(Environment.NewLine);
            sb.Append("totMemberCount = \t" + testFeature1.GetMemberCount());
            sb.Append(Environment.NewLine);
            sb.Append("totSaturated = \t" + testFeature1.GetSaturatedMemberCount());
            sb.Append(Environment.NewLine);

            Console.WriteLine(sb.ToString());
        }
    }
}
