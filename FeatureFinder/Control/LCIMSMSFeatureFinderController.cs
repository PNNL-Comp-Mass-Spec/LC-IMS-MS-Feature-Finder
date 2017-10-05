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

        public IEnumerable<LCIMSMSFeature> LCimsmsFeatures { get; set; }


        public void Execute()
        {
            {
                Logger.Log("Total number of MS Features in _isos.csv file = " + m_isosReader.NumOfUnfilteredMSFeatures);
                Logger.Log("Total number of MS Features we'll consider = " + m_isosReader.MSFeatureList.Count);
                Logger.Log("Creating IMS-MS Features...");

                var filteredMSFeatureList = m_isosReader.MSFeatureList;

                var imsmsfeatureBag = new ConcurrentBag<IMSMSFeature>();

                if (Settings.UseCharge)
                {
                    var groupByScanLCAndChargeQuery = from msFeature in filteredMSFeatureList
                                                      group msFeature by new { msFeature.ScanLC, msFeature.Charge } into newGroup
                                                      select newGroup;

                    Parallel.ForEach(groupByScanLCAndChargeQuery, msFeatureGroup =>
                    {
                        var imsmsFeatureList = ClusterMSFeatures.ClusterByMass(msFeatureGroup);

                        foreach (var imsmsFeature in imsmsFeatureList)
                        {
                            imsmsfeatureBag.Add(imsmsFeature);
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
                        var imsmsFeatureList = ClusterMSFeatures.ClusterByMass(msFeatureGroup);

                        foreach (var imsmsFeature in imsmsFeatureList)
                        {
                            imsmsfeatureBag.Add(imsmsFeature);
                        }
                    });
                }

                Logger.Log("Total Number of Unfiltered IMS-MS Features = " + imsmsfeatureBag.Count);
                //Logger.Log("Filtering out short IMS-MS Features...");

                //IEnumerable<IMSMSFeature> imsmsFeatureEnumerable = FeatureUtil.FilterByMemberCount(imsmsfeatureBag);
                //imsmsfeatureBag = null;

                //Logger.Log("Total Number of Filtered IMS-MS Features = " + imsmsFeatureEnumerable.Count());
                Logger.Log("Creating LC-IMS-MS Features...");

                var lcimsmsFeatureBag = new ConcurrentBag<LCIMSMSFeature>();

                if (Settings.UseCharge)
                {
                    var groupByChargeQuery = from imsmsFeature in imsmsfeatureBag
                                             group imsmsFeature by imsmsFeature.Charge into newGroup
                                             select newGroup;

                    Parallel.ForEach(groupByChargeQuery, imsmsFeatureGroup =>
                    {
                        var lcimsmsFeatureList = ClusterIMSMSFeatures.ClusterByMassAndScanLC(imsmsFeatureGroup);

                        foreach (var lcimsmsFeature in lcimsmsFeatureList)
                        {
                            lcimsmsFeatureBag.Add(lcimsmsFeature);
                        }
                    });
                }
                else
                {
                    var lcimsmsFeatureList = ClusterIMSMSFeatures.ClusterByMassAndScanLC(imsmsfeatureBag);

                    foreach (var lcimsmsFeature in lcimsmsFeatureList)
                    {
                        lcimsmsFeatureBag.Add(lcimsmsFeature);
                    }
                }

                Logger.Log("Total Number of LC-IMS-MS Features = " + lcimsmsFeatureBag.Count);

                IEnumerable<LCIMSMSFeature> lcimsmsFeatureEnumerable;

                if (Settings.IMSDaCorrectionMax > 0 && !Settings.FilterFlaggedData)
                {
                    Logger.Log("Executing Dalton Correction Algorithm on LC-IMS-MS Features...");

                    var daCorrectedLCIMSMSFeatureBag = new ConcurrentBag<LCIMSMSFeature>();
                    var lcimsmsFeatureListBag = new ConcurrentBag<IEnumerable<LCIMSMSFeature>>();

                    if (Settings.UseCharge)
                    {
                        var groupByChargeQuery2 = from lcimsmsFeature in lcimsmsFeatureBag
                                                  group lcimsmsFeature by lcimsmsFeature.Charge into newGroup
                                                  select newGroup;

                        Parallel.ForEach(groupByChargeQuery2, lcimsmsFeatureGroup =>
                        {
                            IEnumerable<IEnumerable<LCIMSMSFeature>> returnList = FeatureUtil.PartitionFeaturesByMass(lcimsmsFeatureGroup);

                            foreach (var lcimsmsFeatureList in returnList)
                            {
                                lcimsmsFeatureListBag.Add(lcimsmsFeatureList);
                            }
                        });
                    }
                    else
                    {
                        IEnumerable<IEnumerable<LCIMSMSFeature>> returnList = FeatureUtil.PartitionFeaturesByMass(lcimsmsFeatureBag);

                        foreach (var lcimsmsFeatureList in returnList)
                        {
                            lcimsmsFeatureListBag.Add(lcimsmsFeatureList);
                        }
                    }

                    Parallel.ForEach(lcimsmsFeatureListBag, lcimsmsFeatureGroup =>
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
                        foreach (var imsmsFeature in lcimsmsFeature.IMSMSFeatureList)
                        {
                            msFeatureListOutput.AddRange(imsmsFeature.MSFeatureList);
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
