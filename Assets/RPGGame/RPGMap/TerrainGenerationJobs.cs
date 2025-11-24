using UnityEngine;
using LibNoise;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RPGGame.Map
{
    /// <summary>
    /// Helper class for multithreaded terrain generation.
    /// Uses a two-phase approach: pre-compute noise values, then process in parallel.
    /// </summary>
    public static class TerrainGenerationParallel
    {
        /// <summary>
        /// Generates heightmap data in parallel using multiple threads.
        /// Uses a two-phase approach to avoid lock contention.
        /// </summary>
        public static float[,] GenerateHeightmapParallel(
            ModuleBase sourceModule,
            int tileX,
            int tileZ,
            int heightmapSize,
            int heightmapResolution,
            Vector3 tileSize)
        {
            float[,] heights = new float[heightmapSize, heightmapSize];
            
            if (sourceModule == null)
            {
                Debug.LogWarning("Heightmap module is null. Using flat terrain.");
                return heights;
            }
            
            // Calculate the world position offset for this tile
            double offsetX = tileX * tileSize.x;
            double offsetZ = tileZ * tileSize.z;
            
            // Phase 1: Pre-compute all noise values in parallel (row by row)
            // LibNoise modules are typically thread-safe for read-only GetValue() operations
            // as they don't modify internal state during evaluation
            double[,] noiseValues = new double[heightmapSize, heightmapSize];
            
            Parallel.For(0, heightmapSize, z =>
            {
                for (int x = 0; x < heightmapSize; x++)
                {
                    // Normalize coordinates to 0-1 range based on resolution
                    double normalizedX = heightmapResolution > 0 ? (double)x / heightmapResolution : 0;
                    double normalizedZ = heightmapResolution > 0 ? (double)z / heightmapResolution : 0;
                    
                    // Convert to world coordinates
                    double worldX = offsetX + normalizedX * tileSize.x;
                    double worldZ = offsetZ + normalizedZ * tileSize.z;
                    
                    // Get noise value - LibNoise modules should be thread-safe for read-only operations
                    // If you encounter issues, uncomment the lock below
                    //lock (sourceModule)
                    {
                        noiseValues[z, x] = sourceModule.GetValue(worldX, 0, worldZ);
                    }
                }
            });
            
            // Phase 2: Process noise values into heights in parallel (no locks needed)
            Parallel.For(0, heightmapSize, z =>
            {
                for (int x = 0; x < heightmapSize; x++)
                {
                    // Linear lerp from [-1,1] to [0,1]: output = (input - min) / (max - min)
                    // Formula: (noiseValue - (-1)) / (1 - (-1)) = (noiseValue + 1) / 2 = (noiseValue + 1.0) * 0.5
                    // This ensures: -1 → 0, 0 → 0.5, 1 → 1.0 (linear mapping, no clamping)
                    float height = (float)((noiseValues[z, x] + 1.0) * 0.5);
                    heights[z, x] = height;
                }
            });
            
            return heights;
        }
        
        /// <summary>
        /// Generates splat map alphamaps in parallel.
        /// Uses a two-phase approach to reduce lock contention.
        /// </summary>
        public static float[,,] GenerateSplatMapsParallel(
            List<SplatOutputData> splatOutputs,
            int tileX,
            int tileZ,
            int alphamapResolution,
            Vector3 tileSize)
        {
            if (splatOutputs == null || splatOutputs.Count == 0)
                return new float[alphamapResolution, alphamapResolution, 0];
            
            float[,,] alphamaps = new float[alphamapResolution, alphamapResolution, splatOutputs.Count];
            
            double offsetX = tileX * tileSize.x;
            double offsetZ = tileZ * tileSize.z;
            
            // Phase 1: Pre-compute all noise values in parallel (row by row)
            // Store noise values for each splat output
            double[,,] noiseValues = new double[alphamapResolution, alphamapResolution, splatOutputs.Count];
            
            Parallel.For(0, alphamapResolution, z =>
            {
                for (int x = 0; x < alphamapResolution; x++)
                {
                    // Convert to world coordinates
                    double normalizedX = (double)x / (alphamapResolution - 1);
                    double normalizedZ = (double)z / (alphamapResolution - 1);
                    
                    double worldX = offsetX + normalizedX * tileSize.x;
                    double worldZ = offsetZ + normalizedZ * tileSize.z;
                    
                    // Get noise values for each splat output
                    // LibNoise modules should be thread-safe for read-only operations
                    // If you encounter issues, uncomment the locks below
                    for (int i = 0; i < splatOutputs.Count; i++)
                    {
                        if (splatOutputs[i].noiseModule != null)
                        {
                            //lock (splatOutputs[i].noiseModule)
                            {
                                noiseValues[z, x, i] = splatOutputs[i].noiseModule.GetValue(worldX, 0, worldZ);
                            }
                        }
                        else
                        {
                            noiseValues[z, x, i] = 0.0;
                        }
                    }
                }
            });
            
            // Phase 2: Process noise values into alphamaps in parallel (no locks needed)
            Parallel.For(0, alphamapResolution, z =>
            {
                for (int x = 0; x < alphamapResolution; x++)
                {
                    // Priority-based blending: later layers (higher orderId) take priority over earlier layers
                    // Process layers in order, using alpha compositing where later layers can overwrite earlier ones
                    float[] layerAlphas = new float[splatOutputs.Count];
                    float remainingAlpha = 1.0f; // Track how much alpha is still available
                    
                    for (int i = 0; i < splatOutputs.Count; i++)
                    {
                        // Remap from [-1,1] to [0,1]
                        float normalizedWeight = (float)((noiseValues[z, x, i] + 1.0) * 0.5);
                        normalizedWeight = Mathf.Clamp01(normalizedWeight);
                        
                        // This layer's alpha is its weight, but limited by remaining alpha
                        // Later layers with strong values will take priority
                        float weight = normalizedWeight * remainingAlpha;
                        layerAlphas[i] = weight;
                        
                        // Reduce remaining alpha for next layers (alpha compositing)
                        // Later layers can only contribute what's left after earlier layers
                        remainingAlpha = Mathf.Max(0f, remainingAlpha - weight);
                    }
                    
                    // Normalize to ensure they sum to 1 (Unity terrain requirement)
                    float totalAlpha = 0f;
                    for (int i = 0; i < splatOutputs.Count; i++)
                    {
                        totalAlpha += layerAlphas[i];
                    }
                    
                    if (totalAlpha > 0.0001f)
                    {
                        for (int i = 0; i < splatOutputs.Count; i++)
                        {
                            alphamaps[z, x, i] = layerAlphas[i] / totalAlpha;
                        }
                    }
                    else
                    {
                        // If no weights, distribute evenly
                        float evenWeight = 1f / splatOutputs.Count;
                        for (int i = 0; i < splatOutputs.Count; i++)
                        {
                            alphamaps[z, x, i] = evenWeight;
                        }
                    }
                }
            });
            
            return alphamaps;
        }
    }
}


