using System.Diagnostics;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that outputs a mask value based on whether the input
    /// falls within a specified height range. Values within range output 1.0, values
    /// outside range output -1.0 (which normalizes to 0.0 for splatting).
    /// This is useful for height-based splatting (e.g., snow on mountain tops, underwater areas).
    /// [OPERATOR]
    /// </summary>
    public class HeightSelector : ModuleBase
    {
        #region Fields

        private double _minHeight = -1.0;
        private double _maxHeight = 1.0;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of HeightSelector.
        /// </summary>
        public HeightSelector()
            : base(1)
        {
        }

        /// <summary>
        /// Initializes a new instance of HeightSelector.
        /// </summary>
        /// <param name="input">The input module.</param>
        public HeightSelector(ModuleBase input)
            : base(1)
        {
            Modules[0] = input;
        }

        /// <summary>
        /// Initializes a new instance of HeightSelector.
        /// </summary>
        /// <param name="input">The input module.</param>
        /// <param name="minHeight">The minimum height threshold.</param>
        /// <param name="maxHeight">The maximum height threshold.</param>
        public HeightSelector(ModuleBase input, double minHeight, double maxHeight)
            : base(1)
        {
            Modules[0] = input;
            MinHeight = minHeight;
            MaxHeight = maxHeight;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the minimum height threshold.
        /// Values below this will output 0.
        /// </summary>
        public double MinHeight
        {
            get { return _minHeight; }
            set { _minHeight = value; }
        }

        /// <summary>
        /// Gets or sets the maximum height threshold.
        /// Values above this will output 0.
        /// </summary>
        public double MaxHeight
        {
            get { return _maxHeight; }
            set { _maxHeight = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the height range bounds.
        /// </summary>
        /// <param name="minHeight">The minimum height threshold.</param>
        /// <param name="maxHeight">The maximum height threshold.</param>
        public void SetBounds(double minHeight, double maxHeight)
        {
            _minHeight = minHeight;
            _maxHeight = maxHeight;
        }

        #endregion

        #region ModuleBase Members

        /// <summary>
        /// Returns the output value for the given input coordinates.
        /// If the input value is within the height range, returns 1.0 (mask value).
        /// Otherwise, returns -1.0 (which normalizes to 0.0 for splatting purposes).
        /// This creates a proper mask where in-range areas have full influence and
        /// out-of-range areas have no influence.
        /// Note: If you want to invert the mask (select areas outside the range),
        /// use an Invert node after this Height Selector.
        /// </summary>
        /// <param name="x">The input coordinate on the x-axis.</param>
        /// <param name="y">The input coordinate on the y-axis.</param>
        /// <param name="z">The input coordinate on the z-axis.</param>
        /// <returns>The resulting output value (1.0 for in-range, -1.0 for out-of-range).</returns>
        public override double GetValue(double x, double y, double z)
        {
            Debug.Assert(Modules[0] != null);
            
            double inputValue = Modules[0].GetValue(x, y, z);
            
            // Ensure minHeight <= maxHeight
            double min = System.Math.Min(_minHeight, _maxHeight);
            double max = System.Math.Max(_minHeight, _maxHeight);
            
            // If value is within range, return 1.0 (full mask influence)
            // Otherwise return -1.0 (which normalizes to 0.0, no influence)
            // This creates a proper binary mask for splatting
            // After normalization: 1.0 → 1.0 (full influence), -1.0 → 0.0 (no influence)
            if (inputValue >= min && inputValue <= max)
            {
                return 1.0;
            }
            
            return -1.0;
        }

        #endregion
    }
}

