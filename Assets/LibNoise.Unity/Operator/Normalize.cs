using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that normalizes the output value from a source module
    /// from the range [-1,1] to [0,1] using linear interpolation.
    /// Formula: output = (input + 1.0) * 0.5
    /// [OPERATOR]
    /// </summary>
    public class Normalize : ModuleBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of Normalize.
        /// </summary>
        public Normalize()
            : base(1)
        {
        }

        /// <summary>
        /// Initializes a new instance of Normalize.
        /// </summary>
        /// <param name="input">The input module.</param>
        public Normalize(ModuleBase input)
            : base(1)
        {
            Modules[0] = input;
        }

        #endregion

        #region ModuleBase Members

        /// <summary>
        /// Returns the output value for the given input coordinates.
        /// </summary>
        /// <param name="x">The input coordinate on the x-axis.</param>
        /// <param name="y">The input coordinate on the y-axis.</param>
        /// <param name="z">The input coordinate on the z-axis.</param>
        /// <returns>The resulting output value, normalized from [-1,1] to [0,1].</returns>
        public override double GetValue(double x, double y, double z)
        {
            Debug.Assert(Modules[0] != null);
            double inputValue = Modules[0].GetValue(x, y, z);
            // Normalize from [-1,1] to [0,1]: (value + 1.0) * 0.5
            return (inputValue + 1.0) * 0.5;
        }

        #endregion
    }
}

