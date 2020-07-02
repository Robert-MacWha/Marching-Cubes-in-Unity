using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainManager))]
public class TerrainManager_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        // Insert all default items
        base.OnInspectorGUI();

        if (GUILayout.Button("Generate Terrain"))
        {
            ((TerrainManager)target).DeleteChildren();
            ((TerrainManager)target).GenerateScene();
        }

        /*  
            Currently Under Construction
         
        if (GUILayout.Button("Generate Terrain With Erosion"))
        {
            ((TerrainManager)target).DeleteChildren();
            ((TerrainManager)target).GenerateSceneWithErosion();
        }

            Currently Under Construction
        */
    }
}
