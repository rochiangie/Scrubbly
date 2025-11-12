using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
// Eliminamos using System.Collections; ya que no usamos corrutinas

public class TrashUIManager : MonoBehaviour
{
    // 📢 CRUCIAL: Referencia al GameObject del Panel COMPLETO.
    [Header("Panel Principal")]
    [SerializeField] private GameObject uiPanelGameObject;

    [Header("Componentes UI")]
    [SerializeField] private TMP_Text trashCountText;
    [SerializeField] private Slider trashSlider;
    [SerializeField] private TMP_Text timerText;

    // ELIMINAMOS TODA LA LÓGICA DE RETARDO
    [Header("Retardo")]
    [Tooltip("La lógica de ocultamiento está deshabilitada. El panel permanece visible.")]
    [SerializeField] private float hideDelay = 3f; // Se mantiene solo como campo, pero no se usa

    private CleaningManager manager;

    void Awake()
    {
        // Aseguramos que el panel esté visible al inicio
        if (uiPanelGameObject != null && !uiPanelGameObject.activeSelf)
        {
            uiPanelGameObject.SetActive(true); // <--- Asegura que el Canvas esté ACTIVO
        }
    }

    void Start()
    {
        // Busca la instancia del Manager.
        manager = FindObjectOfType<CleaningManager>();
        if (manager == null)
        {
            Debug.LogError("TrashUIManager no encontró el CleaningManager.");
        }
    }

    void OnEnable()
    {
        // 1. Suscripción a eventos.
        CleaningManager.OnTrashCountUpdated += UpdateTrashUI;
        CleaningManager.OnTimeUpdated += UpdateTimerUI;

        // 2. Activación Garantizada: Forzamos al Manager a enviar el estado inicial.
        if (manager != null)
        {
            manager.SendCurrentState();
        }
    }

    void OnDisable()
    {
        // 3. Limpieza y desuscripción.
        CleaningManager.OnTrashCountUpdated -= UpdateTrashUI;
        CleaningManager.OnTimeUpdated -= UpdateTimerUI;
        // Eliminamos la detención de la corrutina.
    }

    // 🛑 Eliminamos ScheduleHide() y HidePanelAfterDelay(float delay)

    private void UpdateTrashUI(int cleanedCount, int totalCount)
    {
        // 1. Actualiza el Texto (Basura)
        if (trashCountText != null)
        {
            int remaining = Mathf.Max(0, totalCount - cleanedCount);
            trashCountText.text = $"{remaining} / {totalCount} Restante";
        }

        // 2. Actualiza el Slider
        if (trashSlider != null)
        {
            if (trashSlider.maxValue != totalCount)
            {
                trashSlider.maxValue = totalCount;
            }
            trashSlider.value = cleanedCount;
        }
    }

    private void UpdateTimerUI(float timeRemaining)
    {
        if (timerText != null)
        {
            // Formatea el tiempo a minutos y segundos
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);

            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            // Lógica de color de tiempo bajo (mantenida para visualización)
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
