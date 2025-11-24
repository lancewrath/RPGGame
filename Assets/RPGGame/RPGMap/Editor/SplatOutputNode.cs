using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.IO;

namespace RPGGame.Map.Editor
{
    public class SplatOutputNode : NoiseGraphNode
    {
        public int orderId = 0;
        public string diffuseTexturePath = "";
        public string normalMapPath = "";
        
        // Mask map parameters (packed into RGBA: R=Metallic, G=Occlusion, B=Height, A=Smoothness)
        public float metallic = 0f;
        public float occlusion = 1f;
        public float height = 0f;
        public float smoothness = 0.5f;
        
        // Tiling settings
        public Vector2 tileSize = new Vector2(15f, 15f);
        public Vector2 tileOffset = new Vector2(0f, 0f);
        
        // Cached textures (not serialized, loaded from paths)
        private Texture2D _diffuseTexture;
        private Texture2D _normalMap;
        
        // Method to clear cached textures without clearing paths
        public void ClearCachedTextures()
        {
            _diffuseTexture = null;
            _normalMap = null;
        }
        
        public Texture2D DiffuseTexture
        {
            get
            {
                if (_diffuseTexture == null && !string.IsNullOrEmpty(diffuseTexturePath))
                {
                    string fullPath = Path.Combine(Application.streamingAssetsPath, diffuseTexturePath);
                    if (File.Exists(fullPath))
                    {
                        byte[] data = File.ReadAllBytes(fullPath);
                        _diffuseTexture = new Texture2D(2, 2);
                        _diffuseTexture.LoadImage(data);
                    }
                }
                return _diffuseTexture;
            }
        }
        
        public Texture2D NormalMap
        {
            get
            {
                if (_normalMap == null && !string.IsNullOrEmpty(normalMapPath))
                {
                    string fullPath = Path.Combine(Application.streamingAssetsPath, normalMapPath);
                    if (File.Exists(fullPath))
                    {
                        byte[] data = File.ReadAllBytes(fullPath);
                        _normalMap = new Texture2D(2, 2);
                        _normalMap.LoadImage(data);
                    }
                }
                return _normalMap;
            }
        }
        
        public SplatOutputNode() : base("SplatOutput", "Splat Output")
        {
            CreateInputPort("Noise", Port.Capacity.Single);
            
            RefreshExpandedState();
            RefreshPorts();
            
            // Make splat output node visually distinct
            AddToClassList("splat-output-node");
        }
        
        protected override List<NoisePropertyData> GetSerializedProperties()
        {
            return new List<NoisePropertyData>
            {
                new NoisePropertyData { key = "orderId", value = orderId.ToString(), valueType = "int" },
                new NoisePropertyData { key = "diffuseTexturePath", value = diffuseTexturePath, valueType = "string" },
                new NoisePropertyData { key = "normalMapPath", value = normalMapPath, valueType = "string" },
                new NoisePropertyData { key = "metallic", value = metallic.ToString(), valueType = "float" },
                new NoisePropertyData { key = "occlusion", value = occlusion.ToString(), valueType = "float" },
                new NoisePropertyData { key = "height", value = height.ToString(), valueType = "float" },
                new NoisePropertyData { key = "smoothness", value = smoothness.ToString(), valueType = "float" },
                new NoisePropertyData { key = "tileSizeX", value = tileSize.x.ToString(), valueType = "float" },
                new NoisePropertyData { key = "tileSizeY", value = tileSize.y.ToString(), valueType = "float" },
                new NoisePropertyData { key = "tileOffsetX", value = tileOffset.x.ToString(), valueType = "float" },
                new NoisePropertyData { key = "tileOffsetY", value = tileOffset.y.ToString(), valueType = "float" }
            };
        }
        
        protected override void DeserializeProperties(List<NoisePropertyData> properties)
        {
            foreach (var prop in properties)
            {
                switch (prop.key)
                {
                    case "orderId": int.TryParse(prop.value, out orderId); break;
                    case "diffuseTexturePath": diffuseTexturePath = prop.value ?? ""; break;
                    case "normalMapPath": normalMapPath = prop.value ?? ""; break;
                    case "metallic": float.TryParse(prop.value, out metallic); break;
                    case "occlusion": float.TryParse(prop.value, out occlusion); break;
                    case "height": float.TryParse(prop.value, out height); break;
                    case "smoothness": float.TryParse(prop.value, out smoothness); break;
                    case "tileSizeX": 
                        float.TryParse(prop.value, out float sizeX);
                        tileSize = new Vector2(sizeX, tileSize.y);
                        break;
                    case "tileSizeY":
                        float.TryParse(prop.value, out float sizeY);
                        tileSize = new Vector2(tileSize.x, sizeY);
                        break;
                    case "tileOffsetX":
                        float.TryParse(prop.value, out float offsetX);
                        tileOffset = new Vector2(offsetX, tileOffset.y);
                        break;
                    case "tileOffsetY":
                        float.TryParse(prop.value, out float offsetY);
                        tileOffset = new Vector2(tileOffset.x, offsetY);
                        break;
                }
            }
            // Clear cached textures so they reload from paths
            _diffuseTexture = null;
            _normalMap = null;
        }
        
        // Helper method to set texture paths from Unity Texture2D objects
        public void SetDiffuseTexture(Texture2D texture)
        {
            if (texture == null)
            {
                diffuseTexturePath = "";
                _diffuseTexture = null;
                return;
            }
            
            // Get the asset path
            string assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning("Texture is not an asset. Cannot save path.");
                return;
            }
            
            // Convert to path relative to streaming assets
            string streamingAssetsPath = Application.streamingAssetsPath;
            if (assetPath.StartsWith("Assets/"))
            {
                // If texture is in Assets, we need to copy it or reference it differently
                // For now, store the relative path from Assets
                diffuseTexturePath = assetPath.Replace("Assets/", "");
            }
            else
            {
                // Already a relative path
                diffuseTexturePath = assetPath;
            }
            
            _diffuseTexture = texture;
            NotifyNodeChanged();
        }
        
        public void SetNormalMap(Texture2D texture)
        {
            if (texture == null)
            {
                normalMapPath = "";
                _normalMap = null;
                return;
            }
            
            // Get the asset path
            string assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning("Texture is not an asset. Cannot save path.");
                return;
            }
            
            // Convert to path relative to streaming assets
            if (assetPath.StartsWith("Assets/"))
            {
                normalMapPath = assetPath.Replace("Assets/", "");
            }
            else
            {
                normalMapPath = assetPath;
            }
            
            _normalMap = texture;
            NotifyNodeChanged();
        }
    }
}

