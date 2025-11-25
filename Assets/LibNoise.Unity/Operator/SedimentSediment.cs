using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a sediment detection module that compares pre-erosion and post-erosion heights
    /// to identify areas where material has been deposited (sediment).
    /// [OPERATOR]
    /// </summary>
    public class SedimentSediment : ModuleBase
    {
        #region Fields

        private double _sedimentThreshold = 0.01; // Minimum height increase to be considered sediment

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of SedimentSediment.
        /// </summary>
        public SedimentSediment()
            : base(2)
        {
        }

        /// <summary>
        /// Initializes a new instance of SedimentSediment.
        /// </summary>
        /// <param name="preErosion">The pre-erosion height module.</param>
        /// <param name="postErosion">The post-erosion height module.</param>
        public SedimentSediment(ModuleBase preErosion, ModuleBase postErosion)
            : base(2)
        {
            Modules[0] = preErosion;
            Modules[1] = postErosion;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the threshold for sediment detection (minimum height increase).
        /// </summary>
        public double SedimentThreshold
        {
            get { return _sedimentThreshold; }
            set { _sedimentThreshold = value; }
        }

        #endregion

        #region ModuleBase Members

        /// <summary>
        /// Returns the sediment mask value (0.0 to 1.0) for the given input coordinates.
        /// Higher values indicate stronger sediment presence (material deposited).
        /// </summary>
        /// <param name="x">The input coordinate on the x-axis.</param>
        /// <param name="y">The input coordinate on the y-axis.</param>
        /// <param name="z">The input coordinate on the z-axis.</param>
        /// <returns>The sediment mask value (0.0 to 1.0).</returns>
        public override double GetValue(double x, double y, double z)
        {
            Debug.Assert(Modules[0] != null);
            Debug.Assert(Modules[1] != null);
            
            double preHeight = Modules[0].GetValue(x, y, z);
            double postHeight = Modules[1].GetValue(x, y, z);
            
            // Calculate height difference (positive means material was deposited)
            double heightDiff = postHeight - preHeight;
            
            // If height increased, this is a sediment area
            if (heightDiff > _sedimentThreshold)
            {
                // Normalize to 0.0-1.0 range
                // Use a smooth curve to map height difference to sediment strength
                double normalized = System.Math.Clamp((heightDiff - _sedimentThreshold) / (1.0 - _sedimentThreshold), 0.0, 1.0);
                // Apply smoothstep for smoother transitions
                double t = normalized;
                return 3.0 * t * t - 2.0 * t * t * t; // Smoothstep
            }
            
            return 0.0;
        }

        #endregion
    }
}

