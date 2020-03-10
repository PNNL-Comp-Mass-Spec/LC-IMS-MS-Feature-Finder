using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FeatureFinder.Algorithms;
using FeatureFinder.Data;
using FeatureFinder.Utilities;

namespace FeatureFinder.Control
{
    public class LCIMSMSFeatureFinderController
    {
        private readonly IsosReader m_isosReader;

        #region Constructors
        public LCIMSMSFeatureFinderController(IsosReader isosReader)
        {
            m_isosReader = isosReader;
        }
        #endregion

        #region Public Methods

        public IEnumerable<LCIMSMSFeature> LcImsMsFeatures { get; set; }

        public void Execute()
        {
            {
                Logger.Log("Total number of MS Features in _isos.csv file = " + m_isosReader.NumOfUnfilteredMSFeatures);
                Logger.Log("Total number of MS Features we'll consider = " + m_isosReader.MSFeatureList.Count);
                Logger.Log("Creating IMS-MS Features...");

                var filteredMSFeatureList = m_isosReader.MSFeatureList;

                var imsMsFeatureBag = new ConcurrentBag<imsMsFeature>();

                if (Settings.UseCharge)
                {
                    var groupByScanLCAndChargeQuery = from msFeature in filteredMSFeatureList
                                                      group msFeature by new { msFeature.ScanLC, msFeature.Charge } into newGroup
                                                      select newGroup;

                    Parallel.ForEach(groupByScanLCAndChargeQuery, msFeatureGroup =>
                    {
                        var imsMsFeatureList = ClusterMSFeatures.ClusterByMass(msFeatureGroup);

                        foreach (var imsMsFeature in imsMsFeatureList)
                        {
                            imsMsFeatureBag.Add(imsMsFeature);
                        }
                    });
                }
                else
                {
                    var groupByScanLCQuery = from msFeature in filteredMSFeatureList
                                             group msFeature by msFeature.ScanLC into newGroup
                                             select newGroup;

                    Parallel.ForEach(groupByScanLCQuery, msFeatureGroup =>
                    {
                        var imsMsFeatureList = ClusterMSFeatures.ClusterByMass(msFeatureGroup);

                        foreach (var imsMsFeature in imsMsFeatureList)
                        {
                            imsMsFeatureBag.Add(imsMsFeature);
                        }
                    });
                }

                Logger.Log("Total Number of Unfiltered IMS-MS Features = " + imsMsFeatureBag.Count);

                //Logger.Log("Filtering out short IMS-MS Features...");

                //IEnumerable<imsMsFeature> imsMsFeatureEnumerable = FeatureUtil.FilterByMemberCount(imsMsFeatureBag);
                //imsMsFeatureBag = null;

                //Logger.Log("Total Number of Filtered IMS-MS Features = " + imsMsFeatureEnumerable.Count());

                Logger.Log("Creating LC-IMS-MS Features...");

                var lcImsMsFeatureBag = new ConcurrentBag<LCIMSMSFeature>();

                if (Settings.UseCharge)
                {
                    var groupByChargeQuery = from imsMsFeature in imsMsFeatureBag
                                             group imsMsFeature by imsMsFeature.Charge into newGroup
                                             select newGroup;

                    Parallel.ForEach(groupByChargeQuery, imsMsFeatureGroup =>
                    {
                        var lcimsmsFeatureList = ClusterImsMsFeatures.ClusterByMassAndScanLC(imsMsFeatureGroup);

                        foreach (var lcimsmsFeature in lcimsmsFeatureList)
                        {
                            lcImsMsFeatureBag.Add(lcimsmsFeature);
                        }
                    });
                }
                else
                {
                    var lcimsmsFeatureList = ClusterImsMsFeatures.ClusterByMassAndScanLC(imsMsFeatureBag);

                    foreach (var lcimsmsFeature in lcimsmsFeatureList)
                    {
                        lcImsMsFeatureBag.Add(lcimsmsFeature);
                    }
                }

                Logger.Log("Total Number of LC-IMS-MS Features = " + lcImsMsFeatureBag.Count);

                IEnumerable<LCIMSMSFeature> lcImsMsFeatures;

                if (Settings.IMSDaCorrectionMax > 0 && !Settings.FilterFlaggedData)
                {
                    Logger.Log("Executing Dalton Correction Algorithm on LC-IMS-MS Features...");

                    var daCorrectedLCIMSMSFeatureBag = new ConcurrentBag<LCIMSMSFeature>();
                    var lcImsMsFeatureListBag = new ConcurrentBag<IEnumerable<LCIMSMSFeature>>();

                    if (Settings.UseCharge)
                    {
                        var groupByChargeQuery2 = from lcimsmsFeature in lcImsMsFeatureBag
                                                  group lcimsmsFeature by lcimsmsFeature.Charge into newGroup
                                                  select newGroup;

                        Parallel.ForEach(groupByChargeQuery2, lcimsmsFeatureGroup =>
                        {
                            IEnumerable<IEnumerable<LCIMSMSFeature>> returnList = FeatureUtil.PartitionFeaturesByMass(lcimsmsFeatureGroup);

                            foreach (var lcimsmsFeatureList in returnList)
                            {
                                lcImsMsFeatureListBag.Add(lcimsmsFeatureList);
                            }
                        });
                    }
                    else
                    {
                        IEnumerable<IEnumerable<LCIMSMSFeature>> returnList = FeatureUtil.PartitionFeaturesByMass(lcImsMsFeatureBag);

                        foreach (var lcimsmsFeatureList in returnList)
                        {
                            lcImsMsFeatureListBag.Add(lcimsmsFeatureList);
                        }
                    }

                    Parallel.ForEach(lcImsMsFeatureListBag, lcimsmsFeatureGroup =>
                    {
                        var lcimsmsFeatureList = DaltonCorrection.CorrectLCIMSMSFeatures(lcimsmsFeatureGroup.ToList());

                        foreach (var lcimsmsFeature in lcimsmsFeatureList)
                        {
                            daCorrectedLCIMSMSFeatureBag.Add(lcimsmsFeature);
                        }
                    });

                    lcImsMsFeatures = daCorrectedLCIMSMSFeatureBag;

                    Logger.Log("Total Number of Dalton Corrected LC-IMS-MS Features = " + lcImsMsFeatures.Count());
                }
                else
                {
                    lcImsMsFeatures = lcImsMsFeatureBag;
                }

                Logger.Log("Filtering LC-IMS-MS features based on Member Count...");
                var filteredLcImsMsFeatures = FeatureUtil.FilterByMemberCount(lcImsMsFeatures).ToList();
                Logger.Log("Total Number of Filtered LC-IMS-MS Features = " + filteredLcImsMsFeatures.Count);

                Logger.Log("Splitting LC-IMS-MS Features by LC Scan...");
                var splitLcImsMsFeatures = FeatureUtil.SplitLCIMSMSFeaturesByScanLC(filteredLcImsMsFeatures).ToList();

                IEnumerable<LCIMSMSFeature> featuresAfterScanBasedOrConformationFiltering;
                if (!Settings.UseConformationDetection)
                {
                    featuresAfterScanBasedOrConformationFiltering = FeatureUtil.FilterSingleLCScan(splitLcImsMsFeatures).ToList();
                    Logger.Log("New Total Number of Filtered LC-IMS-MS Features = " + featuresAfterScanBasedOrConformationFiltering.Count());
                }
                else
                {
                    Logger.Log("Number of LC-IMS-MS Features before conformation detection = " + splitLcImsMsFeatures.Count);

                    Logger.Log("Conformation Detection...");
                    var conformationFilteredFeatures = ConformationDetection.DetectConformationsUsingRawData(splitLcImsMsFeatures);
                    //conformationFilteredFeatures = FeatureUtil.FilterSingleLCScan(conformationFilteredFeatures);
                    featuresAfterScanBasedOrConformationFiltering = FeatureUtil.FilterByMemberCount(conformationFilteredFeatures).ToList();
                    Logger.Log("New Total Number of LC-IMS-MS Features = " + featuresAfterScanBasedOrConformationFiltering.Count());
                }

                var massSortedLcImsMsFeatures = FeatureUtil.SortByMass(featuresAfterScanBasedOrConformationFiltering).ToList();

                Logger.Log("Creating filtered Isos file...");

                var msFeatureListOutput = new List<MSFeature>();
                foreach (var lcimsmsFeature in massSortedLcImsMsFeatures)
                {
                    if (Settings.FilterIsosToSinglePoint)
                    {
                        var msFeatureRep = lcimsmsFeature.GetMSFeatureRep();
                        msFeatureListOutput.Add(msFeatureRep);
                    }
                    else
                    {
                        foreach (var imsMsFeature in lcimsmsFeature.imsMsFeatureList)
                        {
                            msFeatureListOutput.AddRange(imsMsFeature.MSFeatureList);
                        }
                    }
                }

                var isosWriter = new IsosWriter(m_isosReader.ColumnMap);
                isosWriter.CreateFilteredIsosFile(msFeatureListOutput);

                Logger.Log("Writing output files...");
                FeatureUtil.WriteLCIMSMSFeatureToFile(massSortedLcImsMsFeatures);

                LcImsMsFeatures = massSortedLcImsMsFeatures;
            }
        }

        #endregion
    }
}
