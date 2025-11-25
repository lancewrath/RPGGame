using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;

namespace RPGGame.Map.Editor
{
    public class NoiseNodePropertyInspector : VisualElement
    {
        private NoiseGraphNode currentNode;
        private ScrollView scrollView;
        private VisualElement contentContainer;
        private System.Func<List<string>> getPortalNamesCallback;
        private System.Action refreshPortalOutValidityCallback;
        private System.Func<NoiseGraphView> getGraphViewCallback;
        
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
        
        public void SetGetPortalNamesCallback(System.Func<List<string>> callback)
        {
            getPortalNamesCallback = callback;
        }
        
        public void SetRefreshPortalOutValidityCallback(System.Action callback)
        {
            refreshPortalOutValidityCallback = callback;
        }
        
        public void SetGetGraphViewCallback(System.Func<NoiseGraphView> callback)
        {
            getGraphViewCallback = callback;
        }
        
        private void RefreshPortalOutValidity()
        {
            refreshPortalOutValidityCallback?.Invoke();
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
                case "Clamp":
                    CreateClampProperties(node as ClampNode);
                    break;
                case "Height Selector":
                    CreateHeightSelectorProperties(node as HeightSelectorNode);
                    break;
                case "Erosion":
                    CreateErosionProperties(node as ErosionNode);
                    break;
                case "Beach":
                    CreateBeachProperties(node as BeachNode);
                    break;
                case "Sediment":
                    CreateSedimentProperties(node as SedimentNode);
                    break;
                case "Cache":
                    CreateCacheProperties(node as CacheNode);
                    break;
                case "Select":
                    CreateSelectProperties(node as SelectNode);
                    break;
                case "Curve":
                    CreateCurveProperties(node as CurveNode);
                    break;
                case "Slope":
                    CreateSlopeProperties(node as SlopeNode);
                    break;
                case "Portal In":
                    CreatePortalInProperties(node as PortalInNode);
                    break;
                case "Portal Out":
                    CreatePortalOutProperties(node as PortalOutNode);
                    break;
                case "SplatOutput":
                    CreateSplatOutputProperties(node as SplatOutputNode);
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
        
        private void CreateClampProperties(ClampNode node)
        {
            if (node == null) return;
            
            AddDoubleField("Minimum", node.minimum, (val) => {
                node.minimum = val;
                currentNode?.NotifyNodeChanged();
            });
            AddDoubleField("Maximum", node.maximum, (val) => {
                node.maximum = val;
                currentNode?.NotifyNodeChanged();
            });
        }
        
        private void CreateHeightSelectorProperties(HeightSelectorNode node)
        {
            if (node == null) return;
            
            AddDoubleField("Min Height", node.minHeight, (val) => {
                node.minHeight = val;
                currentNode?.NotifyNodeChanged();
            });
            AddDoubleField("Max Height", node.maxHeight, (val) => {
                node.maxHeight = val;
                currentNode?.NotifyNodeChanged();
            });
            
            // Add help text
            var helpText = new Label("Outputs the input value only if it falls within the height range. Values outside the range output 0. Useful for height-based splatting (e.g., snow on mountain tops, underwater areas).");
            helpText.style.fontSize = 11;
            helpText.style.color = new Color(0.7f, 0.7f, 0.7f);
            helpText.style.marginTop = 10;
            helpText.style.whiteSpace = WhiteSpace.Normal;
            contentContainer.Add(helpText);
        }
        
        private void CreateErosionProperties(ErosionNode node)
        {
            if (node == null) return;
            
            AddDoubleField("Intensity", node.intensity, (val) => {
                node.intensity = val;
                currentNode?.NotifyNodeChanged();
            });
            AddDoubleField("Iterations", node.iterations, (val) => {
                node.iterations = val;
                currentNode?.NotifyNodeChanged();
            });
            AddDoubleField("Sample Distance", node.sampleDistance, (val) => {
                node.sampleDistance = val;
                currentNode?.NotifyNodeChanged();
            });
        }
        
        private void CreateBeachProperties(BeachNode node)
        {
            if (node == null) return;
            
            AddDoubleField("Water Level", node.waterLevel, (val) => {
                node.waterLevel = val;
                currentNode?.NotifyNodeChanged();
            });
            AddDoubleField("Beach Size", node.beachSize, (val) => {
                node.beachSize = val;
                currentNode?.NotifyNodeChanged();
            });
            AddDoubleField("Beach Height", node.beachHeight, (val) => {
                node.beachHeight = val;
                currentNode?.NotifyNodeChanged();
            });
            AddDoubleField("Smooth Range", node.smoothRange, (val) => {
                node.smoothRange = val;
                currentNode?.NotifyNodeChanged();
            });
            AddDoubleField("Sand Blur", node.sandBlur, (val) => {
                node.sandBlur = val;
                currentNode?.NotifyNodeChanged();
            });
            
            // Add help text about outputs
            var helpText = new Label("This node has two outputs:\n" +
                "• Output: Beach-modified height\n" +
                "• Sand: Sand mask for splatting (0.0-1.0)");
            helpText.style.fontSize = 9;
            helpText.style.color = new Color(0.6f, 0.6f, 0.6f);
            helpText.style.marginTop = 5;
            helpText.style.whiteSpace = WhiteSpace.Normal;
            contentContainer.Add(helpText);
        }
        
        private void CreateSedimentProperties(SedimentNode node)
        {
            if (node == null) return;
            
            AddDoubleField("Cliff Threshold", node.cliffThreshold, (val) => {
                node.cliffThreshold = val;
                currentNode?.NotifyNodeChanged();
            });
            AddDoubleField("Sediment Threshold", node.sedimentThreshold, (val) => {
                node.sedimentThreshold = val;
                currentNode?.NotifyNodeChanged();
            });
            
            // Add help text about inputs and outputs
            var helpText = new Label("This node compares pre-erosion and post-erosion heights:\n" +
                "• Inputs: Pre Erosion, Post Erosion\n" +
                "• Cliff: Detects steep drops (erosion removed material)\n" +
                "• Sediment: Detects material deposits (height increased)\n" +
                "Both outputs are masks (0.0-1.0) for splatting.");
            helpText.style.fontSize = 9;
            helpText.style.color = new Color(0.6f, 0.6f, 0.6f);
            helpText.style.marginTop = 5;
            helpText.style.whiteSpace = WhiteSpace.Normal;
            contentContainer.Add(helpText);
        }
        
        private void CreateCacheProperties(CacheNode node)
        {
            if (node == null) return;
            
            // Cache Scale field
            AddDoubleField("Cache Scale", node.cacheScale, (val) => {
                node.cacheScale = val;
                node.isCached = false; // Invalidate cache when scale changes
                currentNode?.NotifyNodeChanged();
            });
            
            // Cache status label
            var statusContainer = new VisualElement();
            statusContainer.style.marginBottom = 5;
            statusContainer.style.marginTop = 5;
            
            var statusLabel = new Label(node.isCached ? "Status: Cached" : "Status: Not Cached");
            statusLabel.style.fontSize = 11;
            statusLabel.style.color = node.isCached ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.8f, 0.6f, 0.4f);
            statusContainer.Add(statusLabel);
            contentContainer.Add(statusContainer);
            
            // Generate Cache button
            var buttonContainer = new VisualElement();
            buttonContainer.style.marginTop = 5;
            buttonContainer.style.marginBottom = 5;
            
            var generateButton = new Button(() => {
                if (getGraphViewCallback == null)
                {
                    Debug.LogWarning("Cannot generate cache: Graph view not available");
                    return;
                }
                
                var graphView = getGraphViewCallback();
                if (graphView == null)
                {
                    Debug.LogWarning("Cannot generate cache: Graph view is null");
                    return;
                }
                
                // Build the module graph up to this cache node
                var inputModule = NoiseNodePreview.BuildNodeModuleForInput(node, graphView);
                if (inputModule == null)
                {
                    Debug.LogWarning("Cannot generate cache: No input module connected");
                    return;
                }
                
                // Create PreviewCache module and generate cache
                var cacheModule = new LibNoise.Operator.PreviewCache(inputModule);
                cacheModule.CacheSize = 128; // Match preview size
                cacheModule.CacheScale = node.cacheScale;
                cacheModule.GenerateCache();
                
                // Extract cached values from the module and store in the node
                double[,] cachedValues = new double[128, 128];
                for (int z = 0; z < 128; z++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        cachedValues[x, z] = cacheModule.GetCachedValue(x, z);
                    }
                }
                node.SetCachedValues(cachedValues);
                
                currentNode?.NotifyNodeChanged();
                
                // Update status label
                statusLabel.text = "Status: Cached";
                statusLabel.style.color = new Color(0.4f, 0.8f, 0.4f);
                
                Debug.Log($"Cache generated for node {node.NodeGuid}");
            });
            generateButton.text = "Generate Cache";
            generateButton.style.width = Length.Percent(100);
            buttonContainer.Add(generateButton);
            contentContainer.Add(buttonContainer);
            
