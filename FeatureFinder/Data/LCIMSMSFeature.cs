using System;
using System.Collections.Generic;
using System.Linq;
using FeatureFinder.Algorithms;
using FeatureFinder.Data.Maps;
using FeatureFinder.Utilities;
using UIMFLibrary;

namespace FeatureFinder.Data
{
    public class LCIMSMSFeature
    {
        public byte Charge { get; set; }

        public List<IMSMSFeature> IMSMSFeatureList { get; set; }

        public float IMSScore { get; set; }
        public float LCScore { get; set; }
        public double DriftTime { get; set; }
        public int OriginalIndex { get; set; }
        public int MaxMemberCount { get; set; }
        public double AbundanceMaxRaw { get; set; }
        public double AbundanceSumRaw { get; set; }

        public LCIMSMSFeature(byte charge)
        {
            IMSMSFeatureList = new List<IMSMSFeature>();
            Charge = charge;
            IMSScore = 0;
            LCScore = 0;
            DriftTime = 0;
            MaxMemberCount = 0;
            AbundanceMaxRaw = 0;
            AbundanceSumRaw = 0;
        }

        public void AddIMSMSFeature(IMSMSFeature imsmsFeature)
        {
            int lcScan = imsmsFeature.ScanLC;

            foreach (IMSMSFeature otherIMSMSFeature in IMSMSFeatureList.Where(otherIMSMSFeature => otherIMSMSFeature.ScanLC == lcScan))
            {
                FeatureUtil.MergeIMSMSFeatures(imsmsFeature, otherIMSMSFeature);
                IMSMSFeatureList.Add(imsmsFeature);
                IMSMSFeatureList.Remove(otherIMSMSFeature);
                return;
            }

            IMSMSFeatureList.Add(imsmsFeature);
        }

        public int GetMemberCount()
        {
            return IMSMSFeatureList.Sum(imsmsFeature => imsmsFeature.MSFeatureList.Count);
        }

        public int GetSaturatedMemberCount()
        {
            int numSaturated = 0;
            foreach (var imsmsFeature in IMSMSFeatureList)
            {
                foreach (var msFeature in imsmsFeature.MSFeatureList)
                {
                    if (msFeature.IsSaturated)
                    {
                        numSaturated++;
                    }
                }
            }

            return numSaturated;

        }

        /// <summary>
        /// Calculates the average monoisotopic mass for the LC-IMS-MSFeature. 
        /// </summary>
        /// <returns></returns>
        public double CalculateAverageMonoisotopicMass()
        {
            int totalMemberCount = 0;
            double massTotal = 0.0;

            foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
            {
                int memberCount = imsmsFeature.MSFeatureList.Count;
                massTotal += imsmsFeature.CalculateAverageMonoisotopicMass() * memberCount;
                totalMemberCount += memberCount;
            }

            double averageMass = massTotal / totalMemberCount;

            return averageMass;
        }

        public double CalculateAverageMz()
        {
            int totalMemberCount = 0;
            double mzTotal = 0.0;

            foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
            {
                int memberCount = imsmsFeature.MSFeatureList.Count;
                mzTotal += imsmsFeature.CalculateAverageMz() * memberCount;
                totalMemberCount += memberCount;
            }

            double averageMz = mzTotal / totalMemberCount;

            return averageMz;
        }

        public double GetIntensity()
        {
            return IMSMSFeatureList.Sum(imsmsFeature => imsmsFeature.GetIntensity());
        }

        public double GetFlaggedPercentage()
        {
            int numFlagged = 0;
            int numTotal = 0;

            foreach (MSFeature msFeature in IMSMSFeatureList.SelectMany(imsmsFeature => imsmsFeature.MSFeatureList))
            {
                if (msFeature.ErrorFlag == 1)
                {
                    numFlagged++;
                }

                numTotal++;
            }

            double percentage = (double)numFlagged / (double)numTotal;
            return percentage;
        }

        public void GetMinAndMaxScanLC(out int scanLCMinimum, out int scanLCMaximum)
        {
            List<MSFeature> msFeatureList = new List<MSFeature>();

            foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
            {
                msFeatureList.AddRange(imsmsFeature.MSFeatureList);
            }

            var sortByScanLCQuery = from msFeature in msFeatureList
                                    orderby msFeature.ScanLC
                                    select msFeature;

            scanLCMinimum = sortByScanLCQuery.First().ScanLC;
            scanLCMaximum = sortByScanLCQuery.Last().ScanLC;
        }

