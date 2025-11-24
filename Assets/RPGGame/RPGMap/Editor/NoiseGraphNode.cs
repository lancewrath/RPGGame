using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace RPGGame.Map.Editor
{
    public class NoiseGraphNode : Node
    {
        public string NodeGuid { get; private set; }
        public string NodeType { get; protected set; }
        public Action<NoiseGraphNode> OnNodeSelected;
        
        public event Action<NoiseGraphNode> NodeChanged;
        
        public void NotifyNodeChanged()
        {
            NodeChanged?.Invoke(this);
            UpdatePreview();
        }
        
        protected List<Port> inputPorts = new List<Port>();
        protected List<Port> outputPorts = new List<Port>();
        
        public List<Port> InputPorts => inputPorts;
        public List<Port> OutputPorts => outputPorts;
        
        // Preview system
        private VisualElement previewFiller;
        private VisualElement previewContainer;
        private Image previewImage;
        private bool previewExpanded = false;
        protected NoiseGraphView graphView;
        
        public bool HasPreview => NodeType == "Perlin" || NodeType == "Billow" || NodeType == "RidgedMultifractal" || NodeType == "Const" || NodeType == "Curve";
        
        public NoiseGraphNode(string nodeType, string title)
        {
            NodeGuid = GUID.Generate().ToString();
            NodeType = nodeType;
            this.title = title;
            
            var styleSheet = Resources.Load<StyleSheet>("NoiseGraphNodeStyle");
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }
        }
        
        public void InitializePreview(NoiseGraphView graphView)
        {
            this.graphView = graphView;
            
            // Update Portal Out node validity if this is a Portal Out node
            if (this is PortalOutNode portalOutNode)
            {
                portalOutNode.UpdateValidityStyle();
            }
            
            if (!HasPreview)
                return;
            
            // Create preview filler (the collapsible section)
            previewFiller = new VisualElement { name = "previewFiller" };
            previewFiller.AddToClassList("collapsed");
            previewFiller.style.borderTopWidth = 1;
            previewFiller.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            previewFiller.style.paddingTop = 2;
            previewFiller.style.paddingBottom = 2;
            
            var previewDivider = new VisualElement { name = "divider" };
            previewDivider.style.height = 1;
            previewDivider.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            previewDivider.style.marginBottom = 2;
            previewFiller.Add(previewDivider);
            
            // Expand button (arrow down when collapsed)
            var expandButton = new VisualElement { name = "expand" };
            expandButton.style.width = 20;
            expandButton.style.height = 20;
            expandButton.style.alignSelf = Align.Center;
            // Cursor will be handled by the Clickable manipulator
            
            var expandIcon = new Label("▼");
            expandIcon.style.fontSize = 10;
            expandIcon.style.color = new Color(0.7f, 0.7f, 0.7f);
            expandIcon.style.unityTextAlign = TextAnchor.MiddleCenter;
            expandButton.Add(expandIcon);
            expandButton.AddManipulator(new Clickable(() => SetPreviewExpanded(true)));
            previewFiller.Add(expandButton);
            
            mainContainer.Add(previewFiller);
            
            // Create preview container (the actual preview image)
            previewContainer = new VisualElement
            {
                name = "previewContainer",
                style = { overflow = Overflow.Hidden },
                pickingMode = PickingMode.Ignore
            };
            
            previewImage = new Image
            {
                name = "preview",
                pickingMode = PickingMode.Ignore,
                image = Texture2D.whiteTexture
            };
            previewImage.style.width = 128;
            previewImage.style.height = 128;
            previewImage.style.alignSelf = Align.Center;
            previewImage.style.marginTop = 2;
            previewImage.style.marginBottom = 2;
            
            // Collapse button (arrow up when expanded)
            var collapseButton = new VisualElement { name = "collapse" };
            collapseButton.style.position = Position.Absolute;
            collapseButton.style.top = 2;
            collapseButton.style.right = 2;
            collapseButton.style.width = 20;
            collapseButton.style.height = 20;
            collapseButton.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            // Cursor will be handled by the Clickable manipulator
            
            var collapseIcon = new Label("▲");
            collapseIcon.style.fontSize = 10;
            collapseIcon.style.color = new Color(0.9f, 0.9f, 0.9f);
            collapseIcon.style.unityTextAlign = TextAnchor.MiddleCenter;
            collapseButton.Add(collapseIcon);
            collapseButton.AddManipulator(new Clickable(() => SetPreviewExpanded(false)));
            previewImage.Add(collapseButton);
            
            previewContainer.Add(previewImage);
            
            UpdatePreview();
        }
        
        private void SetPreviewExpanded(bool expanded)
        {
            previewExpanded = expanded;
            UpdatePreviewExpandedState();
        }
        
        private void UpdatePreviewExpandedState()
        {
            if (previewFiller == null || previewContainer == null)
                return;
            
            if (previewExpanded)
            {
                if (previewContainer.parent != this)
                {
                    Add(previewContainer);
                    var selectionBorder = this.Q("selection-border");
                    if (selectionBorder != null)
                        previewContainer.PlaceBehind(selectionBorder);
                }
                previewFiller.RemoveFromClassList("collapsed");
                previewFiller.AddToClassList("expanded");
                previewFiller.style.height = 0; // Hide the filler when expanded
            }
            else
            {
                if (previewContainer.parent == this)
                {
                    previewContainer.RemoveFromHierarchy();
                }
                previewFiller.RemoveFromClassList("expanded");
                previewFiller.AddToClassList("collapsed");
                previewFiller.style.height = StyleKeyword.Auto; // Show the filler when collapsed
            }
        }
        
        public void UpdatePreview()
        {
            if (!HasPreview || previewImage == null || graphView == null)
                return;
            
            Texture2D previewTexture = NoiseNodePreview.GeneratePreview(this, graphView);
            if (previewTexture != null)
            {
                previewImage.image = previewTexture;
            }
        }
        
        public virtual NoiseNodeData Serialize()
        {
            return new NoiseNodeData
            {
                guid = NodeGuid,
                nodeType = NodeType,
                position = GetPosition().position,
                properties = GetSerializedProperties()
            };
        }
        
        public virtual void Deserialize(NoiseNodeData data)
        {
            NodeGuid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));
            DeserializeProperties(data.properties);
        }
        
        protected virtual List<NoisePropertyData> GetSerializedProperties()
        {
            return new List<NoisePropertyData>();
        }
        
        protected virtual void DeserializeProperties(List<NoisePropertyData> properties)
        {
        }
        
        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected?.Invoke(this);
        }
        
        protected Port CreateInputPort(string portName, Port.Capacity capacity = Port.Capacity.Single)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Input, capacity, typeof(float));
            port.portName = portName;
            inputPorts.Add(port);
            inputContainer.Add(port);
            return port;
        }
        
        protected Port CreateOutputPort(string portName, Port.Capacity capacity = Port.Capacity.Multi)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, capacity, typeof(float));
            port.portName = portName;
            outputPorts.Add(port);
            outputContainer.Add(port);
            return port;
        }
        
        public int GetInputPortIndex(Port port)
        {
            return inputPorts.IndexOf(port);
        }
        
        public int GetOutputPortIndex(Port port)
        {
            return outputPorts.IndexOf(port);
        }
    }
}

