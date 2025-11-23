using UnityEngine;
using UnityEditor.Experimental.GraphView;

namespace RPGGame.Map.Editor
{
    public class NoiseOutputNode : NoiseGraphNode
    {
        public NoiseOutputNode() : base("Output", "Noise Output")
        {
            CreateInputPort("Noise", Port.Capacity.Single);
            
            RefreshExpandedState();
            RefreshPorts();
            
            // Make output node visually distinct
            AddToClassList("output-node");
        }
        
        protected override void DeserializeProperties(System.Collections.Generic.List<NoisePropertyData> properties)
        {
            // Output node has no properties
        }
        
        protected override System.Collections.Generic.List<NoisePropertyData> GetSerializedProperties()
        {
            // Output node has no properties
            return new System.Collections.Generic.List<NoisePropertyData>();
        }
    }
}

