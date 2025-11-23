using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using LibNoise;
using System;

namespace RPGGame.Map.Editor
{
    public static class NoiseNodePreview
    {
        private const int PreviewSize = 128;
        
        public static Texture2D GeneratePreview(NoiseGraphNode node, NoiseGraphView graphView)
        {
            if (node == null || graphView == null)
                return null;
            
            // Build a temporary module graph for this node
            ModuleBase module = BuildNodeModule(node, graphView);
            if (module == null)
                return null;
            
            // Generate preview texture
            Texture2D preview = new Texture2D(PreviewSize, PreviewSize, TextureFormat.RGB24, false);
            preview.filterMode = FilterMode.Bilinear;
            
            float minValue = float.MaxValue;
            float maxValue = float.MinValue;
            float[,] values = new float[PreviewSize, PreviewSize];
            
            // Sample noise values
            double scale = 0.1; // Scale factor for preview
            for (int y = 0; y < PreviewSize; y++)
            {
                for (int x = 0; x < PreviewSize; x++)
                {
                    double worldX = x * scale;
                    double worldZ = y * scale;
                    double noiseValue = module.GetValue(worldX, 0, worldZ);
                    float value = (float)noiseValue;
                    values[x, y] = value;
                    minValue = Mathf.Min(minValue, value);
                    maxValue = Mathf.Max(maxValue, value);
                }
            }
            
            // Normalize and convert to colors
            float range = maxValue - minValue;
            if (range < 0.0001f) range = 1f; // Avoid division by zero
            
            for (int y = 0; y < PreviewSize; y++)
            {
                for (int x = 0; x < PreviewSize; x++)
                {
                    float normalized = (values[x, y] - minValue) / range;
                    Color color = new Color(normalized, normalized, normalized, 1f);
                    preview.SetPixel(x, y, color);
                }
            }
            
            preview.Apply();
            
            // Clean up module if needed
            if (module is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            return preview;
        }
        
        private static ModuleBase BuildNodeModule(NoiseGraphNode node, NoiseGraphView graphView)
        {
            // For now, build a simple module for generator nodes
            // For operator nodes, we'd need to build the full graph up to this node
            switch (node.NodeType)
            {
                case "Perlin":
                    var perlinNode = node as PerlinNoiseNode;
                    if (perlinNode != null)
                    {
                        var perlin = new LibNoise.Generator.Perlin();
                        perlin.Frequency = perlinNode.frequency;
                        perlin.Lacunarity = perlinNode.lacunarity;
                        perlin.Persistence = perlinNode.persistence;
                        perlin.OctaveCount = perlinNode.octaveCount;
                        perlin.Seed = perlinNode.seed;
                        perlin.Quality = perlinNode.quality;
                        return perlin;
                    }
                    break;
                case "Billow":
                    var billowNode = node as BillowNoiseNode;
                    if (billowNode != null)
                    {
                        var billow = new LibNoise.Generator.Billow();
                        billow.Frequency = billowNode.frequency;
                        billow.Lacunarity = billowNode.lacunarity;
                        billow.Persistence = billowNode.persistence;
                        billow.OctaveCount = billowNode.octaveCount;
                        billow.Seed = billowNode.seed;
                        billow.Quality = billowNode.quality;
                        return billow;
                    }
                    break;
                case "RidgedMultifractal":
                    var ridgedNode = node as RidgedMultifractalNoiseNode;
                    if (ridgedNode != null)
                    {
                        var ridged = new LibNoise.Generator.RidgedMultifractal();
                        ridged.Frequency = ridgedNode.frequency;
                        ridged.Lacunarity = ridgedNode.lacunarity;
                        ridged.OctaveCount = ridgedNode.octaveCount;
                        ridged.Seed = ridgedNode.seed;
                        ridged.Quality = ridgedNode.quality;
                        return ridged;
                    }
                    break;
                case "Const":
                    var constNode = node as ConstNoiseNode;
                    if (constNode != null)
                    {
                        var constModule = new LibNoise.Generator.Const();
                        constModule.Value = constNode.value;
                        return constModule;
                    }
                    break;
                case "Curve":
                    var curveNode = node as CurveNode;
                    if (curveNode != null)
                    {
                        // For preview, we need an input to the curve
                        // Use a simple Perlin noise as input
                        var perlin = new LibNoise.Generator.Perlin();
                        perlin.Frequency = 1.0;
                        
                        var curve = new LibNoise.Operator.Curve(perlin);
                        
                        // Convert AnimationCurve to LibNoise control points
                        // LibNoise Curve.Add(inputValue, outputValue):
                        //   - inputValue: the noise input value threshold
                        //   - outputValue: the output value when that threshold is reached
                        // AnimationCurve mapping:
                        //   - time (X-axis) = input noise value threshold
                        //   - value (Y-axis) = output value
                        var animCurve = curveNode.Curve;
                        if (animCurve != null && animCurve.length >= 4)
                        {
                            foreach (var key in animCurve.keys)
                            {
                                curve.Add(key.time, key.value);
                            }
                        }
                        else
                        {
                            // Default curve
                            curve.Add(-1.0, -1.0);
                            curve.Add(-0.33, -0.33);
                            curve.Add(0.33, 0.33);
                            curve.Add(1.0, 1.0);
                        }
                        
                        return curve;
                    }
                    break;
            }
            
            return null;
        }
    }
}

