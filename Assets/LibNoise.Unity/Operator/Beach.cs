using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a beach noise module that creates beach-like areas near a specified water level.
    /// Detects areas near the water level and creates smooth transitions for beach terrain.
    /// [OPERATOR]
    /// </summary>
    public class Beach : ModuleBase
    {
        #region Fields

        private double _waterLevel = 0.0; // Water level (height threshold)
        private double _beachSize = 0.1; // Size of beach area (distance from water level)
        private double _beachHeight = 0.05; // Height adjustment for beach areas
        private double _smoothRange = 0.02; // Smooth transition range

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Beach.
        /// </summary>
        public Beach()
            : base(1)
        {
        }

        /// <summary>
        /// Initializes a new instance of Beach.
        /// </summary>
        /// <param name="input">The input module.</param>
        public Beach(ModuleBase input)
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
        /// Gets or sets the height adjustment for beach areas.
        /// </summary>
        public double BeachHeight
        {
            get { return _beachHeight; }
            set { _beachHeight = value; }
        }

        /// <summary>
        /// Gets or sets the smooth transition range for beach edges.
        /// </summary>
        public double SmoothRange
        {
            get { return _smoothRange; }
            set { _smoothRange = value; }
        }

        #endregion

        #region ModuleBase Members

        /// <summary>
        /// Returns the output value for the given input coordinates.
        /// </summary>
        /// <param name="x">The input coordinate on the x-axis.</param>
        /// <param name="y">The input coordinate on the y-axis.</param>
        /// <param name="z">The input coordinate on the z-axis.</param>
        /// <returns>The resulting output value with beach areas applied.</returns>
        public override double GetValue(double x, double y, double z)
        {
            Debug.Assert(Modules[0] != null);
            
            double height = Modules[0].GetValue(x, y, z);
            double distanceFromWater = System.Math.Abs(height - _waterLevel);
            
            // Calculate beach influence (1.0 at water level, 0.0 far from water)
            double beachInfluence = 0.0;
            if (distanceFromWater <= _beachSize)
            {
                // Calculate influence based on distance from water level
                double normalizedDistance = distanceFromWater / _beachSize;
                
                // Smooth transition using smoothstep
                if (_smoothRange > 0.0001)
                {
                    // Apply smoothstep for smooth transitions
                    double t = System.Math.Clamp(normalizedDistance / (1.0 + _smoothRange), 0.0, 1.0);
                    beachInfluence = 1.0 - (3.0 * t * t - 2.0 * t * t * t); // Smoothstep
                }
                else
                {
                    beachInfluence = 1.0 - normalizedDistance;
                }
            }
            
            // Apply beach height adjustment
            // Beach areas are raised slightly above water level
            double beachHeightAdjustment = _beachHeight * beachInfluence;
            
            // Blend between original height and beach height
            double targetHeight = _waterLevel + _beachHeight;
            double result = height * (1.0 - beachInfluence) + targetHeight * beachInfluence;
            
            // Ensure beach doesn't go below water level
            if (beachInfluence > 0.5 && result < _waterLevel)
            {
                result = _waterLevel + (result - _waterLevel) * 0.5;
            }
            
            return result;
        }

        #endregion
    }
}

