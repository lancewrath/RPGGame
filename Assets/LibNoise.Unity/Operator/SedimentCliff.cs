using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a cliff detection module that compares pre-erosion and post-erosion heights
    /// to identify steep areas (cliffs) where erosion has created sharp drops.
    /// [OPERATOR]
    /// </summary>
    public class SedimentCliff : ModuleBase
    {
        #region Fields

        private double _cliffThreshold = 0.1; // Minimum height difference to be considered a cliff

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of SedimentCliff.
        /// </summary>
        public SedimentCliff()
            : base(2)
        {
        }

        /// <summary>
        /// Initializes a new instance of SedimentCliff.
        /// </summary>
        /// <param name="preErosion">The pre-erosion height module.</param>
        /// <param name="postErosion">The post-erosion height module.</param>
        public SedimentCliff(ModuleBase preErosion, ModuleBase postErosion)
            : base(2)
        {
            Modules[0] = preErosion;
            Modules[1] = postErosion;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the threshold for cliff detection (minimum height difference).
        /// </summary>
        public double CliffThreshold
        {
            get { return _cliffThreshold; }
            set { _cliffThreshold = value; }
        }

        #endregion

        #region ModuleBase Members

        /// <summary>
        /// Returns the cliff mask value (0.0 to 1.0) for the given input coordinates.
        /// Higher values indicate stronger cliff presence (steep drops from erosion).
        /// </summary>
        /// <param name="x">The input coordinate on the x-axis.</param>
        /// <param name="y">The input coordinate on the y-axis.</param>
        /// <param name="z">The input coordinate on the z-axis.</param>
        /// <returns>The cliff mask value (0.0 to 1.0).</returns>
        public override double GetValue(double x, double y, double z)
        {
            Debug.Assert(Modules[0] != null);
            Debug.Assert(Modules[1] != null);
            
            double preHeight = Modules[0].GetValue(x, y, z);
            double postHeight = Modules[1].GetValue(x, y, z);
            
            // Calculate height difference (erosion removed material)
            double heightDiff = preHeight - postHeight;
            
            // If height difference is significant, this is a cliff area
            if (heightDiff > _cliffThreshold)
            {
                // Normalize to 0.0-1.0 range
                // Use a smooth curve to map height difference to cliff strength
                double normalized = System.Math.Clamp((heightDiff - _cliffThreshold) / (1.0 - _cliffThreshold), 0.0, 1.0);
                // Apply smoothstep for smoother transitions
                double t = normalized;
                return 3.0 * t * t - 2.0 * t * t * t; // Smoothstep
            }
            
            return 0.0;
        }

        #endregion
    }
}

