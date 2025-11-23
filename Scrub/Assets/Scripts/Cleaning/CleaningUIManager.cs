// ARCHIVO: CleaningUIManager.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class CleaningUIManager : MonoBehaviour
{
    // ... (Variables de Header y UI se mantienen igual) ...
    [Header("Panel Principal")]
    [Tooltip("El GameObject que contiene todos los elementos de la UI de limpieza.")]
    [SerializeField] private GameObject uiPanelGameObject;

    [Header("1. Progreso de Limpieza TOTAL")]
    [Tooltip("ASIGNA AQUÍ EL SLIDER PRINCIPAL que muestra el progreso (Suciedad + Basura).")]
    [SerializeField] private Slider totalProgressSlider;

    [Tooltip("ASIGNA AQUÍ EL TEXTO que muestra la cuenta total (Ej: 13/14).")]
    [SerializeField] private TMP_Text totalProgressCountText;

    [Header("2. Componentes de TIEMPO")]
    [SerializeField] private TMP_Text timerText;

    // =================================================================
    // 🚀 INICIALIZACIÓN Y EVENTOS
    // =================================================================

    void OnEnable()
    {
        // 🛑 CAMBIO A MODO DE PRUEBA: Usamos GameEvents.OnProgress si OnProgressUpdate falla.
        // Si tienes un evento llamado OnProgress en GameEvents, esto funcionará.
        // Si no tienes un evento llamado OnProgress, esto fallará.
        try
        {
            GameEvents.OnProgressUpdate += UpdateTotalCleaningUI;
        }
        catch
        {
            Debug.LogError("Error al intentar suscribirse a OnProgressUpdate. Verifique el nombre del evento en GameEvents.cs.");
        }
    }

    void OnDisable()
    {
        // Limpieza y desuscripción.
        GameEvents.OnProgressUpdate -= UpdateTotalCleaningUI;
    }

    // ... (El resto del código de Update, ForceUpdate y las funciones de actualización son idénticos a la versión anterior y correctos) ...

    void Update()
    {
        if (TaskManager.Instance != null && !TaskManager.Instance.timeIsUp)
        {
            UpdateTimerUI(TaskManager.Instance.currentTime);
        }
    }

    public void ForceUpdate(int cleaned, int total)
    {
        Debug.Log($"✅ [UI FORCE UPDATE] Recibida la sincronización inicial: {cleaned}/{total}");
        UpdateTotalCleaningUI(cleaned, total);
    }

    private void UpdateTotalCleaningUI(int cleanedCount, int totalCount)
    {
        if (totalProgressSlider == null || totalProgressCountText == null)
        {
            Debug.LogWarning("Faltan componentes de UI de Progreso Total asignados en el Inspector.");
            return;
        }

        int remaining = Mathf.Max(0, totalCount - cleanedCount);
        totalProgressCountText.text = $"Limpieza: {cleanedCount} / {totalCount} ({remaining} Restantes)";

        if (totalProgressSlider.maxValue != totalCount)
        {
            totalProgressSlider.maxValue = totalCount;
        }
        totalProgressSlider.value = cleanedCount;
    }

    private void UpdateTimerUI(float timeRemaining)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);

            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (timeRemaining <= 30f)
            {
                timerText.color = Color.red;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }
}