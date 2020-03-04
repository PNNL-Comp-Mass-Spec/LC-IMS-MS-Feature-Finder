using System;
using System.Collections.Generic;
using System.IO;

namespace FeatureFinder.Control
{
    public class DeconToolsFilterLoader
    {
        public List<DeconToolsFilter> DeconToolsFilterList = new List<DeconToolsFilter>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="deconToolsFilterFilePath"></param>
        public DeconToolsFilterLoader(string deconToolsFilterFilePath)
        {

            if (!File.Exists(deconToolsFilterFilePath))
            {
                var errorMessage = "DeconTools filter settings could not be loaded; File not found: " + deconToolsFilterFilePath;
                Logger.LogError(errorMessage);
                throw new FileNotFoundException(errorMessage);
            }

            var linesRead = 0;
            DeconToolsFilterList.Clear();

            using (var reader = new StreamReader(deconToolsFilterFilePath))
            {
                if (reader.EndOfStream)
                {
                    var errorMessage = "DeconTools filter file is empty: "+ deconToolsFilterFilePath;
                    Logger.LogError(errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                // Skip the header line, which should have 6 columns:
                // chargeMin	chargeMax	abundanceMin	abundanceMax	iscoreCutoff	fitScoreCutoff
                reader.ReadLine();
                linesRead++;

                while (!reader.EndOfStream)
                {
                    var dataLine = reader.ReadLine();
                    linesRead++;

                    if (string.IsNullOrWhiteSpace(dataLine))
                        continue;

                    var parsedLine = dataLine.Split('\t');

                    if (parsedLine.Length < 6)
                    {
                        var errorMessage = string.Format(
                            "Error loading DeconTools filter settings file; the file should have six tab-delimited columns, but line {0} has {1} columns",
                            linesRead, parsedLine.Length);

                        Logger.LogError(errorMessage);
                        throw new ArgumentException(errorMessage);
                    }

                    try
                    {
                        var zMin = Convert.ToInt32(parsedLine[0]);
                        var zMax = Convert.ToInt32(parsedLine[1]);
                        var abundanceMin = Convert.ToInt32(parsedLine[2]);
                        var abundanceMax = Convert.ToInt32(parsedLine[3]);

                        // Interference score cutoff (0 means no interference; 1 means lots of interference)
                        var iscoreCutoff = Convert.ToDouble(parsedLine[4]);

                        // Isotopic fit score cutoff
                        var fitScoreCutoff = Convert.ToDouble(parsedLine[5]);

                        var f = new DeconToolsFilter(zMin, zMax, abundanceMin, abundanceMax, fitScoreCutoff, iscoreCutoff);
                        DeconToolsFilterList.Add(f);
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = string.Format(
                            "Error reading data from the DeconTools filter settings file on line {0}, {1}",
                            linesRead, dataLine);
                        Logger.LogError(errorMessage, ex);

                        throw new Exception(errorMessage + ": " + ex.Message);
                    }
                }
            }
        }

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

    }
}
