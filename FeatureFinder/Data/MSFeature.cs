using System;

namespace FeatureFinder.Data
{
	public class MSFeature : IComparable<MSFeature>
	{
		public MSFeature()
		{
			FilteredIndex = -1;
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
			return msFeature1.ScanLC != msFeature2.ScanLC ? msFeature1.ScanLC.CompareTo(msFeature2.ScanLC) : msFeature1.DriftTime.CompareTo(msFeature2.DriftTime);
		};

		public static Comparison<MSFeature> ScanLCAndMassComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			return msFeature1.ScanLC != msFeature2.ScanLC ? msFeature1.ScanLC.CompareTo(msFeature2.ScanLC) : msFeature1.MassMonoisotopic.CompareTo(msFeature2.MassMonoisotopic);
		};

		public static Comparison<MSFeature> ChargeAndScanLCComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			return msFeature1.Charge != msFeature2.Charge ? msFeature1.Charge.CompareTo(msFeature2.Charge) : msFeature1.ScanLC.CompareTo(msFeature2.ScanLC);
		};

		public static Comparison<MSFeature> ChargeAndMassComparison = delegate(MSFeature msFeature1, MSFeature msFeature2)
		{
			return msFeature1.Charge != msFeature2.Charge ? msFeature1.Charge.CompareTo(msFeature2.Charge) : msFeature1.MassMonoisotopic.CompareTo(msFeature2.MassMonoisotopic);
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

		public byte Charge { get; set; }
		public byte ErrorFlag { get; set; }

		public int Id { get; set; }
		public int IndexInFile { get; set; }
		public int FilteredIndex { get; set; }
		public int Abundance { get; set; }
		public int ScanLC { get; set; }
		public int ScanIMS { get; set; }
		public int IntensityUnSummed { get; set; }

		public float Fit { get; set; }
		public float InterferenceScore { get; set; }
		public float Fwhm { get; set; }
		public float DriftTime { get; set; }

		public double Mz { get; set; }
		public double MassMonoisotopic { get; set; }
	    public double MassMostAbundantIsotope { get; set; }

	

		public bool IsSaturated { get; set; }
	}
}
