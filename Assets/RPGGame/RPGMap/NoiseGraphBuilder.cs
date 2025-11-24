using System.Collections.Generic;
using System.Linq;
using LibNoise;
using LibNoise.Generator;
using LibNoise.Operator;
using UnityEngine;

namespace RPGGame.Map
{
    public static class NoiseGraphBuilder
    {
        public static ModuleBase BuildModuleGraph(NoiseGraphData graphData)
        {
            if (graphData == null || graphData.nodes == null || graphData.nodes.Count == 0)
            {
                return null;
            }
            
            // Find output node
            NoiseNodeData outputNode = graphData.nodes.FirstOrDefault(n => n.guid == graphData.outputNodeGuid);
            if (outputNode == null)
            {
                // If no output node specified, try to find an Output node
                outputNode = graphData.nodes.FirstOrDefault(n => n.nodeType == "Output");
                if (outputNode == null && graphData.nodes.Count > 0)
                {
                    // Fallback: use the first node
                    outputNode = graphData.nodes[0];
                }
            }
            
            // If the output node is the dedicated Output node, get its input instead
            if (outputNode != null && outputNode.nodeType == "Output")
            {
                // Find the edge connected to the Output node's input
                var outputEdge = graphData.edges.FirstOrDefault(e => e.inputNodeGuid == outputNode.guid);
                if (outputEdge != null)
                {
                    // Use the node connected to the Output node as the actual output
                    outputNode = graphData.nodes.FirstOrDefault(n => n.guid == outputEdge.outputNodeGuid);
                    if (outputNode == null && graphData.nodes.Count > 0)
                    {
                        outputNode = graphData.nodes[0];
                    }
                }
            }
            
            // Build all modules
            Dictionary<string, ModuleBase> builtModules = new Dictionary<string, ModuleBase>();
            Dictionary<string, ModuleBase> portalModules = new Dictionary<string, ModuleBase>(); // Portal name -> module
            
            // First pass: create all modules (skip Output nodes and Portal In/Out nodes - they're handled specially)
            foreach (var nodeData in graphData.nodes)
            {
                // Skip Output nodes - they're just markers
                if (nodeData.nodeType == "Output")
                    continue;
                
                // Skip Portal In and Portal Out nodes - they're handled in a separate pass
                if (nodeData.nodeType == "Portal In" || nodeData.nodeType == "Portal Out")
                    continue;
                    
                ModuleBase module = CreateModule(nodeData);
                if (module != null)
                {
                    builtModules[nodeData.guid] = module;
                }
            }
            
            // Second pass: connect modules (skip edges connected to Output nodes and Portal In nodes)
            // Also skip edges FROM Portal Out nodes - they'll be handled after Portal Out modules are built
            List<NoiseEdgeData> portalOutEdges = new List<NoiseEdgeData>();
            
            foreach (var edge in graphData.edges)
            {
                // Skip edges where input is an Output node (Output nodes don't create modules)
                var inputNode = graphData.nodes.FirstOrDefault(n => n.guid == edge.inputNodeGuid);
                if (inputNode != null && inputNode.nodeType == "Output")
                    continue;
                
                // Skip edges where input is a SplatOutput node (they're handled separately)
                if (inputNode != null && inputNode.nodeType == "SplatOutput")
                    continue;
                
                // Skip edges where input is a Portal In node (they're handled separately)
                if (inputNode != null && inputNode.nodeType == "Portal In")
                    continue;
                
                // Collect edges FROM Portal Out nodes to process later
                var outputNodeData = graphData.nodes.FirstOrDefault(n => n.guid == edge.outputNodeGuid);
                if (outputNodeData != null && outputNodeData.nodeType == "Portal Out")
                {
                    portalOutEdges.Add(edge);
                    continue;
                }
                
                if (builtModules.TryGetValue(edge.outputNodeGuid, out ModuleBase outputModule) &&
                    builtModules.TryGetValue(edge.inputNodeGuid, out ModuleBase inputModule))
                {
                    // Check if the input module can accept a source module at this index
                    // SourceModuleCount is the number of source module slots, so valid indices are 0 to SourceModuleCount-1
                    if (inputModule.SourceModuleCount > 0 && edge.inputPortIndex >= 0 && edge.inputPortIndex < inputModule.SourceModuleCount)
                    {
                        inputModule[edge.inputPortIndex] = outputModule;
                    }
                    else
                    {
                        Debug.LogWarning($"Cannot connect module: input module '{inputNode?.nodeType}' (GUID: {edge.inputNodeGuid}) has SourceModuleCount={inputModule.SourceModuleCount}, but trying to connect to port index {edge.inputPortIndex}");
                    }
                }
                else
                {
                    if (!builtModules.ContainsKey(edge.outputNodeGuid))
                    {
                        Debug.LogWarning($"Cannot connect module: output module not found (GUID: {edge.outputNodeGuid})");
                    }
                    if (!builtModules.ContainsKey(edge.inputNodeGuid))
                    {
                        Debug.LogWarning($"Cannot connect module: input module not found (GUID: {edge.inputNodeGuid})");
                    }
                }
            }
            
            // Third pass: Handle Portal In nodes - find their input modules and store by portal name
            foreach (var portalInNode in graphData.nodes.Where(n => n.nodeType == "Portal In"))
            {
                // Find the edge connected to the Portal In node's input
                var portalInEdge = graphData.edges.FirstOrDefault(e => e.inputNodeGuid == portalInNode.guid);
                if (portalInEdge != null && builtModules.TryGetValue(portalInEdge.outputNodeGuid, out ModuleBase portalInputModule))
                {
                    // Get portal name from properties
                    string portalName = GetPropertyString(portalInNode, "portalName", "Portal");
                    if (!string.IsNullOrEmpty(portalName))
                    {
                        portalModules[portalName] = portalInputModule;
                    }
                }
            }
            
            // Fourth pass: Handle Portal Out nodes - create modules that reference portal modules
            foreach (var portalOutNode in graphData.nodes.Where(n => n.nodeType == "Portal Out"))
            {
                // Get portal name from properties
                string portalName = GetPropertyString(portalOutNode, "selectedPortalName", "");
                if (!string.IsNullOrEmpty(portalName) && portalModules.TryGetValue(portalName, out ModuleBase portalModule))
                {
                    // Portal Out nodes just reference the portal's module
                    builtModules[portalOutNode.guid] = portalModule;
                }
                else
                {
                    Debug.LogWarning($"Portal Out node (GUID: {portalOutNode.guid}) references portal '{portalName}' which doesn't exist or has no input!");
                }
            }
            
            // Fifth pass: Connect edges FROM Portal Out nodes (now that Portal Out modules are built)
            // Only connect edges FROM Portal Out nodes that were successfully resolved
            foreach (var edge in portalOutEdges)
            {
                // Skip if the Portal Out node wasn't successfully resolved (not in builtModules)
                if (!builtModules.ContainsKey(edge.outputNodeGuid))
                {
                    // Portal Out node couldn't be resolved (empty name, missing portal, etc.)
                    // Skip this edge - the input module will get a fallback during validation
                    continue;
                }
                
                var inputNode = graphData.nodes.FirstOrDefault(n => n.guid == edge.inputNodeGuid);
                if (inputNode != null && (inputNode.nodeType == "Output" || inputNode.nodeType == "SplatOutput"))
                    continue;
                
                if (builtModules.TryGetValue(edge.outputNodeGuid, out ModuleBase outputModule) &&
                    builtModules.TryGetValue(edge.inputNodeGuid, out ModuleBase inputModule))
                {
                    if (inputModule.SourceModuleCount > 0 && edge.inputPortIndex >= 0 && edge.inputPortIndex < inputModule.SourceModuleCount)
                    {
                        inputModule[edge.inputPortIndex] = outputModule;
                    }
                    else
                    {
                        Debug.LogWarning($"Cannot connect Portal Out module: input module '{inputNode?.nodeType}' (GUID: {edge.inputNodeGuid}) has SourceModuleCount={inputModule.SourceModuleCount}, but trying to connect to port index {edge.inputPortIndex}");
                    }
                }
                else
                {
                    if (!builtModules.ContainsKey(edge.inputNodeGuid))
                    {
                        Debug.LogWarning($"Cannot connect Portal Out module: input module not found (GUID: {edge.inputNodeGuid})");
                    }
                }
            }
            
            // Validate that all Curve modules have their input connected
            // If a Curve module doesn't have an input, we'll use a default Perlin noise as fallback
            foreach (var kvp in builtModules)
            {
                var nodeData = graphData.nodes.FirstOrDefault(n => n.guid == kvp.Key);
                if (nodeData != null && nodeData.nodeType == "Curve")
                {
                    var curveModule = kvp.Value as LibNoise.Operator.Curve;
                    if (curveModule != null)
                    {
                        // Check if the input module is null by trying to access it
                        // The indexer will throw if it's null, so we catch that
                        try
                        {
                            var test = curveModule[0];
                            // If we get here, the module exists
                        }
                        catch (System.ArgumentNullException)
                        {
                            // Module is null, set a default
                            Debug.LogWarning($"Curve module (GUID: {kvp.Key}) does not have an input module connected! Using default Perlin noise as fallback.");
                            // Use a default Perlin noise as input to prevent crashes
                            var defaultInput = new Perlin();
                            defaultInput.Frequency = 1.0;
                            curveModule[0] = defaultInput;
                        }
                        catch (System.ArgumentOutOfRangeException)
                        {
                            // Index out of range, shouldn't happen but handle it
                            Debug.LogError($"Curve module (GUID: {kvp.Key}) has invalid module index!");
                        }
                    }
                }
                else if (nodeData != null && nodeData.nodeType == "Select")
                {
                    // Validate Select modules have all 3 inputs (A, B, Control)
                    var selectModule = kvp.Value as LibNoise.Operator.Select;
                    if (selectModule != null)
                    {
                        bool needsFallback = false;
                        try
                        {
                            var testA = selectModule[0];
                            var testB = selectModule[1];
                            var testControl = selectModule[2];
                            // If we get here, all modules exist
                        }
                        catch (System.ArgumentNullException)
                        {
                            needsFallback = true;
                        }
                        catch (System.ArgumentOutOfRangeException)
                        {
                            needsFallback = true;
                        }
                        
                        if (needsFallback)
                        {
                            Debug.LogWarning($"Select module (GUID: {kvp.Key}) is missing one or more input modules! Using default Perlin noise as fallback.");
                            // Use default Perlin noise for all missing inputs
                            var defaultInput = new Perlin();
                            defaultInput.Frequency = 1.0;
                            
                            try { var test = selectModule[0]; } catch { selectModule[0] = defaultInput; }
                            try { var test = selectModule[1]; } catch { selectModule[1] = defaultInput; }
                            try { var test = selectModule[2]; } catch { selectModule[2] = defaultInput; }
                        }
                    }
                }
            }
            
            // Return the output module
            if (builtModules.TryGetValue(outputNode.guid, out ModuleBase result))
            {
                return result;
            }
            
            return null;
        }
        
