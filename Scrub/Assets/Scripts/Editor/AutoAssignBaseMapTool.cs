using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AutoAssignBaseMapTool : EditorWindow
{
    private DefaultAsset materialFolder;
    private DefaultAsset textureFolder;

    [MenuItem("Tools/Auto Assign Base Map")]
    public static void ShowWindow()
    {
        GetWindow<AutoAssignBaseMapTool>("Auto Assign Base Map");
    }

    void OnGUI()
    {
        GUILayout.Label("Asignador Automático de Texturas", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        materialFolder = (DefaultAsset)EditorGUILayout.ObjectField("Carpeta de Materiales", materialFolder, typeof(DefaultAsset), false);
        textureFolder = (DefaultAsset)EditorGUILayout.ObjectField("Carpeta de Texturas", textureFolder, typeof(DefaultAsset), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Asignar Texturas (Base Map)"))
        {
            if (materialFolder == null || textureFolder == null)
            {
                EditorUtility.DisplayDialog("Error", "Por favor selecciona ambas carpetas.", "OK");
                return;
            }

            AssignTextures();
        }
    }

    void AssignTextures()
    {
        string matPath = AssetDatabase.GetAssetPath(materialFolder);
        string texPath = AssetDatabase.GetAssetPath(textureFolder);

        // Buscar todos los materiales en la carpeta seleccionada
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { matPath });
        // Buscar todas las texturas en la carpeta seleccionada
        string[] texGuids = AssetDatabase.FindAssets("t:Texture", new[] { texPath });

        List<Texture2D> textures = new List<Texture2D>();
        foreach (var guid in texGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            
            // 🛡️ PROTECCIÓN: Solo agregar si la textura se cargó correctamente
            if (tex != null)
            {
                textures.Add(tex);
            }
        }

        int matchCount = 0;

        foreach (var guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            // Buscar una textura que coincida con el nombre del material
            // Criterio: El nombre de la textura contiene el nombre del material
            // OJO: Esto puede necesitar ajustes según tu convención de nombres
            Texture2D matchingTex = textures.Find(t => t.name.Equals(mat.name, System.StringComparison.OrdinalIgnoreCase) || 
                                                       t.name.Contains(mat.name) || 
                                                       mat.name.Contains(t.name));

            if (matchingTex != null)
            {
                Undo.RecordObject(mat, "Assign Base Map");
                
                // Intentar asignar a propiedades comunes
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", matchingTex);
                    matchCount++;
                }
                else if (mat.HasProperty("_MainTex"))
                {
                    mat.SetTexture("_MainTex", matchingTex);
                    matchCount++;
                }
                
                EditorUtility.SetDirty(mat);
                Debug.Log($"[AutoAssign] Asignado '{matchingTex.name}' a Material '{mat.name}'");
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Completado", $"Se asignaron texturas a {matchCount} materiales.", "OK");
    }
}
