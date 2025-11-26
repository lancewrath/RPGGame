using System;

namespace LibNoise.Operator
{
    /// <summary>
    /// Noise module that calculates the slope (delta) of the input heightmap.
    /// Uses MapMagic's optimized approach: calculates maximum delta between adjacent pixels.
    /// Supports angle-based filtering with smooth transitions.
    /// </summary>
    public class Slope : ModuleBase
    {
        #region Fields
        
        private double _sampleDistance;
        private double _minAngle = 0.0; // Minimum angle in degrees (0 = flat)
        private double _maxAngle = 90.0; // Maximum angle in degrees (90 = vertical)
        private double _smoothRange = 0.0; // Smooth transition range in degrees
        private double _terrainHeight = 1.0; // Terrain height scale for angle calculation
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Initializes a new instance of Slope.
        /// </summary>
        /// <param name="sampleDistance">The distance between sample points for delta calculation. Default is 1.0.</param>
        public Slope(double sampleDistance = 1.0) : base(1)
        {
            _sampleDistance = sampleDistance;
        }
        
        /// <summary>
        /// Initializes a new instance of Slope.
        /// </summary>
        /// <param name="input">The input module.</param>
        /// <param name="sampleDistance">The distance between sample points for delta calculation. Default is 1.0.</param>
        public Slope(ModuleBase input, double sampleDistance = 1.0) : base(1)
        {
            _sampleDistance = sampleDistance;
            Modules[0] = input;
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets or sets the distance between sample points for delta calculation.
        /// Smaller values give more accurate results but may be more sensitive to noise.
        /// </summary>
        public double SampleDistance
        {
            get { return _sampleDistance; }
            set { _sampleDistance = value; }
        }
        
        /// <summary>
        /// Gets or sets the minimum angle in degrees (0 = flat). Values below this will output 0.
        /// </summary>
        public double MinAngle
        {
            get { return _minAngle; }
            set { _minAngle = value; }
        }
        
        /// <summary>
        /// Gets or sets the maximum angle in degrees (90 = vertical). Values above this will output 0.
        /// </summary>
        public double MaxAngle
        {
            get { return _maxAngle; }
            set { _maxAngle = value; }
        }
        
        /// <summary>
        /// Gets or sets the smooth transition range in degrees for blending at angle boundaries.
        /// </summary>
        public double SmoothRange
        {
            get { return _smoothRange; }
            set { _smoothRange = value; }
        }
        
        /// <summary>
        /// Gets or sets the terrain height scale used for angle calculation.
        /// This should match the height scale of your terrain.
        /// </summary>
        public double TerrainHeight
        {
            get { return _terrainHeight; }
            set { _terrainHeight = value; }
        }
        
        #endregion
        
        #region ModuleBase Members
        
        /// <summary>
        /// Returns the output value for the given input coordinates.
        /// Uses MapMagic's optimized delta calculation: max of adjacent pixel deltas.
        /// </summary>
        /// <param name="x">The input coordinate on the x-axis.</param>
        /// <param name="y">The input coordinate on the y-axis.</param>
        /// <param name="z">The input coordinate on the z-axis.</param>
        /// <returns>The resulting output value (0-1 range based on angle filtering).</returns>
        public override double GetValue(double x, double y, double z)
        {
            if (Modules[0] == null)
            {
                throw new ArgumentNullException("Input module cannot be null");
            }
            
            // Sample the input module at the current point and neighbors
            // Using MapMagic's approach: calculate max delta of adjacent pixels
            double centerHeight = Modules[0].GetValue(x, y, z);
            
            // X direction: max of |x+1 - center| and |x-1 - center|
            double heightXPlus = Modules[0].GetValue(x + _sampleDistance, y, z);
            double heightXMinus = Modules[0].GetValue(x - _sampleDistance, y, z);
            double deltaXPlus = Math.Abs(heightXPlus - centerHeight);
            double deltaXMinus = Math.Abs(heightXMinus - centerHeight);
            double deltaX = deltaXPlus > deltaXMinus ? deltaXPlus : deltaXMinus;
            
            // Z direction: max of |z+1 - center| and |z-1 - center|
            double heightZPlus = Modules[0].GetValue(x, y, z + _sampleDistance);
            double heightZMinus = Modules[0].GetValue(x, y, z - _sampleDistance);
            double deltaZPlus = Math.Abs(heightZPlus - centerHeight);
            double deltaZMinus = Math.Abs(heightZMinus - centerHeight);
            double deltaZ = deltaZPlus > deltaZMinus ? deltaZPlus : deltaZMinus;
            
            // Take the maximum of both directions (MapMagic approach)
            double maxDelta = deltaX > deltaZ ? deltaX : deltaZ;
            
            // MapMagic converts angle thresholds to delta thresholds, then filters
            // Formula: deltaThreshold = Tan(angle) * pixelSize / height
            // MapMagic works with normalized heights [0,1], where delta is also in [0,1]
            // Our noise values are in [-1,1], so delta is in [0, 2] range (absolute difference)
            // We need to normalize delta to [0,1] to match MapMagic's approach
            double pixelSize = _sampleDistance; // World units between samples
            
            // Normalize delta from [0, 2] to [0, 1] range (MapMagic uses normalized heights)
            // Max possible delta in noise space is 2.0 (from -1 to 1)
            double maxDeltaNormalized = maxDelta / 2.0;
            
            // Calculate angle ranges with smooth transitions
            double minAng0 = _minAngle - _smoothRange / 2.0;
            double minAng1 = _minAngle + _smoothRange / 2.0;
            double maxAng0 = _maxAngle - _smoothRange / 2.0;
            double maxAng1 = _maxAngle + _smoothRange / 2.0;
            
            // Convert angle thresholds to delta thresholds (MapMagic approach)
            // Formula: deltaThreshold = Tan(angle) * pixelSize / height
            // This gives normalized delta threshold [0,1]
            double minDel0 = Math.Tan(minAng0 * Math.PI / 180.0) * pixelSize / _terrainHeight;
            double minDel1 = Math.Tan(minAng1 * Math.PI / 180.0) * pixelSize / _terrainHeight;
            double maxDel0 = Math.Tan(maxAng0 * Math.PI / 180.0) * pixelSize / _terrainHeight;
            double maxDel1 = Math.Tan(maxAng1 * Math.PI / 180.0) * pixelSize / _terrainHeight;
            
            // Handle edge cases (MapMagic approach)
            if (_minAngle < 0.00001) { minDel0 = -1; minDel1 = -1; }
            if (maxAng0 > 89.9) maxDel0 = 20000000;
            if (maxAng1 > 89.9) maxDel1 = 20000000;
            
            // Apply SelectRange logic: filter delta values by threshold range
            // Returns delta if within range [minDel1, maxDel0], with smooth transitions
            // Use normalized delta for comparison (matches MapMagic's normalized height space)
            double result;
            
            if (maxDeltaNormalized < minDel0 || maxDeltaNormalized > maxDel1)
            {
                result = 0.0; // Outside range
            }
            else if (maxDeltaNormalized > minDel1 && maxDeltaNormalized < maxDel0)
            {
                result = maxDelta; // Fully within range - return the delta value (in noise space)
            }
            else
            {
                // Smooth transition at boundaries
                double minVal = 1.0;
                double maxVal = 1.0;
                
                if (minDel1 > minDel0 && maxDeltaNormalized >= minDel0 && maxDeltaNormalized <= minDel1)
                {
                    // Transition from 0 to full at min boundary
                    minVal = (maxDeltaNormalized - minDel0) / (minDel1 - minDel0);
                }
                
                if (maxDel1 > maxDel0 && maxDeltaNormalized >= maxDel0 && maxDeltaNormalized <= maxDel1)
                {
                    // Transition from full to 0 at max boundary
                    maxVal = 1.0 - (maxDeltaNormalized - maxDel0) / (maxDel1 - maxDel0);
                }
                
                double blendFactor = minVal < maxVal ? minVal : maxVal;
                if (blendFactor < 0.0) blendFactor = 0.0;
                if (blendFactor > 1.0) blendFactor = 1.0;
                
                // Return delta multiplied by blend factor (MapMagic returns filtered delta)
                // Return in noise space to match input/output range
                result = maxDelta * blendFactor;
            }
            
            return result;
        }
        
        #endregion
    }
}

