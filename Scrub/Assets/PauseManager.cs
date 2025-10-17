using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    // === Referencias de UI (Asignar en el Inspector) ===
    [Header("Referencias de UI del Menú de Pausa")]
    [Tooltip("El GameObject del panel de menú que se activará/desactivará.")]
    public GameObject pauseMenuPanel;
    public TMP_Text cleaningProgressText; // Texto para mostrar el progreso de limpieza

    [Header("Referencias de Puntuación Sentimental")]
    public Slider emotionalBalanceSlider;
    public TMP_Text emotionalBalanceText;
    public Slider accumulationSlider;
    public TMP_Text accumulationText;

    // === Sistemas Externos ===
    private MouseLookController mouseLook;
    private TaskManager taskManager;
    private SentimentalScoreManager scoreManager;

    private bool isPaused = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        // Encontrar los sistemas necesarios
        mouseLook = FindObjectOfType<MouseLookController>();
        taskManager = FindObjectOfType<TaskManager>();
        scoreManager = SentimentalScoreManager.Instance;

        // Asegurar que el menú esté oculto y el juego despausado al inicio
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        // Nota: Time.timeScale se mantiene en 1f para que los clics funcionen.
    }

    void Update()
    {
        // 📢 CRÍTICO: Detección de la tecla ENTER para pausar/despausar
        if (Input.GetKeyDown(KeyCode.Return))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            // MODO PAUSA (El juego sigue corriendo, solo los controles se bloquean)

            // 1. Mostrar y actualizar el menú
            if (pauseMenuPanel != null)
            {
                UpdateStatsDisplay();
                pauseMenuPanel.SetActive(true);
            }

            // 2. Bloquear cursor y control de cámara/movimiento
            if (mouseLook != null)
            {
                mouseLook.SetControlsActive(false); // Desbloquea el cursor, desactiva la rotación de cámara
            }
        }
        else
        {
            // MODO JUEGO

            // 1. Ocultar el menú
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }

            // 2. Bloquear cursor y restaurar control de cámara/movimiento
            if (mouseLook != null)
            {
                mouseLook.SetControlsActive(true); // Bloquea el cursor, reactiva la rotación de cámara
            }
        }
    }

    private void UpdateStatsDisplay()
    {
        // Usamos las propiedades del Slider del TaskManager si existen
        if (scoreManager == null || taskManager == null || taskManager.cleaningProgressSlider == null) return;

        // 1. STATS DE LIMPIEZA
        int cleaned = (int)taskManager.cleaningProgressSlider.value;
        int total = (int)taskManager.cleaningProgressSlider.maxValue;

        cleaningProgressText.text = $"Limpieza: {cleaned} / {total}";

        // 2. STATS DE BALANCE EMOCIONAL
        int currentBalance = scoreManager.emotionalBalanceScore;
        int minBalance = scoreManager.minBalanceForGoodEnding;

        emotionalBalanceSlider.maxValue = minBalance * 2; // Rango visual hasta el doble del mínimo
        emotionalBalanceSlider.value = currentBalance;
        emotionalBalanceText.text = $"Balance Emocional: {currentBalance} / {minBalance} (Mínimo)";

        // 3. STATS DE ACUMULACIÓN
        int currentAccumulation = scoreManager.accumulationScore;
        int maxAccumulation = scoreManager.maxAccumulationForGoodEnding;

        accumulationSlider.maxValue = maxAccumulation;
        accumulationSlider.value = currentAccumulation;
        accumulationText.text = $"Acumulación: {currentAccumulation} / {maxAccumulation} (Límite)";
    }

    // Método para que los botones de UI puedan reanudar el juego (ej: Botón 'Reanudar')
    public void ResumeGameButton()
    {
        // 📢 Este método debe ser llamado por el botón 'Reanudar' en la UI del menú de pausa
        if (isPaused)
        {
            TogglePause();
        }
    }
}