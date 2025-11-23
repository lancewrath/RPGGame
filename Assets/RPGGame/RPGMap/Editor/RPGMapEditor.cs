using UnityEngine;
using UnityEditor;

namespace RPGGame.Map.Editor
{
    [CustomEditor(typeof(RPGMap))]
    public class RPGMapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            RPGMap map = (RPGMap)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Heightmap Graph Editor", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Open Graph Editor"))
            {
                RPGMapGeneratorWindow.OpenWindowForMap(map.MapName, map.HeightmapGraphName);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Generate Terrain"))
            {
                map.GenerateTerrain();
            }
            
            GUI.enabled = map.TerrainTileCount > 0;
            if (GUILayout.Button("Regenerate Terrain"))
            {
                map.RegenerateTerrain();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox(
                "Use the Graph Editor to create and edit heightmap node graphs. " +
                "Graphs are saved to StreamingAssets/Worlds/{MapName}/{GraphName}.json",
                MessageType.Info);
        }
    }
}