        public void GetMinAndMaxScanLCAndScanIMSAndMSFeatureRep(out int scanLCMinimum, out int scanLCMaximum, out int scanIMSMinimum, out int scanIMSMaximum, out MSFeature msFeatureRep)
        {
            List<MSFeature> msFeatureList = new List<MSFeature>();

            foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
            {
                msFeatureList.AddRange(imsmsFeature.MSFeatureList);
            }

            var sortByScanLCQuery = from msFeature in msFeatureList
                                    orderby msFeature.ScanLC ascending
                                    select msFeature;

            scanLCMinimum = sortByScanLCQuery.First().ScanLC;
            scanLCMaximum = sortByScanLCQuery.Last().ScanLC;

            var sortByScanIMSQuery = from msFeature in msFeatureList
                                     orderby msFeature.ScanIMS ascending
                                     select msFeature;

            scanIMSMinimum = sortByScanIMSQuery.First().ScanIMS;
            scanIMSMaximum = sortByScanIMSQuery.Last().ScanIMS;

            var sortByAbundanceQuery = from msFeature in msFeatureList
                                       orderby msFeature.Abundance descending
                                       select msFeature;

            msFeatureRep = sortByAbundanceQuery.First();
        }

        public void GetMinAndMaxScanLCAndDriftTimeAndMSFeatureRep(out int scanLCMinimum, out int scanLCMaximum, out double driftTimeMinimum, out double driftTimeMaximum, out MSFeature msFeatureRep)
        {
            List<MSFeature> msFeatureList = new List<MSFeature>();

            foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
            {
                msFeatureList.AddRange(imsmsFeature.MSFeatureList);
            }

            var sortByScanLCQuery = from msFeature in msFeatureList
                                    orderby msFeature.ScanLC ascending
                                    select msFeature;

            scanLCMinimum = sortByScanLCQuery.First().ScanLC;
            scanLCMaximum = sortByScanLCQuery.Last().ScanLC;

            var sortByScanIMSQuery = from msFeature in msFeatureList
                                     orderby msFeature.DriftTime ascending
                                     select msFeature;

            driftTimeMinimum = sortByScanIMSQuery.First().DriftTime;
            driftTimeMaximum = sortByScanIMSQuery.Last().DriftTime;

            var sortByAbundanceQuery = from msFeature in msFeatureList
                                       orderby msFeature.Abundance descending
                                       select msFeature;

            msFeatureRep = sortByAbundanceQuery.First();
        }

        public MSFeature GetMSFeatureRep()
        {
            List<MSFeature> msFeatureList = new List<MSFeature>();

            foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
            {
                msFeatureList.AddRange(imsmsFeature.MSFeatureList);
            }

            var sortByAbundanceQuery = from msFeature in msFeatureList
                                       orderby msFeature.Abundance descending
                                       select msFeature;

            MSFeature msFeatureRep = sortByAbundanceQuery.First();
            return msFeatureRep;
        }


        public List<XYPair>  GetIMSScanProfileFromMSFeatures()
        {
            int scanLCMinimum = 0;
            int scanLCMaximum = 0;
            int scanIMSMinimum = 0;
            int scanIMSMaximum = 0;

            MSFeature msFeatureRep = null;
            GetMinAndMaxScanLCAndScanIMSAndMSFeatureRep(out scanLCMinimum, out scanLCMaximum, out scanIMSMinimum, out scanIMSMaximum, out msFeatureRep);

            List<XYPair> xyPairs = new List<XYPair>();

            for (int imsScan = scanIMSMinimum; imsScan <= scanIMSMaximum; imsScan++)
            {
                float summedIntensityForIMSScan = 0;

                foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
                {
                    summedIntensityForIMSScan +=
                        imsmsFeature.MSFeatureList.Where(p => p.ScanIMS == imsScan).Select(p =>(float)p.IntensityUnSummed).Sum       //note: need float due overflow exception from value exceeding int32
                            ();
                }

                var pair = new XYPair(imsScan,summedIntensityForIMSScan);

                xyPairs.Add(pair);
            }


            ConformationDetection.PadXYPairsWithZeros(ref xyPairs, 5);

            return xyPairs;




        }


