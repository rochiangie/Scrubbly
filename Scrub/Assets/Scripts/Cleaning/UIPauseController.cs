using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIPauseController : MonoBehaviour
{
    // === 1. ESTADO Y CONTROL DE PAUSA ===
    [Header("1. Control de Pausa")]
    public GameObject pauseMenuPanel;

    // Dependencias
    private MouseLookController mouseLook;
    private TaskManager taskManager;

    private bool isPaused = false;

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
        // 🛑 SOLO BUSCA TASK MANAGER. ELIMINAMOS TODAS LAS OTRAS BÚSQUEDAS.
        DontDestroyOnLoad(gameObject);

        mouseLook = FindObjectOfType<MouseLookController>();
        // Intentamos obtener la instancia Singleton, pero debe estar inicializada antes.
        taskManager = TaskManager.Instance;

        if (mouseLook == null) Debug.LogError("UIPauseController: MouseLookController no encontrado.");
        if (taskManager == null) Debug.LogError("UIPauseController: TaskManager.Instance es null. ¡Verifica el Orden de Ejecución!");


        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    void OnEnable()
    {
        GameEvents.OnSentimentalScoreUpdate += UpdateSentimentalUI;
        GameEvents.OnProgressUpdate += UpdateCleaningUI;
    }

    void OnDisable()
    {
        GameEvents.OnSentimentalScoreUpdate -= UpdateSentimentalUI;
        GameEvents.OnProgressUpdate -= UpdateCleaningUI;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && TaskManager.IsDecisionActive == false)
        {
            TogglePause();
        }
    }

    // =========================================================================
    // LÓGICA DE PAUSA
    // =========================================================================

    public void TogglePause()
    {
        if (TaskManager.IsDecisionActive) return;

        isPaused = !isPaused;

        if (isPaused)
        {
            if (pauseMenuPanel != null)
            {
                // 🛑 Llamada a la función corregida.
                UpdateStatsDisplay();
                pauseMenuPanel.SetActive(true);
            }

            if (mouseLook != null)
            {
                mouseLook.SetControlsActive(false);
            }
            Time.timeScale = 0f;
        }
        else
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }

            if (mouseLook != null)
            {
                mouseLook.SetControlsActive(true);
            }
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// Método que actualiza TODOS los stats cuando se pausa el juego.
    /// </summary>
    private void UpdateStatsDisplay()
    {
        // 🛑 ELIMINAMOS LA LÍNEA DE ERROR Y SIMPLIFICAMOS LA VERIFICACIÓN.
        if (taskManager == null)
        {
            taskManager = TaskManager.Instance;
            if (taskManager == null)
            {
                Debug.LogError("No se pueden actualizar las estadísticas: TaskManager no disponible.");
                return;
            }
        }

        // 1. STATS DE LIMPIEZA: Leemos directamente los contadores del TaskManager
        UpdateCleaningUI(taskManager.cleanedCount, taskManager.totalDirt);

        // 2. STATS DE BALANCE EMOCIONAL Y ACUMULACIÓN: TaskManager tiene los scores
        UpdateSentimentalUI(taskManager.emotionalBalanceScore, taskManager.accumulationScore);
    }

    // =========================================================================
    // LÓGICA DE ACTUALIZACIÓN DE UI (CONEXIÓN DEL SLIDER)
    // =========================================================================

    /// <summary>
    /// Actualiza el slider de Limpieza (se llama desde GameEvents.Progress).
    /// </summary>
    private void UpdateCleaningUI(int cleaned, int total)
    {
        // 🛑 Lógica para arreglar el slider de 0 a 1.
        if (total > 0)
        {
            if (cleaningProgressSlider != null)
            {
                // 🛑 CLAVE: El max value debe ser el total de suciedad.
                cleaningProgressSlider.maxValue = total;
                cleaningProgressSlider.value = cleaned;
            }

            if (cleaningProgressText != null)
            {
                cleaningProgressText.text = $"Limpieza: {cleaned} / {total}";
            }
        }
        else // Si totalDirt es 0 (no hay suciedad), mostramos 0/0.
        {
            if (cleaningProgressSlider != null)
            {
                cleaningProgressSlider.maxValue = 1; // Para que no divida por cero
                cleaningProgressSlider.value = 0;
            }
            if (cleaningProgressText != null)
            {
                cleaningProgressText.text = "Limpieza: 0 / 0";
            }
        }
    }

    /// <summary>
    /// Actualiza el slider de Score (se llama desde GameEvents.OnSentimentalScoreUpdate).
    /// </summary>
    private void UpdateSentimentalUI(int currentBalance, int currentAccumulation)
    {
        if (taskManager == null) return;

        // Balance Emocional
        int minBalance = taskManager.minBalanceForGoodEnding;
        emotionalBalanceSlider.maxValue = minBalance > 0 ? minBalance * 2 : 100;
        emotionalBalanceSlider.value = currentBalance;
        emotionalBalanceText.text = $"Balance Emocional: {currentBalance} / {minBalance} (Mínimo)";

        UpdateSliderColor(emotionalBalanceFillImage, currentBalance, minBalance, minBalance * 0.5f, false);

        // Acumulación
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
            // Lógica de Acumulación: Cuanto más cerca del límite (goodThreshold), peor.
            if (currentValue > goodThreshold)
            {
                fillImage.color = critical; // ROJO: ¡Límite excedido o alcanzado!
            }
            else if (currentValue > goodThreshold * 0.7f)
            {
                fillImage.color = warning; // AMARILLO: Cerca del 70% del límite.
            }
            else
            {
                fillImage.color = good; // VERDE: Nivel seguro.
            }
        }
        else // Balance Emocional
        {
            // Lógica de Balance: Cuanto más cerca del mínimo (goodThreshold), mejor.
            // badThreshold generalmente es 50% del mínimo (ej. 0.5f * minBalance)
            if (currentValue >= goodThreshold)
            {
                fillImage.color = good; // VERDE: Se alcanzó o superó el mínimo para el buen final.
            }
            else if (currentValue > badThreshold)
            {
                fillImage.color = warning; // AMARILLO: Por encima de la zona crítica, pero bajo el mínimo.
            }
            else
            {
                fillImage.color = critical; // ROJO: En zona crítica (por debajo del 50% del mínimo).
            }
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