        public static ModuleBase CreateModule(NoiseNodeData nodeData)
        {
            switch (nodeData.nodeType)
            {
                case "Perlin":
                    return CreatePerlin(nodeData);
                case "Billow":
                    return CreateBillow(nodeData);
                case "RidgedMultifractal":
                    return CreateRidgedMultifractal(nodeData);
                case "Const":
                    return CreateConst(nodeData);
                case "Add":
                    return new Add();
                case "Multiply":
                    return new Multiply();
                case "Subtract":
                    return new Subtract();
                case "Min":
                    return new Min();
                case "Max":
                    return new Max();
                case "Blend":
                    return new Blend();
                case "Scale":
                    return CreateScale(nodeData);
                case "ScaleBias":
                    return CreateScaleBias(nodeData);
                case "Clamp":
                    return CreateClamp(nodeData);
                case "Power":
                    return new Power();
                case "Abs":
                    return new Abs();
                case "Invert":
                    return new Invert();
                case "Normalize":
                    return new LibNoise.Operator.Normalize();
                case "Erosion":
                    return CreateErosion(nodeData);
                case "Beach":
                    return CreateBeach(nodeData);
                case "Select":
                    return CreateSelect(nodeData);
                case "Curve":
                    return CreateCurve(nodeData);
                case "Slope":
                    return CreateSlope(nodeData);
                case "Portal In":
                case "Portal Out":
                case "SplatOutput":
                    // These node types are handled specially in BuildModuleGraph, not as regular modules
                    return null;
                default:
                    Debug.LogWarning($"Unknown node type: {nodeData.nodeType}");
                    return null;
            }
        }
        
