using UnityEngine;
using UnityEngine.UI; // Para Slider (si lo usas) o Image
using TMPro;        // Para TextMeshProUGUI
using System;

public class ScoreUIUpdater : MonoBehaviour
{
    [Header("Panel Contenedor")]
    [Tooltip("El GameObject que contiene todas las barras de puntuación. Se activa/desactiva con la tecla.")]
    public GameObject scorePanelContainer;

    [Header("UI de Balance Emocional")]
    [Tooltip("La barra de progreso (Slider o Image Fill) para el Balance Emocional.")]
    public Slider emotionalBalanceSlider; // O Image si usas Fill
    public TMP_Text emotionalBalanceValueText; // Texto para el valor numérico
    [Tooltip("Color de la barra cuando el balance es bueno.")]
    public Color goodBalanceColor = Color.green;
    [Tooltip("Color de la barra cuando el balance es malo.")]
    public Color badBalanceColor = Color.red;

    [Header("UI de Acumulación")]
    [Tooltip("La barra de progreso (Slider o Image Fill) para la Acumulación.")]
    public Slider accumulationSlider; // O Image si usas Fill
    public TMP_Text accumulationValueText; // Texto para el valor numérico
    [Tooltip("Color de la barra cuando la acumulación es baja (bueno).")]
    public Color lowAccumulationColor = Color.green;
    [Tooltip("Color de la barra cuando la acumulación es alta (malo).")]
    public Color highAccumulationColor = Color.red;

    [Header("Referencias del Manager")]
    // Referencia al SentimentalScoreManager para obtener los umbrales
    private SentimentalScoreManager sentimentalManager;

    void Start()
    {
        // Obtener la instancia del SentimentalScoreManager
        sentimentalManager = SentimentalScoreManager.Instance;
        if (sentimentalManager == null)
        {
            Debug.LogError("ScoreUIUpdater: SentimentalScoreManager.Instance no encontrado.");
            return;
        }

        // Inicializar la UI con los valores actuales (al inicio del juego)
        UpdateUI(sentimentalManager.emotionalBalanceScore, sentimentalManager.accumulationScore);

        // Asegurar que el panel esté oculto al inicio
        if (scorePanelContainer != null)
        {
            scorePanelContainer.SetActive(false);
        }
    }

    void OnEnable()
    {
        // Suscribirse a los eventos
        GameEvents.OnSentimentalScoreUpdate += UpdateUI;
        GameEvents.OnToggleScorePanel += ToggleVisibility;
    }

    void OnDisable()
    {
        // Desuscribirse de los eventos
        GameEvents.OnSentimentalScoreUpdate -= UpdateUI;
        GameEvents.OnToggleScorePanel -= ToggleVisibility;
    }

    /// <summary>
    /// Alterna la visibilidad del panel de puntuación.
    /// </summary>
    private void ToggleVisibility()
    {
        if (scorePanelContainer != null)
        {
            // Alterna el estado activo actual
            scorePanelContainer.SetActive(!scorePanelContainer.activeSelf);
            Debug.Log($"[SCORE UI] Panel de Puntuación alternado: {scorePanelContainer.activeSelf}");
        }
    }

    /// <summary>
    /// Actualiza la UI de las barras de puntuación.
    /// </summary>
    /// <param name="currentEmotionalBalance">Puntuación actual de balance emocional.</param>
    /// <param name="currentAccumulation">Puntuación actual de acumulación.</param>
    private void UpdateUI(int currentEmotionalBalance, int currentAccumulation)
    {
        if (sentimentalManager == null) return;

        // --- Actualizar Barra de Balance Emocional ---
        // El rango de visualización máximo es el doble del umbral de victoria.
        float maxBalanceDisplay = sentimentalManager.minBalanceForGoodEnding * 2f;
        if (emotionalBalanceSlider != null)
        {
            emotionalBalanceSlider.minValue = 0;
            emotionalBalanceSlider.maxValue = maxBalanceDisplay;
            emotionalBalanceSlider.value = currentEmotionalBalance;

            // Cambiar color basado en si es bueno o malo
            Image fillImage = emotionalBalanceSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = currentEmotionalBalance >= sentimentalManager.minBalanceForGoodEnding ? goodBalanceColor : badBalanceColor;
            }
        }
        if (emotionalBalanceValueText != null)
        {
            emotionalBalanceValueText.text = $"{currentEmotionalBalance}/{sentimentalManager.minBalanceForGoodEnding} (Mínimo)";
        }

        // --- Actualizar Barra de Acumulación ---
        // El rango de visualización máximo es el umbral de pérdida por acumulación.
        if (accumulationSlider != null)
        {
            accumulationSlider.minValue = 0;
            accumulationSlider.maxValue = sentimentalManager.maxAccumulationForGoodEnding;
            accumulationSlider.value = currentAccumulation;

            // Cambiar color basado en si es bueno o malo (rojo si se pasa el límite)
            Image fillImage = accumulationSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = currentAccumulation <= sentimentalManager.maxAccumulationForGoodEnding ? lowAccumulationColor : highAccumulationColor;
            }
        }
        if (accumulationValueText != null)
        {
            accumulationValueText.text = $"{currentAccumulation}/{sentimentalManager.maxAccumulationForGoodEnding} (Límite)";
        }
    }
}