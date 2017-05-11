using System;
using System.Collections.Generic;

namespace FeatureFinder.Data
{
    [Obsolete("This class is unused")]
    public class LCMSFeature
    {
        private int m_scanLCMinimum;
        private int m_scanLCMaximum;

        private double m_massMinimum;
        private double m_massMaximum;

        // This list is unused
        // private List<MSFeature> m_msFeatureList;

        // Constructor
        //public LCMSFeature()
        //{
        //    m_msFeatureList = new List<MSFeature>();
        //}

        public void Reset()
        {
            m_scanLCMinimum = int.MaxValue;
            m_scanLCMaximum = int.MinValue;
            m_massMinimum = int.MaxValue;
            m_massMaximum = int.MinValue;
        }

        private void Recalculate(MSFeature msFeature)
        {
            if (msFeature.ScanLC < m_scanLCMinimum)
            {
                m_scanLCMinimum = msFeature.ScanLC;
            }
            if (msFeature.ScanLC > m_scanLCMaximum)
            {
                m_scanLCMaximum = msFeature.ScanLC;
            }
            if (msFeature.MassMonoisotopic < m_massMinimum)
            {
                m_massMinimum = msFeature.MassMonoisotopic;
            }
            if (msFeature.MassMonoisotopic > m_massMaximum)
            {
                m_massMaximum = msFeature.MassMonoisotopic;
            }
        }

        public void AddMSFeature(MSFeature msFeature)
        {
            // m_msFeatureList.Add(msFeature);
            Recalculate(msFeature);
        }
    }
}