        private static Perlin CreatePerlin(NoiseNodeData nodeData)
        {
            var perlin = new Perlin();
            perlin.Frequency = GetPropertyDouble(nodeData, "frequency", 1.0);
            perlin.Lacunarity = GetPropertyDouble(nodeData, "lacunarity", 2.0);
            perlin.Persistence = GetPropertyDouble(nodeData, "persistence", 0.5);
            perlin.OctaveCount = GetPropertyInt(nodeData, "octaveCount", 6);
            perlin.Seed = GetPropertyInt(nodeData, "seed", 0);
            perlin.Quality = GetPropertyQualityMode(nodeData, "quality", QualityMode.Medium);
            return perlin;
        }
        
        private static Billow CreateBillow(NoiseNodeData nodeData)
        {
            var billow = new Billow();
            billow.Frequency = GetPropertyDouble(nodeData, "frequency", 1.0);
            billow.Lacunarity = GetPropertyDouble(nodeData, "lacunarity", 2.0);
            billow.Persistence = GetPropertyDouble(nodeData, "persistence", 0.5);
            billow.OctaveCount = GetPropertyInt(nodeData, "octaveCount", 6);
            billow.Seed = GetPropertyInt(nodeData, "seed", 0);
            billow.Quality = GetPropertyQualityMode(nodeData, "quality", QualityMode.Medium);
            return billow;
        }
        
