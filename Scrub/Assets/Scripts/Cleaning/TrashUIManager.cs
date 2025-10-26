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

    [Header("Retardo")]
    [Tooltip("Tiempo en segundos que el panel permanece visible al completar la tarea.")]
    [SerializeField] private float hideDelay = 3f;

    private CleaningManager manager;

    void Awake()
    {
        // El Awake corre porque el script está en un objeto activo (Canvas).
        // Asegura que el Panel referenciado esté inactivo al inicio.
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
        // 1. Se suscribe al evento del manager.
        CleaningManager.OnTrashCountUpdated += UpdateTrashUI;

        // 2. 📢 ACTIVACIÓN GARANTIZADA: Forzamos al Manager a enviar el estado inicial.
        if (manager != null)
        {
            // Nota: Este método debe existir en CleaningManager.cs para enviar el estado.
            manager.SendCurrentState();
        }
    }

    void OnDisable()
    {
        // 3. Limpieza: Desuscribirse y cancelar invocaciones.
        CleaningManager.OnTrashCountUpdated -= UpdateTrashUI;
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
            // Si hay basura pendiente, asegúrate de que el panel esté ACTIVO.
            if (uiPanelGameObject != null && !uiPanelGameObject.activeSelf)
            {
                uiPanelGameObject.SetActive(true);
            }

            // Cancela cualquier ocultación pendiente
            CancelInvoke(nameof(HidePanel));
        }
        else if (cleanedCount >= totalCount)
        {
            // Tarea completada: programa la ocultación después del retardo.
            if (uiPanelGameObject != null && uiPanelGameObject.activeSelf)
            {
                if (!IsInvoking(nameof(HidePanel)))
                {
                    Invoke(nameof(HidePanel), hideDelay);
                    Debug.Log($"Tarea completada. Panel se ocultará en {hideDelay} segundos.");
                }
            }
        }

        // 2. Actualiza el Texto
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
}