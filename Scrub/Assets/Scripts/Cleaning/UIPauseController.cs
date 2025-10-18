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
    private CleaningController cleaningController; // <-- NECESARIO PARA OBTENER LOS CONTEOS

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
    public Image emotionalBalanceFillImage;

    public Slider accumulationSlider;
    public TMP_Text accumulationText;
    [Tooltip("Asigna aquí la IMAGEN de Relleno (Fill Area) del Slider de Acumulación.")]
    public Image accumulationFillImage;


    // =========================================================================

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // 🛑 Búsqueda de sistemas críticos (usando FindObjectOfType y Singleton).
        mouseLook = FindObjectOfType<MouseLookController>();
        taskManager = FindObjectOfType<TaskManager>();
        // Buscamos el CleaningController que tiene los datos cleanedCount/totalDirt
        cleaningController = FindObjectOfType<CleaningController>();
        scoreManager = SentimentalScoreManager.Instance; // Singleton

        // Verificación de referencias (útil para la depuración)
        if (mouseLook == null) Debug.LogError("UIPauseController: MouseLookController no encontrado.");
        if (taskManager == null) Debug.LogError("UIPauseController: TaskManager no encontrado.");
        if (cleaningController == null) Debug.LogError("UIPauseController: CleaningController no encontrado. **¡Este es el script que tiene los conteos!**");
        if (scoreManager == null) Debug.LogError("UIPauseController: SentimentalScoreManager.Instance es null.");

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
        // Abre/Cierra con ENTER, y solo si no estamos en medio de una decisión S/N.
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
            if (pauseMenuPanel != null)
            {
                // **CLAVE:** Actualiza los valores antes de mostrar el menú
                UpdateStatsDisplay();
                pauseMenuPanel.SetActive(true);
            }

            if (mouseLook != null)
            {
                mouseLook.SetControlsActive(false);
            }
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
        }
    }

    // Método que actualiza TODOS los stats cuando se pausa el juego
    private void UpdateStatsDisplay()
    {
        // **🛑 LA LÍNEA DE ERROR HA SIDO ELIMINADA Y CORREGIDA AQUÍ.**
        if (scoreManager == null || cleaningController == null)
        {
            // Intentamos buscar de nuevo por si se cargaron tarde
            if (scoreManager == null) scoreManager = SentimentalScoreManager.Instance;
            if (cleaningController == null) cleaningController = FindObjectOfType<CleaningController>();

            if (scoreManager == null || cleaningController == null)
            {
                Debug.LogError("No se pueden actualizar las estadísticas: Falta el ScoreManager o CleaningController en la escena.");
                return;
            }
        }

        // 🛑 1. STATS DE LIMPIEZA: Leemos las variables directamente del CleaningController
        UpdateCleaningUI(cleaningController.cleanedCount, cleaningController.totalDirt);

        // 2. STATS DE BALANCE EMOCIONAL Y ACUMULACIÓN
        UpdateSentimentalUI(scoreManager.emotionalBalanceScore, scoreManager.accumulationScore);
    }

    // Método llamado por GameEvents.OnProgressUpdate (cuando se limpia algo)
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
        else // total == 0 o no inicializado
        {
            if (cleaningProgressSlider != null)
            {
                cleaningProgressSlider.maxValue = 1;
                cleaningProgressSlider.value = 0;
            }
            if (cleaningProgressText != null)
            {
                cleaningProgressText.text = "Limpieza: 0% / --";
            }
        }
    }

    // Método llamado por GameEvents.OnSentimentalScoreUpdate (cuando cambia el score)
    private void UpdateSentimentalUI(int currentBalance, int currentAccumulation)
    {
        if (scoreManager == null) return;

        // Balance Emocional
        int minBalance = scoreManager.minBalanceForGoodEnding;
        emotionalBalanceSlider.maxValue = minBalance * 2;
        emotionalBalanceSlider.value = currentBalance;
        emotionalBalanceText.text = $"Balance Emocional: {currentBalance} / {minBalance} (Mínimo)";

        UpdateSliderColor(
            emotionalBalanceFillImage,
            currentBalance,
            minBalance,
            minBalance * 0.5f,
            false
        );

        // Acumulación
        int maxAccumulation = scoreManager.maxAccumulationForGoodEnding;
        accumulationSlider.maxValue = maxAccumulation;
        accumulationSlider.value = currentAccumulation;
        accumulationText.text = $"Acumulación: {currentAccumulation} / {maxAccumulation} (Límite)";

        UpdateSliderColor(
            accumulationFillImage,
            currentAccumulation,
            maxAccumulation,
            0,
            true
        );
    }

    // Función para el control de color (para retroalimentación visual)
    private void UpdateSliderColor(Image fillImage, float currentValue, float goodThreshold, float badThreshold, bool isAccumulation)
    {
        if (fillImage == null) return;

        Color good = Color.green;
        Color warning = Color.yellow;
        Color critical = Color.red;

        if (isAccumulation)
        {
            if (currentValue > goodThreshold)
            {
                fillImage.color = critical;
            }
            else if (currentValue > goodThreshold * 0.7f)
            {
                fillImage.color = warning;
            }
            else
            {
                fillImage.color = good;
            }
        }
        else // Balance Emocional
        {
            if (currentValue >= goodThreshold)
            {
                fillImage.color = good;
            }
            else if (currentValue > badThreshold)
            {
                fillImage.color = warning;
            }
            else
            {
                fillImage.color = critical;
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