// ToolHandler.cs
using UnityEngine;
using UnityEngine.UI;

public class ToolHandler : MonoBehaviour
{
    [Header("Tool Configuration")]
    [Tooltip("La posición (Transform) donde se instanciará la herramienta (ej. en la mano del jugador).")]
    public Transform toolSlot;

    // Si tienes un manager de sliders, asígnalo aquí para cerrarlo cuando se abra el panel de herramientas.
    [Header("Interoperabilidad UI")]
    [Tooltip("Referencia al Manager que controla el panel de Sliders (opcional).")]
    public MonoBehaviour slidersManager;
    [Tooltip("Referencia al componente de control de cámara/mouse del jugador (Ej: MouseLook, FirstPersonController).")]
    public MonoBehaviour mouseLook;

    // Referencia que se asignará en Awake() buscando por Tag
    private GameObject selectionPanelUI;
    private ToolSelectionPanel toolSelectionPanelScript;

    // Variables de estado
    private GameObject currentToolObject;
    private bool isToolsPanelOpen = false;

    // 🚨 CONSTANTE DE TAG: ASEGÚRATE DE USAR ESTE TAG EN EL OBJETO UI DE TU ESCENA
    private const string ToolPanelTag = "ToolPanelUI";

    void Awake()
    {
        // 1. BUSCAR EL PANEL EN LA ESCENA POR TAG (SOLUCIÓN AL PROBLEMA DE PREFABS)
        GameObject panelObj = GameObject.FindGameObjectWithTag(ToolPanelTag);

        if (panelObj != null)
        {
            selectionPanelUI = panelObj;

            // 2. Continúa la lógica de inicialización
            selectionPanelUI.SetActive(false);
            toolSelectionPanelScript = selectionPanelUI.GetComponent<ToolSelectionPanel>();
        }
        else
        {
            Debug.LogError($"FATAL ERROR: El Panel de Herramientas no se encontró en la escena. Asegúrate de que tiene el tag '{ToolPanelTag}'.", this);
        }
    }

    void Start()
    {
        // Inicialización cruzada del Panel UI
        if (toolSelectionPanelScript != null)
        {
            toolSelectionPanelScript.Initialize(this);
        }
        else if (selectionPanelUI != null)
        {
            Debug.LogError("ToolSelectionPanel Script no encontrado en el GameObject del Panel. ¿Está adjunto?", selectionPanelUI);
        }
    }

    void Update()
    {
        // Lógica de uso y destrucción de la herramienta
        if (currentToolObject != null)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                CleanTool cleanScript = currentToolObject.GetComponent<CleanTool>();
                cleanScript?.Clean();
            }

            if (Input.GetKeyUp(KeyCode.F))
            {
                DestroyCurrentTool();
            }
        }
    }

    /// <summary>
    /// Alterna la visibilidad del panel de herramientas sin pausar el tiempo, 
    /// y gestiona el control de mouse/cámara.
    /// </summary>
    public void ToggleToolSelectionPanel()
    {
        if (selectionPanelUI != null)
        {
            isToolsPanelOpen = !isToolsPanelOpen;

            if (isToolsPanelOpen)
            {
                // -- LÓGICA DE ABRIR PANEL --

                // 1. Cerrar otros paneles (ej. Sliders)
                if (slidersManager != null && slidersManager.gameObject.activeSelf)
                {
                    slidersManager.gameObject.SetActive(false);
                }

                // 2. Activa el panel de herramientas
                selectionPanelUI.SetActive(true);

                // 3. Desactiva el control de mouse/cámara
                SetMouseControlsActive(false);

                // ❌ OMITIMOS Time.timeScale = 0f; ❌
            }
            else
            {
                // -- LÓGICA DE CERRAR PANEL --

                // 1. Desactiva el panel de herramientas
                selectionPanelUI.SetActive(false);

                // 2. Reactiva el control de mouse/cámara
                SetMouseControlsActive(true);

                // ❌ OMITIMOS Time.timeScale = 1f; ❌
            }

            Debug.Log($"[TOOL HANDLER] Panel de Herramientas {(isToolsPanelOpen ? "ABIERTO" : "CERRADO")}.");
        }
    }

    // Función auxiliar para llamar a SetControlsActive y gestionar el cursor.
    private void SetMouseControlsActive(bool isActive)
    {
        if (mouseLook != null)
        {
            // Usamos SendMessage para llamar a un método que no podemos referenciar directamente.
            // Requiere que tu script de control de mouse tenga un método público: 
            // 'public void SetControlsActive(bool state)'
            mouseLook.SendMessage("SetControlsActive", isActive, SendMessageOptions.DontRequireReceiver);

            // Gestión del Cursor
            Cursor.lockState = isActive ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isActive;
        }
    }

    // Función pública llamada por ToolSelectionPanel para instanciar la herramienta
    public void SelectAndInstantiateTool(GameObject toolPrefab)
    {
        DestroyCurrentTool();

        if (toolSlot != null && toolPrefab != null)
        {
            currentToolObject = Instantiate(toolPrefab, toolSlot.position, toolSlot.rotation, toolSlot);
        }

        // Cerrar el panel después de la selección
        ToggleToolSelectionPanel();
    }

    private void DestroyCurrentTool()
    {
        if (currentToolObject != null)
        {
            Destroy(currentToolObject);
            currentToolObject = null;
        }
    }
}