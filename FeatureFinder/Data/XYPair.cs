namespace FeatureFinder.Data
{
    public class XYPair
    {
        public double XValue { get; set; }
        public double YValue { get; set; }

        public XYPair(double xValue, double yValue)
        {
            XValue = xValue;
            YValue = yValue;
        }


        public override string ToString()
        {
            return XValue + "; " + YValue;
        }
    }
}
