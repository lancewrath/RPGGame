using UnityEngine;
using LibNoise;
using System.Collections.Generic;
using System.IO;

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
        
        private Dictionary<Vector2Int, GameObject> terrainTiles = new Dictionary<Vector2Int, GameObject>();
        private ModuleBase heightmapModule;
        private Transform tilesParent;
        
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
        }
        
        private float[,] GenerateHeightmap(int tileX, int tileZ, int heightmapSize)
        {
            float[,] heights = new float[heightmapSize, heightmapSize];
            
            if (heightmapModule == null)
            {
                Debug.LogWarning("Heightmap module is null. Using flat terrain.");
                return heights;
            }
            
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
                    
                    // Get noise value (LibNoise returns -1 to 1, normalize to 0-1)
                    double noiseValue = heightmapModule.GetValue(worldX, 0, worldZ);
                    float height = (float)((noiseValue + 1.0) * 0.5); // Normalize from [-1,1] to [0,1]
                    
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
                return;
            }
            
            try
            {
                string json = File.ReadAllText(graphPath);
                NoiseGraphData graphData = JsonUtility.FromJson<NoiseGraphData>(json);
                
                // Build the module graph from the node graph
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
            }
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
    }
}
