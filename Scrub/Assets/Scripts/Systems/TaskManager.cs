using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class TaskManager : MonoBehaviour
{
    private int totalDirt = 0;
    private int cleanedCount = 0;

    [Header("UI y Paneles")]
    public TimedUIPanel notificationPanel;

    [Tooltip("El GameObject del panel de 'Ganaste' (debe estar inactivo al inicio).")]
    public GameObject winPanel;

    [Header("UI de Progreso de Limpieza")]
    public Slider cleaningProgressSlider;
    public TMPro.TMP_Text progressText;

    private const string WIN_PANEL_ERROR = "[TASK MANAGER] El Panel de Victoria (Win Panel) no está asignado. La escena no continuará.";

    void Awake()
    {
        // 📢 CORRECCIÓN: Contar todos los GameObjects que tienen el Tag "Dirt".
        // Esto es más robusto y cumple con tu regla de negocio (limpiar objetos con ese Tag).
        GameObject[] allDirtObjects = GameObject.FindGameObjectsWithTag("Dirt");
        totalDirt = allDirtObjects.Length;
        cleanedCount = 0; // Siempre empezamos con 0 objetos limpios

        Debug.Log($"[TASK MANAGER] Inicializado. Tareas de limpieza Total (Tag 'Dirt'): {totalDirt}. Limpiado: {cleanedCount}.");
    }

    void Start()
    {
        if (winPanel == null)
        {
            Debug.LogError(WIN_PANEL_ERROR);
        }
        else if (winPanel.activeSelf)
        {
            winPanel.SetActive(false);
        }

        // Suscribirse a los eventos
        GameEvents.OnAnyDirtCleaned += HandleCleaned;
        GameEvents.OnProgressUpdate += UpdateCleaningUI;

        // 📢 Configuración Inicial del Slider y el Texto.
        UpdateCleaningUI(cleanedCount, totalDirt);
        GameEvents.Progress(cleanedCount, totalDirt);

        if (notificationPanel != null)
        {
            notificationPanel.ShowAndHide();
        }

        if (totalDirt == 0)
        {
            HandleWinCondition();
        }
    }

    void OnDestroy()
    {
        GameEvents.OnAnyDirtCleaned -= HandleCleaned;
        GameEvents.OnProgressUpdate -= UpdateCleaningUI;
    }

    /// <summary>
    /// Llamado por GameEvents.OnAnyDirtCleaned cuando un objeto es destruido con 'F'.
    /// </summary>
    void HandleCleaned()
    {
        cleanedCount++;

        Debug.Log($"[TASK MANAGER] Suciedad limpiada: {cleanedCount} / {totalDirt}.");

        // 📢 Disparar el evento para que UpdateCleaningUI se ejecute.
        GameEvents.Progress(cleanedCount, totalDirt);

        if (cleanedCount >= totalDirt)
        {
            HandleWinCondition();
        }
    }

    /// <summary>
    /// Método que actualiza el Slider y el texto (Se suscribe a OnProgressUpdate).
    /// </summary>
    private void UpdateCleaningUI(int cleaned, int total)
    {
        // 1. Caso: La limpieza está en curso (total > 0).
        if (total > 0)
        {
            if (cleaningProgressSlider != null)
            {
                cleaningProgressSlider.maxValue = total;
                cleaningProgressSlider.value = cleaned;
            }

            if (progressText != null)
            {
                progressText.text = $"Limpieza: {cleaned} / {total}";
            }
        }
        // 2. Caso: El nivel no tiene nada que limpiar (total = 0).
        else
        {
            if (cleaningProgressSlider != null)
            {
                cleaningProgressSlider.maxValue = 1;
                cleaningProgressSlider.value = 1;
            }
            if (progressText != null)
            {
                progressText.text = "Limpieza: 100% (Listo)";
            }
        }
    }

    /// <summary>
    /// Llamado al completar todas las tareas de limpieza.
    /// </summary>
    private void HandleWinCondition()
    {
        GameEvents.OnAnyDirtCleaned -= HandleCleaned;

        // Notificar que la fase de limpieza ha terminado.
        GameEvents.AllDone();

        if (winPanel == null)
        {
            Debug.LogError(WIN_PANEL_ERROR);
        }
    }
}