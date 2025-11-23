using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RPGGame.Map.Editor
{
    public class PerlinNoiseNode : NoiseGraphNode
    {
        public double frequency = 1.0;
        public double lacunarity = 2.0;
        public double persistence = 0.5;
        public int octaveCount = 6;
        public int seed = 0;
        public LibNoise.QualityMode quality = LibNoise.QualityMode.Medium;
        
        public PerlinNoiseNode() : base("Perlin", "Perlin Noise")
        {
            CreateOutputPort("Output");
            
            RefreshExpandedState();
            RefreshPorts();
        }
        
        protected override List<NoisePropertyData> GetSerializedProperties()
        {
            return new List<NoisePropertyData>
            {
                new NoisePropertyData { key = "frequency", value = frequency.ToString(), valueType = "double" },
                new NoisePropertyData { key = "lacunarity", value = lacunarity.ToString(), valueType = "double" },
                new NoisePropertyData { key = "persistence", value = persistence.ToString(), valueType = "double" },
                new NoisePropertyData { key = "octaveCount", value = octaveCount.ToString(), valueType = "int" },
                new NoisePropertyData { key = "seed", value = seed.ToString(), valueType = "int" },
                new NoisePropertyData { key = "quality", value = quality.ToString(), valueType = "QualityMode" }
            };
        }
        
        protected override void DeserializeProperties(List<NoisePropertyData> properties)
        {
            foreach (var prop in properties)
            {
                switch (prop.key)
                {
                    case "frequency": double.TryParse(prop.value, out frequency); break;
                    case "lacunarity": double.TryParse(prop.value, out lacunarity); break;
                    case "persistence": double.TryParse(prop.value, out persistence); break;
                    case "octaveCount": int.TryParse(prop.value, out octaveCount); break;
                    case "seed": int.TryParse(prop.value, out seed); break;
                    case "quality": System.Enum.TryParse(prop.value, out quality); break;
                }
            }
        }
    }
    
    public class BillowNoiseNode : NoiseGraphNode
    {
        public double frequency = 1.0;
        public double lacunarity = 2.0;
        public double persistence = 0.5;
        public int octaveCount = 6;
        public int seed = 0;
        public LibNoise.QualityMode quality = LibNoise.QualityMode.Medium;
        
        public BillowNoiseNode() : base("Billow", "Billow Noise")
        {
            CreateOutputPort("Output");
            RefreshExpandedState();
            RefreshPorts();
        }
        
        protected override List<NoisePropertyData> GetSerializedProperties()
        {
            return new List<NoisePropertyData>
            {
                new NoisePropertyData { key = "frequency", value = frequency.ToString(), valueType = "double" },
                new NoisePropertyData { key = "lacunarity", value = lacunarity.ToString(), valueType = "double" },
                new NoisePropertyData { key = "persistence", value = persistence.ToString(), valueType = "double" },
                new NoisePropertyData { key = "octaveCount", value = octaveCount.ToString(), valueType = "int" },
                new NoisePropertyData { key = "seed", value = seed.ToString(), valueType = "int" },
                new NoisePropertyData { key = "quality", value = quality.ToString(), valueType = "QualityMode" }
            };
        }
        
        protected override void DeserializeProperties(List<NoisePropertyData> properties)
        {
            foreach (var prop in properties)
            {
                switch (prop.key)
                {
                    case "frequency": double.TryParse(prop.value, out frequency); break;
                    case "lacunarity": double.TryParse(prop.value, out lacunarity); break;
                    case "persistence": double.TryParse(prop.value, out persistence); break;
                    case "octaveCount": int.TryParse(prop.value, out octaveCount); break;
                    case "seed": int.TryParse(prop.value, out seed); break;
                    case "quality": System.Enum.TryParse(prop.value, out quality); break;
                }
            }
        }
    }
    
    public class RidgedMultifractalNoiseNode : NoiseGraphNode
    {
        public double frequency = 1.0;
        public double lacunarity = 2.0;
        public int octaveCount = 6;
        public int seed = 0;
        public LibNoise.QualityMode quality = LibNoise.QualityMode.Medium;
        
        public RidgedMultifractalNoiseNode() : base("RidgedMultifractal", "Ridged Multifractal")
        {
            CreateOutputPort("Output");
            RefreshExpandedState();
            RefreshPorts();
        }
        
        protected override List<NoisePropertyData> GetSerializedProperties()
        {
            return new List<NoisePropertyData>
            {
                new NoisePropertyData { key = "frequency", value = frequency.ToString(), valueType = "double" },
                new NoisePropertyData { key = "lacunarity", value = lacunarity.ToString(), valueType = "double" },
                new NoisePropertyData { key = "octaveCount", value = octaveCount.ToString(), valueType = "int" },
                new NoisePropertyData { key = "seed", value = seed.ToString(), valueType = "int" },
                new NoisePropertyData { key = "quality", value = quality.ToString(), valueType = "QualityMode" }
            };
        }
        
        protected override void DeserializeProperties(List<NoisePropertyData> properties)
        {
            foreach (var prop in properties)
            {
                switch (prop.key)
                {
                    case "frequency": double.TryParse(prop.value, out frequency); break;
                    case "lacunarity": double.TryParse(prop.value, out lacunarity); break;
                    case "octaveCount": int.TryParse(prop.value, out octaveCount); break;
                    case "seed": int.TryParse(prop.value, out seed); break;
                    case "quality": System.Enum.TryParse(prop.value, out quality); break;
                }
            }
        }
    }
}

