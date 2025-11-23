using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGGame.Map
{
    [Serializable]
    public class NoiseGraphData
    {
        public List<NoiseNodeData> nodes = new List<NoiseNodeData>();
        public List<NoiseEdgeData> edges = new List<NoiseEdgeData>();
        public string outputNodeGuid; // GUID of the output node
    }
    
    [Serializable]
    public class NoiseNodeData
    {
        public string guid;
        public string nodeType; // "Perlin", "Billow", "RidgedMultifractal", "Add", "Multiply", etc.
        public Vector2 position;
        public List<NoisePropertyData> properties = new List<NoisePropertyData>();
    }
    
    [Serializable]
    public class NoisePropertyData
    {
        public string key;
        public string value; // Store as string, parse when needed
        public string valueType; // "double", "int", "string", "QualityMode"
    }
    
    [Serializable]
    public class NoiseEdgeData
    {
        public string inputNodeGuid;
        public int inputPortIndex;
        public string outputNodeGuid;
        public int outputPortIndex;
    }
}

