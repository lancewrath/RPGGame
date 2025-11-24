using UnityEngine;
using System.Collections.Generic;
using System;

namespace RPGGame.Map.Editor
{
    public class AddNode : NoiseGraphNode
    {
        public AddNode() : base("Add", "Add")
        {
            CreateInputPort("A");
            CreateInputPort("B");
            CreateOutputPort("Output");
            RefreshExpandedState();
            RefreshPorts();
        }
    }
    
    public class MultiplyNode : NoiseGraphNode
    {
        public MultiplyNode() : base("Multiply", "Multiply")
        {
            CreateInputPort("A");
            CreateInputPort("B");
            CreateOutputPort("Output");
            RefreshExpandedState();
            RefreshPorts();
        }
    }
    
    public class SubtractNode : NoiseGraphNode
    {
        public SubtractNode() : base("Subtract", "Subtract")
        {
            CreateInputPort("A");
            CreateInputPort("B");
            CreateOutputPort("Output");
            RefreshExpandedState();
            RefreshPorts();
        }
    }
    
    public class MinNode : NoiseGraphNode
    {
        public MinNode() : base("Min", "Min")
        {
            CreateInputPort("A");
            CreateInputPort("B");
            CreateOutputPort("Output");
            RefreshExpandedState();
            RefreshPorts();
        }
    }
    
    public class MaxNode : NoiseGraphNode
    {
        public MaxNode() : base("Max", "Max")
        {
            CreateInputPort("A");
            CreateInputPort("B");
            CreateOutputPort("Output");
            RefreshExpandedState();
            RefreshPorts();
        }
    }
    
    public class BlendNode : NoiseGraphNode
    {
        public BlendNode() : base("Blend", "Blend")
        {
            CreateInputPort("A");
            CreateInputPort("B");
            CreateInputPort("Control");
            CreateOutputPort("Output");
            RefreshExpandedState();
            RefreshPorts();
        }
    }
    
    public class PowerNode : NoiseGraphNode
    {
        public PowerNode() : base("Power", "Power")
        {
            CreateInputPort("Base");
            CreateInputPort("Exponent");
            CreateOutputPort("Output");
            RefreshExpandedState();
            RefreshPorts();
        }
    }
    
    public class AbsNode : NoiseGraphNode
    {
        public AbsNode() : base("Abs", "Absolute")
        {
            CreateInputPort("Input");
            CreateOutputPort("Output");
            RefreshExpandedState();
            RefreshPorts();
        }
    }
    
    public class InvertNode : NoiseGraphNode
    {
        public InvertNode() : base("Invert", "Invert")
        {
            CreateInputPort("Input");
            CreateOutputPort("Output");
            RefreshExpandedState();
            RefreshPorts();
        }
    }
    
    public class SelectNode : NoiseGraphNode
    {
        public double minimum = -1.0;
        public double maximum = 1.0;
        public double fallOff = 0.0;
        
        public SelectNode() : base("Select", "Select")
        {
            CreateInputPort("A");
            CreateInputPort("B");
            CreateInputPort("Control");
            CreateOutputPort("Output");
            RefreshExpandedState();
            RefreshPorts();
        }
        
        protected override List<NoisePropertyData> GetSerializedProperties()
        {
            return new List<NoisePropertyData>
            {
                new NoisePropertyData { key = "minimum", value = minimum.ToString(), valueType = "double" },
                new NoisePropertyData { key = "maximum", value = maximum.ToString(), valueType = "double" },
                new NoisePropertyData { key = "fallOff", value = fallOff.ToString(), valueType = "double" }
            };
        }
        
        protected override void DeserializeProperties(List<NoisePropertyData> properties)
        {
            foreach (var prop in properties)
            {
                switch (prop.key)
                {
                    case "minimum": double.TryParse(prop.value, out minimum); break;
                    case "maximum": double.TryParse(prop.value, out maximum); break;
                    case "fallOff": double.TryParse(prop.value, out fallOff); break;
                }
            }
        }
    }
    
    public class CurveNode : NoiseGraphNode
    {
        [SerializeField]
        private AnimationCurve curve;
        
        public AnimationCurve Curve
        {
            get
            {
                if (curve == null || curve.length < 4)
                {
                    // Ensure we have at least 4 keys
                    curve = new AnimationCurve();
                    curve.AddKey(-1f, -1f);
                    curve.AddKey(-0.33f, -0.33f);
                    curve.AddKey(0.33f, 0.33f);
                    curve.AddKey(1f, 1f);
                }
                return curve;
            }
            set
            {
                if (value == null || value.length < 4)
                {
                    Debug.LogWarning("Curve must have at least 4 keys. Using default curve.");
                    curve = new AnimationCurve();
                    curve.AddKey(-1f, -1f);
                    curve.AddKey(-0.33f, -0.33f);
                    curve.AddKey(0.33f, 0.33f);
                    curve.AddKey(1f, 1f);
                }
                else
                {
                    curve = value;
                }
                NotifyNodeChanged();
            }
        }
        
        public CurveNode() : base("Curve", "Curve")
        {
            CreateInputPort("Input");
            CreateOutputPort("Output");
            
            // Initialize with at least 4 keys
            // X-axis (time) = input noise value threshold (typically -1 to 1)
            // Y-axis (value) = output value (typically -1 to 1, but can be any range)
            curve = new AnimationCurve();
            curve.AddKey(-1f, -1f);   // Low noise input → low output
            curve.AddKey(-0.33f, -0.33f);
            curve.AddKey(0.33f, 0.33f);
            curve.AddKey(1f, 1f);     // High noise input → high output
            
            RefreshExpandedState();
            RefreshPorts();
        }
        
