namespace FeatureFinder.Control
{
    public class DeconToolsFilter
    {
        public int ChargeMinimum { get; set; }
        public int ChargeMaximum { get; set; }
        public int AbundanceMinimum { get; set; }
        public int AbundanceMaximum { get; set; }

        public double FitScoreMaximum { get; set; }
        public double InterferenceScoreMaximum { get; set; }

        /// <summary>
        /// Constructor without values
        /// </summary>
        public DeconToolsFilter()
        {

        }

        /// <summary>
        /// Constructor with values
        /// </summary>
        /// <param name="chargeMinimum"></param>
        /// <param name="chargeMaximum"></param>
        /// <param name="abundanceMinimum"></param>
        /// <param name="abundanceMaximum"></param>
        /// <param name="fitScoreMaximum"></param>
        /// <param name="interferenceScoreMaximum"></param>
        public DeconToolsFilter(int chargeMinimum, int chargeMaximum, int abundanceMinimum, int abundanceMaximum, double fitScoreMaximum, double interferenceScoreMaximum)
        {
            ChargeMinimum = chargeMinimum;
            ChargeMaximum = chargeMaximum;
            AbundanceMinimum = abundanceMinimum;
            AbundanceMaximum = abundanceMaximum;
            FitScoreMaximum = fitScoreMaximum;
            InterferenceScoreMaximum = interferenceScoreMaximum;
        }
    }
}
