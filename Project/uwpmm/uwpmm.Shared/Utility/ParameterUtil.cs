﻿using Kazyx.Uwpmm.DataModel;

namespace Kazyx.Uwpmm.Utility
{
    public class ParameterUtil
    {
        /// <summary>
        /// Convert to the nearest color temperture candidate value.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int AsValidColorTemperture(int source, CameraStatus status)
        {
            var candidates = status.ColorTempertureCandidates[status.WhiteBalance.current];
            if (candidates.Length < 2)
            {
                return -1;
            }
            var step = candidates[1] - candidates[0];

            var index_below = (source - candidates[0]) / step;
            if (index_below == candidates.Length - 1)
            {
                return candidates[index_below];
            }

            var diff_below = source - candidates[index_below];
            var diff_above = candidates[index_below + 1] - source;

            return diff_below < diff_above ? candidates[index_below] : candidates[index_below + 1];
        }
    }
}
