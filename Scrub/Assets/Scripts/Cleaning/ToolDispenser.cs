using UnityEngine;

public class ToolDispenser : MonoBehaviour
{
    // Asegúrate de que esta referencia esté asignada en el Inspector
    public UIPauseController pauseController;

    [Header("Tool Prefabs")]
    public GameObject cleaningToolPrefab;
    public GameObject cleanToolPrefab;

    [Header("Configuración de Spawn")]
    public Transform spawnLocation;
    public float spawnLaunchForce = 3.0f;


    // --- Funciones Llamadas por Botones de UI ---

    public void DispenseCleaningTool()
    {
        if (cleaningToolPrefab == null || spawnLocation == null) return;
        SpawnTool(cleaningToolPrefab);
        ClosePanel(); // 🚨 Cierre de panel aquí
    }

    public void DispenseCleanTool()
    {
        if (cleanToolPrefab == null || spawnLocation == null) return;
        SpawnTool(cleanToolPrefab);
        ClosePanel(); // 🚨 Cierre de panel aquí
    }

    private void SpawnTool(GameObject toolPrefab)
    {
        GameObject spawnedTool = Instantiate(toolPrefab, spawnLocation.position, spawnLocation.rotation);

        Rigidbody rb = spawnedTool.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(spawnLocation.forward * spawnLaunchForce, ForceMode.Impulse);
        }

        Debug.Log($"ToolDispenser: Objeto {spawnedTool.name} instanciado.");
    }

    private void ClosePanel()
    {
        if (pauseController != null)
        {
            // Asumimos que SetIsPaused(false) es el método para cerrar y reanudar el tiempo.
            pauseController.SetIsPaused(false);
            Debug.Log("ToolDispenser: Panel cerrado tras seleccionar herramienta.");
        }
    }
}