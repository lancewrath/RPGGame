using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a beach sand mask module that outputs a mask for beach sand areas.
    /// This is a secondary output from the Beach module, used for splatting sand textures.
    /// [OPERATOR]
    /// </summary>
    public class BeachSand : ModuleBase
    {
        #region Fields

        private double _waterLevel = 0.0; // Water level (height threshold)
        private double _beachSize = 0.1; // Size of beach area (distance from water level)
        private double _sandBlur = 0.05; // Blur amount for sand mask

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of BeachSand.
        /// </summary>
        public BeachSand()
            : base(1)
        {
        }

        /// <summary>
        /// Initializes a new instance of BeachSand.
        /// </summary>
        /// <param name="input">The input module.</param>
        public BeachSand(ModuleBase input)
            : base(1)
        {
            Modules[0] = input;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the water level (height threshold for beach detection).
        /// </summary>
        public double WaterLevel
        {
            get { return _waterLevel; }
            set { _waterLevel = value; }
        }

        /// <summary>
        /// Gets or sets the size of the beach area (distance from water level).
        /// </summary>
        public double BeachSize
        {
            get { return _beachSize; }
            set { _beachSize = value; }
        }

        /// <summary>
        /// Gets or sets the blur amount for the sand mask (smoothing distance).
        /// </summary>
        public double SandBlur
        {
            get { return _sandBlur; }
            set { _sandBlur = value; }
        }

        #endregion

        #region ModuleBase Members

        /// <summary>
        /// Returns the sand mask value (0.0 to 1.0) for the given input coordinates.
        /// Higher values indicate stronger sand presence.
        /// </summary>
        /// <param name="x">The input coordinate on the x-axis.</param>
        /// <param name="y">The input coordinate on the y-axis.</param>
        /// <param name="z">The input coordinate on the z-axis.</param>
        /// <returns>The sand mask value (0.0 to 1.0).</returns>
        public override double GetValue(double x, double y, double z)
        {
            Debug.Assert(Modules[0] != null);
            
            double height = Modules[0].GetValue(x, y, z);
            double distanceFromWater = System.Math.Abs(height - _waterLevel);
            
            // Calculate sand mask (1.0 at water level, 0.0 far from water)
            double sandMask = 0.0;
            double effectiveBeachSize = _beachSize + _sandBlur;
            
            if (distanceFromWater <= effectiveBeachSize)
            {
                // Calculate influence based on distance from water level
                double normalizedDistance = distanceFromWater / effectiveBeachSize;
                
                // Smooth transition using smoothstep
                double t = System.Math.Clamp(normalizedDistance, 0.0, 1.0);
                sandMask = 1.0 - (3.0 * t * t - 2.0 * t * t * t); // Smoothstep
            }
            
            // Normalize to 0.0-1.0 range (remap from -1,1 to 0,1 if needed)
            // Since we're already calculating 0-1, we can return as-is
            return System.Math.Clamp(sandMask, 0.0, 1.0);
        }

        #endregion
    }
}

