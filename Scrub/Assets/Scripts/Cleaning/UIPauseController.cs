using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPauseController : MonoBehaviour
{
    // === 1. ESTADO Y CONTROL DE PAUSA ===
    [Header("1. Control de Pausa")]
    [Tooltip("El GameObject del panel de menú que se activará/desactivará.")]
    public GameObject pauseMenuPanel;

    // Dependencias
    private MouseLookController mouseLook;
    private TaskManager taskManager;
    private SentimentalScoreManager scoreManager;

    private bool isPaused = false;

    // === 2. UI LIMPIEZA ===
    [Header("2. Referencias de UI de Limpieza")]
    public TMP_Text cleaningProgressText;
    public Slider cleaningProgressSlider;

    // === 3. UI SENTIMENTAL ===
    [Header("3. Referencias de Puntuación Sentimental")]
    public Slider emotionalBalanceSlider;
    public TMP_Text emotionalBalanceText;
    [Tooltip("Asigna aquí la IMAGEN de Relleno (Fill Area) del Slider de Balance.")]
    public Image emotionalBalanceFillImage; // <-- NUEVO CAMPO PARA EL COLOR

    public Slider accumulationSlider;
    public TMP_Text accumulationText;
    [Tooltip("Asigna aquí la IMAGEN de Relleno (Fill Area) del Slider de Acumulación.")]
    public Image accumulationFillImage; // <-- NUEVO CAMPO PARA EL COLOR

    // =========================================================================

    void Awake()
    {
        // Configuración de Persistencia.
        DontDestroyOnLoad(gameObject);

        // Buscar los sistemas necesarios
        mouseLook = FindObjectOfType<MouseLookController>();
        if (mouseLook == null) Debug.LogError("UIPauseController: MouseLookController no encontrado.");

        taskManager = FindObjectOfType<TaskManager>();
        if (taskManager == null) Debug.LogError("UIPauseController: TaskManager no encontrado.");

        // Se asume que SentimentalScoreManager.Instance ya se inicializó
        scoreManager = SentimentalScoreManager.Instance;
        if (scoreManager == null) Debug.LogError("UIPauseController: SentimentalScoreManager.Instance es null. ¡Verifica el Singleton!");

        // Inicializar UI.
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    void OnEnable()
    {
        // Suscripciones para actualizar la UI
        GameEvents.OnSentimentalScoreUpdate += UpdateSentimentalUI;
        GameEvents.OnProgressUpdate += UpdateCleaningUI;
    }

    void OnDisable()
    {
        // Desuscripciones
        GameEvents.OnSentimentalScoreUpdate -= UpdateSentimentalUI;
        GameEvents.OnProgressUpdate -= UpdateCleaningUI;
    }

    void Update()
    {
        // 📢 PAUSA CON TECLA ENTER
        if (Input.GetKeyDown(KeyCode.Return) && !SentimentalScoreManager.IsDecisionActive)
        {
            TogglePause();
        }
    }

    // =========================================================================
    // LÓGICA DE PAUSA
    // =========================================================================

    public void TogglePause()
    {
        if (SentimentalScoreManager.IsDecisionActive) return;

        isPaused = !isPaused;

        if (isPaused)
        {
            // MODO PAUSA
            if (pauseMenuPanel != null)
            {
                // **CLAVE:** Actualiza los valores antes de mostrar el menú
                UpdateStatsDisplay();
                pauseMenuPanel.SetActive(true);
            }

            // ACCIÓN CLAVE: Detiene la cámara y libera el cursor.
            if (mouseLook != null)
            {
                mouseLook.SetControlsActive(false);
            }
        }
        else
        {
            // MODO JUEGO
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }

            // ACCIÓN CLAVE: Restaura el control de cámara y bloquea el cursor.
            if (mouseLook != null)
            {
                mouseLook.SetControlsActive(true);
            }
        }
    }

    // Método que actualiza TODOS los stats cuando se pausa el juego
    private void UpdateStatsDisplay()
    {
        if (scoreManager == null || taskManager == null)
        {
            // Intenta buscar la instancia si falló en Awake.
            if (scoreManager == null) scoreManager = SentimentalScoreManager.Instance;
            if (taskManager == null) taskManager = FindObjectOfType<TaskManager>();

            if (scoreManager == null || taskManager == null) return;
        }

        // 📢 La lógica de actualización se llama aquí
        UpdateCleaningUI(taskManager.cleanedCount, taskManager.totalDirt);
        UpdateSentimentalUI(scoreManager.emotionalBalanceScore, scoreManager.accumulationScore);
    }

    // Método para que los botones de UI puedan reanudar el juego (ej: Botón 'Reanudar')
    public void ResumeGameButton()
    {
        if (isPaused)
        {
            TogglePause();
        }
    }

    // =========================================================================
    // LÓGICA DE ACTUALIZACIÓN DE UI
    // =========================================================================

    // 📢 NUEVA FUNCIÓN: Controla los colores de los sliders
    private void UpdateSliderColor(Image fillImage, float currentValue, float goodThreshold, float badThreshold, bool isAccumulation)
    {
        if (fillImage == null) return;

        Color good = Color.green;
        Color warning = Color.yellow;
        Color critical = Color.red;

        if (isAccumulation)
        {
            // Lógica de Acumulación: Más cerca del límite (goodThreshold) es peor.
            if (currentValue < goodThreshold * 0.7f) // Buena gestión (menos del 70% del límite)
            {
                fillImage.color = good;
            }
            else if (currentValue < goodThreshold) // Cerca del límite (70% - 100%)
            {
                fillImage.color = warning;
            }
            else // Sobre el límite (Mala gestión/Acumulador)
            {
                fillImage.color = critical;
            }
        }
        else
        {
            // Lógica de Balance Emocional: Alcanzar el mínimo (goodThreshold) es bueno.
            if (currentValue >= goodThreshold) // Balance Óptimo
            {
                fillImage.color = good;
            }
            else if (currentValue > badThreshold) // Zona de Alerta (usamos 50% del mínimo como "bad threshold")
            {
                fillImage.color = warning;
            }
            else // Balance Muy Bajo (Riesgo de final malo)
            {
                fillImage.color = critical;
            }
        }
    }

    // Actualización de Limpieza (Suscrita a GameEvents.OnProgressUpdate)
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
        else // total == 0
        {
            if (cleaningProgressSlider != null)
            {
                cleaningProgressSlider.maxValue = 1;
                cleaningProgressSlider.value = 1;
            }
            if (cleaningProgressText != null)
            {
                cleaningProgressText.text = "Limpieza: 100% (Listo)";
            }
        }
    }

    // Actualización Sentimental (Suscrita a GameEvents.OnSentimentalScoreUpdate)
    private void UpdateSentimentalUI(int currentBalance, int currentAccumulation)
    {
        if (scoreManager == null) return;

        // Balance Emocional
        int minBalance = scoreManager.minBalanceForGoodEnding;
        emotionalBalanceSlider.maxValue = minBalance * 2;
        emotionalBalanceSlider.value = currentBalance;
        emotionalBalanceText.text = $"Balance Emocional: {currentBalance} / {minBalance} (Mínimo)";

        // Aplica el color. Balance Emocional es bueno si es alto.
        UpdateSliderColor(
            emotionalBalanceFillImage,
            currentBalance,
            minBalance,
            minBalance * 0.5f, // Umbral de advertencia al 50% del mínimo
            false // No es acumulación
        );

        // Acumulación
        int maxAccumulation = scoreManager.maxAccumulationForGoodEnding;
        accumulationSlider.maxValue = maxAccumulation;
        accumulationSlider.value = currentAccumulation;
        accumulationText.text = $"Acumulación: {currentAccumulation} / {maxAccumulation} (Límite)";

        // Aplica el color. Acumulación es buena si es baja.
        UpdateSliderColor(
            accumulationFillImage,
            currentAccumulation,
            maxAccumulation,
            0, // Umbral no usado en esta lógica, pero se pasa.
            true // Es acumulación
        );
    }
}