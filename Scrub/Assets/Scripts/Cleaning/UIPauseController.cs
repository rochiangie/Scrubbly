using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class UIPauseController : MonoBehaviour
{
    // === 1. ESTADO Y CONTROL DE PAUSA ===
    [Header("1. Control de Pausa")]
    [Tooltip("El GameObject del panel de menú que se activará/desactivará.")]
    public GameObject pauseMenuPanel;

    private MouseLookController mouseLook;
    private bool isPaused = false;

    // === 2. UI GLOBAL: Referencias a otros Managers ===
    private TaskManager taskManager;
    private SentimentalScoreManager scoreManager;

    // === 3. UI LIMPIEZA ===
    [Header("2. Referencias de UI de Limpieza")]
    public TMP_Text cleaningProgressText;
    public Slider cleaningProgressSlider;

    // === 4. UI SENTIMENTAL ===
    [Header("3. Referencias de Puntuación Sentimental")]
    public Slider emotionalBalanceSlider;
    public TMP_Text emotionalBalanceText;
    public Slider accumulationSlider;
    public TMP_Text accumulationText;

    // =========================================================================

    void Awake()
    {
        // 📢 Configuración de Persistencia
        DontDestroyOnLoad(gameObject);

        // 📢 Buscar los sistemas necesarios
        mouseLook = FindObjectOfType<MouseLookController>();
        taskManager = FindObjectOfType<TaskManager>();
        scoreManager = SentimentalScoreManager.Instance;

        // Inicializar UI
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
        GameEvents.OnSentimentalScoreUpdate -= UpdateSentimentalUI;
        GameEvents.OnProgressUpdate -= UpdateCleaningUI;
    }

    void Update()
    {
        // 📢 Toggle de Pausa (TECLA ENTER)
        if (Input.GetKeyDown(KeyCode.Return))
        {
            TogglePause();
        }
    }

    // =========================================================================
    // LÓGICA DE PAUSA (BLOQUEA LA CÁMARA Y HABILITA EL MOUSE)
    // =========================================================================

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            // 📢 MODO PAUSA (El tiempo sigue corriendo: Time.timeScale = 1f)

            if (pauseMenuPanel != null)
            {
                UpdateStatsDisplay();
                pauseMenuPanel.SetActive(true);
            }

            // ACCIÓN CLAVE: Llama a MouseLookController para:
            // 1. Detener la rotación de la cámara (por su chequeo en Update)
            // 2. Bloquear el movimiento del jugador (por PlayerMovement.FixedUpdate)
            // 3. Liberar el cursor para clics (Cursor.lockState = None)
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

    // Método que actualiza TODOS los stats cuando se pausa el juego (llamado por TogglePause)
    private void UpdateStatsDisplay()
    {
        if (scoreManager == null || taskManager == null) return;

        // 📢 ASUNCIÓN: Las variables cleanedCount y totalDirt son públicas en TaskManager
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

        // Acumulación
        int maxAccumulation = scoreManager.maxAccumulationForGoodEnding;
        accumulationSlider.maxValue = maxAccumulation;
        accumulationSlider.value = currentAccumulation;
        accumulationText.text = $"Acumulación: {currentAccumulation} / {maxAccumulation} (Límite)";
    }
}