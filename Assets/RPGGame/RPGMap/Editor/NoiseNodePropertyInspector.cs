using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace RPGGame.Map.Editor
{
    public class NoiseNodePropertyInspector : VisualElement
    {
        private NoiseGraphNode currentNode;
        private ScrollView scrollView;
        private VisualElement contentContainer;
        
        public NoiseNodePropertyInspector()
        {
            name = "NodePropertyInspector";
            style.width = 300;
            style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            style.borderLeftWidth = 1;
            style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            
            // Header
            var header = new Label("Node Settings");
            header.style.fontSize = 14;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.paddingTop = 5;
            header.style.paddingBottom = 5;
            header.style.paddingLeft = 10;
            header.style.paddingRight = 10;
            header.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            Add(header);
            
            // Scroll view for content
            scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            Add(scrollView);
            
            contentContainer = new VisualElement();
            contentContainer.style.paddingLeft = 10;
            contentContainer.style.paddingRight = 10;
            contentContainer.style.paddingTop = 5;
            contentContainer.style.paddingBottom = 5;
            scrollView.Add(contentContainer);
        }
        
        public void UpdateSelection(NoiseGraphNode node)
        {
            currentNode = node;
            contentContainer.Clear();
            
            if (node == null)
            {
                var label = new Label("No node selected");
                label.style.color = new Color(0.7f, 0.7f, 0.7f);
                contentContainer.Add(label);
                return;
            }
            
            // Show node type
            var nodeTypeLabel = new Label($"Node Type: {node.NodeType}");
            nodeTypeLabel.style.fontSize = 12;
            nodeTypeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nodeTypeLabel.style.marginBottom = 10;
            contentContainer.Add(nodeTypeLabel);
            
            // Show properties based on node type
            switch (node.NodeType)
            {
                case "Perlin":
                    CreatePerlinProperties(node as PerlinNoiseNode);
                    break;
                case "Billow":
                    CreateBillowProperties(node as BillowNoiseNode);
                    break;
                case "RidgedMultifractal":
                    CreateRidgedMultifractalProperties(node as RidgedMultifractalNoiseNode);
                    break;
                case "Const":
                    CreateConstProperties(node as ConstNoiseNode);
                    break;
                case "Select":
                    CreateSelectProperties(node as SelectNode);
                    break;
                case "Curve":
                    CreateCurveProperties(node as CurveNode);
                    break;
                case "Output":
                    var outputInfo = new Label("This is the final noise output node. Connect your noise graph to this node's input.");
                    outputInfo.style.color = new Color(0.8f, 0.8f, 0.8f);
                    outputInfo.style.whiteSpace = WhiteSpace.Normal;
                    outputInfo.style.marginBottom = 10;
                    contentContainer.Add(outputInfo);
                    break;
                default:
                    var noPropsLabel = new Label("No editable properties");
                    noPropsLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                    contentContainer.Add(noPropsLabel);
                    break;
            }
        }
        
        private void CreatePerlinProperties(PerlinNoiseNode node)
        {
            if (node == null) return;
            
            AddDoubleField("Frequency", node.frequency, (val) => node.frequency = val);
            AddDoubleField("Lacunarity", node.lacunarity, (val) => node.lacunarity = val);
            AddDoubleField("Persistence", node.persistence, (val) => node.persistence = val);
            AddIntField("Octave Count", node.octaveCount, (val) => node.octaveCount = val);
            AddIntField("Seed", node.seed, (val) => node.seed = val);
            AddEnumField("Quality", node.quality, (val) => node.quality = val);
        }
        
        private void CreateBillowProperties(BillowNoiseNode node)
        {
            if (node == null) return;
            
            AddDoubleField("Frequency", node.frequency, (val) => node.frequency = val);
            AddDoubleField("Lacunarity", node.lacunarity, (val) => node.lacunarity = val);
            AddDoubleField("Persistence", node.persistence, (val) => node.persistence = val);
            AddIntField("Octave Count", node.octaveCount, (val) => node.octaveCount = val);
            AddIntField("Seed", node.seed, (val) => node.seed = val);
            AddEnumField("Quality", node.quality, (val) => node.quality = val);
        }
        
        private void CreateRidgedMultifractalProperties(RidgedMultifractalNoiseNode node)
        {
            if (node == null) return;
            
            AddDoubleField("Frequency", node.frequency, (val) => node.frequency = val);
            AddDoubleField("Lacunarity", node.lacunarity, (val) => node.lacunarity = val);
            AddIntField("Octave Count", node.octaveCount, (val) => node.octaveCount = val);
            AddIntField("Seed", node.seed, (val) => node.seed = val);
            AddEnumField("Quality", node.quality, (val) => node.quality = val);
        }
        
        private void CreateConstProperties(ConstNoiseNode node)
        {
            if (node == null) return;
            
            AddDoubleField("Value", node.value, (val) => node.value = val);
        }
        
        private void AddDoubleField(string label, double value, Action<double> onChanged)
        {
            var container = new VisualElement();
            container.style.marginBottom = 5;
            
            var labelElement = new Label(label);
            labelElement.style.fontSize = 11;
            labelElement.style.color = new Color(0.8f, 0.8f, 0.8f);
            container.Add(labelElement);
            
            var field = new DoubleField();
            field.value = value;
            field.RegisterValueChangedCallback(evt => {
                onChanged(evt.newValue);
                currentNode?.NotifyNodeChanged();
            });
            field.style.marginTop = 2;
            container.Add(field);
            
            contentContainer.Add(container);
        }
        
        private void AddIntField(string label, int value, Action<int> onChanged)
        {
            var container = new VisualElement();
            container.style.marginBottom = 5;
            
            var labelElement = new Label(label);
            labelElement.style.fontSize = 11;
            labelElement.style.color = new Color(0.8f, 0.8f, 0.8f);
            container.Add(labelElement);
            
            var field = new IntegerField();
            field.value = value;
            field.RegisterValueChangedCallback(evt => {
                onChanged(evt.newValue);
                currentNode?.NotifyNodeChanged();
            });
            field.style.marginTop = 2;
            container.Add(field);
            
            contentContainer.Add(container);
        }
        
        private void AddEnumField<T>(string label, T value, Action<T> onChanged) where T : Enum
        {
            var container = new VisualElement();
            container.style.marginBottom = 5;
            
            var labelElement = new Label(label);
            labelElement.style.fontSize = 11;
            labelElement.style.color = new Color(0.8f, 0.8f, 0.8f);
            container.Add(labelElement);
            
            var field = new EnumField(value);
            field.RegisterValueChangedCallback(evt => {
                onChanged((T)evt.newValue);
                currentNode?.NotifyNodeChanged();
            });
            field.style.marginTop = 2;
            container.Add(field);
            
            contentContainer.Add(container);
        }
        
        private void CreateSelectProperties(SelectNode node)
        {
            if (node == null) return;
            
            AddDoubleField("Minimum", node.minimum, (val) => node.minimum = val);
            AddDoubleField("Maximum", node.maximum, (val) => node.maximum = val);
            AddDoubleField("Fall Off", node.fallOff, (val) => node.fallOff = val);
        }
        
        private void CreateCurveProperties(CurveNode node)
        {
            if (node == null) return;
            
            var container = new VisualElement();
            container.style.marginBottom = 5;
            
            var labelElement = new Label("Curve (min 4 keys)");
            labelElement.style.fontSize = 11;
            labelElement.style.color = new Color(0.8f, 0.8f, 0.8f);
            container.Add(labelElement);
            
            // Add help text explaining the mapping
            var helpText = new Label("X-axis = Input noise value (-1 to 1)\nY-axis = Output value");
            helpText.style.fontSize = 9;
            helpText.style.color = new Color(0.6f, 0.6f, 0.6f);
            helpText.style.marginBottom = 2;
            helpText.style.whiteSpace = WhiteSpace.Normal;
            container.Add(helpText);
            
            // Use IMGUIContainer to display Unity's AnimationCurve editor
            var curveContainer = new IMGUIContainer(() => {
                EditorGUI.BeginChangeCheck();
                AnimationCurve newCurve = EditorGUILayout.CurveField(node.Curve, GUILayout.Height(100));
                if (EditorGUI.EndChangeCheck())
                {
                    // Validate that curve has at least 4 keys
                    if (newCurve != null && newCurve.length >= 4)
                    {
                        node.Curve = newCurve;
                        currentNode?.NotifyNodeChanged();
                    }
                    else
                    {
                        Debug.LogWarning("Curve must have at least 4 keys. Operation cancelled.");
                    }
                }
            });
            curveContainer.style.marginTop = 2;
            curveContainer.style.height = 100;
            container.Add(curveContainer);
            
            // Add warning label if curve has less than 4 keys
            var warningLabel = new Label("⚠ Curve must have at least 4 keys");
            warningLabel.style.fontSize = 10;
            warningLabel.style.color = new Color(1f, 0.6f, 0f);
            warningLabel.style.marginTop = 2;
            warningLabel.style.display = node.Curve.length < 4 ? DisplayStyle.Flex : DisplayStyle.None;
            container.Add(warningLabel);
            
            // Update warning visibility when curve changes
            node.NodeChanged += (n) => {
                if (n == node)
                {
                    warningLabel.style.display = node.Curve.length < 4 ? DisplayStyle.Flex : DisplayStyle.None;
                    curveContainer.MarkDirtyRepaint();
                }
            };
            
            contentContainer.Add(container);
        }
    }
}

