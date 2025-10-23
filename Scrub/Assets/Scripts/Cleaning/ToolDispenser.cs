using UnityEngine;

// 🚨 Asegúrate de que este script esté adjunto a un GameObject en la escena.
public class ToolDispenser : MonoBehaviour
{
    [Header("Referencias de UI/Managers")]
    [Tooltip("Asigna el controlador del panel UI aquí.")]
    public UIPauseController pauseController;

    [Header("Tool Prefabs")]
    [Tooltip("El prefab de la Escoba (debe tener ToolDescriptor y Rigidbody).")]
    public GameObject escobaPrefab;

    [Tooltip("El prefab de la segunda herramienta (ej. Aspiradora o Mopa).")]
    public GameObject segundaToolPrefab;

    [Header("Configuración de Spawn")]
    [Tooltip("El Transform donde se instanciarán las herramientas (cerca del panel).")]
    public Transform spawnLocation;

    [Tooltip("La fuerza inicial para lanzar el objeto y separarlo del panel.")]
    public float spawnLaunchForce = 3.0f;

    void Start()
    {
        if (spawnLocation == null)
        {
            Debug.LogError($"ToolDispenser: ¡SpawnLocation es nulo en {gameObject.name}! Asigna un punto cerca del dispensador.");
        }
    }


    // --- Funciones Llamadas por Botones de UI ---

    /// <summary>
    /// Spawnea la Escoba. Asigna esto al evento OnClick() del botón de Escoba.
    /// </summary>
    public void DispenseEscoba()
    {
        if (escobaPrefab == null)
        {
            Debug.LogError("Dispenser: Escoba Prefab no asignado.");
            return;
        }
        SpawnTool(escobaPrefab);
        ClosePanel(); // 🚨 Cierre de panel garantizado
    }

    /// <summary>
    /// Spawnea la Segunda Herramienta. Asigna esto al evento OnClick() del otro botón.
    /// </summary>
    public void DispenseSegundaTool()
    {
        if (segundaToolPrefab == null)
        {
            Debug.LogError("Dispenser: Segunda Tool Prefab no asignada.");
            return;
        }
        SpawnTool(segundaToolPrefab);
        ClosePanel(); // 🚨 Cierre de panel garantizado
    }

    // --- Lógica de Spawn ---

    private void SpawnTool(GameObject toolPrefab)
    {
        if (spawnLocation == null)
        {
            Debug.LogError("No se puede spawnear: SpawnLocation es nulo.");
            return;
        }

        // Instancia la herramienta en la ubicación definida
        GameObject spawnedTool = Instantiate(toolPrefab, spawnLocation.position, spawnLocation.rotation);

        // Aplica una ligera fuerza para separarla del panel
        Rigidbody rb = spawnedTool.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Lanzamos la herramienta hacia adelante 
            rb.AddForce(spawnLocation.forward * spawnLaunchForce, ForceMode.Impulse);
        }
        else
        {
            Debug.LogWarning($"ToolDispenser: El prefab {toolPrefab.name} no tiene Rigidbody. No se aplicará fuerza de lanzamiento.");
        }

        Debug.Log($"ToolDispenser: Objeto {spawnedTool.name} instanciado y listo para ser recogido.");
    }

    // --- Lógica de Cierre ---

    /// <summary>
    /// Cierra el panel de UI y reanuda el juego.
    /// </summary>
    private void ClosePanel()
    {
        if (pauseController != null)
        {
            // Asumimos que SetIsPaused(false) es el método para cerrar el UI/reanudar el tiempo.
            pauseController.SetIsPaused(false);
            Debug.Log("ToolDispenser: Panel cerrado tras seleccionar herramienta.");
        }
        else
        {
            Debug.LogError("ToolDispenser: pauseController es nulo. No se pudo cerrar el panel. Asígnalo en el Inspector.");
        }
    }
}