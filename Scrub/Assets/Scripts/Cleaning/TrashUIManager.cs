using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TrashUIManager : MonoBehaviour
{
    // 📢 CRUCIAL: Referencia al GameObject del Panel COMPLETO que está inactivo (Arrastra el Panel aquí).
    [Header("Panel Principal")]
    [SerializeField] private GameObject uiPanelGameObject;

    [Header("Componentes UI")]
    [SerializeField] private TMP_Text trashCountText;
    [SerializeField] private Slider trashSlider;
    [SerializeField] private TMP_Text timerText; // Texto para el temporizador (debe ser asignado en el Manager y aquí)

    [Header("Retardo")]
    [Tooltip("Tiempo en segundos que el panel permanece visible al completar la tarea.")]
    [SerializeField] private float hideDelay = 3f;

    private CleaningManager manager;

    void Awake()
    {
        // Se ejecuta porque este script está en un objeto activo.
        // Asegura que el Panel referenciado (el hijo) esté inactivo al inicio.
        if (uiPanelGameObject != null)
        {
            uiPanelGameObject.SetActive(false);
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

        // 2. 📢 ACTIVACIÓN GARANTIZADA: Forzamos al Manager a enviar el estado inicial
        // Esto asegura que la UI se actualice inmediatamente con 0 / Total.
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
        CancelInvoke(nameof(HidePanel));
    }

    // Método privado que realmente oculta el panel
    private void HidePanel()
    {
        if (uiPanelGameObject != null)
        {
            uiPanelGameObject.SetActive(false);
        }
    }

    private void UpdateTrashUI(int cleanedCount, int totalCount)
    {
        // 1. LÓGICA DE ACTIVACIÓN/DESACTIVACIÓN DEL PANEL

        if (totalCount > 0 && cleanedCount < totalCount)
        {
            // Si hay basura pendiente, asegura la visibilidad.
            if (uiPanelGameObject != null && !uiPanelGameObject.activeSelf)
            {
                uiPanelGameObject.SetActive(true);
            }

            CancelInvoke(nameof(HidePanel));
        }
        else if (cleanedCount >= totalCount)
        {
            // Tarea completada: programa la ocultación.
            if (uiPanelGameObject != null && uiPanelGameObject.activeSelf)
            {
                if (!IsInvoking(nameof(HidePanel)))
                {
                    Invoke(nameof(HidePanel), hideDelay);
                    Debug.Log($"Tarea completada. Panel se ocultará en {hideDelay} segundos.");
                }
            }
        }

        // 2. Actualiza el Texto (Basura)
        if (trashCountText != null)
        {
            int remaining = Mathf.Max(0, totalCount - cleanedCount);
            trashCountText.text = $"{remaining} / {totalCount} Restante";
        }

        // 3. Actualiza el Slider
        if (trashSlider != null)
        {
            if (trashSlider.maxValue != totalCount)
            {
                trashSlider.maxValue = totalCount;
            }
            trashSlider.value = cleanedCount;
        }
    }

    // 📢 NUEVO: Función para actualizar el texto del tiempo
    private void UpdateTimerUI(float timeRemaining)
    {
        if (timerText != null)
        {
            // Formatea el tiempo a minutos y segundos
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);

            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            // Opcional: Cambia de color cuando el tiempo es bajo
            if (timeRemaining <= 30f)
            {
                timerText.color = Color.red;
            }
            else
            {
                timerText.color = Color.white; // Vuelve a blanco si sube el tiempo
            }
        }
    }
}