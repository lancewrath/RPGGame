using UnityEngine;
using LibNoise;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RPGGame.Map
{
    public class RPGMap : MonoBehaviour
    {
        [Header("Map Settings")]
        [SerializeField] private string mapName = "DefaultMap";
        [SerializeField] private int tileRadius = 2; // Number of tiles from center in each direction
        [SerializeField] private Vector3 tileSize = new Vector3(100f, 20f, 100f); // Width, Height, Length
        
        [Header("Heightmap Settings")]
        [SerializeField] private string heightmapGraphName = "default_heightmap";
        [SerializeField] private int heightmapResolution = 256;
        
        [Header("Terrain Settings")]
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private PhysicsMaterial terrainPhysicMaterial;
        
        [Header("Performance Settings")]
        [SerializeField] private bool useMultithreading = true;
        
        private Dictionary<Vector2Int, GameObject> terrainTiles = new Dictionary<Vector2Int, GameObject>();
        private ModuleBase heightmapModule;
        private Transform tilesParent;
        private List<SplatOutputData> splatOutputs = new List<SplatOutputData>();
        
        private void Start()
        {
            GenerateTerrain();
        }
        
        public void GenerateTerrain()
        {
            // Clear existing tiles
            ClearTerrain();
            
            // Load heightmap graph
            LoadHeightmapGraph();
            
            // Populate cache nodes before generating terrain
            // This caches expensive operations (like erosion) so they don't need to be recalculated for splat maps
            PopulateCacheNodes();
            
            // Create parent for tiles
            if (tilesParent == null)
            {
                tilesParent = new GameObject("TerrainTiles").transform;
                tilesParent.SetParent(transform);
            }
            
            // Generate tiles in a square pattern around center
            for (int x = -tileRadius; x <= tileRadius; x++)
            {
                for (int z = -tileRadius; z <= tileRadius; z++)
                {
                    CreateTerrainTile(x, z);
                }
            }
            
            // Weld edges to remove seams
            WeldTerrainEdges();
        }
        
        public void RegenerateTerrain()
        {
            // If no terrain exists, generate it from scratch
            if (terrainTiles.Count == 0)
            {
                GenerateTerrain();
                return;
            }
            
            // Reload heightmap graph (in case it changed)
            LoadHeightmapGraph();
            
            // Populate cache nodes before regenerating terrain
            PopulateCacheNodes();
            
            // Regenerate heights for existing terrain tiles
            int heightmapSize = heightmapResolution + 1;
            
            foreach (var kvp in terrainTiles)
            {
                Vector2Int tileCoord = kvp.Key;
                GameObject tileObj = kvp.Value;
                
                if (tileObj == null) continue;
                
                Terrain terrain = tileObj.GetComponentInChildren<Terrain>();
                if (terrain == null) continue;
                
                // Generate new heightmap
                float[,] heights = GenerateHeightmap(tileCoord.x, tileCoord.y, heightmapSize);
                
                // Apply new heights
                terrain.terrainData.SetHeights(0, 0, heights);
            }
            
            // Re-weld edges to remove seams
            WeldTerrainEdges();
        }
        
        private void WeldTerrainEdges()
        {
            // Unity terrain heightmaps are resolution+1 in size
            int heightmapSize = heightmapResolution + 1;
            
            // First pass: collect all heights
            Dictionary<Vector2Int, float[,]> heightCache = new Dictionary<Vector2Int, float[,]>();
            foreach (var kvp in terrainTiles)
            {
                Terrain terrain = kvp.Value.GetComponentInChildren<Terrain>();
                if (terrain != null)
                {
                    heightCache[kvp.Key] = terrain.terrainData.GetHeights(0, 0, heightmapSize, heightmapSize);
                }
            }
            
            // Second pass: weld edges by averaging
            foreach (var kvp in heightCache)
            {
                Vector2Int coord = kvp.Key;
                float[,] heights = kvp.Value;
                
                // Right edge (positive X) - weld with right neighbor
                // The right edge is at index heightmapSize-1, left edge of neighbor is at 0
                if (heightCache.TryGetValue(new Vector2Int(coord.x + 1, coord.y), out float[,] rightHeights))
                {
                    for (int z = 0; z < heightmapSize; z++)
                    {
                        float avgHeight = (heights[z, heightmapSize - 1] + rightHeights[z, 0]) * 0.5f;
                        heights[z, heightmapSize - 1] = avgHeight;
                        rightHeights[z, 0] = avgHeight;
                    }
                }
                
                // Top edge (positive Z) - weld with top neighbor
                // The top edge is at index heightmapSize-1, bottom edge of neighbor is at 0
                if (heightCache.TryGetValue(new Vector2Int(coord.x, coord.y + 1), out float[,] topHeights))
                {
                    for (int x = 0; x < heightmapSize; x++)
                    {
                        float avgHeight = (heights[heightmapSize - 1, x] + topHeights[0, x]) * 0.5f;
                        heights[heightmapSize - 1, x] = avgHeight;
                        topHeights[0, x] = avgHeight;
                    }
                }
                
                // Top-right corner - weld with diagonal neighbor
                if (heightCache.TryGetValue(new Vector2Int(coord.x + 1, coord.y + 1), out float[,] diagHeights))
                {
                    float avgHeight = (heights[heightmapSize - 1, heightmapSize - 1] + 
                                      diagHeights[0, 0]) * 0.5f;
                    heights[heightmapSize - 1, heightmapSize - 1] = avgHeight;
                    diagHeights[0, 0] = avgHeight;
                }
            }
            
            // Third pass: apply all updated heights
            foreach (var kvp in heightCache)
            {
                if (terrainTiles.TryGetValue(kvp.Key, out GameObject tileObj))
                {
                    Terrain terrain = tileObj.GetComponentInChildren<Terrain>();
                    if (terrain != null)
                    {
                        terrain.terrainData.SetHeights(0, 0, kvp.Value);
                    }
                }
            }
        }
        
        private void CreateTerrainTile(int tileX, int tileZ)
        {
            Vector2Int tileCoord = new Vector2Int(tileX, tileZ);
            
            // Calculate world position
            Vector3 position = new Vector3(
                tileX * tileSize.x,
                0f,
                tileZ * tileSize.z
            );
            
            // Create terrain GameObject
            GameObject tileObj = new GameObject($"TerrainTile_{tileX}_{tileZ}");
            tileObj.transform.SetParent(tilesParent);
            tileObj.transform.position = position;
            
            // Create TerrainData
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = heightmapResolution;
            terrainData.size = tileSize;
            
            // Generate heightmap (Unity requires heightmap to be resolution+1 in each dimension)
            int heightmapSize = heightmapResolution + 1;
            float[,] heights = GenerateHeightmap(tileX, tileZ, heightmapSize);
            terrainData.SetHeights(0, 0, heights);
            
            // Create Terrain
            Terrain terrain = Terrain.CreateTerrainGameObject(terrainData).GetComponent<Terrain>();
            terrain.transform.SetParent(tileObj.transform);
            terrain.transform.localPosition = Vector3.zero;
            
            if (terrainMaterial != null)
            {
                terrain.materialTemplate = terrainMaterial;
            }
            
            // Apply physics material to terrain collider if it exists
            if (terrainPhysicMaterial != null)
            {
                TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
                if (terrainCollider != null)
                {
                    terrainCollider.material = terrainPhysicMaterial;
                }
            }
            
            terrainTiles[tileCoord] = tileObj;
            
            // Apply splat maps if any exist
            if (splatOutputs != null && splatOutputs.Count > 0)
            {
                ApplySplatMaps(terrain, tileX, tileZ);
            }
        }
        
        private void ApplySplatMaps(Terrain terrain, int tileX, int tileZ)
        {
            if (splatOutputs == null || splatOutputs.Count == 0)
                return;
            
            TerrainData terrainData = terrain.terrainData;
            int alphamapResolution = terrainData.alphamapResolution;
            
            // Create terrain layers from splat outputs
            List<TerrainLayer> terrainLayers = new List<TerrainLayer>();
            foreach (var splatData in splatOutputs)
            {
                TerrainLayer layer = new TerrainLayer();
                
                // Load diffuse texture
                if (!string.IsNullOrEmpty(splatData.diffuseTexturePath))
                {
                    string diffusePath = System.IO.Path.Combine(Application.streamingAssetsPath, splatData.diffuseTexturePath);
                    if (System.IO.File.Exists(diffusePath))
                    {
                        byte[] data = System.IO.File.ReadAllBytes(diffusePath);
                        Texture2D diffuseTex = new Texture2D(2, 2);
                        diffuseTex.LoadImage(data);
                        layer.diffuseTexture = diffuseTex;
                    }
                }
                
                // Load normal map
                if (!string.IsNullOrEmpty(splatData.normalMapPath))
                {
                    string normalPath = System.IO.Path.Combine(Application.streamingAssetsPath, splatData.normalMapPath);
                    if (System.IO.File.Exists(normalPath))
                    {
                        byte[] data = System.IO.File.ReadAllBytes(normalPath);
                        Texture2D normalTex = new Texture2D(2, 2);
                        normalTex.LoadImage(data);
                        layer.normalMapTexture = normalTex;
                    }
                }
                
                // Set tiling settings
                layer.tileSize = splatData.tileSize;
                layer.tileOffset = splatData.tileOffset;
                
                // Generate mask map texture
                // Mask map format: R=Metallic, G=Occlusion, B=Height, A=Smoothness
                Texture2D maskMap = GenerateMaskMap(splatData.metallic, splatData.occlusion, splatData.height, splatData.smoothness);
                layer.maskMapTexture = maskMap;
                
                terrainLayers.Add(layer);
            }
            
            // Set terrain layers
            terrainData.terrainLayers = terrainLayers.ToArray();
            
            // Generate splat maps from noise modules
            float[,,] alphamaps;
            
            if (useMultithreading)
            {
                alphamaps = TerrainGenerationParallel.GenerateSplatMapsParallel(
                    splatOutputs,
                    tileX,
                    tileZ,
                    alphamapResolution,
                    tileSize
                );
            }
            else
            {
                // Fallback to sequential generation
                alphamaps = new float[alphamapResolution, alphamapResolution, splatOutputs.Count];
                
                double offsetX = tileX * tileSize.x;
                double offsetZ = tileZ * tileSize.z;
                
                for (int x = 0; x < alphamapResolution; x++)
                {
                    for (int z = 0; z < alphamapResolution; z++)
                    {
                        // Convert to world coordinates
                        double normalizedX = (double)x / (alphamapResolution - 1);
                        double normalizedZ = (double)z / (alphamapResolution - 1);
                        
                        double worldX = offsetX + normalizedX * tileSize.x;
                        double worldZ = offsetZ + normalizedZ * tileSize.z;
                        
                        // Priority-based blending: later layers (higher orderId) take priority over earlier layers
                        // Process layers in order, using alpha compositing where later layers can overwrite earlier ones
                        float[] layerAlphas = new float[splatOutputs.Count];
                        float remainingAlpha = 1.0f; // Track how much alpha is still available
                        
                        for (int i = 0; i < splatOutputs.Count; i++)
                        {
                            float weight = 0f;
                            
                            if (splatOutputs[i].noiseModule != null)
                            {
                                double noiseValue = splatOutputs[i].noiseModule.GetValue(worldX, 0, worldZ);
                                // Remap from [-1,1] to [0,1] using linear interpolation
                                float normalizedWeight = (float)((noiseValue + 1.0) * 0.5);
                                normalizedWeight = Mathf.Clamp01(normalizedWeight);
                                
                                // This layer's alpha is its weight, but limited by remaining alpha
                                // Later layers with strong values will take priority
                                weight = normalizedWeight * remainingAlpha;
                            }
                            
                            layerAlphas[i] = weight;
                            
                            // Reduce remaining alpha for next layers (alpha compositing)
                            // Later layers can only contribute what's left after earlier layers
                            remainingAlpha = Mathf.Max(0f, remainingAlpha - weight);
                        }
                        
                        // Normalize to ensure they sum to 1 (Unity terrain requirement)
                        float totalAlpha = 0f;
                        for (int i = 0; i < splatOutputs.Count; i++)
                        {
                            totalAlpha += layerAlphas[i];
                        }
                        
                        if (totalAlpha > 0.0001f)
                        {
                            for (int i = 0; i < splatOutputs.Count; i++)
                            {
                                alphamaps[z, x, i] = layerAlphas[i] / totalAlpha;
                            }
                        }
                        else
                        {
                            // If no weights, distribute evenly
                            float evenWeight = 1f / splatOutputs.Count;
                            for (int i = 0; i < splatOutputs.Count; i++)
                            {
                                alphamaps[z, x, i] = evenWeight;
                            }
                        }
                    }
                }
            }
            
            // Apply alphamaps to terrain
            terrainData.SetAlphamaps(0, 0, alphamaps);
        }
        
        private Texture2D GenerateMaskMap(float metallic, float occlusion, float height, float smoothness)
        {
            // Create a 1x1 texture with the mask map values
            // Format: R=Metallic, G=Occlusion, B=Height, A=Smoothness
            Texture2D maskMap = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            Color maskColor = new Color(
                Mathf.Clamp01(metallic),      // R channel
                Mathf.Clamp01(occlusion),     // G channel
                Mathf.Clamp01(height),        // B channel
                Mathf.Clamp01(smoothness)     // A channel
            );
            maskMap.SetPixel(0, 0, maskColor);
            maskMap.Apply();
            return maskMap;
        }
        
        private float[,] GenerateHeightmap(int tileX, int tileZ, int heightmapSize)
        {
            if (heightmapModule == null)
            {
                Debug.LogWarning("Heightmap module is null. Using flat terrain.");
                return new float[heightmapSize, heightmapSize];
            }
            
            // Use parallel generation if enabled
            if (useMultithreading)
            {
                return TerrainGenerationParallel.GenerateHeightmapParallel(
                    heightmapModule,
                    tileX,
                    tileZ,
                    heightmapSize,
                    heightmapResolution,
                    tileSize
                );
            }
            
            // Fallback to sequential generation
            float[,] heights = new float[heightmapSize, heightmapSize];
            
            // Calculate the world position offset for this tile
            double offsetX = tileX * tileSize.x;
            double offsetZ = tileZ * tileSize.z;
            
            // Generate height values
            // heightmapSize is resolution + 1, so we sample from 0 to resolution (inclusive)
            for (int x = 0; x < heightmapSize; x++)
            {
                for (int z = 0; z < heightmapSize; z++)
                {
                    // Normalize coordinates to 0-1 range based on resolution (not heightmapSize)
                    // This ensures the last pixel samples at the exact edge of the tile
                    double normalizedX = heightmapResolution > 0 ? (double)x / heightmapResolution : 0;
                    double normalizedZ = heightmapResolution > 0 ? (double)z / heightmapResolution : 0;
                    
                    // Convert to world coordinates
                    double worldX = offsetX + normalizedX * tileSize.x;
                    double worldZ = offsetZ + normalizedZ * tileSize.z;
                    
                    // Get noise value (LibNoise returns -1 to 1)
                    double noiseValue = heightmapModule.GetValue(worldX, 0, worldZ);
                    
                    // Linear lerp from [-1,1] to [0,1]: output = (input - min) / (max - min)
                    // Formula: (noiseValue - (-1)) / (1 - (-1)) = (noiseValue + 1) / 2 = (noiseValue + 1.0) * 0.5
                    // This ensures: -1 → 0, 0 → 0.5, 1 → 1.0 (linear mapping, no clamping)
                    float height = (float)((noiseValue + 1.0) * 0.5);
                    
                    heights[z, x] = height; // Note: TerrainData uses [z, x] indexing
                }
            }
            
            return heights;
        }
        
        private void LoadHeightmapGraph()
        {
            string graphPath = GetHeightmapGraphPath();
            
            if (!File.Exists(graphPath))
            {
                Debug.LogWarning($"Heightmap graph not found at {graphPath}. Using default noise.");
                CreateDefaultHeightmap();
                splatOutputs.Clear();
                return;
            }
            
            try
            {
                string json = File.ReadAllText(graphPath);
                NoiseGraphData graphData = JsonUtility.FromJson<NoiseGraphData>(json);
                
                // Extract splat outputs
                splatOutputs = SplatOutputBuilder.ExtractSplatOutputs(graphData);
                
                // Build splat output modules
                // Use composite keys "nodeGuid:outputPortIndex" to support multiple outputs per node
                Dictionary<string, ModuleBase> builtModules = new Dictionary<string, ModuleBase>();
                
                // Helper function to get module key
                System.Func<string, int, string> GetModuleKey = (nodeGuid, outputPortIndex) => $"{nodeGuid}:{outputPortIndex}";
                
                // First pass: create all modules (skip Output, SplatOutput, and Portal nodes)
                foreach (var nodeData in graphData.nodes)
                {
                    if (nodeData.nodeType == "Output" || nodeData.nodeType == "SplatOutput" || 
                        nodeData.nodeType == "Portal In" || nodeData.nodeType == "Portal Out")
                        continue;
                    
                    // Handle nodes with multiple outputs (same logic as BuildModuleGraph)
                    if (nodeData.nodeType == "Beach")
                    {
                        ModuleBase beachModule = NoiseGraphBuilder.CreateBeach(nodeData);
                        if (beachModule != null)
                        {
                            builtModules[GetModuleKey(nodeData.guid, 0)] = beachModule;
                        }
                        
                        ModuleBase sandModule = NoiseGraphBuilder.CreateBeachSand(nodeData);
                        if (sandModule != null)
                        {
                            builtModules[GetModuleKey(nodeData.guid, 1)] = sandModule;
                        }
                    }
                    else if (nodeData.nodeType == "Sediment")
                    {
                        ModuleBase cliffModule = NoiseGraphBuilder.CreateSedimentCliff(nodeData);
                        if (cliffModule != null)
                        {
                            builtModules[GetModuleKey(nodeData.guid, 0)] = cliffModule;
                        }
                        
                        ModuleBase sedimentModule = NoiseGraphBuilder.CreateSedimentSediment(nodeData);
                        if (sedimentModule != null)
                        {
                            builtModules[GetModuleKey(nodeData.guid, 1)] = sedimentModule;
                        }
                    }
                    else
                    {
                        // Single output nodes (default to port 0)
                        ModuleBase module = NoiseGraphBuilder.CreateModule(nodeData);
                        if (module != null)
                        {
                            builtModules[GetModuleKey(nodeData.guid, 0)] = module;
                        }
                    }
                }
                
                // Second pass: connect modules (skip edges to Output, SplatOutput, and Portal In nodes)
                foreach (var edge in graphData.edges)
                {
                    var inputNode = graphData.nodes.FirstOrDefault(n => n.guid == edge.inputNodeGuid);
                    if (inputNode != null && (inputNode.nodeType == "Output" || inputNode.nodeType == "SplatOutput" || inputNode.nodeType == "Portal In"))
                        continue;
                    
                    // Skip edges FROM Portal Out nodes - they'll be handled by BuildModuleGraph
                    var outputNode = graphData.nodes.FirstOrDefault(n => n.guid == edge.outputNodeGuid);
                    if (outputNode != null && outputNode.nodeType == "Portal Out")
                        continue;
                    
                    // Get modules using composite keys
                    string outputModuleKey = GetModuleKey(edge.outputNodeGuid, edge.outputPortIndex);
                    string inputModuleKey = GetModuleKey(edge.inputNodeGuid, 0);
                    
                    if (builtModules.TryGetValue(outputModuleKey, out ModuleBase outputModule) &&
                        builtModules.TryGetValue(inputModuleKey, out ModuleBase inputModule))
                    {
                        if (inputModule.SourceModuleCount > 0 && edge.inputPortIndex >= 0 && edge.inputPortIndex < inputModule.SourceModuleCount)
                        {
                            inputModule[edge.inputPortIndex] = outputModule;
                            
                            // Special handling for nodes with multiple outputs that share inputs (same as BuildModuleGraph)
                            if (inputNode != null && inputNode.nodeType == "Beach" && edge.inputPortIndex == 0)
                            {
                                string sandModuleKey = GetModuleKey(edge.inputNodeGuid, 1);
                                if (builtModules.TryGetValue(sandModuleKey, out ModuleBase sandModule))
                                {
                                    sandModule[0] = outputModule;
                                }
                            }
                            
                            if (inputNode != null && inputNode.nodeType == "Sediment")
                            {
                                if (edge.inputPortIndex == 0) // PreErosion
                                {
                                    string cliffModuleKey = GetModuleKey(edge.inputNodeGuid, 0);
                                    string sedimentModuleKey = GetModuleKey(edge.inputNodeGuid, 1);
                                    if (builtModules.TryGetValue(cliffModuleKey, out ModuleBase cliffModule))
                                    {
                                        cliffModule[0] = outputModule;
                                    }
                                    if (builtModules.TryGetValue(sedimentModuleKey, out ModuleBase sedimentModule))
                                    {
                                        sedimentModule[0] = outputModule;
                                    }
                                }
                                else if (edge.inputPortIndex == 1) // PostErosion
                                {
                                    string cliffModuleKey = GetModuleKey(edge.inputNodeGuid, 0);
                                    string sedimentModuleKey = GetModuleKey(edge.inputNodeGuid, 1);
                                    if (builtModules.TryGetValue(cliffModuleKey, out ModuleBase cliffModule))
                                    {
                                        cliffModule[1] = outputModule;
                                    }
                                    if (builtModules.TryGetValue(sedimentModuleKey, out ModuleBase sedimentModule))
                                    {
                                        sedimentModule[1] = outputModule;
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Build portal modules (similar to BuildModuleGraph logic)
                Dictionary<string, ModuleBase> portalModules = new Dictionary<string, ModuleBase>();
                
                // Handle Portal In nodes - find their input modules and store by portal name
                foreach (var portalInNode in graphData.nodes.Where(n => n.nodeType == "Portal In"))
                {
                    var portalInEdge = graphData.edges.FirstOrDefault(e => e.inputNodeGuid == portalInNode.guid);
                    if (portalInEdge != null)
                    {
                        string portalOutputKey = GetModuleKey(portalInEdge.outputNodeGuid, portalInEdge.outputPortIndex);
                        if (builtModules.TryGetValue(portalOutputKey, out ModuleBase portalInputModule))
                        {
                            string portalName = GetPropertyString(portalInNode, "portalName", "Portal");
                            if (!string.IsNullOrEmpty(portalName))
                            {
                                portalModules[portalName] = portalInputModule;
                            }
                        }
                    }
                }
                
                // Handle Portal Out nodes - create modules that reference portal modules
                foreach (var portalOutNode in graphData.nodes.Where(n => n.nodeType == "Portal Out"))
                {
                    string portalName = GetPropertyString(portalOutNode, "selectedPortalName", "");
                    if (!string.IsNullOrEmpty(portalName) && portalModules.TryGetValue(portalName, out ModuleBase portalModule))
                    {
                        builtModules[GetModuleKey(portalOutNode.guid, 0)] = portalModule;
                    }
                }
                
                // Connect edges FROM Portal Out nodes (now that Portal Out modules are built)
                // Only connect edges FROM Portal Out nodes that were successfully resolved
                foreach (var edge in graphData.edges)
                {
                    var outputNode = graphData.nodes.FirstOrDefault(n => n.guid == edge.outputNodeGuid);
                    if (outputNode != null && outputNode.nodeType == "Portal Out")
                    {
                        // Skip if the Portal Out node wasn't successfully resolved (not in builtModules)
                        if (!builtModules.ContainsKey(edge.outputNodeGuid))
                        {
                            // Portal Out node couldn't be resolved (empty name, missing portal, etc.)
                            // Skip this edge - the input module will get a fallback during validation
                            continue;
                        }
                        
                        var inputNode = graphData.nodes.FirstOrDefault(n => n.guid == edge.inputNodeGuid);
                        if (inputNode != null && (inputNode.nodeType == "Output" || inputNode.nodeType == "SplatOutput"))
                            continue;
                        
                        if (builtModules.TryGetValue(edge.outputNodeGuid, out ModuleBase outputModule) &&
                            builtModules.TryGetValue(edge.inputNodeGuid, out ModuleBase inputModule))
                        {
                            if (inputModule.SourceModuleCount > 0 && edge.inputPortIndex >= 0 && edge.inputPortIndex < inputModule.SourceModuleCount)
                            {
                                inputModule[edge.inputPortIndex] = outputModule;
                            }
                        }
                    }
                }
                
                // Validate Select modules (similar to BuildModuleGraph validation)
                foreach (var kvp in builtModules)
                {
                    var nodeData = graphData.nodes.FirstOrDefault(n => n.guid == kvp.Key);
                    if (nodeData != null && nodeData.nodeType == "Select")
                    {
                        var selectModule = kvp.Value as LibNoise.Operator.Select;
                        if (selectModule != null)
                        {
                            bool needsFallback = false;
                            try
                            {
                                var testA = selectModule[0];
                                var testB = selectModule[1];
                                var testControl = selectModule[2];
                            }
                            catch (System.ArgumentNullException)
                            {
                                needsFallback = true;
                            }
                            catch (System.ArgumentOutOfRangeException)
                            {
                                needsFallback = true;
                            }
                            
                            if (needsFallback)
                            {
                                Debug.LogWarning($"Select module (GUID: {kvp.Key}) is missing one or more input modules! Using default Perlin noise as fallback.");
                                var defaultInput = new LibNoise.Generator.Perlin();
                                defaultInput.Frequency = 1.0;
                                
                                try { var test = selectModule[0]; } catch { selectModule[0] = defaultInput; }
                                try { var test = selectModule[1]; } catch { selectModule[1] = defaultInput; }
                                try { var test = selectModule[2]; } catch { selectModule[2] = defaultInput; }
                            }
                        }
                    }
                }
                
                // Build splat output modules
                foreach (var splatData in splatOutputs)
                {
                    // Use the source node GUID and output port index to get the built module
                    // This could be a Portal Out node, or a node with multiple outputs (like Beach Sand)
                    if (!string.IsNullOrEmpty(splatData.sourceNodeGuid))
                    {
                        string sourceModuleKey = GetModuleKey(splatData.sourceNodeGuid, splatData.sourceOutputPortIndex);
                        if (builtModules.TryGetValue(sourceModuleKey, out ModuleBase sourceModule))
                        {
                            splatData.noiseModule = sourceModule;
                        }
                    }
                }
                
                // Build the heightmap module (for terrain height)
                heightmapModule = NoiseGraphBuilder.BuildModuleGraph(graphData);
                
                if (heightmapModule == null)
                {
                    Debug.LogWarning("Failed to build heightmap module. Using default noise.");
                    CreateDefaultHeightmap();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading heightmap graph: {e.Message}");
                CreateDefaultHeightmap();
                splatOutputs.Clear();
            }
        }
        
        private void PopulateCacheNodes()
        {
            if (heightmapModule == null)
                return;
            
            // Collect all cache modules from the heightmap module and all splat output modules
            HashSet<LibNoise.Operator.PreviewCache> allCacheModules = new HashSet<LibNoise.Operator.PreviewCache>();
            
            // Find cache modules in heightmap module
            var heightmapCaches = NoiseGraphBuilder.FindAllCacheModules(heightmapModule);
            foreach (var cache in heightmapCaches)
            {
                allCacheModules.Add(cache);
            }
            
            // Find cache modules in all splat output modules
            if (splatOutputs != null)
            {
                foreach (var splatData in splatOutputs)
                {
                    if (splatData.noiseModule != null)
                    {
                        var splatCaches = NoiseGraphBuilder.FindAllCacheModules(splatData.noiseModule);
                        foreach (var cache in splatCaches)
                        {
                            allCacheModules.Add(cache);
                        }
                    }
                }
            }
            
            if (allCacheModules.Count == 0)
                return; // No cache nodes to populate
            
            // Calculate terrain bounds
            // Tiles range from -tileRadius to +tileRadius in both X and Z
            double minX = -tileRadius * tileSize.x;
            double minZ = -tileRadius * tileSize.z;
            double maxX = (tileRadius + 1) * tileSize.x; // +1 to include the edge of the last tile
            double maxZ = (tileRadius + 1) * tileSize.z;
            
            // Use a cache resolution that matches or exceeds the heightmap resolution
            // This ensures good quality while still providing performance benefits
            int cacheResolution = heightmapResolution;
            
            // Populate all cache modules
            Debug.Log($"Populating {allCacheModules.Count} cache node(s) for terrain region [{minX:F2}, {minZ:F2}] to [{maxX:F2}, {maxZ:F2}] at resolution {cacheResolution}");
            
            foreach (var cacheModule in allCacheModules)
            {
                cacheModule.PopulateCacheForRegion(minX, minZ, maxX, maxZ, cacheResolution);
            }
            
            Debug.Log("Cache population complete.");
        }
        
        private void CreateDefaultHeightmap()
        {
            // Create a simple default Perlin noise
            var perlin = new LibNoise.Generator.Perlin();
            perlin.Frequency = 0.01;
            perlin.Seed = 0;
            heightmapModule = perlin;
        }
        
        private string GetHeightmapGraphPath()
        {
            string worldFolder = Path.Combine(Application.streamingAssetsPath, "Worlds", mapName);
            string graphFile = $"{heightmapGraphName}.json";
            return Path.Combine(worldFolder, graphFile);
        }
        
        private string GetPropertyString(NoiseNodeData nodeData, string key, string defaultValue)
        {
            var prop = nodeData.properties?.FirstOrDefault(p => p.key == key);
            if (prop != null && !string.IsNullOrEmpty(prop.value))
                return prop.value;
            return defaultValue;
        }
        
        private void ClearTerrain()
        {
            foreach (var tile in terrainTiles.Values)
            {
                if (tile != null)
                {
                    DestroyImmediate(tile);
                }
            }
            terrainTiles.Clear();
            
            if (tilesParent != null)
            {
                DestroyImmediate(tilesParent.gameObject);
                tilesParent = null;
            }
        }
        
        private void OnDestroy()
        {
            if (heightmapModule != null)
            {
                heightmapModule.Dispose();
                heightmapModule = null;
            }
        }
        
        // Public getters for editor
        public string MapName => mapName;
        public string HeightmapGraphName => heightmapGraphName;
        public int TerrainTileCount => terrainTiles.Count;
    }
}
