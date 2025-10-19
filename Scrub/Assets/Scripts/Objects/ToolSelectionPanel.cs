// ToolSelectionPanel.cs
using UnityEngine;
using UnityEngine.UI;

public class ToolSelectionPanel : MonoBehaviour
{
    [Header("Tool Prefabs")]
    public GameObject broomPrefab;
    public GameObject spongePrefab;
    public GameObject vacuumPrefab;
    public GameObject dusterPrefab;

    [Header("UI Buttons")]
    public Button broomButton;
    public Button spongeButton;
    public Button vacuumButton;
    public Button dusterButton;

    private ToolHandler toolHandler;

    // Esta firma es la que espera ToolHandler.cs
    public void Initialize(ToolHandler handler)
    {
        toolHandler = handler;
        if (toolHandler != null)
        {
            SetupButtonListeners();
        }
        else
        {
            Debug.LogError("FALLO: ToolHandler es NULL al inicializar el Panel.", this);
        }
    }

    private void SetupButtonListeners()
    {
        // 🟢 DEBUG 1: Confirmar que la función se llama 🟢
        Debug.Log("[TOOL PANEL] Asignando listeners a los botones.");

        // Asignación de Listeners
        broomButton?.onClick.AddListener(() => OnToolSelected(broomPrefab));
        spongeButton?.onClick.AddListener(() => OnToolSelected(spongePrefab));
        vacuumButton?.onClick.AddListener(() => OnToolSelected(vacuumPrefab));
        dusterButton?.onClick.AddListener(() => OnToolSelected(dusterPrefab));

        // 🟢 DEBUG 2: Confirmar que al menos un botón se asignó
        if (broomButton != null || spongeButton != null)
        {
            Debug.Log("[TOOL PANEL] Listeners asignados. Listo para detectar clics.");
        }
        else
        {
            Debug.LogError("[TOOL PANEL] Ningún botón está asignado en el Inspector. ¡Los clics no funcionarán!");
        }
    }

    private void OnToolSelected(GameObject toolPrefab)
    {
        // 🟢 DEBUG 3: Confirmar que el clic fue detectado 🟢
        string toolName = (toolPrefab != null) ? toolPrefab.name : "NULL_PREFAB";
        Debug.Log($"[TOOL PANEL CLICK] Clic detectado. Seleccionando tool: {toolName}");

        if (toolHandler != null && toolPrefab != null)
        {
            toolHandler.SelectAndInstantiateTool(toolPrefab);
        }
        else
        {
            Debug.LogError($"[TOOL PANEL CLICK] Error al instanciar. ToolHandler es NULL: {toolHandler == null}. ToolPrefab es NULL: {toolPrefab == null}.");
        }
    }
}