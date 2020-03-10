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

        public List<imsMsFeature> imsMsFeatureList { get; set; }

        public float IMSScore { get; set; }
        public float LCScore { get; set; }
        public double DriftTime { get; set; }
        public int OriginalIndex { get; set; }
        public int MaxMemberCount { get; set; }
        public double AbundanceMaxRaw { get; set; }
        public double AbundanceSumRaw { get; set; }

        public LCIMSMSFeature(byte charge)
        {
            imsMsFeatureList = new List<imsMsFeature>();
            Charge = charge;
            IMSScore = 0;
            LCScore = 0;
            DriftTime = 0;
            MaxMemberCount = 0;
            AbundanceMaxRaw = 0;
            AbundanceSumRaw = 0;
        }

        public void AddImsMsFeature(imsMsFeature imsMsFeature)
        {
            var lcScan = imsMsFeature.ScanLC;

            foreach (var otherImsMsFeature in imsMsFeatureList.Where(otherImsMsFeature => otherImsMsFeature.ScanLC == lcScan))
            {
                FeatureUtil.MergeImsMsFeatures(imsMsFeature, otherImsMsFeature);
                imsMsFeatureList.Add(imsMsFeature);
                imsMsFeatureList.Remove(otherImsMsFeature);
                return;
            }

            imsMsFeatureList.Add(imsMsFeature);
        }

        public int GetMemberCount()
        {
            return imsMsFeatureList.Sum(imsMsFeature => imsMsFeature.MSFeatureList.Count);
        }

        public int GetSaturatedMemberCount()
        {
            var numSaturated = 0;
            foreach (var imsMsFeature in imsMsFeatureList)
            {
                foreach (var msFeature in imsMsFeature.MSFeatureList)
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
            var totalMemberCount = 0;
            var massTotal = 0.0;

            foreach (var imsMsFeature in imsMsFeatureList)
            {
                var memberCount = imsMsFeature.MSFeatureList.Count;
                massTotal += imsMsFeature.CalculateAverageMonoisotopicMass() * memberCount;
                totalMemberCount += memberCount;
            }

            var averageMass = massTotal / totalMemberCount;

            return averageMass;
        }

        [Obsolete("Unused")]
        public double CalculateAverageMz()
        {
            var totalMemberCount = 0;
            var mzTotal = 0.0;

            foreach (var imsMsFeature in imsMsFeatureList)
            {
                var memberCount = imsMsFeature.MSFeatureList.Count;
                mzTotal += imsMsFeature.CalculateAverageMz() * memberCount;
                totalMemberCount += memberCount;
            }

            var averageMz = mzTotal / totalMemberCount;

            return averageMz;
        }

        [Obsolete("Unused")]
        public double GetIntensity()
        {
            return imsMsFeatureList.Sum(imsMsFeature => imsMsFeature.GetIntensity());
        }

        public double GetFlaggedPercentage()
        {
            var numFlagged = 0;
            var numTotal = 0;

            foreach (var msFeature in imsMsFeatureList.SelectMany(imsMsFeature => imsMsFeature.MSFeatureList))
            {
                if (msFeature.ErrorFlag == 1)
                {
                    numFlagged++;
                }

                numTotal++;
            }

            var percentage = numFlagged / (double)numTotal;
            return percentage;
        }

        [Obsolete("Unused")]
        public void GetMinAndMaxScanLC(out int scanLCMinimum, out int scanLCMaximum)
        {
            var msFeatureList = new List<MSFeature>();

            foreach (var imsMsFeature in imsMsFeatureList)
            {
                msFeatureList.AddRange(imsMsFeature.MSFeatureList);
            }

            var sortByScanLCQuery = (from msFeature in msFeatureList
                                    orderby msFeature.ScanLC
                                    select msFeature).ToList();

            scanLCMinimum = sortByScanLCQuery.First().ScanLC;
            scanLCMaximum = sortByScanLCQuery.Last().ScanLC;
        }

        public void GetMinAndMaxScanIMS(
            out int scanIMSMinimum,
            out int scanIMSMaximum)
        {
            GetMinAndMaxScanLCAndScanIMSAndMSFeatureRep(out _, out _, out scanIMSMinimum, out scanIMSMaximum, out _);
        }

        public void GetMinAndMaxScanLCAndScanIMSAndMSFeatureRep(
            out int scanLCMinimum,
            out int scanLCMaximum,
            out int scanIMSMinimum,
            out int scanIMSMaximum,
            out MSFeature msFeatureRep)
        {
            var msFeatureList = new List<MSFeature>();

            foreach (var imsMsFeature in imsMsFeatureList)
            {
                msFeatureList.AddRange(imsMsFeature.MSFeatureList);
            }

            var sortByScanLCQuery = (from msFeature in msFeatureList
                                    orderby msFeature.ScanLC
                                    select msFeature).ToList();

            scanLCMinimum = sortByScanLCQuery.First().ScanLC;
            scanLCMaximum = sortByScanLCQuery.Last().ScanLC;

            var sortByScanIMSQuery = (from msFeature in msFeatureList
                                     orderby msFeature.ScanIMS
                                     select msFeature).ToList();

            scanIMSMinimum = sortByScanIMSQuery.First().ScanIMS;
            scanIMSMaximum = sortByScanIMSQuery.Last().ScanIMS;

            var sortByAbundanceQuery = from msFeature in msFeatureList
                                       orderby msFeature.Abundance descending
                                       select msFeature;

            msFeatureRep = sortByAbundanceQuery.First();
        }

        [Obsolete("Unused")]
        public void GetMinAndMaxScanLCAndDriftTimeAndMSFeatureRep(
            out int scanLCMinimum, out int scanLCMaximum,
            out double driftTimeMinimum, out double driftTimeMaximum, out MSFeature msFeatureRep)
        {
            var msFeatureList = new List<MSFeature>();

            foreach (var imsMsFeature in imsMsFeatureList)
            {
                msFeatureList.AddRange(imsMsFeature.MSFeatureList);
            }

            var sortByScanLCQuery = (from msFeature in msFeatureList
                                    orderby msFeature.ScanLC
                                    select msFeature).ToList();

            scanLCMinimum = sortByScanLCQuery.First().ScanLC;
            scanLCMaximum = sortByScanLCQuery.Last().ScanLC;

            var sortByScanIMSQuery = (from msFeature in msFeatureList
                                     orderby msFeature.DriftTime
                                     select msFeature).ToList();

            driftTimeMinimum = sortByScanIMSQuery.First().DriftTime;
            driftTimeMaximum = sortByScanIMSQuery.Last().DriftTime;

            var sortByAbundanceQuery = from msFeature in msFeatureList
                                       orderby msFeature.Abundance descending
                                       select msFeature;

            msFeatureRep = sortByAbundanceQuery.First();
        }

        public MSFeature GetMSFeatureRep()
        {
            var msFeatureList = new List<MSFeature>();

            foreach (var imsMsFeature in imsMsFeatureList)
            {
                msFeatureList.AddRange(imsMsFeature.MSFeatureList);
            }

            var sortByAbundanceQuery = from msFeature in msFeatureList
                                       orderby msFeature.Abundance descending
                                       select msFeature;

            var msFeatureRep = sortByAbundanceQuery.First();
            return msFeatureRep;
        }


        public List<XYPair>  GetIMSScanProfileFromMSFeatures()
        {
            GetMinAndMaxScanIMS(out var scanIMSMinimum, out var scanIMSMaximum);

            var xyPairs = new List<XYPair>();

            for (var imsScan = scanIMSMinimum; imsScan <= scanIMSMaximum; imsScan++)
            {
                float summedIntensityForIMSScan = 0;

                foreach (var imsMsFeature in imsMsFeatureList)
                {
                    summedIntensityForIMSScan +=
                        imsMsFeature.MSFeatureList.Where(p => p.ScanIMS == imsScan).Select(p =>(float)p.IntensityUnSummed).Sum       //note: need float due overflow exception from value exceeding int32
                            ();
                }

                var pair = new XYPair(imsScan,summedIntensityForIMSScan);

                xyPairs.Add(pair);
            }


            ConformationDetection.PadXYPairsWithZeros(ref xyPairs, 5);

            return xyPairs;




        }


        public List<XYPair> GetIMSScanProfileFromRawData(DataReader uimfReader, UIMFData.FrameType frameType, double binWidth, double calibrationSlope, double calibrationIntercept)
        {

            GetMinAndMaxScanLCAndScanIMSAndMSFeatureRep(
                out var scanLCMinimum, out var scanLCMaximum,
                out var scanIMSMinimum, out var scanIMSMaximum, out var msFeatureRep);

            // double currentFWHM = msFeatureRep.Fwhm;
            // var currentMonoMZ = msFeatureRep.MassMonoisotopic/msFeatureRep.Charge + 1.0072649;
            var mzMostAbundantIsotope = msFeatureRep.MassMostAbundantIsotope / msFeatureRep.Charge + 1.00727649;


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

            var frameMinimum = ScanLCMap.Mapping[scanLCMinimum];
            var frameMaximum = ScanLCMap.Mapping[scanLCMaximum];


            int[] scanValues = null;
            int[] intensityValues = null;

            var sigma = msFeatureRep.Fwhm / 2.35;
            var toleranceInMZ = 2 * sigma ;    //  this is a +/- value;  so    4* sigma = 95% of a normal distribution

            //Before: a wide m/z was used when generating the drift time profile.
            //uimfReader.GetDriftTimeProfile(frameIndexMinimum, frameIndexMaximum, frameType, scanIMSMinimum, scanIMSMaximum, midPointMZ, wideToleranceInMZ, ref scanValues, ref intensityValues);

            //now:  a narrow m/z range is used when generating the drift time profile
            uimfReader.GetDriftTimeProfile(frameMinimum, frameMaximum, frameType, scanIMSMinimum, scanIMSMaximum, mzMostAbundantIsotope, toleranceInMZ, ref scanValues, ref intensityValues);

            var imsScanProfile = intensityValues.Select((t, i) => new XYPair(scanIMSMinimum + i, t)).ToList();

            ConformationDetection.PadXYPairsWithZeros(ref imsScanProfile, 5);

            return imsScanProfile;
        }

        [Obsolete("Unused")]
        public void PrintLCAndDriftTimeMap()
        {
            var msFeatureList = new List<MSFeature>();

            foreach (var imsMsFeature in imsMsFeatureList)
            {
                msFeatureList.AddRange(imsMsFeature.MSFeatureList);
            }

            var groupByScanLCQuery = from msFeature in msFeatureList
                                     group msFeature by new { msFeature.ScanLC } into newGroup
                                     select newGroup;

            foreach (IEnumerable<MSFeature> item in groupByScanLCQuery)
            {
                var msFeatureGroup = item.ToList();

                Console.Write("LC Scan = " + msFeatureGroup.First().ScanLC + ": ");

                var orderByDriftTimeQuery = from msFeature in msFeatureGroup
                                            orderby msFeature.ScanLC, msFeature.DriftTime ascending
                                            select msFeature;

                foreach (var msFeature in orderByDriftTimeQuery)
                {
                    Console.Write(msFeature.DriftTime + ",");
                }

                Console.Write("\n");
            }
        }
    }
}