            // Clear Cache button
            var clearButton = new Button(() => {
                node.SetCachedValues(null);
                currentNode?.NotifyNodeChanged();
                
                // Update status label
                statusLabel.text = "Status: Not Cached";
                statusLabel.style.color = new Color(0.8f, 0.6f, 0.4f);
            });
            clearButton.text = "Clear Cache";
            clearButton.style.width = Length.Percent(100);
            clearButton.style.marginTop = 5;
            buttonContainer.Add(clearButton);
            
            var helpText = new Label("Generate a cached preview of the input module. This speeds up preview generation for nodes connected after the cache.");
            helpText.style.fontSize = 9;
            helpText.style.color = new Color(0.6f, 0.6f, 0.6f);
            helpText.style.marginTop = 5;
            helpText.style.whiteSpace = WhiteSpace.Normal;
            contentContainer.Add(helpText);
        }
        
        private void CreateSelectProperties(SelectNode node)
        {
            if (node == null) return;
            
            AddDoubleField("Minimum", node.minimum, (val) => node.minimum = val);
            AddDoubleField("Maximum", node.maximum, (val) => node.maximum = val);
            AddDoubleField("Fall Off", node.fallOff, (val) => node.fallOff = val);
            
            // Add help text explaining how Select works
            var helpText = new Label("How Select works:\n" +
                "• A and B are the two input values to choose between\n" +
                "• Control determines which one to use\n" +
                "• If Control < Minimum: outputs A\n" +
                "• If Control > Maximum: outputs B\n" +
                "• If Control is between Min/Max: blends between A and B");
            helpText.style.fontSize = 9;
            helpText.style.color = new Color(0.6f, 0.6f, 0.6f);
            helpText.style.marginTop = 5;
            helpText.style.whiteSpace = WhiteSpace.Normal;
            contentContainer.Add(helpText);
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
        
        private void CreateSlopeProperties(SlopeNode node)
        {
            if (node == null) return;
            
            AddDoubleField("Sample Distance", node.sampleDistance, (val) => {
                node.sampleDistance = val;
                currentNode?.NotifyNodeChanged();
            });
            
            AddDoubleField("Min Angle", node.minAngle, (val) => {
                node.minAngle = Mathf.Clamp((float)val, 0f, 90f);
                currentNode?.NotifyNodeChanged();
            });
            
            AddDoubleField("Max Angle", node.maxAngle, (val) => {
                node.maxAngle = Mathf.Clamp((float)val, 0f, 90f);
                currentNode?.NotifyNodeChanged();
            });
            
            AddDoubleField("Smooth Range", node.smoothRange, (val) => {
                node.smoothRange = Mathf.Max(0f, (float)val);
                currentNode?.NotifyNodeChanged();
            });
            
            AddDoubleField("Terrain Height", node.terrainHeight, (val) => {
                node.terrainHeight = Mathf.Max(0.001f, (float)val);
                currentNode?.NotifyNodeChanged();
            });
            
            // Add help text
            var helpText = new Label("Angle-based slope filtering:\n" +
                "• Min/Max Angle: Filter slopes by angle range (0° = flat, 90° = vertical)\n" +
                "• Smooth Range: Blending range at boundaries\n" +
                "• Terrain Height: Height scale for angle calculation");
            helpText.style.fontSize = 9;
            helpText.style.color = new Color(0.6f, 0.6f, 0.6f);
            helpText.style.marginTop = 5;
            helpText.style.whiteSpace = WhiteSpace.Normal;
            contentContainer.Add(helpText);
        }
        
        private void CreateSplatOutputProperties(SplatOutputNode node)
        {
            if (node == null) return;
            
            AddIntField("Order ID", node.orderId, (val) => {
                node.orderId = val;
                currentNode?.NotifyNodeChanged();
            });
            
            // Diffuse texture field
            var diffuseContainer = new VisualElement();
            diffuseContainer.style.marginBottom = 5;
            
            var diffuseLabel = new Label("Diffuse Texture");
            diffuseLabel.style.fontSize = 11;
            diffuseLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            diffuseContainer.Add(diffuseLabel);
            
            // Horizontal container for field and browse button
            var diffuseFieldRow = new VisualElement();
            diffuseFieldRow.style.flexDirection = FlexDirection.Row;
            diffuseFieldRow.style.marginTop = 2;
            
            // Use IMGUIContainer to get drag-and-drop support for assets
            var diffuseFieldContainer = new IMGUIContainer(() => {
                EditorGUI.BeginChangeCheck();
                
                // Try to load the texture from the stored path to show it in the field
                Texture2D currentTexture = node.DiffuseTexture; // This will load from path if needed
                if (currentTexture == null && !string.IsNullOrEmpty(node.diffuseTexturePath))
                {
                    // Try to load as asset first (from Assets/StreamingAssets/)
                    string assetPath = "Assets/StreamingAssets/" + node.diffuseTexturePath;
                    currentTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                }
                
                Texture2D newTexture = EditorGUILayout.ObjectField("", currentTexture, typeof(Texture2D), false) as Texture2D;
                
                if (EditorGUI.EndChangeCheck())
                {
                    if (newTexture != null)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(newTexture);
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            // Check if it's in StreamingAssets
                            if (assetPath.StartsWith("Assets/StreamingAssets/"))
                            {
                                // Store relative path from StreamingAssets
                                node.diffuseTexturePath = assetPath.Substring("Assets/StreamingAssets/".Length);
                            }
                            else if (assetPath.StartsWith("Assets/"))
                            {
                                // Store relative path from Assets
                                node.diffuseTexturePath = assetPath.Substring("Assets/".Length);
                            }
                            else
                            {
                                // Store as-is
                                node.diffuseTexturePath = assetPath;
                            }
                            node.SetDiffuseTexture(newTexture);
                            currentNode?.NotifyNodeChanged();
                        }
                    }
                    else
                    {
                        node.diffuseTexturePath = "";
                        node.SetDiffuseTexture(null);
                        currentNode?.NotifyNodeChanged();
                    }
                }
            });
            diffuseFieldContainer.style.flexGrow = 1;
            diffuseFieldContainer.style.height = 18;
            