        public List<XYPair> GetIMSScanProfileFromRawData(DataReader uimfReader, DataReader.FrameType frameType, double binWidth, double calibrationSlope, double calibrationIntercept)
        {
            int scanLCMinimum = 0;
            int scanLCMaximum = 0;
            int scanIMSMinimum = 0;
            int scanIMSMaximum = 0;

            MSFeature msFeatureRep = null;

            GetMinAndMaxScanLCAndScanIMSAndMSFeatureRep(out scanLCMinimum, out scanLCMaximum, out scanIMSMinimum, out scanIMSMaximum, out msFeatureRep);

            double currentFWHM = msFeatureRep.Fwhm;
            double currentMonoMZ = msFeatureRep.MassMonoisotopic/msFeatureRep.Charge + 1.0072649;
            double mzMostAbundantIsotope = msFeatureRep.MassMostAbundantIsotope / msFeatureRep.Charge + 1.00727649;


                    ////[gord] the following commented-out code sets the m/z range too wide. Can be removed later
                    //List<double> startMZ = new List<double>();
                    //List<double> endMZ = new List<double>();

                    //// Set ranges over which to look for the original data in the UIMF.
                    //double charge = Convert.ToDouble(this.Charge);
                    //for (int i = 0; i < 3; i++)
                    //{
                    //    startMZ.Add(currentMonoMZ + (1.003 * i / charge) - (0.5 * currentFWHM));
                    //    endMZ.Add(currentMonoMZ + (1.003 * i / charge) + (0.5 * currentFWHM));
                    //}

                    //double minMZ = startMZ[0];
                    //double maxMZ = endMZ[endMZ.Count - 1];

                    //double midPointMZ = (maxMZ + minMZ) / 2;
                    //double wideToleranceInMZ = midPointMZ - minMZ;
            
            int frameMinimum = ScanLCMap.Mapping[scanLCMinimum];
            int frameMaximum = ScanLCMap.Mapping[scanLCMaximum];

           
            int[] scanValues = null;
            int[] intensityVals = null;

            double sigma = msFeatureRep.Fwhm / 2.35;
            double toleranceInMZ = 2 * sigma ;    //  this is a +/- value;  so    4* sigma = 95% of a normal distribution

            //Before: a wide m/z was used when generating the drift time profile. 
            //uimfReader.GetDriftTimeProfile(frameIndexMinimum, frameIndexMaximum, frameType, scanIMSMinimum, scanIMSMaximum, midPointMZ, wideToleranceInMZ, ref scanValues, ref intensityVals);

            //now:  a narrow m/z range is used when generating the drift time profile
            uimfReader.GetDriftTimeProfile(frameMinimum, frameMaximum, frameType, scanIMSMinimum, scanIMSMaximum, mzMostAbundantIsotope, toleranceInMZ, ref scanValues, ref intensityVals);

            List<XYPair> imsScanProfile = intensityVals.Select((t, i) => new XYPair(scanIMSMinimum + i, t)).ToList();

            ConformationDetection.PadXYPairsWithZeros(ref imsScanProfile, 5);

            return imsScanProfile;
        }

        public void PrintLCAndDriftTimeMap()
        {
            List<MSFeature> msFeatureList = new List<MSFeature>();

            foreach (IMSMSFeature imsmsFeature in IMSMSFeatureList)
            {
                msFeatureList.AddRange(imsmsFeature.MSFeatureList);
            }

            var groupByScanLCQuery = from msFeature in msFeatureList
                                     group msFeature by new { msFeature.ScanLC } into newGroup
                                     select newGroup;

            foreach (IEnumerable<MSFeature> msFeatureGroup in groupByScanLCQuery)
            {
                Console.Write("LC Scan = " + msFeatureGroup.First().ScanLC + ": ");

                var orderByDriftTimeQuery = from msFeature in msFeatureGroup
                                            orderby msFeature.ScanLC, msFeature.DriftTime ascending
                                            select msFeature;

                foreach (MSFeature msFeature in orderByDriftTimeQuery)
                {
                    Console.Write(msFeature.DriftTime + ",");
                }

                Console.Write("\n");
            }
        }
    }
}
