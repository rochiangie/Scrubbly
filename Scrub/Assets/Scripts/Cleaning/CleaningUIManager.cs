using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class CleaningUIManager : MonoBehaviour
{
    [Header("Panel Principal")]
    [Tooltip("El GameObject que contiene todos los elementos de la UI de limpieza.")]
    [SerializeField] private GameObject uiPanelGameObject;

    [Header("1. Progreso de Limpieza TOTAL (Opcional)")]
    [Tooltip("Slider General (Suma todo)")]
    [SerializeField] private Slider totalProgressSlider;
    [SerializeField] private TMP_Text totalProgressCountText;

    [Header("2. Sliders por Categoría")]
    [Tooltip("VIDRIO")]
    [SerializeField] private Slider glassSlider;
    [SerializeField] private TMP_Text glassText;

    [Tooltip("PAPEL / CARTON")]
    [SerializeField] private Slider paperSlider;
    [SerializeField] private TMP_Text paperText;

    [Tooltip("PLASTICO")]
    [SerializeField] private Slider plasticSlider;
    [SerializeField] private TMP_Text plasticText;

    [Tooltip("PELIGROSOS")]
    [SerializeField] private Slider hazardousSlider;
    [SerializeField] private TMP_Text hazardousText;

    [Tooltip("RESIDUOS (Manchas/Bolsas)")]
    [SerializeField] private Slider residueSlider;
    [SerializeField] private TMP_Text residueText;

    [Header("3. Componentes de TIEMPO")]
    [SerializeField] private TMP_Text timerText;

    // =================================================================
    // 🚀 INICIALIZACIÓN Y EVENTOS
    // =================================================================

    void OnEnable()
    {
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
        GameEvents.OnProgressUpdate -= UpdateTotalCleaningUI;
    }

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
        // Actualizar UI Total (Existente)
        if (totalProgressSlider != null)
        {
            if (totalProgressSlider.maxValue != totalCount) totalProgressSlider.maxValue = totalCount;
            totalProgressSlider.value = cleanedCount;
        }

        if (totalProgressCountText != null)
        {
            int remaining = Mathf.Max(0, totalCount - cleanedCount);
            totalProgressCountText.text = $"Total: {cleanedCount}/{totalCount}";
        }

        // Actualizar UI Detallada (Nueva)
        if (TaskManager.Instance != null)
        {
            UpdateSpecificSlider(glassSlider, glassText, TaskManager.Instance.cleanedGlass, TaskManager.Instance.totalGlass, "Vidrio");
            UpdateSpecificSlider(paperSlider, paperText, TaskManager.Instance.cleanedPaper, TaskManager.Instance.totalPaper, "Papel");
            UpdateSpecificSlider(plasticSlider, plasticText, TaskManager.Instance.cleanedPlastic, TaskManager.Instance.totalPlastic, "Plastico");
            UpdateSpecificSlider(hazardousSlider, hazardousText, TaskManager.Instance.cleanedHazardous, TaskManager.Instance.totalHazardous, "Peligrosos");
            
            // Residuos = DirtSpots
            UpdateSpecificSlider(residueSlider, residueText, TaskManager.Instance.cleanedDirtSpots, TaskManager.Instance.totalDirtSpots, "Residuos");
        }
    }

    private void UpdateSpecificSlider(Slider slider, TMP_Text text, int current, int total, string label)
    {
        if (slider != null)
        {
            if (slider.maxValue != total) slider.maxValue = total;
            slider.value = current;
        }
        if (text != null)
        {
            int remaining = Mathf.Max(0, total - current);
            text.text = $"{label}: {current}/{total} ({remaining} faltan)";
        }
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