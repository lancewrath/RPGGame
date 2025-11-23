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
                    curve.AddKey(0f, -1f);
                    curve.AddKey(0.33f, -0.33f);
                    curve.AddKey(0.67f, 0.33f);
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
                    curve.AddKey(0f, -1f);
                    curve.AddKey(0.33f, -0.33f);
                    curve.AddKey(0.67f, 0.33f);
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
            curve = new AnimationCurve();
            curve.AddKey(0f, -1f);
            curve.AddKey(0.33f, -0.33f);
            curve.AddKey(0.67f, 0.33f);
            curve.AddKey(1f, 1f);
            
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
}

