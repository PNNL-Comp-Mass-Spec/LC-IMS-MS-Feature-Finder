using System;
using System.Collections.Generic;
using System.Text;
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

            using (StreamReader sr = new StreamReader(filterTableTextFile))
            {
                sr.ReadLine();   //headerline

                while (sr.Peek() != -1)
                {

                    string line = sr.ReadLine();

                	if (line == null) continue;
                	string[] parsedLine = line.Split('\t');

                	if (parsedLine.Length != 6)
                	{
                		Logger.Log("Error loading DeconTools filter settings file.");
                		throw new ArgumentException("Error loading DeconTools filter settings file.");
                	}

                	int zMin = Convert.ToInt32(parsedLine[0]);
                	int zMax = Convert.ToInt32(parsedLine[1]);
                	int abundanceMin = Convert.ToInt32(parsedLine[2]);
                	int abundanceMax = Convert.ToInt32(parsedLine[3]);

                	double iscoreCutoff = Convert.ToDouble(parsedLine[4]);
                	double fitScoreCutoff = Convert.ToDouble(parsedLine[5]);

                	DeconToolsFilter f = new DeconToolsFilter(zMin, zMax, abundanceMin, abundanceMax, fitScoreCutoff, iscoreCutoff);
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
            StringBuilder sb = new StringBuilder();
            foreach (var filter in DeconToolsFilterList)
            {
                sb.Append(filter.ChargeMinimum);
                sb.Append("\t");
                sb.Append(filter.ChargeMaximum);
                sb.Append("\t");
                sb.Append(filter.AbundanceMinimum);
                sb.Append("\t");
                sb.Append(filter.AbundanceMaximum);
                sb.Append("\t");
                sb.Append(filter.FitScoreMaximum);
                sb.Append("\t");
                sb.Append(filter.InterferenceScoreMaximum);
                sb.Append(Environment.NewLine);
            }

            Console.WriteLine(sb.ToString());
        }



        #endregion

        #region Private Methods

        #endregion

    }
}
