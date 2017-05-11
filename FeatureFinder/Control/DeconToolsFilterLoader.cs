using System;
using System.Collections.Generic;
using System.IO;

namespace FeatureFinder.Control
{
    public class DeconToolsFilterLoader
    {
        public List<DeconToolsFilter> DeconToolsFilterList = new List<DeconToolsFilter>();

        #region Constructors

        public DeconToolsFilterLoader(string filterTableTextFile)
        {

            if (!File.Exists(filterTableTextFile))
            {
                Logger.Log("File not found error. DeconTools filter settings could not be loaded.");
                throw new FileNotFoundException("File not found error. DeconTools filter settings could not be loaded.");
            }

            using (var sr = new StreamReader(filterTableTextFile))
            {
                sr.ReadLine();   //headerline

                while (sr.Peek() != -1)
                {

                    var line = sr.ReadLine();

                    if (line == null) continue;
                    var parsedLine = line.Split('\t');

                    if (parsedLine.Length != 6)
                    {
                        Logger.Log("Error loading DeconTools filter settings file.");
                        throw new ArgumentException("Error loading DeconTools filter settings file.");
                    }

                    var zMin = Convert.ToInt32(parsedLine[0]);
                    var zMax = Convert.ToInt32(parsedLine[1]);
                    var abundanceMin = Convert.ToInt32(parsedLine[2]);
                    var abundanceMax = Convert.ToInt32(parsedLine[3]);

                    var iscoreCutoff = Convert.ToDouble(parsedLine[4]);
                    var fitScoreCutoff = Convert.ToDouble(parsedLine[5]);

                    var f = new DeconToolsFilter(zMin, zMax, abundanceMin, abundanceMax, fitScoreCutoff, iscoreCutoff);
                    DeconToolsFilterList.Add(f);
                }
            }



        }


        #endregion

        #region Properties

        #endregion

        #region Public Methods


        public void DisplayFilters()
        {
            foreach (var filter in DeconToolsFilterList)
            {
                var filterValues = new List<string>
                {
                    filter.ChargeMinimum.ToString(),
                    filter.ChargeMaximum.ToString(),
                    filter.AbundanceMinimum.ToString(),
                    filter.AbundanceMaximum.ToString(),
                    filter.FitScoreMaximum.ToString("0.000"),
                    filter.InterferenceScoreMaximum.ToString("0.000")
                };
                Console.WriteLine(string.Join("\t", filterValues));
            }

        }



        #endregion

        #region Private Methods

        #endregion

    }
}
