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
            
            // Convert delta to angle
            // angle = atan(delta / (sampleDistance * terrainHeightScale))
            // For terrain, we need to account for the height scale
            double pixelSize = _sampleDistance; // Assuming 1:1 mapping for noise coordinates
            double deltaNormalized = maxDelta / (_terrainHeight * pixelSize);
            double angleRad = Math.Atan(deltaNormalized);
            double angleDeg = angleRad * (180.0 / Math.PI);
            
            // Apply angle filtering with smooth transitions
            return FilterByAngle(angleDeg);
        }
        
        /// <summary>
        /// Filters the angle value based on min/max angle ranges with smooth transitions.
        /// Returns 0-1 range where 1 means the angle is within the desired range.
        /// </summary>
        private double FilterByAngle(double angleDeg)
        {
            // Calculate angle ranges with smooth transitions
            double minAng0 = _minAngle - _smoothRange / 2.0;
            double minAng1 = _minAngle + _smoothRange / 2.0;
            double maxAng0 = _maxAngle - _smoothRange / 2.0;
            double maxAng1 = _maxAngle + _smoothRange / 2.0;
            
            // Handle edge cases
            if (_minAngle < 0.00001) { minAng0 = -1; minAng1 = -1; }
            if (maxAng0 > 89.9) maxAng0 = 20000000;
            if (maxAng1 > 89.9) maxAng1 = 20000000;
            
            // Apply SelectRange logic (similar to MapMagic)
            double result;
            
            if (angleDeg < minAng0 || angleDeg > maxAng1)
            {
                result = 0.0;
            }
            else if (angleDeg > minAng1 && angleDeg < maxAng0)
            {
                result = 1.0;
            }
            else
            {
                // Smooth transition
                double minVal = 1.0;
                double maxVal = 1.0;
                
                if (minAng1 > minAng0 && angleDeg >= minAng0 && angleDeg <= minAng1)
                {
                    minVal = (angleDeg - minAng0) / (minAng1 - minAng0);
                }
                
                if (maxAng1 > maxAng0 && angleDeg >= maxAng0 && angleDeg <= maxAng1)
                {
                    maxVal = 1.0 - (angleDeg - maxAng0) / (maxAng1 - maxAng0);
                }
                
                result = minVal < maxVal ? minVal : maxVal;
                if (result < 0.0) result = 0.0;
                if (result > 1.0) result = 1.0;
            }
            
            return result;
        }
        
        #endregion
    }
}

