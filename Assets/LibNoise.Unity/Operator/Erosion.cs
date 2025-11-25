using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a simplified erosion noise module that smooths terrain by simulating
    /// water erosion effects. Uses gradient-based smoothing to erode high areas more than low areas.
    /// This is a simplified version - for full erosion simulation, use a matrix-based approach.
    /// [OPERATOR]
    /// </summary>
    public class Erosion : ModuleBase
    {
        #region Fields

        private double _intensity = 0.5; // Erosion intensity (0.0 to 1.0)
        private double _iterations = 1.0; // Number of erosion iterations (affects smoothing)
        private double _sampleDistance = 1.0; // Distance for gradient sampling

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Erosion.
        /// </summary>
        public Erosion()
            : base(1)
        {
        }

        /// <summary>
        /// Initializes a new instance of Erosion.
        /// </summary>
        /// <param name="input">The input module.</param>
        public Erosion(ModuleBase input)
            : base(1)
        {
            Modules[0] = input;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the erosion intensity.
        /// </summary>
        public double Intensity
        {
            get { return _intensity; }
            set { _intensity = value; }
        }

        /// <summary>
        /// Gets or sets the number of erosion iterations (affects smoothing amount).
        /// </summary>
        public double Iterations
        {
            get { return _iterations; }
            set { _iterations = value; }
        }

        /// <summary>
        /// Gets or sets the sample distance for gradient calculation.
        /// </summary>
        public double SampleDistance
        {
            get { return _sampleDistance; }
            set { _sampleDistance = value; }
        }

        #endregion

        #region ModuleBase Members

        /// <summary>
        /// Returns the output value for the given input coordinates.
        /// </summary>
        /// <param name="x">The input coordinate on the x-axis.</param>
        /// <param name="y">The input coordinate on the y-axis.</param>
        /// <param name="z">The input coordinate on the z-axis.</param>
        /// <returns>The resulting output value with erosion applied.</returns>
        public override double GetValue(double x, double y, double z)
        {
            Debug.Assert(Modules[0] != null);
            
            double centerValue = Modules[0].GetValue(x, y, z);
            double result = centerValue;
            
            // Sample neighboring points to calculate local height differences
            double sampleDist = _sampleDistance;
            
            double valueX1 = Modules[0].GetValue(x - sampleDist, y, z);
            double valueX2 = Modules[0].GetValue(x + sampleDist, y, z);
            double valueZ1 = Modules[0].GetValue(x, y, z - sampleDist);
            double valueZ2 = Modules[0].GetValue(x, y, z + sampleDist);
            
            // Find minimum height around this point (like MapMagic's approach)
            double minNeighbor = centerValue;
            if (valueX1 < minNeighbor) minNeighbor = valueX1;
            if (valueX2 < minNeighbor) minNeighbor = valueX2;
            if (valueZ1 < minNeighbor) minNeighbor = valueZ1;
            if (valueZ2 < minNeighbor) minNeighbor = valueZ2;
            
            // Calculate "erode line" - halfway between current and minimum (MapMagic approach)
            double erodeLine = (centerValue + minNeighbor) * 0.5;
            
            // Only erode if we're above the erode line
            if (centerValue > erodeLine)
            {
                // Calculate how much to erode
                double heightAboveErodeLine = centerValue - erodeLine;
                
                // Erosion amount based on intensity and height difference
                // Higher areas erode more, steeper slopes erode more
                double heightDifference = centerValue - minNeighbor;
                double erosionFactor = System.Math.Min(heightDifference * _intensity * 2.0, 1.0);
                
                // Apply erosion - reduce height toward erode line
                double erosionAmount = heightAboveErodeLine * erosionFactor;
                result = centerValue - erosionAmount;
            }
            
            // Apply multiple iterations for cumulative effect
            int iterations = (int)System.Math.Round(_iterations);
            if (iterations > 1)
            {
                // Apply smoothing/blending with neighbors for additional iterations
                double avgNeighbor = (valueX1 + valueX2 + valueZ1 + valueZ2) / 4.0;
                
                // Each iteration adds more smoothing
                for (int i = 1; i < iterations && i < 10; i++) // Cap at 10 iterations
                {
                    double blendFactor = _intensity * 0.15 * (1.0 / i); // Decreasing effect per iteration
                    result = result * (1.0 - blendFactor) + avgNeighbor * blendFactor;
                }
            }
            
            return result;
        }

        #endregion
    }
}