        private static RidgedMultifractal CreateRidgedMultifractal(NoiseNodeData nodeData)
        {
            var ridged = new RidgedMultifractal();
            ridged.Frequency = GetPropertyDouble(nodeData, "frequency", 1.0);
            ridged.Lacunarity = GetPropertyDouble(nodeData, "lacunarity", 2.0);
            ridged.OctaveCount = GetPropertyInt(nodeData, "octaveCount", 6);
            ridged.Seed = GetPropertyInt(nodeData, "seed", 0);
            ridged.Quality = GetPropertyQualityMode(nodeData, "quality", QualityMode.Medium);
            return ridged;
        }
        
        private static LibNoise.Generator.Const CreateConst(NoiseNodeData nodeData)
        {
            var constModule = new LibNoise.Generator.Const();
            constModule.Value = GetPropertyDouble(nodeData, "value", 0.0);
            return constModule;
        }
        
        private static Scale CreateScale(NoiseNodeData nodeData)
        {
            var scale = new Scale();
            scale.X = GetPropertyDouble(nodeData, "x", 1.0);
            scale.Y = GetPropertyDouble(nodeData, "y", 1.0);
            scale.Z = GetPropertyDouble(nodeData, "z", 1.0);
            return scale;
        }
        
        private static ScaleBias CreateScaleBias(NoiseNodeData nodeData)
        {
            var scaleBias = new ScaleBias();
            scaleBias.Scale = GetPropertyDouble(nodeData, "scale", 1.0);
            scaleBias.Bias = GetPropertyDouble(nodeData, "bias", 0.0);
            return scaleBias;
        }
        
        private static LibNoise.Operator.Clamp CreateClamp(NoiseNodeData nodeData)
        {
            var clamp = new LibNoise.Operator.Clamp();
            clamp.Minimum = GetPropertyDouble(nodeData, "minimum", -1.0);
            clamp.Maximum = GetPropertyDouble(nodeData, "maximum", 1.0);
            return clamp;
        }
        
        private static LibNoise.Operator.Erosion CreateErosion(NoiseNodeData nodeData)
        {
            var erosion = new LibNoise.Operator.Erosion();
            erosion.Intensity = GetPropertyDouble(nodeData, "intensity", 0.5);
            erosion.Iterations = GetPropertyDouble(nodeData, "iterations", 1.0);
            erosion.SampleDistance = GetPropertyDouble(nodeData, "sampleDistance", 1.0);
            return erosion;
        }
        
        private static LibNoise.Operator.Beach CreateBeach(NoiseNodeData nodeData)
        {
            var beach = new LibNoise.Operator.Beach();
            beach.WaterLevel = GetPropertyDouble(nodeData, "waterLevel", 0.0);
            beach.BeachSize = GetPropertyDouble(nodeData, "beachSize", 0.1);
            beach.BeachHeight = GetPropertyDouble(nodeData, "beachHeight", 0.05);
            beach.SmoothRange = GetPropertyDouble(nodeData, "smoothRange", 0.02);
            return beach;
        }
        
        private static LibNoise.Operator.Select CreateSelect(NoiseNodeData nodeData)
        {
            var select = new LibNoise.Operator.Select();
            select.Minimum = GetPropertyDouble(nodeData, "minimum", -1.0);
            select.Maximum = GetPropertyDouble(nodeData, "maximum", 1.0);
            select.FallOff = GetPropertyDouble(nodeData, "fallOff", 0.0);
            return select;
        }
        
