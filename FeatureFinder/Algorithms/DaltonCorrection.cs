﻿using System;
using System.Collections.Generic;
using System.Linq;
using FeatureFinder.Data;
using FeatureFinder.Control;
using FeatureFinder.Utilities;

namespace FeatureFinder.Algorithms
{
    public static class DaltonCorrection
    {
        public static IEnumerable<LCIMSMSFeature> CorrectLCIMSMSFeatures(List<LCIMSMSFeature> lcImsMsFeatureEnumerable)
        {
            var massToleranceBase = Settings.MassMonoisotopicConstraint;

            // ReSharper disable once NotAccessedVariable
            var totalFound = 0;

            foreach (var lcimsmsFeature in lcImsMsFeatureEnumerable)
            {
                if (lcimsmsFeature.imsMsFeatureList.Count == 0)
                {
                    continue;
                }

                var averageMass = lcimsmsFeature.CalculateAverageMonoisotopicMass();
                var massTolerance = massToleranceBase * averageMass / 1000000.0;

                var errorFlagPercentage = lcimsmsFeature.GetFlaggedPercentage();

                var searchForDaErrorQuery = from otherLCIMSMSFeature in lcImsMsFeatureEnumerable
                                            where
                                                Math.Abs(averageMass - otherLCIMSMSFeature.CalculateAverageMonoisotopicMass()) >= (1 - massTolerance)
                                                && Math.Abs(averageMass - otherLCIMSMSFeature.CalculateAverageMonoisotopicMass()) <= (1 + massTolerance)
                                                && otherLCIMSMSFeature.imsMsFeatureList.Count > 0
                                            orderby Math.Abs(errorFlagPercentage - otherLCIMSMSFeature.GetFlaggedPercentage()) descending
                                            select otherLCIMSMSFeature;

                foreach (var lcimsmsFeatureToCheck in searchForDaErrorQuery.AsParallel().Where(lcimsmsFeatureToCheck => Math.Abs(errorFlagPercentage - lcimsmsFeatureToCheck.GetFlaggedPercentage()) > 0.3))
                {
                    //bool featuresFitTogether = FeatureUtil.DoLCIMSMSFeaturesFitTogether(lcimsmsFeature, lcimsmsFeatureToCheck);

                    //if (featuresFitTogether)
                    //{
                    if (errorFlagPercentage > lcimsmsFeatureToCheck.GetFlaggedPercentage())
                    {
                        FeatureUtil.MergeLCIMSMSFeatures(lcimsmsFeatureToCheck, lcimsmsFeature);
                    }
                    else
                    {
                        FeatureUtil.MergeLCIMSMSFeatures(lcimsmsFeature, lcimsmsFeatureToCheck);
                    }

                    totalFound++;
                    break;
                    //}
                }
            }

            var newFeatureListQuery = lcImsMsFeatureEnumerable.Where(lcimsmsFeature => lcimsmsFeature.imsMsFeatureList.Count > 0);

            return newFeatureListQuery.AsEnumerable();
        }
    }
}