        protected override List<NoisePropertyData> GetSerializedProperties()
        {
            var properties = new List<NoisePropertyData>();
            
            // Serialize curve keys as JSON
            if (curve != null && curve.length >= 4)
            {
                var keys = new List<SerializableKeyframe>();
                foreach (var key in curve.keys)
                {
                    keys.Add(new SerializableKeyframe
                    {
                        time = key.time,
                        value = key.value,
                        inTangent = key.inTangent,
                        outTangent = key.outTangent
                    });
                }
                string curveJson = JsonUtility.ToJson(new SerializableAnimationCurve { keys = keys });
                properties.Add(new NoisePropertyData 
                { 
                    key = "curve", 
                    value = curveJson, 
                    valueType = "AnimationCurve" 
                });
            }
            
            return properties;
        }
        
        protected override void DeserializeProperties(List<NoisePropertyData> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.key == "curve" && prop.valueType == "AnimationCurve")
                {
                    try
                    {
                        var serializedCurve = JsonUtility.FromJson<SerializableAnimationCurve>(prop.value);
                        if (serializedCurve != null && serializedCurve.keys != null && serializedCurve.keys.Count >= 4)
                        {
                            curve = new AnimationCurve();
                            foreach (var key in serializedCurve.keys)
                            {
                                curve.AddKey(new Keyframe(key.time, key.value, key.inTangent, key.outTangent));
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to deserialize curve: {e.Message}");
                    }
                }
            }
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
    }
    
    public class SlopeNode : NoiseGraphNode
    {
        public double sampleDistance = 1.0;
        public double minAngle = 0.0; // Minimum angle in degrees
        public double maxAngle = 90.0; // Maximum angle in degrees
        public double smoothRange = 0.0; // Smooth transition range in degrees
        public double terrainHeight = 1.0; // Terrain height scale
        
        public SlopeNode() : base("Slope", "Slope")
        {
            CreateInputPort("Heightmap");
            CreateOutputPort("Slope");
            
            RefreshExpandedState();
            RefreshPorts();
        }
        
        protected override List<NoisePropertyData> GetSerializedProperties()
        {
            return new List<NoisePropertyData>
            {
                new NoisePropertyData { key = "sampleDistance", value = sampleDistance.ToString(), valueType = "double" },
                new NoisePropertyData { key = "minAngle", value = minAngle.ToString(), valueType = "double" },
                new NoisePropertyData { key = "maxAngle", value = maxAngle.ToString(), valueType = "double" },
                new NoisePropertyData { key = "smoothRange", value = smoothRange.ToString(), valueType = "double" },
                new NoisePropertyData { key = "terrainHeight", value = terrainHeight.ToString(), valueType = "double" }
            };
        }
        
        protected override void DeserializeProperties(List<NoisePropertyData> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.key == "sampleDistance")
                {
                    double.TryParse(prop.value, out sampleDistance);
                }
                else if (prop.key == "minAngle")
                {
                    double.TryParse(prop.value, out minAngle);
                }
                else if (prop.key == "maxAngle")
                {
                    double.TryParse(prop.value, out maxAngle);
                }
                else if (prop.key == "smoothRange")
                {
                    double.TryParse(prop.value, out smoothRange);
                }
                else if (prop.key == "terrainHeight")
                {
                    double.TryParse(prop.value, out terrainHeight);
                }
            }
        }
    }
    
    /// <summary>
    /// Portal In node - accepts an input and stores it with a name for use by Portal Out nodes.
    /// This helps organize node graphs by avoiding crossing connections.
    /// </summary>
    public class PortalInNode : NoiseGraphNode
    {
        public string portalName = "Portal";
        
        public PortalInNode() : base("Portal In", "Portal In")
        {
            CreateInputPort("Input");
            CreateOutputPort("Output"); // Hidden output for internal use
            RefreshExpandedState();
            RefreshPorts();
            AddToClassList("portal-in-node");
        }
        
        protected override List<NoisePropertyData> GetSerializedProperties()
        {
            return new List<NoisePropertyData>
            {
                new NoisePropertyData { key = "portalName", value = portalName, valueType = "string" }
            };
        }
        
        protected override void DeserializeProperties(List<NoisePropertyData> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.key == "portalName")
                {
                    portalName = prop.value ?? "Portal";
                }
            }
        }
    }
    
    /// <summary>
    /// Portal Out node - outputs the value from a connected Portal In node.
    /// Select a Portal In node by name from the dropdown.
    /// </summary>
    public class PortalOutNode : NoiseGraphNode
    {
        public string selectedPortalName = "";
        
        public PortalOutNode() : base("Portal Out", "Portal Out")
        {
            CreateOutputPort("Output");
            RefreshExpandedState();
            RefreshPorts();
            AddToClassList("portal-out-node");
        }
        
        protected override List<NoisePropertyData> GetSerializedProperties()
        {
            return new List<NoisePropertyData>
            {
                new NoisePropertyData { key = "selectedPortalName", value = selectedPortalName, valueType = "string" }
            };
        }
        
        protected override void DeserializeProperties(List<NoisePropertyData> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.key == "selectedPortalName")
                {
                    selectedPortalName = prop.value ?? "";
                }
            }
        }
    }
}

