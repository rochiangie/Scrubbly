using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIPauseController : MonoBehaviour
{
    // === 1. ESTADO Y CONTROL DE PAUSA Y MENÚS ===
    [Header("1. Control de Pausa y Menús")]
    public GameObject pauseMenuPanel; // Menú principal de pausa (ESC)

    [Tooltip("El GameObject del panel de Tools (ENTER/TAB).")]
    public GameObject toolMenuPanel; // 🚨 ESTE ES EL PANEL DE TOOLS 🚨

    // 🚀 Panel de Decisión (Memorie Objects)
    [Header("4. Panel de Decisión (Memorie)")]
    public GameObject decisionPanelGameObject;
    private RectTransform decisionPanelRectTransform;

    // 🚀 CRÍTICO: Referencias de Texto y Callback para Decisión
    [Header("5. Referencias de Texto de Decisión")]
    public TMP_Text itemNameText;
    public TMP_Text sentimentalValueText;
    private Action<bool> onDecisionMade;

    // Dependencias
    private MouseLookController mouseLook;
    private TaskManager taskManager;
    private Camera mainCamera;

    private bool isPaused = false;
    private bool isToolMenuOpen = false;

    // === 2. UI LIMPIEZA ===
    [Header("2. Referencias de UI de Limpieza")]
    public TMP_Text cleaningProgressText;
    public Slider cleaningProgressSlider;

    // === 3. UI SENTIMENTAL ===
    [Header("3. Referencias de Puntuación Sentimental")]
    public Slider emotionalBalanceSlider;
    public TMP_Text emotionalBalanceText;
    public Image emotionalBalanceFillImage;

    public Slider accumulationSlider;
    public TMP_Text accumulationText;
    public Image accumulationFillImage;

    // === 6. LÓGICA DE SPAWN DE TOOLS ===
    [Header("6. Configuración de Spawn de Tools")]
    [Tooltip("El prefab de la Escoba (Para el botón 1).")]
    public GameObject escobaPrefab;
    [Tooltip("El prefab de la segunda herramienta (Para el botón 2).")]
    public GameObject segundaToolPrefab;
    [Tooltip("El Transform donde se instanciarán las herramientas.")]
    public Transform spawnLocation;
    [Tooltip("La fuerza inicial para lanzar el objeto.")]
    public float spawnLaunchForce = 3.0f;

    // Asumimos que PlayerInteraction está en el mismo objeto o se accede globalmente para el HoldPoint
    private PlayerInteraction playerInteraction;


    public bool IsPaused { get; private set; } // Propiedad pública para chequeo externo

    // =========================================================================

    void Awake()
    {
        mouseLook = FindObjectOfType<MouseLookController>();
        mainCamera = Camera.main;

        // Intentamos encontrar PlayerInteraction en la escena o en el mismo objeto
        playerInteraction = FindObjectOfType<PlayerInteraction>();

        // CRÍTICO: Asegurar que los paneles empiezan ocultos al iniciar
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (toolMenuPanel != null) toolMenuPanel.SetActive(false);

        // Inicialización del Panel de Decisión
        if (decisionPanelGameObject != null)
        {
            decisionPanelGameObject.SetActive(false);
            decisionPanelRectTransform = decisionPanelGameObject.GetComponent<RectTransform>();
            if (decisionPanelRectTransform == null)
            {
                Debug.LogError("UIPauseController: decisionPanelGameObject no tiene un RectTransform.");
            }
        }
    }

    void Start()
    {
        taskManager = TaskManager.Instance;

        if (mouseLook == null) Debug.LogError("UIPauseController: MouseLookController no encontrado.");
        if (taskManager == null) Debug.LogError("UIPauseController: TaskManager.Instance es null.");
        if (spawnLocation == null) Debug.LogWarning("UIPauseController: SpawnLocation no asignado. Las tools aparecerán en la raíz.");


        HandleCursorAndCamera(false);
    }

    void OnEnable() { /* ... */ }
    void OnDisable() { /* ... */ }

    void Update()
    {
        // 1. Pausa (Escape)
        if (Input.GetKeyDown(KeyCode.Escape) && !TaskManager.IsDecisionActive)
        {
            TogglePause();
        }

        // 2. PANEL DE TOOLS (Enter/Tab)
        // 🚨 AQUÍ EL CÓDIGO PARA ABRIR EL PANEL CON ENTER 🚨
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Tab))
        {
            // Solo abrimos el panel si el juego no está pausado y no estamos en decisión.
            if (Time.timeScale > 0f && !TaskManager.IsDecisionActive)
            {
                ToggleToolsPanel();
            }
        }

        // 3. 🚀 Lógica de Input de Decisión (Y/N)
        if (decisionPanelGameObject != null && decisionPanelGameObject.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Y))
            {
                OnDecisionInput(true);
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                OnDecisionInput(false);
            }
        }
    }

    // =========================================================================
    // 🚨 FUNCIÓN PARA LOS BOTONES DE UI (REEMPLAZA TOOL DISPENSER) 🚨
    // =========================================================================

    /// <summary>
    /// FUNCIÓN DEL BOTÓN 1: Instancia la Escoba y cierra el panel.
    /// Asignar esta función al OnClick() del botón de Escoba.
    /// </summary>
    public void SelectEscobaAndClose()
    {
        if (escobaPrefab == null)
        {
            Debug.LogError("UIPauseController: Escoba Prefab no asignado. No se puede spawnear.");
            return;
        }
        SpawnTool(escobaPrefab);
        CloseToolsPanel(); // 🚨 Cierre de panel garantizado
    }

    /// <summary>
    /// FUNCIÓN DEL BOTÓN 2: Instancia la Segunda Herramienta y cierra el panel.
    /// Asignar esta función al OnClick() del segundo botón.
    /// </summary>
    public void SelectSegundaToolAndClose()
    {
        if (segundaToolPrefab == null)
        {
            Debug.LogError("UIPauseController: Segunda Tool Prefab no asignada. No se puede spawnear.");
            return;
        }
        SpawnTool(segundaToolPrefab);
        CloseToolsPanel(); // 🚨 Cierre de panel garantizado
    }

    private void SpawnTool(GameObject toolPrefab)
    {
        Transform targetSpawn = spawnLocation != null ? spawnLocation : transform;

        GameObject spawnedTool = Instantiate(toolPrefab, targetSpawn.position, targetSpawn.rotation);

        Rigidbody rb = spawnedTool.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Lanzamos la herramienta hacia adelante si tiene Rigidbody
            rb.AddForce(targetSpawn.forward * spawnLaunchForce, ForceMode.Impulse);
        }

        Debug.Log($"UIPauseController: Objeto {spawnedTool.name} instanciado y listo para ser recogido.");
    }

    // =========================================================================
    // 🚀 FUNCIÓN DE DECISIÓN DE MEMORIE 🚀
    // =========================================================================

    public void ShowToolsPanelAtWorldPosition(string itemName, int value, Action<bool> callback)
    {
        if (decisionPanelGameObject == null) return;

        // 1. Cerrar otros menús si están abiertos.
        if (isPaused) TogglePause();
        if (isToolMenuOpen) CloseToolsPanel(); // Usar CloseToolsPanel() para cerrar

        // 2. Configurar Textos y Callback
        onDecisionMade = callback;
        if (itemNameText != null) itemNameText.text = $"Objeto: {itemName}";
        if (sentimentalValueText != null) sentimentalValueText.text = $"Valor Sentimental: {value}";

        // 3. Pausar el juego y bloquear controles
        Time.timeScale = 0f;
        HandleCursorAndCamera(true);
        if (taskManager != null) TaskManager.SetDecisionActive(true);

        // 4. SOLO ACTIVAMOS EL PANEL 
        decisionPanelGameObject.SetActive(true);

        Debug.Log($"Panel de Decisión activado. Objeto: {itemName}");
    }

    private void OnDecisionInput(bool isKept)
    {
        if (onDecisionMade != null)
        {
            onDecisionMade.Invoke(isKept);
        }

        HideDecisionPanel();

        onDecisionMade = null;
    }

    public void HideDecisionPanel()
    {
        if (decisionPanelGameObject != null)
        {
            decisionPanelGameObject.SetActive(false);
        }

        Time.timeScale = 1f;
        HandleCursorAndCamera(false);
        if (taskManager != null) TaskManager.SetDecisionActive(false);

        isPaused = false;
        isToolMenuOpen = false;

        Debug.Log("Panel de decisión oculto. Juego reanudado.");
    }

    // EN EL SCRIPT UIPauseController.cs
    // ... (código anterior)

    // =========================================================================
    // LÓGICA DE PAUSA Y TOOLS
    // =========================================================================

    public void TogglePause()
    {
        if (taskManager != null && TaskManager.IsDecisionActive) return;

        if (isToolMenuOpen) CloseToolsPanel(); // Usar CloseToolsPanel()

        isPaused = !isPaused;

        if (isPaused)
        {
            if (pauseMenuPanel != null)
            {
                UpdateStatsDisplay();
                pauseMenuPanel.SetActive(true);
            }

            Time.timeScale = 0f;
            // 🛑 En pausa por ESC: Bloquea Controles/Cámara y muestra Cursor
            HandleCursorAndCamera(true);
        }
        else
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }

            Time.timeScale = 1f;
            // 🛑 Reanudar: Activa Controles/Cámara y esconde Cursor
            HandleCursorAndCamera(false);
        }
    }

    public void ToggleToolsPanel()
    {
        // Si el juego está pausado (por ESC) o en decisión, no abrimos/cerramos tools
        if (isPaused || TaskManager.IsDecisionActive) return;

        if (isToolMenuOpen)
        {
            CloseToolsPanel();
        }
        else
        {
            // Abrir
            isToolMenuOpen = true;
            if (toolMenuPanel != null)
            {
                toolMenuPanel.SetActive(true);
            }

            // 🛑 BLOQUEO DE CÁMARA E INDEPENDENCIA DE TIEMPO 🛑
            // No pausamos el tiempo, solo bloqueamos la cámara y mostramos el cursor.
            if (mouseLook != null)
            {
                mouseLook.SetLockState(true); // 🚨 Bloquea explícitamente la cámara 🚨
            }

            // Muestra el cursor para interactuar con la UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("Panel Tools abierto con tecla Enter/Tab. Cámara bloqueada.");
        }
    }

    /// <summary>
    /// 🚨 CIERRE ESPECÍFICO DEL PANEL DE TOOLS 🚨
    /// Forzada para cerrar el panel de Tools y reanudar el juego (Llamada después de seleccionar la tool).
    /// </summary>
    public void CloseToolsPanel()
    {
        if (isToolMenuOpen && toolMenuPanel != null)
        {
            toolMenuPanel.SetActive(false); // Ocultar el panel
            isToolMenuOpen = false;

            // 🛑 DESBLOQUEO DE CÁMARA 🛑
            // Solo desbloqueamos la cámara y escondemos el cursor.
            if (mouseLook != null)
            {
                mouseLook.SetLockState(false); // 🚨 Desbloquea explícitamente la cámara 🚨
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("Panel Tools cerrado.");
        }
    }
    public void SetIsPaused(bool isPaused)
    {
        // Solo alternamos la pausa si el estado deseado es diferente al actual.
        // Esto previene un loop y asegura que TogglePause() haga el trabajo.
        if (this.isPaused != isPaused)
        {
            TogglePause();
        }
    }
    /// <summary>
    /// Gestiona el bloqueo del cursor y la activación de los controles (USADO SOLO PARA PAUSA/DECISION).
    /// </summary>
    private void HandleCursorAndCamera(bool activateMenu)
    {
        if (mouseLook != null)
        {
            // En este caso, SetControlsActive desactiva la lógica de Input completa
            mouseLook.SetControlsActive(!activateMenu);
        }

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

    // ... (resto del código)

    // =========================================================================
    // LÓGICA DE ACTUALIZACIÓN DE UI (STATS)
    // =========================================================================

    private void UpdateStatsDisplay()
    {
        if (taskManager == null)
        {
            taskManager = TaskManager.Instance;
            if (taskManager == null) return;
        }

        int total = taskManager.totalDirtSpots + taskManager.totalTrashItems;
        int cleaned = taskManager.cleanedDirtSpots + taskManager.cleanedTrashItems;

        UpdateCleaningUI(cleaned, total);
        UpdateSentimentalUI(taskManager.emotionalBalanceScore, taskManager.accumulationScore);
    }

    private void UpdateCleaningUI(int cleaned, int total)
    {
        if (total > 0)
        {
            if (cleaningProgressSlider != null)
            {
                cleaningProgressSlider.maxValue = total;
                cleaningProgressSlider.value = cleaned;
            }

            if (cleaningProgressText != null)
            {
                cleaningProgressText.text = $"Limpieza: {cleaned} / {total}";
            }
        }
        else
        {
            if (cleaningProgressSlider != null)
            {
                cleaningProgressSlider.maxValue = 1;
                cleaningProgressSlider.value = 0;
            }
            if (cleaningProgressText != null)
            {
                cleaningProgressText.text = "Limpieza: 0 / 0";
            }
        }
    }

    private void UpdateSentimentalUI(int currentBalance, int currentAccumulation)
    {
        if (taskManager == null) return;

        int minBalance = taskManager.minBalanceForGoodEnding;
        emotionalBalanceSlider.maxValue = minBalance > 0 ? minBalance * 2 : 100;
        emotionalBalanceSlider.value = currentBalance;
        emotionalBalanceText.text = $"Balance Emocional: {currentBalance} / {minBalance} (Mínimo)";

        UpdateSliderColor(emotionalBalanceFillImage, currentBalance, minBalance, minBalance * 0.5f, false);

        int maxAccumulation = taskManager.maxAccumulationForGoodEnding;
        accumulationSlider.maxValue = maxAccumulation > 0 ? maxAccumulation : 100;
        accumulationSlider.value = currentAccumulation;
        accumulationText.text = $"Acumulación: {currentAccumulation} / {maxAccumulation} (Límite)";

        UpdateSliderColor(accumulationFillImage, currentAccumulation, maxAccumulation, 0, true);
    }

    private void UpdateSliderColor(Image fillImage, float currentValue, float goodThreshold, float badThreshold, bool isAccumulation)
    {
        if (fillImage == null) return;

        Color good = Color.green;
        Color warning = Color.yellow;
        Color critical = Color.red;

        if (isAccumulation)
        {
            if (currentValue >= goodThreshold) fillImage.color = critical;
            else if (currentValue > goodThreshold * 0.7f) fillImage.color = warning;
            else fillImage.color = good;
        }
        else // Balance Emocional
        {
            if (currentValue >= goodThreshold) fillImage.color = good;
            else if (currentValue > badThreshold) fillImage.color = warning;
            else fillImage.color = critical;
        }
    }

    public void ResumeGameButton()
    {
        if (isPaused)
        {
            TogglePause();
        }
    }
}