        private static LibNoise.Operator.Curve CreateCurve(NoiseNodeData nodeData)
        {
            var curveModule = new LibNoise.Operator.Curve();
            
            // Get the curve property
            var curveProp = nodeData.properties?.FirstOrDefault(p => p.key == "curve" && p.valueType == "AnimationCurve");
            if (curveProp != null && !string.IsNullOrEmpty(curveProp.value))
            {
                try
                {
                    var serializedCurve = JsonUtility.FromJson<SerializableAnimationCurve>(curveProp.value);
                    if (serializedCurve != null && serializedCurve.keys != null && serializedCurve.keys.Count >= 4)
                    {
                        // Convert AnimationCurve keys to LibNoise control points
                        // LibNoise Curve.Add(inputValue, outputValue):
                        //   - inputValue: the noise input value threshold
                        //   - outputValue: the output value when that threshold is reached
                        // AnimationCurve mapping:
                        //   - time (X-axis) = input noise value threshold
                        //   - value (Y-axis) = output value
                        // So: key.time → inputValue, key.value → outputValue
                        foreach (var key in serializedCurve.keys)
                        {
                            curveModule.Add(key.time, key.value);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Curve node has less than 4 keys. Using default curve.");
                        // Add default curve points
                        // inputValue (noise threshold) → outputValue (result)
                        curveModule.Add(-1.0, -1.0);  // Low noise input → low output
                        curveModule.Add(-0.33, -0.33);
                        curveModule.Add(0.33, 0.33);
                        curveModule.Add(1.0, 1.0);     // High noise input → high output
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to deserialize curve: {e.Message}. Using default curve.");
                    // Add default curve points
                    curveModule.Add(0.0, -1.0);
                    curveModule.Add(0.33, -0.33);
                    curveModule.Add(0.67, 0.33);
                    curveModule.Add(1.0, 1.0);
                }
            }
            else
            {
                // No curve data, use default
                curveModule.Add(0.0, -1.0);
                curveModule.Add(0.33, -0.33);
                curveModule.Add(0.67, 0.33);
                curveModule.Add(1.0, 1.0);
            }
            
            return curveModule;
        }
        
        [System.Serializable]
        private class SerializableAnimationCurve
        {
            public List<SerializableKeyframe> keys;
        }
        
        [System.Serializable]
        private class SerializableKeyframe
        {
            public float time;
            public float value;
            public float inTangent;
            public float outTangent;
        }
        
        private static double GetPropertyDouble(NoiseNodeData nodeData, string key, double defaultValue)
        {
            var prop = nodeData.properties?.FirstOrDefault(p => p.key == key);
            if (prop != null && double.TryParse(prop.value, out double result))
                return result;
            return defaultValue;
        }
        
        private static string GetPropertyString(NoiseNodeData nodeData, string key, string defaultValue)
        {
            var prop = nodeData.properties?.FirstOrDefault(p => p.key == key);
            if (prop != null && !string.IsNullOrEmpty(prop.value))
                return prop.value;
            return defaultValue;
        }
        
        private static int GetPropertyInt(NoiseNodeData nodeData, string key, int defaultValue)
        {
            var prop = nodeData.properties?.FirstOrDefault(p => p.key == key);
            if (prop != null && int.TryParse(prop.value, out int result))
                return result;
            return defaultValue;
        }
        
        private static QualityMode GetPropertyQualityMode(NoiseNodeData nodeData, string key, QualityMode defaultValue)
        {
            var prop = nodeData.properties?.FirstOrDefault(p => p.key == key);
            if (prop != null && System.Enum.TryParse<QualityMode>(prop.value, out QualityMode result))
                return result;
            return defaultValue;
        }
        
        private static LibNoise.Operator.Slope CreateSlope(NoiseNodeData nodeData)
        {
            double sampleDistance = GetPropertyDouble(nodeData, "sampleDistance", 1.0);
            var slope = new LibNoise.Operator.Slope(sampleDistance);
            
            // Set angle filtering parameters
            slope.MinAngle = GetPropertyDouble(nodeData, "minAngle", 0.0);
            slope.MaxAngle = GetPropertyDouble(nodeData, "maxAngle", 90.0);
            slope.SmoothRange = GetPropertyDouble(nodeData, "smoothRange", 0.0);
            slope.TerrainHeight = GetPropertyDouble(nodeData, "terrainHeight", 1.0);
            
            return slope;
        }
    }
}

