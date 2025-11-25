using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using LibNoise;
using System;
using System.Linq;

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
            // LibNoise modules typically output values in the range -1.0 to 1.0
            // We normalize from -1.0 to 1.0 to 0.0 to 1.0 for color values
            const float noiseMin = -1.0f;
            const float noiseMax = 1.0f;
            const float noiseRange = noiseMax - noiseMin; // 2.0
            
            for (int y = 0; y < PreviewSize; y++)
            {
                for (int x = 0; x < PreviewSize; x++)
                {
                    // Normalize from [-1.0, 1.0] to [0.0, 1.0]
                    // Formula: (value - noiseMin) / noiseRange
                    // = (value - (-1.0)) / 2.0 = (value + 1.0) / 2.0
                    float normalized = (values[x, y] - noiseMin) / noiseRange;
                    // Clamp to ensure valid color range [0.0, 1.0]
                    normalized = Mathf.Clamp01(normalized);
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
        
        private static ModuleBase BuildNodeModule(NoiseGraphNode node, NoiseGraphView graphView, System.Collections.Generic.HashSet<string> visitedNodes = null)
        {
            if (visitedNodes == null)
                visitedNodes = new System.Collections.Generic.HashSet<string>();
            
            // Prevent infinite recursion
            if (visitedNodes.Contains(node.NodeGuid))
                return null;
            visitedNodes.Add(node.NodeGuid);
            
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
                case "Cache":
                    var cacheNode = node as CacheNode;
                    if (cacheNode != null)
                    {
                        // If cache is available, create a module that uses cached values
                        if (cacheNode.isCached)
                        {
                            var cachedValues = cacheNode.GetCachedValues();
                            if (cachedValues != null)
                            {
                                return new CachedValueModule(cachedValues, cacheNode.cacheScale);
                            }
                        }
                        
                        // Otherwise, build the input module normally
                        ModuleBase inputModule = null;
                        if (node.InputPorts.Count > 0)
                        {
                            var inputPort = node.InputPorts[0];
                            var connections = inputPort.connections;
                            if (connections != null)
                            {
                                foreach (var edge in connections)
                                {
                                    if (edge.output != null && edge.output.node is NoiseGraphNode outputNode)
                                    {
                                        inputModule = BuildNodeModule(outputNode, graphView, visitedNodes);
                                        break;
                                    }
                                }
                            }
                        }
                        
                        // Fallback to Perlin if no input is connected
                        if (inputModule == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            inputModule = perlin;
                        }
                        
                        return inputModule;
                    }
                    break;
                case "Curve":
                    var curveNode = node as CurveNode;
                    if (curveNode != null)
                    {
                        // Use the helper method to get the input module (consistent with other nodes)
                        ModuleBase inputModule = GetInputModule(node, graphView, visitedNodes, 0);
                        
                        // Fallback to Perlin if no input is connected
                        if (inputModule == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            inputModule = perlin;
                        }
                        
                        var curve = new LibNoise.Operator.Curve(inputModule);
                        
                        // Convert AnimationCurve to LibNoise control points
                        // LibNoise Curve.Add(inputValue, outputValue):
                        //   - inputValue: the noise input value threshold
                        //   - outputValue: the output value when that threshold is reached
                        // AnimationCurve mapping (matches MapMagic's UnityCurve):
                        //   - time (X-axis) = input noise value threshold
                        //   - value (Y-axis) = output value
                        // So: key.time → inputValue, key.value → outputValue
                        var animCurve = curveNode.Curve;
                        if (animCurve != null && animCurve.length >= 4)
                        {
                            // Sort keys by time to ensure correct order (LibNoise sorts internally, but let's be explicit)
                            var sortedKeys = animCurve.keys.OrderBy(k => k.time).ToArray();
                            foreach (var key in sortedKeys)
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
                case "Clamp":
                    var clampNode = node as ClampNode;
                    if (clampNode != null)
                    {
                        ModuleBase inputModule = GetInputModule(node, graphView, visitedNodes, 0);
                        if (inputModule == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            inputModule = perlin;
                        }
                        var clamp = new LibNoise.Operator.Clamp(inputModule);
                        clamp.Minimum = clampNode.minimum;
                        clamp.Maximum = clampNode.maximum;
                        return clamp;
                    }
                    break;
                case "Add":
                    var addNode = node as AddNode;
                    if (addNode != null)
                    {
                        ModuleBase inputA = GetInputModule(node, graphView, visitedNodes, 0);
                        ModuleBase inputB = GetInputModule(node, graphView, visitedNodes, 1);
                        if (inputA == null || inputB == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            if (inputA == null) inputA = perlin;
                            if (inputB == null) inputB = perlin;
                        }
                        return new LibNoise.Operator.Add(inputA, inputB);
                    }
                    break;
                case "Multiply":
                    var multiplyNode = node as MultiplyNode;
                    if (multiplyNode != null)
                    {
                        ModuleBase inputA = GetInputModule(node, graphView, visitedNodes, 0);
                        ModuleBase inputB = GetInputModule(node, graphView, visitedNodes, 1);
                        if (inputA == null || inputB == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            if (inputA == null) inputA = perlin;
                            if (inputB == null) inputB = perlin;
                        }
                        return new LibNoise.Operator.Multiply(inputA, inputB);
                    }
                    break;
                case "Subtract":
                    var subtractNode = node as SubtractNode;
                    if (subtractNode != null)
                    {
                        ModuleBase inputA = GetInputModule(node, graphView, visitedNodes, 0);
                        ModuleBase inputB = GetInputModule(node, graphView, visitedNodes, 1);
                        if (inputA == null || inputB == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            if (inputA == null) inputA = perlin;
                            if (inputB == null) inputB = perlin;
                        }
                        return new LibNoise.Operator.Subtract(inputA, inputB);
                    }
                    break;
                case "Min":
                    var minNode = node as MinNode;
                    if (minNode != null)
                    {
                        ModuleBase inputA = GetInputModule(node, graphView, visitedNodes, 0);
                        ModuleBase inputB = GetInputModule(node, graphView, visitedNodes, 1);
                        if (inputA == null || inputB == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            if (inputA == null) inputA = perlin;
                            if (inputB == null) inputB = perlin;
                        }
                        return new LibNoise.Operator.Min(inputA, inputB);
                    }
                    break;
                case "Max":
                    var maxNode = node as MaxNode;
                    if (maxNode != null)
                    {
                        ModuleBase inputA = GetInputModule(node, graphView, visitedNodes, 0);
                        ModuleBase inputB = GetInputModule(node, graphView, visitedNodes, 1);
                        if (inputA == null || inputB == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            if (inputA == null) inputA = perlin;
                            if (inputB == null) inputB = perlin;
                        }
                        return new LibNoise.Operator.Max(inputA, inputB);
                    }
                    break;
                case "Blend":
                    var blendNode = node as BlendNode;
                    if (blendNode != null)
                    {
                        ModuleBase inputA = GetInputModule(node, graphView, visitedNodes, 0);
                        ModuleBase inputB = GetInputModule(node, graphView, visitedNodes, 1);
                        ModuleBase control = GetInputModule(node, graphView, visitedNodes, 2);
                        if (inputA == null || inputB == null || control == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            if (inputA == null) inputA = perlin;
                            if (inputB == null) inputB = perlin;
                            if (control == null) control = perlin;
                        }
                        return new LibNoise.Operator.Blend(inputA, inputB, control);
                    }
                    break;
                case "Power":
                    var powerNode = node as PowerNode;
                    if (powerNode != null)
                    {
                        ModuleBase inputA = GetInputModule(node, graphView, visitedNodes, 0);
                        ModuleBase inputB = GetInputModule(node, graphView, visitedNodes, 1);
                        if (inputA == null || inputB == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            if (inputA == null) inputA = perlin;
                            if (inputB == null) inputB = perlin;
                        }
                        return new LibNoise.Operator.Power(inputA, inputB);
                    }
                    break;
                case "Abs":
                    var absNode = node as AbsNode;
                    if (absNode != null)
                    {
                        ModuleBase inputModule = GetInputModule(node, graphView, visitedNodes, 0);
                        if (inputModule == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            inputModule = perlin;
                        }
                        return new LibNoise.Operator.Abs(inputModule);
                    }
                    break;
                case "Invert":
                    var invertNode = node as InvertNode;
                    if (invertNode != null)
                    {
                        ModuleBase inputModule = GetInputModule(node, graphView, visitedNodes, 0);
                        if (inputModule == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            inputModule = perlin;
                        }
                        return new LibNoise.Operator.Invert(inputModule);
                    }
                    break;
                case "Normalize":
                    var normalizeNode = node as NormalizeNode;
                    if (normalizeNode != null)
                    {
                        ModuleBase inputModule = GetInputModule(node, graphView, visitedNodes, 0);
                        if (inputModule == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            inputModule = perlin;
                        }
                        return new LibNoise.Operator.Normalize(inputModule);
                    }
                    break;
                case "Beach":
                    var beachNode = node as BeachNode;
                    if (beachNode != null)
                    {
                        ModuleBase inputModule = GetInputModule(node, graphView, visitedNodes, 0);
                        if (inputModule == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            inputModule = perlin;
                        }
                        var beach = new LibNoise.Operator.Beach(inputModule);
                        beach.WaterLevel = beachNode.waterLevel;
                        beach.BeachSize = beachNode.beachSize;
                        beach.BeachHeight = beachNode.beachHeight;
                        beach.SmoothRange = beachNode.smoothRange;
                        return beach;
                    }
                    break;
                case "Sediment":
                    var sedimentNode = node as SedimentNode;
                    if (sedimentNode != null)
                    {
                        ModuleBase preErosion = GetInputModule(node, graphView, visitedNodes, 0);
                        ModuleBase postErosion = GetInputModule(node, graphView, visitedNodes, 1);
                        if (preErosion == null || postErosion == null)
                        {
                            var perlin = new LibNoise.Generator.Perlin();
                            perlin.Frequency = 1.0;
                            if (preErosion == null) preErosion = perlin;
                            if (postErosion == null) postErosion = perlin;
                        }
                        // Preview shows the Cliff output (port 0)
                        var cliff = new LibNoise.Operator.SedimentCliff(preErosion, postErosion);
                        cliff.CliffThreshold = sedimentNode.cliffThreshold;
                        return cliff;
                    }
                    break;
            }
            
            return null;
        }
        
        /// <summary>
        /// Helper method to get an input module for a node at a specific input port index.
        /// </summary>
        private static ModuleBase GetInputModule(NoiseGraphNode node, NoiseGraphView graphView, System.Collections.Generic.HashSet<string> visitedNodes, int inputPortIndex)
        {
            if (node.InputPorts.Count <= inputPortIndex)
                return null;
            
            var inputPort = node.InputPorts[inputPortIndex];
            ModuleBase inputModule = null;
            
            // Try to get connections from the port first
            var connections = inputPort.connections;
            if (connections != null && connections.Count() > 0)
            {
                foreach (var edge in connections)
                {
                    if (edge.output != null && edge.output.node is NoiseGraphNode outputNode)
                    {
                        // Check if the output node is a Cache node with cached values
                        if (outputNode is CacheNode cacheOutputNode && cacheOutputNode.isCached)
                        {
                            var cachedValues = cacheOutputNode.GetCachedValues();
                            if (cachedValues != null)
                            {
                                return new CachedValueModule(cachedValues, cacheOutputNode.cacheScale);
                            }
                        }
                        
                        inputModule = BuildNodeModule(outputNode, graphView, visitedNodes);
                        break;
                    }
                }
            }
            else
            {
                // Fallback: query graph view edges directly
                foreach (var edge in graphView.edges.ToList())
                {
                    if (edge.input == inputPort && edge.output != null && edge.output.node is NoiseGraphNode outputNode)
                    {
                        // Check if the output node is a Cache node with cached values
                        if (outputNode is CacheNode cacheOutputNode && cacheOutputNode.isCached)
                        {
                            var cachedValues = cacheOutputNode.GetCachedValues();
                            if (cachedValues != null)
                            {
                                return new CachedValueModule(cachedValues, cacheOutputNode.cacheScale);
                            }
                        }
                        
                        inputModule = BuildNodeModule(outputNode, graphView, visitedNodes);
                        break;
                    }
                }
            }
            
            return inputModule;
        }
        
        /// <summary>
        /// Builds the input module for a given node (for nodes that need to cache their input).
        /// </summary>
        public static ModuleBase BuildNodeModuleForInput(NoiseGraphNode node, NoiseGraphView graphView)
        {
            if (node == null || graphView == null)
                return null;
            
            // Use the helper method to get the first input module
            return GetInputModule(node, graphView, new System.Collections.Generic.HashSet<string>(), 0);
        }
    }
    
    /// <summary>
    /// A simple module that returns cached values for preview generation.
    /// </summary>
    internal class CachedValueModule : ModuleBase
    {
        private double[,] cachedValues;
        private double cacheScale;
        private const int CacheSize = 128;
        
        public CachedValueModule(double[,] values, double scale) : base(0)
        {
            cachedValues = values;
            cacheScale = scale;
        }
        
        public override double GetValue(double x, double y, double z)
        {
            if (cachedValues == null)
                return 0.0;
            
            // Convert world coordinates to cache indices
            int cacheX = Mathf.Clamp((int)(x / cacheScale), 0, CacheSize - 1);
            int cacheZ = Mathf.Clamp((int)(z / cacheScale), 0, CacheSize - 1);
            
            return cachedValues[cacheX, cacheZ];
        }
    }
}

