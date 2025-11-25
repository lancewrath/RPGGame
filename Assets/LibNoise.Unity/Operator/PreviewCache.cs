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
        /// Populates the cache for a specific world region. This is used during terrain generation
        /// to automatically cache expensive operations (like erosion) so they don't need to be
        /// recalculated for splat map generation.
        /// </summary>
        /// <param name="minX">Minimum X world coordinate to cache.</param>
        /// <param name="minZ">Minimum Z world coordinate to cache.</param>
        /// <param name="maxX">Maximum X world coordinate to cache.</param>
        /// <param name="maxZ">Maximum Z world coordinate to cache.</param>
        /// <param name="resolution">Resolution of the cache (number of samples per axis). If 0, uses CacheSize.</param>
        public void PopulateCacheForRegion(double minX, double minZ, double maxX, double maxZ, int resolution = 0)
        {
            Debug.Assert(Modules[0] != null, "Input module must be set before populating cache");
            
            if (resolution <= 0)
            {
                resolution = _cacheSize;
            }
            
            // Store region bounds
            _cacheMinX = minX;
            _cacheMinZ = minZ;
            _cacheMaxX = maxX;
            _cacheMaxZ = maxZ;
            _cacheSize = resolution;
            
            // Calculate scale based on region size (for reference, but we use interpolation in GetValue)
            double regionWidth = maxX - minX;
            double regionHeight = maxZ - minZ;
            double avgScale = System.Math.Max(regionWidth, regionHeight) / (resolution > 1 ? resolution - 1 : 1);
            _cacheScale = avgScale;
            
            _cachedValues = new double[_cacheSize, _cacheSize];
            
            // Sample the input module across the region
            for (int z = 0; z < _cacheSize; z++)
            {
                for (int x = 0; x < _cacheSize; x++)
                {
                    // Interpolate world coordinates across the region
                    double tX = _cacheSize > 1 ? (double)x / (_cacheSize - 1) : 0;
                    double tZ = _cacheSize > 1 ? (double)z / (_cacheSize - 1) : 0;
                    
                    double worldX = minX + tX * (maxX - minX);
                    double worldZ = minZ + tZ * (maxZ - minZ);
                    
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
        /// returns the cached value using bilinear interpolation for smooth results.
        /// Otherwise, falls back to the input module.
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
                    // Convert world coordinates to cache indices using interpolation
                    double tX = (_cacheMaxX > _cacheMinX) ? (x - _cacheMinX) / (_cacheMaxX - _cacheMinX) : 0;
                    double tZ = (_cacheMaxZ > _cacheMinZ) ? (z - _cacheMinZ) / (_cacheMaxZ - _cacheMinZ) : 0;
                    
                    // Clamp to valid range
                    tX = System.Math.Max(0, System.Math.Min(1, tX));
                    tZ = System.Math.Max(0, System.Math.Min(1, tZ));
                    
                    // Convert to cache coordinate space (0 to cacheSize-1)
                    double cacheCoordX = tX * (_cacheSize - 1);
                    double cacheCoordZ = tZ * (_cacheSize - 1);
                    
                    // Get the four surrounding cache points for bilinear interpolation
                    int x0 = (int)System.Math.Floor(cacheCoordX);
                    int z0 = (int)System.Math.Floor(cacheCoordZ);
                    int x1 = System.Math.Min(x0 + 1, _cacheSize - 1);
                    int z1 = System.Math.Min(z0 + 1, _cacheSize - 1);
                    
                    // Ensure indices are within bounds
                    x0 = System.Math.Max(0, System.Math.Min(_cacheSize - 1, x0));
                    z0 = System.Math.Max(0, System.Math.Min(_cacheSize - 1, z0));
                    
                    // Get the four corner values
                    double v00 = _cachedValues[x0, z0]; // Bottom-left
                    double v10 = _cachedValues[x1, z0]; // Bottom-right
                    double v01 = _cachedValues[x0, z1]; // Top-left
                    double v11 = _cachedValues[x1, z1]; // Top-right
                    
                    // Calculate fractional parts for interpolation
                    double fx = cacheCoordX - x0;
                    double fz = cacheCoordZ - z0;
                    
                    // Bilinear interpolation
                    // First interpolate along X axis
                    double v0 = v00 * (1 - fx) + v10 * fx; // Bottom edge
                    double v1 = v01 * (1 - fx) + v11 * fx; // Top edge
                    
                    // Then interpolate along Z axis
                    double result = v0 * (1 - fz) + v1 * fz;
                    
                    return result;
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

