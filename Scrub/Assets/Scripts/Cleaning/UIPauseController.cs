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
    public GameObject toolMenuPanel;

    // 🚀 Panel de Decisión (Memorie Objects)
    [Header("4. Panel de Decisión (Memorie)")]
    public GameObject decisionPanelGameObject;
    private RectTransform decisionPanelRectTransform;

    // 🚀 CRÍTICO: Referencias de Texto y Callback para Decisión
    [Header("5. Referencias de Texto de Decisión")]
    public TMP_Text itemNameText;
    public TMP_Text sentimentalValueText;
    private Action<bool> onDecisionMade; // Almacena el método a ejecutar (callback)

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


    // =========================================================================

    void Awake()
    {
        mouseLook = FindObjectOfType<MouseLookController>();
        mainCamera = Camera.main;

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
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Tab))
        {
            if (Time.timeScale > 0f && !TaskManager.IsDecisionActive)
            {
                ToggleToolsPanel();
            }
        }

        // 3. 🚀 Lógica de Input de Decisión (Y/N)
        if (decisionPanelGameObject != null && decisionPanelGameObject.activeSelf)
        {
            // Tecla 'Y' (Sí / Keep)
            if (Input.GetKeyDown(KeyCode.Y))
            {
                OnDecisionInput(true);
            }

            // Tecla 'N' (No / Discard)
            else if (Input.GetKeyDown(KeyCode.N))
            {
                OnDecisionInput(false);
            }
        }
    }

    // =========================================================================
    // 🚀 FUNCIÓN DE DECISIÓN DE MEMORIE (PARA Q/CLICK) 🚀
    // =========================================================================

    /// <summary>
    /// Muestra el panel de Decisión en su posición anclada, actualizando los textos y el callback.
    /// </summary>
    public void ShowToolsPanelAtWorldPosition(string itemName, int value, Action<bool> callback)
    {
        if (decisionPanelGameObject == null) return;

        // 1. Cerrar otros menús si están abiertos.
        if (isPaused) TogglePause();
        if (isToolMenuOpen) ToggleToolsPanel();

        // 2. 🚀 Configurar Textos y Callback (Añadido chequeo de Null)
        onDecisionMade = callback;
        if (itemNameText != null) itemNameText.text = $"Objeto: {itemName}";
        if (sentimentalValueText != null) sentimentalValueText.text = $"Valor Sentimental: {value}";

        // 3. Pausar el juego y bloquear controles
        Time.timeScale = 0f;
        HandleCursorAndCamera(true);
        if (taskManager != null) TaskManager.SetDecisionActive(true);

        // 4. SOLO ACTIVAMOS EL PANEL (aparecerá donde esté anclado en el Canvas)
        decisionPanelGameObject.SetActive(true);

        Debug.Log($"Panel de Decisión activado. Objeto: {itemName}");
    }

    /// <summary>
    /// Se llama cuando el jugador presiona 'Y' o 'N'. Ejecuta el callback y reanuda el juego.
    /// </summary>
    private void OnDecisionInput(bool isKept)
    {
        // 1. Ejecutar el callback (DecideAndNotify en MemorieObject.cs)
        if (onDecisionMade != null)
        {
            onDecisionMade.Invoke(isKept);
        }

        // 2. Ocultar la UI y reanudar el juego (Llamando al HideDecisionPanel)
        HideDecisionPanel();

        // 3. Limpiar el callback
        onDecisionMade = null;
    }


    /// <summary>
    /// Función para reanudar el juego desde el panel de decisión (llamada por un botón o OnDecisionInput).
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
        if (taskManager != null) TaskManager.SetDecisionActive(false);

        isPaused = false;
        isToolMenuOpen = false;

        Debug.Log("Panel de decisión oculto. Juego reanudado.");
    }

    // =========================================================================
    // LÓGICA DE PAUSA Y TOOLS
    // =========================================================================

    public void TogglePause()
    {
        if (taskManager != null && TaskManager.IsDecisionActive) return;

        if (isToolMenuOpen) ToggleToolsPanel();

        isPaused = !isPaused;

        if (isPaused)
        {
            if (pauseMenuPanel != null)
            {
                UpdateStatsDisplay();
                pauseMenuPanel.SetActive(true);
            }

            Time.timeScale = 0f;
            HandleCursorAndCamera(true);
        }
        else
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }

            Time.timeScale = 1f;
            HandleCursorAndCamera(false);
        }
    }

    public void ToggleToolsPanel()
    {
        if (Time.timeScale == 0f || TaskManager.IsDecisionActive) return;

        if (isPaused) return;

        isToolMenuOpen = !isToolMenuOpen;

        if (toolMenuPanel != null)
        {
            toolMenuPanel.SetActive(isToolMenuOpen);
        }

        HandleCursorAndCamera(isToolMenuOpen);
    }

    /// <summary>
    /// Gestiona el bloqueo del cursor y la activación de los controles.
    /// </summary>
    private void HandleCursorAndCamera(bool activateMenu)
    {
        if (mouseLook != null)
        {
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