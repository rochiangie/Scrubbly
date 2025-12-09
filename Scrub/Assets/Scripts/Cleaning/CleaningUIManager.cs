using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class CleaningUIManager : MonoBehaviour
{
    [Header("Panel Principal")]
    [SerializeField] private GameObject uiPanelGameObject;

    [Header("1. Progreso de Limpieza TOTAL")]
    [SerializeField] private Slider totalProgressSlider;
    [SerializeField] private TMP_Text totalProgressCountText;

    [Header("2. Sliders por Categoría")]
    [SerializeField] private Slider glassSlider;
    [SerializeField] private TMP_Text glassText;

    [SerializeField] private Slider paperSlider;
    [SerializeField] private TMP_Text paperText;

    [SerializeField] private Slider plasticSlider;
    [SerializeField] private TMP_Text plasticText;

    [SerializeField] private Slider hazardousSlider;
    [SerializeField] private TMP_Text hazardousText;

    [SerializeField] private Slider organicSlider;
    [SerializeField] private TMP_Text organicText;

    [SerializeField] private Slider residueSlider;
    [SerializeField] private TMP_Text residueText;

    [Header("3. Componentes de TIEMPO")]
    [SerializeField] private TMP_Text timerText;

    [Header("4. Notificaciones")]
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private float notificationDuration = 3f;

    // Memoria para evitar actualizaciones innecesarias
    private int lastGlass = -1, lastGlassTotal = -1;
    private int lastPaper = -1, lastPaperTotal = -1;
    private int lastPlastic = -1, lastPlasticTotal = -1;
    private int lastHazardous = -1, lastHazardousTotal = -1;
    private int lastOrganic = -1, lastOrganicTotal = -1;
    private int lastResidue = -1, lastResidueTotal = -1;
    private int lastTotalCleaned = -1, lastTotalCount = -1;

    void OnEnable()
    {
        GameEvents.OnProgressUpdate += UpdateUI;
        if (notificationText != null) notificationText.text = ""; // Limpiar al inicio
    }

    void OnDisable()
    {
        GameEvents.OnProgressUpdate -= UpdateUI;
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
        // Reseteamos la memoria para forzar la actualización
        lastGlass = -1; 
        UpdateUI(cleaned, total);
    }

    // Este método se llama cada vez que cambia ALGO en el progreso
    private void UpdateUI(int globalCleaned, int globalTotal)
    {
        if (TaskManager.Instance == null) return;

        // 1. Actualizar Slider Global (Solo si cambió)
        if (globalCleaned != lastTotalCleaned || globalTotal != lastTotalCount)
        {
            UpdateSlider(totalProgressSlider, totalProgressCountText, globalCleaned, globalTotal, "Total");
            lastTotalCleaned = globalCleaned;
            lastTotalCount = globalTotal;
        }

        // 2. Actualizar Vidrio
        CheckCategoryUpdate(ref lastGlass, ref lastGlassTotal, TaskManager.Instance.cleanedGlass, TaskManager.Instance.totalGlass, glassSlider, glassText, "Vidrio");

        // 3. Actualizar Papel
        CheckCategoryUpdate(ref lastPaper, ref lastPaperTotal, TaskManager.Instance.cleanedPaper, TaskManager.Instance.totalPaper, paperSlider, paperText, "Papel / cartón");

        // 4. Actualizar Plástico
        CheckCategoryUpdate(ref lastPlastic, ref lastPlasticTotal, TaskManager.Instance.cleanedPlastic, TaskManager.Instance.totalPlastic, plasticSlider, plasticText, "Plásticos");

        // 5. Actualizar Peligrosos
        CheckCategoryUpdate(ref lastHazardous, ref lastHazardousTotal, TaskManager.Instance.cleanedHazardous, TaskManager.Instance.totalHazardous, hazardousSlider, hazardousText, "Peligrosos");

        // 6. Actualizar Orgánicos (NUEVO)
        // Usamos 'cleanedBolsas' como proxy para Orgánicos si no hay variable específica, o asumimos que TaskManager agrupa ahí.
        // Si 'Organico' es un tag separado, asegúrate de que TaskManager lo cuente.
        CheckCategoryUpdate(ref lastOrganic, ref lastOrganicTotal, TaskManager.Instance.cleanedBolsas, TaskManager.Instance.totalBolsas, organicSlider, organicText, "Orgánicos");

        // 7. Actualizar Residuos (Manchas)
        int currentResidue = TaskManager.Instance.cleanedDirtSpots;
        int totalResidue = TaskManager.Instance.totalDirtSpots;
        CheckCategoryUpdate(ref lastResidue, ref lastResidueTotal, currentResidue, totalResidue, residueSlider, residueText, "Manchas");
    }

    private void CheckCategoryUpdate(ref int lastVal, ref int lastTotal, int current, int total, Slider slider, TMP_Text text, string label)
    {
        if (current != lastVal || total != lastTotal)
        {
            UpdateSlider(slider, text, current, total, label);
            
            // Notificación de completado (solo si acabamos de llegar al total y el total es > 0)
            if (current >= total && total > 0 && lastVal < total && lastVal != -1)
            {
                ShowCompletionNotification(label);
            }

            lastVal = current;
            lastTotal = total;
        }
    }

    private void UpdateSlider(Slider slider, TMP_Text text, int current, int total, string label)
    {
        if (slider != null)
        {
            // Asegurar que el maxValue sea correcto y mayor a 0
            float newMax = Mathf.Max(1, total);
            
            // Solo asignar si es diferente para evitar "dirty flags" internos de Unity UI
            if (Mathf.Abs(slider.maxValue - newMax) > 0.01f) 
            {
                slider.maxValue = newMax;
            }

            slider.value = current;
        }

        if (text != null)
        {
            text.text = $"{label}: {current}/{total}";
        }
    }

    private void UpdateTimerUI(float timeRemaining)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            timerText.color = timeRemaining <= 30f ? Color.red : Color.white;
        }
    }

    private void ShowCompletionNotification(string categoryName)
    {
        if (notificationText != null)
        {
            StopCoroutine("HideNotificationRoutine");
            notificationText.text = $"¡{categoryName} Completado!";
            notificationText.gameObject.SetActive(true);
            StartCoroutine(HideNotificationRoutine());
        }
        Debug.Log($"[UI] 🎉 Categoría completada: {categoryName}");
    }

    private IEnumerator HideNotificationRoutine()
    {
        yield return new WaitForSeconds(notificationDuration);
        if (notificationText != null) notificationText.gameObject.SetActive(false);
    }
}