            // Add drag-and-drop support for files
            diffuseFieldContainer.RegisterCallback<DragUpdatedEvent>(evt => {
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }
                else if (DragAndDrop.paths.Length > 0)
                {
                    // Check if any path is in StreamingAssets
                    foreach (string path in DragAndDrop.paths)
                    {
                        if (path.StartsWith(Application.streamingAssetsPath) || 
                            path.StartsWith("Assets/StreamingAssets/"))
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            break;
                        }
                    }
                }
            });
            
            diffuseFieldContainer.RegisterCallback<DragPerformEvent>(evt => {
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    // Handle Unity asset drag
                    Texture2D draggedTexture = DragAndDrop.objectReferences[0] as Texture2D;
                    if (draggedTexture != null)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(draggedTexture);
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            if (assetPath.StartsWith("Assets/StreamingAssets/"))
                            {
                                node.diffuseTexturePath = assetPath.Substring("Assets/StreamingAssets/".Length);
                            }
                            else if (assetPath.StartsWith("Assets/"))
                            {
                                node.diffuseTexturePath = assetPath.Substring("Assets/".Length);
                            }
                            node.SetDiffuseTexture(draggedTexture);
                            currentNode?.NotifyNodeChanged();
                        }
                    }
                }
                else if (DragAndDrop.paths.Length > 0)
                {
                    // Handle file path drag
                    foreach (string path in DragAndDrop.paths)
                    {
                        string normalizedPath = path.Replace('\\', '/');
                        if (normalizedPath.StartsWith(Application.streamingAssetsPath.Replace('\\', '/')))
                        {
                            node.diffuseTexturePath = normalizedPath.Substring(Application.streamingAssetsPath.Length + 1);
                            node.ClearCachedTextures();
                            currentNode?.NotifyNodeChanged();
                            break;
                        }
                        else if (normalizedPath.StartsWith("Assets/StreamingAssets/"))
                        {
                            node.diffuseTexturePath = normalizedPath.Substring("Assets/StreamingAssets/".Length);
                            node.SetDiffuseTexture(null);
                            currentNode?.NotifyNodeChanged();
                            break;
                        }
                    }
                }
                DragAndDrop.AcceptDrag();
            });
            
            diffuseFieldRow.Add(diffuseFieldContainer);
            
            // Browse button for selecting files
            var diffuseBrowseButton = new Button(() => {
                string startingPath = Application.streamingAssetsPath;
                if (!string.IsNullOrEmpty(node.diffuseTexturePath))
                {
                    string dirPath = System.IO.Path.GetDirectoryName(node.diffuseTexturePath);
                    if (!string.IsNullOrEmpty(dirPath))
                    {
                        startingPath = System.IO.Path.Combine(Application.streamingAssetsPath, dirPath);
                    }
                }
                
                string selectedPath = UnityEditor.EditorUtility.OpenFilePanel("Select Diffuse Texture", startingPath, "png,jpg,jpeg,tga,tiff,tif");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Convert to relative path from StreamingAssets
                    string normalizedPath = selectedPath.Replace('\\', '/');
                    string streamingAssetsNormalized = Application.streamingAssetsPath.Replace('\\', '/');
                    
                    if (normalizedPath.StartsWith(streamingAssetsNormalized))
                    {
                        node.diffuseTexturePath = normalizedPath.Substring(streamingAssetsNormalized.Length + 1);
                        // Load the texture immediately to show in preview
                        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, node.diffuseTexturePath);
                        if (System.IO.File.Exists(fullPath))
                        {
                            byte[] data = System.IO.File.ReadAllBytes(fullPath);
                            Texture2D loadedTexture = new Texture2D(2, 2);
                            loadedTexture.LoadImage(data);
                            // Store in the cached texture field via reflection
                            var field = typeof(SplatOutputNode).GetField("_diffuseTexture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (field != null)
                            {
                                field.SetValue(node, loadedTexture);
                            }
                        }
                        currentNode?.NotifyNodeChanged();
                    }
                    else
                    {
                        Debug.LogWarning("Selected file must be in StreamingAssets folder.");
                    }
                }
            });
            diffuseBrowseButton.text = "Browse";
            diffuseBrowseButton.style.width = 60;
            diffuseBrowseButton.style.marginLeft = 5;
            diffuseFieldRow.Add(diffuseBrowseButton);
            
            diffuseContainer.Add(diffuseFieldRow);
            
            // Add texture preview
            var diffusePreviewContainer = new VisualElement();
            diffusePreviewContainer.style.marginTop = 5;
            diffusePreviewContainer.style.height = 64;
            diffusePreviewContainer.style.width = 64;
            diffusePreviewContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            diffusePreviewContainer.style.borderTopWidth = 1;
            diffusePreviewContainer.style.borderBottomWidth = 1;
            diffusePreviewContainer.style.borderLeftWidth = 1;
            diffusePreviewContainer.style.borderRightWidth = 1;
            diffusePreviewContainer.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            diffusePreviewContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            diffusePreviewContainer.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            diffusePreviewContainer.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            
            var diffusePreviewImage = new Image();
            diffusePreviewImage.style.width = 64;
            diffusePreviewImage.style.height = 64;
            diffusePreviewImage.scaleMode = ScaleMode.ScaleToFit;
            
            // Update preview when texture changes
            System.Action updateDiffusePreview = () => {
                Texture2D previewTex = node.DiffuseTexture;
                if (previewTex != null)
                {
                    diffusePreviewImage.image = previewTex;
                    diffusePreviewContainer.style.display = DisplayStyle.Flex;
                }
                else
                {
                    diffusePreviewContainer.style.display = DisplayStyle.None;
                }
            };
            
            updateDiffusePreview();
            node.NodeChanged += (n) => {
                if (n == node)
                {
                    updateDiffusePreview();
                }
            };
            
            diffusePreviewContainer.Add(diffusePreviewImage);
            diffuseContainer.Add(diffusePreviewContainer);
            
            contentContainer.Add(diffuseContainer);
            
            // Show current path
            if (!string.IsNullOrEmpty(node.diffuseTexturePath))
            {
                var pathLabel = new Label($"Path: {node.diffuseTexturePath}");
                pathLabel.style.fontSize = 9;
                pathLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                pathLabel.style.marginTop = 2;
                diffuseContainer.Add(pathLabel);
            }
            
            // Normal map field
            var normalContainer = new VisualElement();
            normalContainer.style.marginBottom = 5;
            
            var normalLabel = new Label("Normal Map");
            normalLabel.style.fontSize = 11;
            normalLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            normalContainer.Add(normalLabel);
            
            // Horizontal container for field and browse button
            var normalFieldRow = new VisualElement();
            normalFieldRow.style.flexDirection = FlexDirection.Row;
            normalFieldRow.style.marginTop = 2;
            
            // Use IMGUIContainer to get drag-and-drop support for assets
            var normalFieldContainer = new IMGUIContainer(() => {
                EditorGUI.BeginChangeCheck();
                
                // Try to load the texture from the stored path to show it in the field
                Texture2D currentTexture = node.NormalMap; // This will load from path if needed
                if (currentTexture == null && !string.IsNullOrEmpty(node.normalMapPath))
                {
                    // Try to load as asset first (from Assets/StreamingAssets/)
                    string assetPath = "Assets/StreamingAssets/" + node.normalMapPath;
                    currentTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                }
                
                Texture2D newTexture = EditorGUILayout.ObjectField("", currentTexture, typeof(Texture2D), false) as Texture2D;
                
                if (EditorGUI.EndChangeCheck())
                {
                    if (newTexture != null)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(newTexture);
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            // Check if it's in StreamingAssets
                            if (assetPath.StartsWith("Assets/StreamingAssets/"))
                            {
                                // Store relative path from StreamingAssets
                                node.normalMapPath = assetPath.Substring("Assets/StreamingAssets/".Length);
                            }
                            else if (assetPath.StartsWith("Assets/"))
                            {
                                // Store relative path from Assets
                                node.normalMapPath = assetPath.Substring("Assets/".Length);
                            }
                            else
                            {
                                // Store as-is
                                node.normalMapPath = assetPath;
                            }
                            node.SetNormalMap(newTexture);
                            currentNode?.NotifyNodeChanged();
                        }
                    }
                    else
                    {
                        node.normalMapPath = "";
                        node.ClearCachedTextures();
                        currentNode?.NotifyNodeChanged();
                    }
                }
            });
            normalFieldContainer.style.flexGrow = 1;
            normalFieldContainer.style.height = 18;
            
            // Add drag-and-drop support for files
            normalFieldContainer.RegisterCallback<DragUpdatedEvent>(evt => {
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }
                else if (DragAndDrop.paths.Length > 0)
                {
                    // Check if any path is in StreamingAssets
                    foreach (string path in DragAndDrop.paths)
                    {
                        if (path.StartsWith(Application.streamingAssetsPath) || 
                            path.StartsWith("Assets/StreamingAssets/"))
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            break;
                        }
                    }
                }
            });
            
            normalFieldContainer.RegisterCallback<DragPerformEvent>(evt => {
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    // Handle Unity asset drag
                    Texture2D draggedTexture = DragAndDrop.objectReferences[0] as Texture2D;
                    if (draggedTexture != null)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(draggedTexture);
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            if (assetPath.StartsWith("Assets/StreamingAssets/"))
                            {
                                node.normalMapPath = assetPath.Substring("Assets/StreamingAssets/".Length);
                            }
                            else if (assetPath.StartsWith("Assets/"))
                            {
                                node.normalMapPath = assetPath.Substring("Assets/".Length);
                            }
                            node.SetNormalMap(draggedTexture);
                            currentNode?.NotifyNodeChanged();
                        }
                    }
                }
                else if (DragAndDrop.paths.Length > 0)
                {
                    // Handle file path drag
                    foreach (string path in DragAndDrop.paths)
                    {
                        string normalizedPath = path.Replace('\\', '/');
                        if (normalizedPath.StartsWith(Application.streamingAssetsPath.Replace('\\', '/')))
                        {
                            node.normalMapPath = normalizedPath.Substring(Application.streamingAssetsPath.Length + 1);
                            node.SetNormalMap(null);
                            currentNode?.NotifyNodeChanged();
                            break;
                        }
                        else if (normalizedPath.StartsWith("Assets/StreamingAssets/"))
                        {
                            node.normalMapPath = normalizedPath.Substring("Assets/StreamingAssets/".Length);
                            node.ClearCachedTextures();
                            currentNode?.NotifyNodeChanged();
                            break;
                        }
                    }
                }
                DragAndDrop.AcceptDrag();
            });
            
            normalFieldRow.Add(normalFieldContainer);
            
            // Browse button for selecting files
            var normalBrowseButton = new Button(() => {
                string startingPath = Application.streamingAssetsPath;
                if (!string.IsNullOrEmpty(node.normalMapPath))
                {
                    string dirPath = System.IO.Path.GetDirectoryName(node.normalMapPath);
                    if (!string.IsNullOrEmpty(dirPath))
                    {
                        startingPath = System.IO.Path.Combine(Application.streamingAssetsPath, dirPath);
                    }
                }
                
                string selectedPath = UnityEditor.EditorUtility.OpenFilePanel("Select Normal Map", startingPath, "png,jpg,jpeg,tga,tiff,tif");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Convert to relative path from StreamingAssets
                    string normalizedPath = selectedPath.Replace('\\', '/');
                    string streamingAssetsNormalized = Application.streamingAssetsPath.Replace('\\', '/');
                    
                    if (normalizedPath.StartsWith(streamingAssetsNormalized))
                    {
                        node.normalMapPath = normalizedPath.Substring(streamingAssetsNormalized.Length + 1);
                        // Load the texture immediately to show in preview
                        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, node.normalMapPath);
                        if (System.IO.File.Exists(fullPath))
                        {
                            byte[] data = System.IO.File.ReadAllBytes(fullPath);
                            Texture2D loadedTexture = new Texture2D(2, 2);
                            loadedTexture.LoadImage(data);
                            // Store in the cached texture field via reflection
                            var field = typeof(SplatOutputNode).GetField("_normalMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (field != null)
                            {
                                field.SetValue(node, loadedTexture);
                            }
                        }
                        currentNode?.NotifyNodeChanged();
                    }
                    else
                    {
                        Debug.LogWarning("Selected file must be in StreamingAssets folder.");
                    }
                }
            });
            normalBrowseButton.text = "Browse";
            normalBrowseButton.style.width = 60;
            normalBrowseButton.style.marginLeft = 5;
            normalFieldRow.Add(normalBrowseButton);
            
            normalContainer.Add(normalFieldRow);
            
            // Add texture preview
            var normalPreviewContainer = new VisualElement();
            normalPreviewContainer.style.marginTop = 5;
            normalPreviewContainer.style.height = 64;
            normalPreviewContainer.style.width = 64;
            normalPreviewContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            normalPreviewContainer.style.borderTopWidth = 1;
            normalPreviewContainer.style.borderBottomWidth = 1;
            normalPreviewContainer.style.borderLeftWidth = 1;
            normalPreviewContainer.style.borderRightWidth = 1;
            normalPreviewContainer.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            normalPreviewContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            normalPreviewContainer.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            normalPreviewContainer.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            
            var normalPreviewImage = new Image();
            normalPreviewImage.style.width = 64;
            normalPreviewImage.style.height = 64;
            normalPreviewImage.scaleMode = ScaleMode.ScaleToFit;
            
            // Update preview when texture changes
            System.Action updateNormalPreview = () => {
                Texture2D previewTex = node.NormalMap;
                if (previewTex != null)
                {
                    normalPreviewImage.image = previewTex;
                    normalPreviewContainer.style.display = DisplayStyle.Flex;
                }
                else
                {
                    normalPreviewContainer.style.display = DisplayStyle.None;
                }
            };
            
            updateNormalPreview();
            node.NodeChanged += (n) => {
                if (n == node)
                {
                    updateNormalPreview();
                }
            };
            
            normalPreviewContainer.Add(normalPreviewImage);
            normalContainer.Add(normalPreviewContainer);
            
            contentContainer.Add(normalContainer);
            
            // Show current path
            if (!string.IsNullOrEmpty(node.normalMapPath))
            {
                var pathLabel = new Label($"Path: {node.normalMapPath}");
                pathLabel.style.fontSize = 9;
                pathLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                pathLabel.style.marginTop = 2;
                normalContainer.Add(pathLabel);
            }
            
            // Mask map parameters
            var maskHeader = new Label("Mask Map Parameters");
            maskHeader.style.fontSize = 11;
            maskHeader.style.color = new Color(0.8f, 0.8f, 0.8f);
            maskHeader.style.marginTop = 10;
            maskHeader.style.marginBottom = 5;
            contentContainer.Add(maskHeader);
            
            AddFloatField("Metallic (R)", node.metallic, (val) => {
                node.metallic = val;
                currentNode?.NotifyNodeChanged();
            });
            
            AddFloatField("Occlusion (G)", node.occlusion, (val) => {
                node.occlusion = val;
                currentNode?.NotifyNodeChanged();
            });
            
            AddFloatField("Height (B)", node.height, (val) => {
                node.height = val;
                currentNode?.NotifyNodeChanged();
            });
            
            AddFloatField("Smoothness (A)", node.smoothness, (val) => {
                node.smoothness = val;
                currentNode?.NotifyNodeChanged();
            });
            
            // Tiling settings
            var tilingHeader = new Label("Tiling Settings");
            tilingHeader.style.fontSize = 11;
            tilingHeader.style.color = new Color(0.8f, 0.8f, 0.8f);
            tilingHeader.style.marginTop = 10;
            tilingHeader.style.marginBottom = 5;
            contentContainer.Add(tilingHeader);
            
            // Tile Size
            AddFloatField("Tile Size X", node.tileSize.x, (val) => {
                node.tileSize = new Vector2(val, node.tileSize.y);
                currentNode?.NotifyNodeChanged();
            });
            
            AddFloatField("Tile Size Y", node.tileSize.y, (val) => {
                node.tileSize = new Vector2(node.tileSize.x, val);
                currentNode?.NotifyNodeChanged();
            });
            
            // Tile Offset
            AddFloatField("Tile Offset X", node.tileOffset.x, (val) => {
                node.tileOffset = new Vector2(val, node.tileOffset.y);
                currentNode?.NotifyNodeChanged();
            });
            
            AddFloatField("Tile Offset Y", node.tileOffset.y, (val) => {
                node.tileOffset = new Vector2(node.tileOffset.x, val);
                currentNode?.NotifyNodeChanged();
            });
        }
        
        private void CreatePortalInProperties(PortalInNode node)
        {
            if (node == null) return;
            
            AddStringField("Portal Name", node.portalName, (val) => {
                node.portalName = val;
                currentNode?.NotifyNodeChanged();
                // Refresh all Portal Out nodes when a Portal In name changes
                RefreshPortalOutValidity();
            });
            
            var helpText = new Label("Enter a unique name for this portal. Portal Out nodes can reference this portal by name.");
            helpText.style.fontSize = 9;
            helpText.style.color = new Color(0.6f, 0.6f, 0.6f);
            helpText.style.marginTop = 5;
            helpText.style.whiteSpace = WhiteSpace.Normal;
            contentContainer.Add(helpText);
        }
        
        private void CreatePortalOutProperties(PortalOutNode node)
        {
            if (node == null) return;
            
            // Get list of portal names from the graph
            List<string> portalNames = new List<string>();
            if (getPortalNamesCallback != null)
            {
                portalNames = getPortalNamesCallback();
            }
            
            // Add empty option
            if (!portalNames.Contains(""))
                portalNames.Insert(0, "");
            
            // Create dropdown
            var container = new VisualElement();
            container.style.marginBottom = 5;
            
            var labelElement = new Label("Portal In");
            labelElement.style.fontSize = 11;
            labelElement.style.color = new Color(0.8f, 0.8f, 0.8f);
            container.Add(labelElement);
            
            var popupField = new PopupField<string>(portalNames, 
                portalNames.Contains(node.selectedPortalName) ? node.selectedPortalName : "");
            popupField.RegisterValueChangedCallback(evt => {
                node.selectedPortalName = evt.newValue;
                node.UpdateValidityStyle();
                currentNode?.NotifyNodeChanged();
            });
            container.Add(popupField);
            contentContainer.Add(container);
            
            var helpText = new Label("Select a Portal In node to output its input value. This helps organize node graphs by avoiding crossing connections.");
            helpText.style.fontSize = 9;
            helpText.style.color = new Color(0.6f, 0.6f, 0.6f);
            helpText.style.marginTop = 5;
            helpText.style.whiteSpace = WhiteSpace.Normal;
            contentContainer.Add(helpText);
        }
        
        private void AddStringField(string label, string value, Action<string> onChanged)
        {
            var container = new VisualElement();
            container.style.marginBottom = 5;
            
            var labelElement = new Label(label);
            labelElement.style.fontSize = 11;
            labelElement.style.color = new Color(0.8f, 0.8f, 0.8f);
            container.Add(labelElement);
            
            var field = new TextField();
            field.value = value ?? "";
            field.RegisterValueChangedCallback(evt => {
                onChanged(evt.newValue);
            });
            container.Add(field);
            contentContainer.Add(container);
        }
        
        private void AddFloatField(string label, float value, Action<float> onChanged)
        {
            var container = new VisualElement();
            container.style.marginBottom = 5;
            
            var labelElement = new Label(label);
            labelElement.style.fontSize = 11;
            labelElement.style.color = new Color(0.8f, 0.8f, 0.8f);
            container.Add(labelElement);
            
            var field = new FloatField();
            field.value = value;
            field.RegisterValueChangedCallback(evt => {
                onChanged(evt.newValue);
                currentNode?.NotifyNodeChanged();
            });
            field.style.marginTop = 2;
            container.Add(field);
            
            contentContainer.Add(container);
        }
    }
}

