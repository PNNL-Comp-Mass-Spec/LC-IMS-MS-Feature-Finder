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

        public DeconToolsFilter()
        {

        }

        public DeconToolsFilter(int chargeMinimum, int chargeMaximum, int abundanceMinimum, int abundanceMaximum, double fitScoreMaximum, double interferenceScoreMaximum)
        {
            this.ChargeMinimum = chargeMinimum;
            this.ChargeMaximum = chargeMaximum;
            this.AbundanceMinimum = abundanceMinimum;
            this.AbundanceMaximum = abundanceMaximum;
            this.FitScoreMaximum = fitScoreMaximum;
            this.InterferenceScoreMaximum = interferenceScoreMaximum;
        }
    }
}
