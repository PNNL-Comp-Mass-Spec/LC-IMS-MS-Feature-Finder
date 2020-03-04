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
                        var lcimsmsFeatureList = DaltonCorrection.CorrectLCIMSMSFeatures(lcimsmsFeatureGroup);

                        foreach (var lcimsmsFeature in lcimsmsFeatureList)
                        {
                            daCorrectedLCIMSMSFeatureBag.Add(lcimsmsFeature);
                        }
                    });

                    lcimsmsFeatureEnumerable = daCorrectedLCIMSMSFeatureBag;

                    Logger.Log("Total Number of Dalton Corrected LC-IMS-MS Features = " + lcimsmsFeatureEnumerable.Count());
                }
                else
                {
                    lcimsmsFeatureEnumerable = lcimsmsFeatureBag;
                }

                Logger.Log("Filtering LC-IMS-MS features based on Member Count...");
                lcimsmsFeatureEnumerable = FeatureUtil.FilterByMemberCount(lcimsmsFeatureEnumerable);
                Logger.Log("Total Number of Filtered LC-IMS-MS Features = " + lcimsmsFeatureEnumerable.Count());

                Logger.Log("Splitting LC-IMS-MS Features by LC Scan...");
                lcimsmsFeatureEnumerable = FeatureUtil.SplitLCIMSMSFeaturesByScanLC(lcimsmsFeatureEnumerable);
                if (!Settings.UseConformationDetection)
                {
                    lcimsmsFeatureEnumerable = FeatureUtil.FilterSingleLCScan(lcimsmsFeatureEnumerable);
                }
                Logger.Log("New Total Number of Filtered LC-IMS-MS Features = " + lcimsmsFeatureEnumerable.Count());

                if (Settings.UseConformationDetection)
                {
                    Logger.Log("Conformation Detection...");
                    lcimsmsFeatureEnumerable = ConformationDetection.DetectConformationsUsingRawData(lcimsmsFeatureEnumerable.ToList());
                    //lcimsmsFeatureEnumerable = FeatureUtil.FilterSingleLCScan(lcimsmsFeatureEnumerable);
                    lcimsmsFeatureEnumerable = FeatureUtil.FilterByMemberCount(lcimsmsFeatureEnumerable);
                    Logger.Log("New Total Number of LC-IMS-MS Features = " + lcimsmsFeatureEnumerable.Count());
                }

                lcimsmsFeatureEnumerable = FeatureUtil.SortByMass(lcimsmsFeatureEnumerable);

                Logger.Log("Creating filtered Isos file...");

                var msFeatureListOutput = new List<MSFeature>();
                foreach (var lcimsmsFeature in lcimsmsFeatureEnumerable)
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

                var isosWriter = new IsosWriter(msFeatureListOutput, m_isosReader.ColumnMap);

                Logger.Log("Writing output files...");
                FeatureUtil.WriteLCIMSMSFeatureToFile(lcimsmsFeatureEnumerable);

                LCimsmsFeatures = lcimsmsFeatureEnumerable;
            }
        }

        #endregion
    }
}
