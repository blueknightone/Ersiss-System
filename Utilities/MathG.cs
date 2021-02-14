using System;

namespace ErsissSystem.Utilities
{
    public static class MathG
    {
        /// <summary>
        /// Compares two floats and returns whether they are near each other within a given threshold.
        /// </summary>
        /// <param name="a">The first float to compare.</param>
        /// <param name="b">The second float to compare. (Optional, default: 0f)</param>
        /// <param name="threshold">The threshold variance to pass text. (Optional, default: float.Epsilon</param>
        /// <returns></returns>
        public static bool Approximately(float a, float b, float threshold = float.Epsilon)
        {
            return Math.Abs(a - b) <= threshold;
        }
    }
}