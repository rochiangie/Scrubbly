using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class TextureAutoAssigner : EditorWindow
{
    // Variables para almacenar las referencias a las carpetas seleccionadas
    private Object materialsFolder;
    private Object texturesFolder;

    // Propiedades de Shader para Albedo (Base Map)
    // _BaseColorMap es más robusto para URP/HDRP que _MainTex
    private static readonly int BaseMapPropID = Shader.PropertyToID("_MainTex");

    [MenuItem("Tools/Material/Asignar Texturas por Nombre")]
    public static void ShowWindow()
    {
        GetWindow<TextureAutoAssigner>("Asignar Texturas");
    }

    private void OnGUI()
    {
        GUILayout.Label("Asignación Automática de Base Map (Albedo)", EditorStyles.boldLabel);

        // 1. Selector para la carpeta de Materiales
        EditorGUILayout.Space();
        materialsFolder = EditorGUILayout.ObjectField("Carpeta de Materiales:", materialsFolder, typeof(Object), false);

        // 2. Selector para la carpeta de Texturas
        EditorGUILayout.Space();
        texturesFolder = EditorGUILayout.ObjectField("Carpeta de Texturas:", texturesFolder, typeof(Object), false);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("Asignar Base Maps"))
        {
            if (materialsFolder == null || texturesFolder == null)
            {
                EditorUtility.DisplayDialog("Error", "Por favor, arrastra las carpetas de Materiales y Texturas.", "OK");
            }
            else
            {
                ProcessAssignment();
            }
        }
    }

    private void ProcessAssignment()
    {
        string materialsPath = AssetDatabase.GetAssetPath(materialsFolder);
        string texturesPath = AssetDatabase.GetAssetPath(texturesFolder);

        if (string.IsNullOrEmpty(materialsPath) || string.IsNullOrEmpty(texturesPath))
        {
            EditorUtility.DisplayDialog("Error", "Las rutas de las carpetas no son válidas.", "OK");
            return;
        }

        // --- Propiedades de Shader (Base Color) ---
        // Buscamos _BaseColorMap (HDRP/URP) primero, luego _MainTex (Standard/Universal)
        int BaseColorPropID = Shader.PropertyToID("_BaseColorMap");
        int MainTexPropID = Shader.PropertyToID("_MainTex");

        // Sufijos a IGNORAR (Normal, AO, Roughness, Metallic, etc.)
        string[] detailSuffixes = { "_N", "_AO", "_R", "_S", "_NM", "_Height", "_Mask", "_Metallic", "_Metal", "_MRAO", "_mre", "_ORM", "_H", "_A" };

        // --- 1. Crear un mapa de nombres de texturas (con prefijo T_ quitado) ---
        string[] allTextureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { texturesPath });

        // Diccionario para almacenar [Nombre de Textura sin prefijo T_] -> Textura
        Dictionary<string, Texture2D> simplifiedTextureMap = new Dictionary<string, Texture2D>();

        foreach (string texGuid in allTextureGuids)
        {
            string texPath = AssetDatabase.GUIDToAssetPath(texGuid);
            string rawTextureName = Path.GetFileNameWithoutExtension(texPath);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            if (texture != null)
            {
                bool isDetailMap = false;

                // Filtra si tiene un sufijo de mapa de detalle
                foreach (string suffix in detailSuffixes)
                {
                    if (rawTextureName.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
                    {
                        isDetailMap = true;
                        break;
                    }
                }

                if (!isDetailMap)
                {
                    // LIMPIAMOS EL PREFIJO T_ y lo usamos como clave
                    string key = rawTextureName.StartsWith("T_", System.StringComparison.OrdinalIgnoreCase)
                                        ? rawTextureName.Substring(2)
                                        : rawTextureName;

                    if (!simplifiedTextureMap.ContainsKey(key))
                    {
                        simplifiedTextureMap[key] = texture;
                    }
                }
            }
        }

        // --- 2. Asignar las texturas a los Materiales ---
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { materialsPath });
        int assignedCount = 0;

        Undo.RecordObject(this, "Batch Texture Assignment");

        foreach (string matGuid in materialGuids)
        {
            string matPath = AssetDatabase.GUIDToAssetPath(matGuid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            string materialName = Path.GetFileNameWithoutExtension(matPath); // Ej: "M_Backsplash"

            // LIMPIAMOS EL PREFIJO M_ y lo usamos como clave de búsqueda
            string searchKey = materialName.StartsWith("M_", System.StringComparison.OrdinalIgnoreCase)
                                            ? materialName.Substring(2)
                                            : materialName;

            Texture2D foundTexture = null;

            // Iteramos sobre las texturas de Albedo simplificadas para buscar la coincidencia.
            foreach (var entry in simplifiedTextureMap)
            {
                string textureKey = entry.Key; // Ej: Backsplash_D

                // CRITERIO FINAL DE COINCIDENCIA:
                // La textura debe contener el nombre base del material, o el nombre del material debe contener la textura.
                // Esto maneja M_Glass01 vs T_Glass
                if (textureKey.Contains(searchKey) || searchKey.Contains(textureKey))
                {
                    foundTexture = entry.Value;
                    break;
                }
            }

            // --- Asignación ---
            if (foundTexture != null)
            {
                int finalPropID = -1;

                // Intenta asignar a _BaseColorMap (HDRP/URP)
                if (material.HasProperty(BaseColorPropID))
                {
                    finalPropID = BaseColorPropID;
                }
                // Si falla, intenta asignar a _MainTex (Standard/Universal)
                else if (material.HasProperty(MainTexPropID))
                {
                    finalPropID = MainTexPropID;
                }

                if (finalPropID != -1)
                {
                    Undo.RecordObject(material, $"Assign Base Map to {material.name}");
                    material.SetTexture(finalPropID, foundTexture);
                    assignedCount++;
                    Debug.Log($"[AutoAssigner] ÉXITO: Asignado {foundTexture.name} a {materialName} usando PropID: {finalPropID}");
                }
                else
                {
                    Debug.LogWarning($"[AutoAssigner] Fallo en la asignación: El material {materialName} no tiene la propiedad _BaseColorMap ni _MainTex.");
                }
            }
            else
            {
                Debug.LogWarning($"[AutoAssigner] No se encontró Albedo para el material: {materialName} (Base: {searchKey}).");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Éxito", $"Asignación completa. Se encontraron y asignaron {assignedCount} texturas.", "OK");
    }
}