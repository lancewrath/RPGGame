using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using LibNoise;

namespace RPGGame.Map
{
    [System.Serializable]
    public class SplatOutputData
    {
        public int orderId;
        public string splatNodeGuid; // GUID of the SplatOutput node itself
        public string sourceNodeGuid; // GUID of the node connected to the splat output's input
        public string diffuseTexturePath;
        public string normalMapPath;
        
        // Mask map parameters (packed into RGBA: R=Metallic, G=Occlusion, B=Height, A=Smoothness)
        public float metallic = 0f;
        public float occlusion = 1f;
        public float height = 0f;
        public float smoothness = 0.5f;
        
        // Tiling settings
        public Vector2 tileSize = new Vector2(15f, 15f);
        public Vector2 tileOffset = new Vector2(0f, 0f);
        
        [System.NonSerialized]
        public ModuleBase noiseModule; // The noise module connected to this splat output's input (not serialized)
    }
    
    public static class SplatOutputBuilder
    {
        public static List<SplatOutputData> ExtractSplatOutputs(NoiseGraphData graphData)
        {
            List<SplatOutputData> splatOutputs = new List<SplatOutputData>();
            
            if (graphData == null || graphData.nodes == null)
                return splatOutputs;
            
            // Find all SplatOutput nodes
            var splatNodes = graphData.nodes.Where(n => n.nodeType == "SplatOutput").ToList();
            
            foreach (var nodeData in splatNodes)
            {
                var splatData = new SplatOutputData
                {
                    splatNodeGuid = nodeData.guid,
                    orderId = GetPropertyInt(nodeData, "orderId", 0),
                    diffuseTexturePath = GetPropertyString(nodeData, "diffuseTexturePath", ""),
                    normalMapPath = GetPropertyString(nodeData, "normalMapPath", ""),
                    metallic = GetPropertyFloat(nodeData, "metallic", 0f),
                    occlusion = GetPropertyFloat(nodeData, "occlusion", 1f),
                    height = GetPropertyFloat(nodeData, "height", 0f),
                    smoothness = GetPropertyFloat(nodeData, "smoothness", 0.5f),
                    tileSize = new Vector2(
                        GetPropertyFloat(nodeData, "tileSizeX", 15f),
                        GetPropertyFloat(nodeData, "tileSizeY", 15f)
                    ),
                    tileOffset = new Vector2(
                        GetPropertyFloat(nodeData, "tileOffsetX", 0f),
                        GetPropertyFloat(nodeData, "tileOffsetY", 0f)
                    )
                };
                
                // Find the edge connected to this splat output's input
                var inputEdge = graphData.edges.FirstOrDefault(e => e.inputNodeGuid == nodeData.guid);
                if (inputEdge != null)
                {
                    // Store the source node GUID so we can build its module later
                    splatData.sourceNodeGuid = inputEdge.outputNodeGuid;
                }
                
                splatOutputs.Add(splatData);
            }
            
            // Sort by order ID
            splatOutputs.Sort((a, b) => a.orderId.CompareTo(b.orderId));
            
            return splatOutputs;
        }
        
        private static int GetPropertyInt(NoiseNodeData nodeData, string key, int defaultValue)
        {
            var prop = nodeData.properties?.FirstOrDefault(p => p.key == key);
            if (prop != null && int.TryParse(prop.value, out int result))
                return result;
            return defaultValue;
        }
        
        private static string GetPropertyString(NoiseNodeData nodeData, string key, string defaultValue)
        {
            var prop = nodeData.properties?.FirstOrDefault(p => p.key == key);
            if (prop != null)
                return prop.value ?? defaultValue;
            return defaultValue;
        }
        
        private static float GetPropertyFloat(NoiseNodeData nodeData, string key, float defaultValue)
        {
            var prop = nodeData.properties?.FirstOrDefault(p => p.key == key);
            if (prop != null && float.TryParse(prop.value, out float result))
                return result;
            return defaultValue;
        }
    }
}

