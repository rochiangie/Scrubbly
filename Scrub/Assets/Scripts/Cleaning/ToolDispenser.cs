using UnityEngine;

public class ToolDispenser : MonoBehaviour
{
    // 🚨 Asigna estos GameObjects a los botones en el Inspector
    [Header("Tool Prefabs")]
    public GameObject cleaningToolPrefab; // El prefab de la primera herramienta
    public GameObject cleanToolPrefab;    // El prefab de la segunda herramienta

    [Header("Configuración de Spawn")]
    [Tooltip("El Transform donde se instanciarán las herramientas (cerca del panel).")]
    public Transform spawnLocation;

    [Tooltip("La fuerza inicial para lanzar el objeto y separarlo del panel.")]
    public float spawnLaunchForce = 1f;

    // --- Funciones Llamadas por Botones de UI ---

    /// <summary>
    /// Spawnea la CleaningTool (Ej. Aspiradora). Llamada por el Botón 1.
    /// </summary>
    public void DispenseCleaningTool()
    {
        if (cleaningToolPrefab == null || spawnLocation == null)
        {
            Debug.LogError("Dispenser: Prefab o SpawnLocation de CleaningTool no asignado.");
            return;
        }
        SpawnTool(cleaningToolPrefab);
    }

    /// <summary>
    /// Spawnea la CleanTool (Ej. Mopa). Llamada por el Botón 2.
    /// </summary>
    public void DispenseCleanTool()
    {
        if (cleanToolPrefab == null || spawnLocation == null)
        {
            Debug.LogError("Dispenser: Prefab o SpawnLocation de CleanTool no asignado.");
            return;
        }
        SpawnTool(cleanToolPrefab);
    }

    private void SpawnTool(GameObject toolPrefab)
    {
        // Instancia la herramienta en la ubicación definida
        GameObject spawnedTool = Instantiate(toolPrefab, spawnLocation.position, spawnLocation.rotation);

        // Aplica una ligera fuerza para separarla
        Rigidbody rb = spawnedTool.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(spawnLocation.forward * spawnLaunchForce, ForceMode.Impulse);
        }

        Debug.Log($"ToolDispenser: Objeto {spawnedTool.name} instanciado y listo para ser recogido.");
    }
}