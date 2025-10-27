using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TrashUIManager : MonoBehaviour
{
    // 📢 CRUCIAL: Referencia al GameObject del Panel COMPLETO.
    // Aunque ya no controlamos su visibilidad, la necesitamos para saber qué objeto contiene los componentes.
    [Header("Panel Principal")]
    [SerializeField] private GameObject uiPanelGameObject;

    [Header("Componentes UI")]
    [SerializeField] private TMP_Text trashCountText;
    [SerializeField] private Slider trashSlider;
    [SerializeField] private TMP_Text timerText;

    // 🛑 ELIMINAMOS EL RETARDO: Ya no se usa para ocultar el panel.
    [Header("Retardo")]
    [Tooltip("El retardo ya no se usa, el panel estará siempre visible.")]
    [SerializeField] private float hideDelay = 3f;

    private CleaningManager manager;

    void Awake()
    {
        // 🛑 ELIMINAMOS LA LÓGICA DE OCULTAR AL INICIO.
        // El panel debe estar activo en el Editor de Unity.
    }

    void Start()
    {
        // Busca la instancia del Manager.
        manager = FindObjectOfType<CleaningManager>();
        if (manager == null)
        {
            Debug.LogError("TrashUIManager no encontró el CleaningManager.");
        }

        // 📢 Configuración del Panel en el Editor
        if (uiPanelGameObject != null && !uiPanelGameObject.activeSelf)
        {
            Debug.LogWarning("El Panel de UI está inactivo en el Editor. Actívalo manualmente para que sea visible.");
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
        // 🛑 Eliminamos CancelInvoke(nameof(HidePanel));
    }

    // 🛑 Eliminamos el método HidePanel().

    private void UpdateTrashUI(int cleanedCount, int totalCount)
    {
        // 🛑 ELIMINAMOS TODA LA LÓGICA DE ACTIVACIÓN/DESACTIVACIÓN DEL PANEL.

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