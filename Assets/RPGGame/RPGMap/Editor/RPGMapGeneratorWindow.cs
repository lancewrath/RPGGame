using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RPGGame.Map.Editor
{
    public class RPGMapGeneratorWindow : EditorWindow
    {
        private NoiseGraphView graphView;
        private string currentGraphName = "default_heightmap";
        private string currentMapName = "DefaultMap";
        private VisualElement toolbar;
        private NoiseNodePropertyInspector propertyInspector;
        
        [MenuItem("Window/RPG Map/Heightmap Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<RPGMapGeneratorWindow>();
            window.titleContent = new GUIContent("Heightmap Graph Editor");
            window.Show();
        }
        
        public static void OpenWindowForMap(string mapName, string graphName)
        {
            var window = GetWindow<RPGMapGeneratorWindow>();
            window.titleContent = new GUIContent($"Heightmap Graph Editor - {mapName}");
            window.currentMapName = mapName;
            window.currentGraphName = graphName;
            window.LoadGraph();
            window.Show();
        }
        
        private void OnEnable()
        {
            GenerateToolbar();
            rootVisualElement.Add(toolbar);
            
            // Create content container with horizontal layout (graph view + inspector)
            var contentContainer = new VisualElement();
            contentContainer.style.flexDirection = FlexDirection.Row;
            contentContainer.style.flexGrow = 1;
            rootVisualElement.Add(contentContainer);
            
            ConstructGraphView();
            contentContainer.Add(graphView);
            
            GeneratePropertyInspector();
            contentContainer.Add(propertyInspector);
            
            GenerateMiniMap();
        }
        
        private void OnDisable()
        {
            // Cleanup is handled automatically by Unity
        }
        
        private void ConstructGraphView()
        {
            graphView = new NoiseGraphView(this)
            {
                name = "Noise Graph View"
            };
            
            graphView.OnNodeSelected = (node) => {
                if (propertyInspector != null)
                    propertyInspector.UpdateSelection(node);
            };
            
            // Make graph view take up remaining space
            graphView.style.flexGrow = 1;
        }
        
        private void GeneratePropertyInspector()
        {
            propertyInspector = new NoiseNodePropertyInspector();
            // Inspector has fixed width, positioned on the right
            propertyInspector.style.flexShrink = 0;
            
            // Set callback to get portal names from graph view
            propertyInspector.SetGetPortalNamesCallback(() => {
                return graphView != null ? graphView.GetPortalNames() : new List<string>();
            });
            
            // Set callback to refresh Portal Out validity
            propertyInspector.SetRefreshPortalOutValidityCallback(() => {
                if (graphView != null)
                    graphView.RefreshPortalOutValidity();
            });
            
            // Set callback to get graph view for cache generation
            propertyInspector.SetGetGraphViewCallback(() => {
                return graphView;
            });
        }
        
        private void GenerateToolbar()
        {
            toolbar = new VisualElement();
            toolbar.name = "toolbar";
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.height = 30;
            toolbar.style.paddingLeft = 5;
            toolbar.style.paddingRight = 5;
            toolbar.style.paddingTop = 3;
            toolbar.style.paddingBottom = 3;
            toolbar.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            
            var mapNameField = new TextField("Map Name:");
            mapNameField.value = currentMapName;
            mapNameField.style.width = 250;
            mapNameField.style.marginRight = 5;
            mapNameField.RegisterValueChangedCallback(evt => currentMapName = evt.newValue);
            toolbar.Add(mapNameField);
            
            var graphNameField = new TextField("Graph Name:");
            graphNameField.value = currentGraphName;
            graphNameField.style.width = 250;
            graphNameField.style.marginRight = 5;
            graphNameField.RegisterValueChangedCallback(evt => currentGraphName = evt.newValue);
            toolbar.Add(graphNameField);
            
            // Add spacer
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);
            
            var loadButton = new Button(() => LoadGraph()) { text = "Load Graph" };
            loadButton.style.marginRight = 5;
            toolbar.Add(loadButton);
            
            var saveButton = new Button(() => SaveGraph()) { text = "Save Graph" };
            toolbar.Add(saveButton);
            
            rootVisualElement.Add(toolbar);
        }
        
        private void GenerateMiniMap()
        {
            var miniMap = new MiniMap { anchored = true };
            miniMap.SetPosition(new Rect(10, 30, 200, 140));
            graphView.Add(miniMap);
        }
        
        private void SaveGraph()
        {
            if (string.IsNullOrEmpty(currentMapName) || string.IsNullOrEmpty(currentGraphName))
            {
                EditorUtility.DisplayDialog("Error", "Map name and Graph name must be specified.", "OK");
                return;
            }
            
            string worldFolder = Path.Combine(Application.streamingAssetsPath, "Worlds", currentMapName);
            if (!Directory.Exists(worldFolder))
            {
                Directory.CreateDirectory(worldFolder);
            }
            
            NoiseGraphData graphData = graphView.SerializeGraph();
            string json = JsonUtility.ToJson(graphData, true);
            string filePath = Path.Combine(worldFolder, $"{currentGraphName}.json");
            
            File.WriteAllText(filePath, json);
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", $"Graph saved to:\n{filePath}", "OK");
        }
        
        private void LoadGraph()
        {
            if (string.IsNullOrEmpty(currentMapName) || string.IsNullOrEmpty(currentGraphName))
            {
                EditorUtility.DisplayDialog("Error", "Map name and Graph name must be specified.", "OK");
                return;
            }
            
            string worldFolder = Path.Combine(Application.streamingAssetsPath, "Worlds", currentMapName);
            string filePath = Path.Combine(worldFolder, $"{currentGraphName}.json");
            
            if (!File.Exists(filePath))
            {
                EditorUtility.DisplayDialog("Error", $"Graph file not found:\n{filePath}", "OK");
                return;
            }
            
            try
            {
                string json = File.ReadAllText(filePath);
                NoiseGraphData graphData = JsonUtility.FromJson<NoiseGraphData>(json);
                graphView.DeserializeGraph(graphData);
                EditorUtility.DisplayDialog("Success", "Graph loaded successfully.", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load graph:\n{e.Message}", "OK");
            }
        }
    }
    
    public class NoiseGraphView : GraphView
    {
        private RPGMapGeneratorWindow window;
        private NoiseGraphNode outputNode;
        public Action<NoiseGraphNode> OnNodeSelected;
        
        public NoiseGraphView(RPGMapGeneratorWindow editorWindow)
        {
            window = editorWindow;
            
            AddManipulators();
            AddGridBackground();
            AddStyles();
            
            AddSearchWindow();
        }
        
        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            UpdateSelection();
        }
        
        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            UpdateSelection();
        }
        
        public override void ClearSelection()
        {
            base.ClearSelection();
            UpdateSelection();
        }
        
        private void UpdateSelection()
        {
            NoiseGraphNode selectedNode = null;
            var currentSelection = selection.ToList();
            if (currentSelection.Count > 0 && currentSelection[0] is NoiseGraphNode node)
            {
                selectedNode = node;
            }
            OnNodeSelected?.Invoke(selectedNode);
        }
        
        // Get list of portal names from all Portal In nodes in the graph
        public List<string> GetPortalNames()
        {
            List<string> portalNames = new List<string>();
            foreach (var node in nodes.ToList().OfType<PortalInNode>())
            {
                if (!string.IsNullOrEmpty(node.portalName) && !portalNames.Contains(node.portalName))
                {
                    portalNames.Add(node.portalName);
                }
            }
            return portalNames;
        }
        
        // Method to refresh previews when graph changes
        public void RefreshNodePreviews()
        {
            foreach (var node in nodes.ToList().OfType<NoiseGraphNode>())
            {
                if (node.HasPreview)
                {
                    node.UpdatePreview();
                }
            }
        }
        
        // Method to refresh Portal Out node validity when graph changes
        public void RefreshPortalOutValidity()
        {
            foreach (var node in nodes.ToList().OfType<PortalOutNode>())
            {
                node.UpdateValidityStyle();
            }
        }
        
        private void AddManipulators()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());
        }
        
        private void AddGridBackground()
        {
            var grid = new GridBackground();
            grid.StretchToParentSize();
            Insert(0, grid);
        }
        
        private void AddStyles()
        {
            var styleSheet = Resources.Load<StyleSheet>("NoiseGraphViewStyle");
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }
        }
        
        private void AddSearchWindow()
        {
            nodeCreationRequest = context =>
            {
                var searchWindow = ScriptableObject.CreateInstance<NoiseNodeSearchWindow>();
                searchWindow.Initialize(this);
                
                // Convert the mouse position to graph view coordinates when opening the search window
                // This ensures we have the correct position even on multi-monitor setups
                Vector2 screenPos = context.screenMousePosition;
                var window = EditorWindow.focusedWindow;
                
                if (window != null)
                {
                    var windowRoot = window.rootVisualElement;
                    // Convert screen to window root's parent coordinate space (same as ShaderGraph)
                    Vector2 windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent, screenPos);
                    // Store this converted position for use in OnSelectEntry
                    searchWindow.SetMousePosition(windowMousePosition);
                }
                
                // Pass the original screen mouse position (SearchWindow expects screen coordinates)
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
            };
        }
        
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            
            ports.ForEach(port =>
            {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });
            
            return compatiblePorts;
        }
        
        public NoiseGraphData SerializeGraph()
        {
            var graphData = new NoiseGraphData();
            
            // Serialize all nodes
            foreach (var node in nodes.ToList().OfType<NoiseGraphNode>())
            {
                graphData.nodes.Add(node.Serialize());
            }
            
            // Serialize all edges
            foreach (var edge in edges.ToList())
            {
                if (edge.input.node is NoiseGraphNode inputNode && edge.output.node is NoiseGraphNode outputNodeData)
                {
                    graphData.edges.Add(new NoiseEdgeData
                    {
                        inputNodeGuid = inputNode.NodeGuid,
                        inputPortIndex = inputNode.GetInputPortIndex(edge.input),
                        outputNodeGuid = outputNodeData.NodeGuid,
                        outputPortIndex = outputNodeData.GetOutputPortIndex(edge.output)
                    });
                }
            }
            
            // Find output node (dedicated Output node, or node with no output connections, or first node if none)
            var outputNode = nodes.ToList().OfType<NoiseOutputNode>().FirstOrDefault();
            if (outputNode != null)
            {
                graphData.outputNodeGuid = outputNode.NodeGuid;
            }
            else
            {
                // Fallback: find node with no output connections
                var nodesWithOutputs = nodes.ToList().OfType<NoiseGraphNode>()
                    .Where(n => n.OutputPorts.Count > 0 && !edges.ToList().Any(e => e.output.node == n));
                
                if (nodesWithOutputs.Any())
                {
                    graphData.outputNodeGuid = nodesWithOutputs.First().NodeGuid;
                }
                else if (graphData.nodes.Count > 0)
                {
                    graphData.outputNodeGuid = graphData.nodes[0].guid;
                }
            }
            
            return graphData;
        }
        
        public void DeserializeGraph(NoiseGraphData graphData)
        {
            // Clear existing graph
            DeleteElements(graphElements.ToList());
            
            // Create nodes
            Dictionary<string, NoiseGraphNode> nodeMap = new Dictionary<string, NoiseGraphNode>();
            
            foreach (var nodeData in graphData.nodes)
            {
                NoiseGraphNode node = CreateNodeFromType(nodeData.nodeType);
                if (node != null)
                {
                    node.Deserialize(nodeData);
                    node.InitializePreview(this);
                    AddElement(node);
                    nodeMap[nodeData.guid] = node;
                }
            }
            
            // Refresh Portal Out node validity after all nodes are loaded
            // (in case Portal In nodes are loaded after Portal Out nodes that reference them)
            RefreshPortalOutValidity();
            
            // Create edges
            foreach (var edgeData in graphData.edges)
            {
                if (nodeMap.TryGetValue(edgeData.inputNodeGuid, out NoiseGraphNode inputNode) &&
                    nodeMap.TryGetValue(edgeData.outputNodeGuid, out NoiseGraphNode outputNode))
                {
                    if (inputNode.InputPorts.Count > edgeData.inputPortIndex &&
                        outputNode.OutputPorts.Count > edgeData.outputPortIndex)
                    {
                        var inputPort = inputNode.InputPorts[edgeData.inputPortIndex];
                        var outputPort = outputNode.OutputPorts[edgeData.outputPortIndex];
                        
                        var edge = outputPort.ConnectTo(inputPort);
                        AddElement(edge);
                    }
                }
            }
        }
        
        private NoiseGraphNode CreateNodeFromType(string nodeType)
        {
            switch (nodeType)
            {
                case "Perlin": return new PerlinNoiseNode();
                case "Billow": return new BillowNoiseNode();
                case "RidgedMultifractal": return new RidgedMultifractalNoiseNode();
                case "Const": return new ConstNoiseNode();
                case "Add": return new AddNode();
                case "Multiply": return new MultiplyNode();
                case "Subtract": return new SubtractNode();
                case "Min": return new MinNode();
                case "Max": return new MaxNode();
                case "Blend": return new BlendNode();
                case "Power": return new PowerNode();
                case "Abs": return new AbsNode();
                case "Invert": return new InvertNode();
                case "Normalize": return new NormalizeNode();
                case "Erosion": return new ErosionNode();
                case "Beach": return new BeachNode();
                case "Sediment": return new SedimentNode();
                case "Clamp": return new ClampNode();
                case "Height Selector": return new HeightSelectorNode();
                case "Cache": return new CacheNode();
                case "Select": return new SelectNode();
                case "Curve": return new CurveNode();
                case "Slope": return new SlopeNode();
                case "Portal In": return new PortalInNode();
                case "Portal Out": return new PortalOutNode();
                case "Output": return new NoiseOutputNode();
                case "SplatOutput": return new SplatOutputNode();
                default: return null;
            }
        }
    }
    
    public class NoiseNodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private NoiseGraphView graphView;
        private Texture2D indentationIcon;
        private Vector2? cachedMousePosition; // Store converted mouse position
        
        public void Initialize(NoiseGraphView graphView)
        {
            this.graphView = graphView;
            indentationIcon = new Texture2D(1, 1);
            indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
            indentationIcon.Apply();
        }
        
        public void SetMousePosition(Vector2 windowMousePosition)
        {
            cachedMousePosition = windowMousePosition;
        }
        
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
                new SearchTreeGroupEntry(new GUIContent("Generators"), 1),
                new SearchTreeEntry(new GUIContent("Perlin Noise", indentationIcon))
                {
                    level = 2,
                    userData = typeof(PerlinNoiseNode)
                },
                new SearchTreeEntry(new GUIContent("Billow Noise", indentationIcon))
                {
                    level = 2,
                    userData = typeof(BillowNoiseNode)
                },
                new SearchTreeEntry(new GUIContent("Ridged Multifractal", indentationIcon))
                {
                    level = 2,
                    userData = typeof(RidgedMultifractalNoiseNode)
                },
                new SearchTreeEntry(new GUIContent("Constant", indentationIcon))
                {
                    level = 2,
                    userData = typeof(ConstNoiseNode)
                },
                new SearchTreeGroupEntry(new GUIContent("Operators"), 1),
                new SearchTreeEntry(new GUIContent("Add", indentationIcon))
                {
                    level = 2,
                    userData = typeof(AddNode)
                },
                new SearchTreeEntry(new GUIContent("Multiply", indentationIcon))
                {
                    level = 2,
                    userData = typeof(MultiplyNode)
                },
                new SearchTreeEntry(new GUIContent("Subtract", indentationIcon))
                {
                    level = 2,
                    userData = typeof(SubtractNode)
                },
                new SearchTreeEntry(new GUIContent("Min", indentationIcon))
                {
                    level = 2,
                    userData = typeof(MinNode)
                },
                new SearchTreeEntry(new GUIContent("Max", indentationIcon))
                {
                    level = 2,
                    userData = typeof(MaxNode)
                },
                new SearchTreeEntry(new GUIContent("Blend", indentationIcon))
                {
                    level = 2,
                    userData = typeof(BlendNode)
                },
                new SearchTreeEntry(new GUIContent("Power", indentationIcon))
                {
                    level = 2,
                    userData = typeof(PowerNode)
                },
                new SearchTreeEntry(new GUIContent("Absolute", indentationIcon))
                {
                    level = 2,
                    userData = typeof(AbsNode)
                },
                new SearchTreeEntry(new GUIContent("Invert", indentationIcon))
                {
                    level = 2,
                    userData = typeof(InvertNode)
                },
                new SearchTreeEntry(new GUIContent("Normalize", indentationIcon))
                {
                    level = 2,
                    userData = typeof(NormalizeNode)
                },
                new SearchTreeEntry(new GUIContent("Erosion", indentationIcon))
                {
                    level = 2,
                    userData = typeof(ErosionNode)
                },
                new SearchTreeEntry(new GUIContent("Beach", indentationIcon))
                {
                    level = 2,
                    userData = typeof(BeachNode)
                },
                new SearchTreeEntry(new GUIContent("Sediment", indentationIcon))
                {
                    level = 2,
                    userData = typeof(SedimentNode)
                },
                new SearchTreeEntry(new GUIContent("Clamp", indentationIcon))
                {
                    level = 2,
                    userData = typeof(ClampNode)
                },
                new SearchTreeEntry(new GUIContent("Height Selector", indentationIcon))
                {
                    level = 2,
                    userData = typeof(HeightSelectorNode)
                },
                new SearchTreeEntry(new GUIContent("Select", indentationIcon))
                {
                    level = 2,
                    userData = typeof(SelectNode)
                },
                new SearchTreeEntry(new GUIContent("Curve", indentationIcon))
                {
                    level = 2,
                    userData = typeof(CurveNode)
                },
                new SearchTreeEntry(new GUIContent("Slope", indentationIcon))
                {
                    level = 2,
                    userData = typeof(SlopeNode)
                },
                new SearchTreeGroupEntry(new GUIContent("Utility"), 1),
                new SearchTreeEntry(new GUIContent("Cache", indentationIcon))
                {
                    level = 2,
                    userData = typeof(CacheNode)
                },
                new SearchTreeEntry(new GUIContent("Portal In", indentationIcon))
                {
                    level = 2,
                    userData = typeof(PortalInNode)
                },
                new SearchTreeEntry(new GUIContent("Portal Out", indentationIcon))
                {
                    level = 2,
                    userData = typeof(PortalOutNode)
                },
                new SearchTreeGroupEntry(new GUIContent("Output"), 1),
                new SearchTreeEntry(new GUIContent("Noise Output", indentationIcon))
                {
                    level = 2,
                    userData = typeof(NoiseOutputNode)
                },
                new SearchTreeEntry(new GUIContent("Splat Output", indentationIcon))
                {
                    level = 2,
                    userData = typeof(SplatOutputNode)
                }
            };
            
            return tree;
        }
        
        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            var window = EditorWindow.focusedWindow;
            if (window == null || graphView == null) return false;
            
            Vector2 windowMousePosition;
            
            // Use cached mouse position if available (converted when search window opened)
            // Otherwise fall back to converting from screen coordinates
            if (cachedMousePosition.HasValue)
            {
                windowMousePosition = cachedMousePosition.Value;
            }
            else
            {
                // Fallback: convert screen coordinates (original approach)
                var windowRoot = window.rootVisualElement;
                Vector2 screenPos = context.screenMousePosition;
                windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent, screenPos);
            }
            
            // Convert to contentViewContainer local coordinates
            // WorldToLocal automatically accounts for the view transform (zoom/pan)
            Vector2 graphLocalPos = graphView.contentViewContainer.WorldToLocal(windowMousePosition);
            
            // Clear cached position after use
            cachedMousePosition = null;
            
            NoiseGraphNode node = null;
            Type nodeType = searchTreeEntry.userData as Type;
            
            if (nodeType == typeof(PerlinNoiseNode))
                node = new PerlinNoiseNode();
            else if (nodeType == typeof(BillowNoiseNode))
                node = new BillowNoiseNode();
            else if (nodeType == typeof(RidgedMultifractalNoiseNode))
                node = new RidgedMultifractalNoiseNode();
            else if (nodeType == typeof(ConstNoiseNode))
                node = new ConstNoiseNode();
            else if (nodeType == typeof(AddNode))
                node = new AddNode();
            else if (nodeType == typeof(MultiplyNode))
                node = new MultiplyNode();
            else if (nodeType == typeof(SubtractNode))
                node = new SubtractNode();
            else if (nodeType == typeof(MinNode))
                node = new MinNode();
            else if (nodeType == typeof(MaxNode))
                node = new MaxNode();
            else if (nodeType == typeof(BlendNode))
                node = new BlendNode();
            else if (nodeType == typeof(PowerNode))
                node = new PowerNode();
            else if (nodeType == typeof(AbsNode))
                node = new AbsNode();
            else if (nodeType == typeof(InvertNode))
                node = new InvertNode();
            else if (nodeType == typeof(NormalizeNode))
                node = new NormalizeNode();
            else if (nodeType == typeof(ErosionNode))
                node = new ErosionNode();
            else if (nodeType == typeof(BeachNode))
                node = new BeachNode();
            else if (nodeType == typeof(SedimentNode))
                node = new SedimentNode();
            else if (nodeType == typeof(ClampNode))
                node = new ClampNode();
            else if (nodeType == typeof(HeightSelectorNode))
                node = new HeightSelectorNode();
            else if (nodeType == typeof(CacheNode))
                node = new CacheNode();
            else if (nodeType == typeof(SelectNode))
                node = new SelectNode();
            else if (nodeType == typeof(CurveNode))
                node = new CurveNode();
            else if (nodeType == typeof(SlopeNode))
                node = new SlopeNode();
            else if (nodeType == typeof(PortalInNode))
                node = new PortalInNode();
            else if (nodeType == typeof(PortalOutNode))
                node = new PortalOutNode();
            else if (nodeType == typeof(NoiseOutputNode))
                node = new NoiseOutputNode();
            else if (nodeType == typeof(SplatOutputNode))
                node = new SplatOutputNode();
            
            if (node != null)
            {
                node.SetPosition(new Rect(graphLocalPos, Vector2.zero));
                node.InitializePreview(graphView);
                graphView.AddElement(node);
                return true;
            }
            
            return false;
        }
    }
}

