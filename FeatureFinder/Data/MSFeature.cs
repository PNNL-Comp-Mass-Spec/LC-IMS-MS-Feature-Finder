using System;
using System.Collections.Generic;

namespace FeatureFinder.Data
{
	public class MSFeature : IComparable<MSFeature>
	{
		private byte m_charge;
		private byte m_errorFlag;

		private int m_id;
		private int m_indexInFile;
		private int m_filteredIndex;
		private int m_abundance;
		private int m_scanLC;
		private int m_scanIMS;

		private float m_mz;
		private float m_fit;
		private float m_fwhm;
		private float m_driftTime;
		private float m_massMonoisotopic;

		public MSFeature()
		{
			m_filteredIndex = -1;
		}

		public int CompareTo(MSFeature otherMSFeature)
		{
			return this.Id.CompareTo(otherMSFeature.Id);
		}

		public static Comparison<MSFeature> MassComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			return msFeature1.MassMonoisotopic.CompareTo(msFeature2.MassMonoisotopic);
		};

		public static Comparison<MSFeature> MassDescendingComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			return msFeature2.MassMonoisotopic.CompareTo(msFeature1.MassMonoisotopic);
		};

		public static Comparison<MSFeature> ScanLCComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			return msFeature1.ScanLC.CompareTo(msFeature2.ScanLC);
		};

		public static Comparison<MSFeature> DriftsComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			return msFeature1.DriftTime.CompareTo(msFeature2.DriftTime);
		};

		public static Comparison<MSFeature> ScanIMSComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			return msFeature1.ScanIMS.CompareTo(msFeature2.ScanIMS);
		};

		public static Comparison<MSFeature> IDComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			return msFeature1.Id.CompareTo(msFeature2.Id);
		};

		public static Comparison<MSFeature> AbundanceDescendingComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			return msFeature2.Abundance.CompareTo(msFeature1.Abundance);
		};

		public static Comparison<MSFeature> ScanLCAndDriftComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			if (msFeature1.ScanLC != msFeature2.ScanLC)
			{
				return msFeature1.ScanLC.CompareTo(msFeature2.ScanLC);
			}
			else
			{
				return msFeature1.DriftTime.CompareTo(msFeature2.DriftTime);
			}
		};

		public static Comparison<MSFeature> ScanLCAndMassComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			if (msFeature1.ScanLC != msFeature2.ScanLC)
			{
				return msFeature1.ScanLC.CompareTo(msFeature2.ScanLC);
			}
			else
			{
				return msFeature1.MassMonoisotopic.CompareTo(msFeature2.MassMonoisotopic);
			}
		};

		public static Comparison<MSFeature> ChargeAndScanLCComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			if (msFeature1.Charge != msFeature2.Charge)
			{
				return msFeature1.Charge.CompareTo(msFeature2.Charge);
			}
			else
			{
				return msFeature1.ScanLC.CompareTo(msFeature2.ScanLC);
			}
		};

		public static Comparison<MSFeature> ChargeAndMassComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			if (msFeature1.Charge != msFeature2.Charge)
			{
				return msFeature1.Charge.CompareTo(msFeature2.Charge);
			}
			else
			{
				return msFeature1.MassMonoisotopic.CompareTo(msFeature2.MassMonoisotopic);
			}
		};

		public static Comparison<MSFeature> ScanLCAndScanIMSAndMassComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			if (msFeature1.ScanLC != msFeature2.ScanLC)
			{
				return msFeature1.ScanLC.CompareTo(msFeature2.ScanLC);
			}
			else if (msFeature1.ScanIMS != msFeature2.ScanIMS)
			{
				return msFeature1.ScanIMS.CompareTo(msFeature2.ScanIMS);
			}
			else
			{
				return msFeature1.MassMonoisotopic.CompareTo(msFeature2.MassMonoisotopic);
			}
		};

		public byte Charge
		{
			get { return m_charge; }
			set { m_charge = value; }
		}

		public byte ErrorFlag
		{
			get { return m_errorFlag; }
			set { m_errorFlag = value; }
		}

		public int Id
		{
			get { return m_id; }
			set { m_id = value; }
		}

		public int IndexInFile
		{
			get { return m_indexInFile; }
			set { m_indexInFile = value; }
		}

		public int FilteredIndex
		{
			get { return m_filteredIndex; }
			set { m_filteredIndex = value; }
		}

		public int Abundance
		{
			get { return m_abundance; }
			set { m_abundance = value; }
		}

		public int ScanLC
		{
			get { return m_scanLC; }
			set { m_scanLC = value; }
		}

		public int ScanIMS
		{
			get { return m_scanIMS; }
			set { m_scanIMS = value; }
		}

		public float Fit
		{
			get { return m_fit; }
			set { m_fit = value; }
		}

		public float Fwhm
		{
			get { return m_fwhm; }
			set { m_fwhm = value; }
		}

		public float DriftTime
		{
			get { return m_driftTime; }
			set { m_driftTime = value; }
		}

		public float Mz
		{
			get { return m_mz; }
			set { m_mz = value; }
		}

		public float MassMonoisotopic
		{
			get { return m_massMonoisotopic; }
			set { m_massMonoisotopic = value; }
		}
	}
}
