using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ToolPanelIdea : MonoBehaviour
{
    // === 1. PANELES Y ESTADO ===
    [Header("1. Paneles de Menú")]
    [Tooltip("El GameObject del panel de Tools que NO pausa el juego (ENTER/TAB).")]
    public GameObject toolMenuPanel;

    [Tooltip("El GameObject del panel de PAUSA que SÍ pausa el juego (ESC).")]
    public GameObject pauseMenuPanel;

    // 🚀 NUEVO: Panel de Decisión (Memorie Objects)
    [Header("2. Panel de Decisión (Memorie)")]
    [Tooltip("Panel que aparece sobre el objeto Memorie (Pausa el juego).")]
    public GameObject decisionPanelGameObject;
    private RectTransform decisionPanelRectTransform; // Para moverlo en la pantalla

    [Header("Dependencias")]
    public MouseLookController mouseLookComponent;

    private bool isPaused = false; // Controla Time.timeScale (Solo para PauseMenu)
    private bool isToolMenuOpen = false; // Controla el panel de tools (Time.timeScale = 1f)
    private Camera mainCamera; // Referencia a la cámara para WorldToScreenPoint


    void Awake()
    {
        if (toolMenuPanel != null) toolMenuPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        mainCamera = Camera.main; // Inicializamos la cámara

        // 🚀 Configuración del Panel de Decisión
        if (decisionPanelGameObject != null)
        {
            decisionPanelGameObject.SetActive(false);
            decisionPanelRectTransform = decisionPanelGameObject.GetComponent<RectTransform>();
            if (decisionPanelRectTransform == null)
            {
                Debug.LogError("ToolPanelIdea: decisionPanelGameObject no tiene un RectTransform.", decisionPanelGameObject);
            }
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (mouseLookComponent == null)
        {
            Debug.LogError("ToolPanelIdea: El componente MouseLookController NO está asignado en el Inspector.", this);
        }
    }

    // =========================================================================
    // 🚀 NUEVA FUNCIÓN: MOVER Y ABRIR PANEL DE DECISIÓN (LLAMADO POR RAYCAST) 🚀
    // =========================================================================

    /// <summary>
    /// Muestra el panel de Decisión proyectándolo sobre una posición 3D del mundo.
    /// Esta acción SIEMPRE pausa el juego.
    /// </summary>
    public void ShowToolsPanelAtWorldPosition(MemorieObject memorieObject, Vector3 worldPosition)
    {
        if (mainCamera == null || decisionPanelRectTransform == null)
        {
            Debug.LogError("ToolPanelIdea: Faltan referencias de cámara o panel de decisión para proyectar.");
            return;
        }

        // 1. Cerrar cualquier otro menú abierto
        if (isPaused) TogglePause();
        if (isToolMenuOpen) ToggleToolsPanel();

        // 2. Proyectar posición 3D a 2D
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);

        // 3. Verificar si el objeto está visible (frente a la cámara)
        if (screenPoint.z < 0)
        {
            Debug.LogWarning("Objeto de memoria detrás de la cámara. Cancelando panel de decisión.");
            return;
        }

        // 4. Pausar el juego y bloquear controles
        Time.timeScale = 0f;
        HandleCursorAndCamera(true);
        // 🛑 Asumimos que TaskManager.IsDecisionActive se establece aquí o en el caller.

        // 5. Posicionar y activar el panel
        decisionPanelRectTransform.position = screenPoint;
        decisionPanelGameObject.SetActive(true);

        Debug.Log($"Panel de Decisión mostrado en posición del objeto: {screenPoint}");

        // 🛑 Aquí debería ir la lógica para cargar los datos del memorieObject
        // LoadMemorieData(memorieObject);
    }

    /// <summary>
    /// Función para reanudar el juego desde el panel de decisión (llamada por un botón).
    /// </summary>
    public void HideDecisionPanel()
    {
        if (decisionPanelGameObject != null)
        {
            decisionPanelGameObject.SetActive(false);
        }

        // Restaurar el juego
        Time.timeScale = 1f;
        HandleCursorAndCamera(false);

        // 🛑 Asumimos que TaskManager.SetDecisionActive(false) se llama aquí o en el caller.

        isPaused = false; // Asegurar que el estado de pausa principal esté limpio
        isToolMenuOpen = false;

        Debug.Log("Panel de decisión oculto. Juego reanudado.");
    }

    // =========================================================================
    // 1. FUNCIÓN PAUSA PRINCIPAL (Llamada por ESC)
    // =========================================================================

    public void TogglePause()
    {
        // 🛑 Si el panel de decisión está activo, no permitimos la pausa principal.
        // Si no tienes una bandera TaskManager.IsDecisionActive, puedes usar:
        if (decisionPanelGameObject != null && decisionPanelGameObject.activeSelf) return;

        // Si el panel de tools está abierto, lo cerramos antes de pausar
        if (isToolMenuOpen) ToggleToolsPanel();

        isPaused = !isPaused;

        if (isPaused)
        {
            // --- ABRIR MENÚ PAUSA Y CONGELAR TIEMPO ---
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);

            Time.timeScale = 0f;
            HandleCursorAndCamera(true);
        }
        else
        {
            // --- CERRAR MENÚ PAUSA Y REANUDAR TIEMPO ---
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

            Time.timeScale = 1f;
            HandleCursorAndCamera(false);
        }
    }

    // =========================================================================
    // 2. FUNCIÓN TOOLS PANEL (Llamada por ENTER/TAB)
    // =========================================================================

    public void ToggleToolsPanel()
    {
        // Si el juego está en pausa (o en decisión), no se puede abrir el panel de tools.
        if (Time.timeScale == 0f) return;

        isToolMenuOpen = !isToolMenuOpen;

        if (toolMenuPanel != null)
        {
            toolMenuPanel.SetActive(isToolMenuOpen);
        }

        // Bloqueo de cámara y cursor, pero Time.timeScale sigue siendo 1.
        HandleCursorAndCamera(isToolMenuOpen);
    }

    // =========================================================================
    // LÓGICA DE GESTIÓN DE ESTADO (Función Unificada)
    // =========================================================================

    private void HandleCursorAndCamera(bool activateMenu)
    {
        // 1. BLOQUEO DIRECTO DE CÁMARA
        if (mouseLookComponent != null)
        {
            // Desactiva la rotación si el menú está activo (activateMenu=true)
            mouseLookComponent.enabled = !activateMenu;
        }

        // 2. LIBERAR/BLOQUEAR CURSOR
        if (activateMenu)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ResumeGameButton()
    {
        // Asumimos que este botón está en el panel de PAUSA
        if (isPaused)
        {
            TogglePause();
        }
    }
}