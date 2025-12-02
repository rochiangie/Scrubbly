using UnityEngine;
using System.Collections.Generic;

// Se elimina la línea #if UNITY_EDITOR / using UnityEditor; / #endif

public class Finder : MonoBehaviour
{
    // Opcional: Patrón Singleton para acceso fácil
    public static Finder Instance;

    [Tooltip("Tags que identifican los objetos que deben ser recogidos/gestionados.")]
    public List<string> targetTags = new List<string> { "Basura", "Vidrio", "Peligroso" };

    [Header("Objetos Encontrados")]
    [Tooltip("Lista de todos los GameObjects activos en la escena que coinciden con los tags objetivo.")]
    public List<GameObject> foundObjects = new List<GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        FindAllTaggedObjects();
    }

    /// <summary>
    /// Busca y lista todos los GameObjects en la escena que tienen cualquiera de los tags definidos en targetTags.
    /// </summary>
    /// <summary>
    /// Busca y lista todos los GameObjects en la escena que tienen cualquiera de los tags definidos en targetTags.
    /// </summary>
    public void FindAllTaggedObjects()
    {
        foundObjects.Clear();

        Debug.Log("--- 🔎 INICIANDO ESCANEO DE TAGS OBJETIVO ---");

        int totalFound = 0;

        // Itera sobre cada tag definido en la lista targetTags
        foreach (string tag in targetTags)
        {
            // === SOLUCIÓN: Solo verificamos si el tag está vacío. ===
            // Quitamos la línea que usa UnityEditor.UnityEditorInternal
            if (string.IsNullOrEmpty(tag))
            {
                Debug.LogWarning("⚠️ Se encontró un tag vacío en la lista targetTags. Saltando.");
                continue;
            }
            // =======================================================

            // Obtiene todos los objetos activos en la escena con este tag
            GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(tag);

            // ... (el resto del código sigue igual)
            if (objectsWithTag.Length > 0)
            {
                Debug.Log($"✅ Tag '{tag}' encontrado: {objectsWithTag.Length} objetos.");

                foreach (GameObject go in objectsWithTag)
                {
                    foundObjects.Add(go);
                    Debug.Log($"   -> Añadido: {go.name} (Tag: {tag})");
                    totalFound++;
                }
            }
            else
            {
                Debug.Log($"❌ Tag '{tag}' no encontrado en la escena (0 objetos).");
            }
        }

        Debug.Log($"--- 📜 ESCANEO FINALIZADO: {totalFound} objetos objetivo totales encontrados. ---");
    }
}