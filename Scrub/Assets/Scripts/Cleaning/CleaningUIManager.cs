using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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

    [SerializeField] private Slider residueSlider;
    [SerializeField] private TMP_Text residueText;

    [Header("3. Componentes de TIEMPO")]
    [SerializeField] private TMP_Text timerText;

    // Memoria para evitar actualizaciones innecesarias
    private int lastGlass = -1, lastGlassTotal = -1;
    private int lastPaper = -1, lastPaperTotal = -1;
    private int lastPlastic = -1, lastPlasticTotal = -1;
    private int lastHazardous = -1, lastHazardousTotal = -1;
    private int lastResidue = -1, lastResidueTotal = -1;
    private int lastTotalCleaned = -1, lastTotalCount = -1;

    void OnEnable()
    {
        GameEvents.OnProgressUpdate += UpdateUI;
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

        // 2. Actualizar Vidrio (Solo si cambió)
        int currentGlass = TaskManager.Instance.cleanedGlass;
        int totalGlass = TaskManager.Instance.totalGlass;
        if (currentGlass != lastGlass || totalGlass != lastGlassTotal)
        {
            UpdateSlider(glassSlider, glassText, currentGlass, totalGlass, "Vidrio");
            lastGlass = currentGlass;
            lastGlassTotal = totalGlass;
        }

        // 3. Actualizar Papel
        int currentPaper = TaskManager.Instance.cleanedPaper;
        int totalPaper = TaskManager.Instance.totalPaper;
        if (currentPaper != lastPaper || totalPaper != lastPaperTotal)
        {
            UpdateSlider(paperSlider, paperText, currentPaper, totalPaper, "Papel / cartón");
            lastPaper = currentPaper;
            lastPaperTotal = totalPaper;
        }

        // 4. Actualizar Plástico
        int currentPlastic = TaskManager.Instance.cleanedPlastic;
        int totalPlastic = TaskManager.Instance.totalPlastic;
        if (currentPlastic != lastPlastic || totalPlastic != lastPlasticTotal)
        {
            UpdateSlider(plasticSlider, plasticText, currentPlastic, totalPlastic, "Plásticos");
            lastPlastic = currentPlastic;
            lastPlasticTotal = totalPlastic;
        }

        // 5. Actualizar Peligrosos
        int currentHazardous = TaskManager.Instance.cleanedHazardous;
        int totalHazardous = TaskManager.Instance.totalHazardous;
        if (currentHazardous != lastHazardous || totalHazardous != lastHazardousTotal)
        {
            UpdateSlider(hazardousSlider, hazardousText, currentHazardous, totalHazardous, "Peligrosos");
            lastHazardous = currentHazardous;
            lastHazardousTotal = totalHazardous;
        }

        // 6. Actualizar Residuos (Manchas + Bolsas)
        int currentResidue = TaskManager.Instance.cleanedDirtSpots + TaskManager.Instance.cleanedBolsas;
        int totalResidue = TaskManager.Instance.totalDirtSpots + TaskManager.Instance.totalBolsas;
        if (currentResidue != lastResidue || totalResidue != lastResidueTotal)
        {
            UpdateSlider(residueSlider, residueText, currentResidue, totalResidue, "Residuos/bolsas");
            lastResidue = currentResidue;
            lastResidueTotal = totalResidue;
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
}