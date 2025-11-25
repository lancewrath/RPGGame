using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that caches a 2D preview of the output from a source module.
    /// This is useful for preview generation in the editor, allowing expensive noise graphs
    /// to be cached and reused for preview purposes.
    /// [OPERATOR]
    /// </summary>
    public class PreviewCache : ModuleBase
    {
        #region Fields

        private double[,] _cachedValues;
        private bool _isCached;
        private double _cacheScale;
        private int _cacheSize;
        private double _cacheMinX;
        private double _cacheMinZ;
        private double _cacheMaxX;
        private double _cacheMaxZ;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of PreviewCache.
        /// </summary>
        public PreviewCache()
            : base(1)
        {
            _cacheSize = 128; // Default preview size
            _cacheScale = 0.1; // Default scale factor
            _isCached = false;
        }

        /// <summary>
        /// Initializes a new instance of PreviewCache.
        /// </summary>
        /// <param name="input">The input module.</param>
        public PreviewCache(ModuleBase input)
            : base(1)
        {
            Modules[0] = input;
            _cacheSize = 128;
            _cacheScale = 0.1;
            _isCached = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the cache size (width and height of the cached preview).
        /// </summary>
        public int CacheSize
        {
            get { return _cacheSize; }
            set
            {
                if (value > 0 && value != _cacheSize)
                {
                    _cacheSize = value;
                    _isCached = false; // Invalidate cache when size changes
                }
            }
        }

        /// <summary>
        /// Gets or sets the scale factor used when generating the cache.
        /// </summary>
        public double CacheScale
        {
            get { return _cacheScale; }
            set
            {
                if (value != _cacheScale)
                {
                    _cacheScale = value;
                    _isCached = false; // Invalidate cache when scale changes
                }
            }
        }

        /// <summary>
        /// Gets whether the cache has been generated.
        /// </summary>
        public bool IsCached
        {
            get { return _isCached && _cachedValues != null; }
        }

        #endregion

        #region ModuleBase Members

        /// <summary>
        /// Gets or sets a source module by index.
        /// </summary>
        public override ModuleBase this[int index]
        {
            get { return base[index]; }
            set
            {
                base[index] = value;
                _isCached = false; // Invalidate cache when input changes
            }
        }

        /// <summary>
        /// Generates the cache by sampling the input module.
        /// </summary>
        public void GenerateCache()
        {
            Debug.Assert(Modules[0] != null, "Input module must be set before generating cache");
            
            _cachedValues = new double[_cacheSize, _cacheSize];
            _cacheMinX = 0;
            _cacheMinZ = 0;
            _cacheMaxX = (_cacheSize - 1) * _cacheScale;
            _cacheMaxZ = (_cacheSize - 1) * _cacheScale;
            
            for (int z = 0; z < _cacheSize; z++)
            {
                for (int x = 0; x < _cacheSize; x++)
                {
                    double worldX = x * _cacheScale;
                    double worldZ = z * _cacheScale;
                    _cachedValues[x, z] = Modules[0].GetValue(worldX, 0, worldZ);
                }
            }
            
            _isCached = true;
        }

        /// <summary>
        /// Clears the cached values.
        /// </summary>
        public void ClearCache()
        {
            _cachedValues = null;
            _isCached = false;
        }

        /// <summary>
        /// Returns the output value for the given input coordinates.
        /// If the cache is available and the coordinates are within the cached range,
        /// returns the cached value. Otherwise, falls back to the input module.
        /// </summary>
        /// <param name="x">The input coordinate on the x-axis.</param>
        /// <param name="y">The input coordinate on the y-axis.</param>
        /// <param name="z">The input coordinate on the z-axis.</param>
        /// <returns>The resulting output value.</returns>
        public override double GetValue(double x, double y, double z)
        {
            Debug.Assert(Modules[0] != null);
            
            // If cache is available and coordinates are within cached range, use cache
            if (_isCached && _cachedValues != null)
            {
                if (x >= _cacheMinX && x <= _cacheMaxX && z >= _cacheMinZ && z <= _cacheMaxZ)
                {
                    // Convert world coordinates to cache indices
                    int cacheX = Mathf.Clamp((int)(x / _cacheScale), 0, _cacheSize - 1);
                    int cacheZ = Mathf.Clamp((int)(z / _cacheScale), 0, _cacheSize - 1);
                    return _cachedValues[cacheX, cacheZ];
                }
            }
            
            // Fall back to input module if cache not available or out of range
            return Modules[0].GetValue(x, y, z);
        }

        /// <summary>
        /// Gets a cached value directly by cache coordinates (for preview generation).
        /// </summary>
        /// <param name="cacheX">The X index in the cache (0 to CacheSize-1).</param>
        /// <param name="cacheZ">The Z index in the cache (0 to CacheSize-1).</param>
        /// <returns>The cached value, or 0 if cache is not available or indices are out of range.</returns>
        public double GetCachedValue(int cacheX, int cacheZ)
        {
            if (_isCached && _cachedValues != null)
            {
                if (cacheX >= 0 && cacheX < _cacheSize && cacheZ >= 0 && cacheZ < _cacheSize)
                {
                    return _cachedValues[cacheX, cacheZ];
                }
            }
            return 0.0;
        }

        #endregion
    }
}

