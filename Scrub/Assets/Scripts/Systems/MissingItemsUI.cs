// MissingItemsUI.cs
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text;

public class MissingItemsUI : MonoBehaviour
{
    [Header("UI Elementos")]
    [Tooltip("TMP_Text que muestra el tiempo restante (MM:SS).")]
    public TMP_Text timerText;

    [Tooltip("El GameObject contenedor que aparece/desaparece (ej. Panel_Lista).")]
    public GameObject missingItemsContainer;

    [Tooltip("TMP_Text que muestra la lista de objetos faltantes.")]
    public TMP_Text missingItemsListText;

    private bool listIsActive = false;
    private TaskManager taskManager;

    void Awake()
    {
        // 1. Suscribirse a los eventos
        GameEvents.ActivateMissingItemList += ShowMissingItemsList;

        // 2. Ocultar el panel de lista al inicio
        if (missingItemsContainer != null)
        {
            missingItemsContainer.SetActive(false);
        }
    }

    void Start()
    {
        // 3. Obtener la referencia al TaskManager (Singleton)
        taskManager = TaskManager.Instance;
        if (taskManager == null)
        {
            Debug.LogError("MissingItemsUI: TaskManager no se encontró. El tiempo y la lista no funcionarán.");
        }
    }

    void OnDestroy()
    {
        GameEvents.ActivateMissingItemList -= ShowMissingItemsList;
    }

    void Update()
    {
        // Actualizar el tiempo en cada frame
        if (taskManager != null && !taskManager.timeIsUp)
        {
            UpdateTimerDisplay(taskManager.currentTime);
        }

        // Opcional: Si la lista está activa, la actualizamos en cada frame (por si el nombre desaparece rápido)
        if (listIsActive && taskManager != null)
        {
            UpdateMissingItemsDisplay(taskManager.remainingItemNames);
        }
    }

    // =========================================================================
    // LÓGICA DE TIEMPO
    // =========================================================================

    private void UpdateTimerDisplay(float timeInSeconds)
    {
        if (timerText == null) return;

        // Calcular minutos y segundos (Formato MM:SS)
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // =========================================================================
    // LÓGICA DE LISTA DE OBJETOS (Llamada por GameEvents)
    // =========================================================================

    private void ShowMissingItemsList(List<string> items)
    {
        if (missingItemsContainer == null) return;

        listIsActive = true;
        missingItemsContainer.SetActive(true);

        // Llama a la función que dibuja el texto
        UpdateMissingItemsDisplay(items);
    }

    private void UpdateMissingItemsDisplay(List<string> items)
    {
        if (missingItemsListText == null) return;

        // Construye la lista de nombres para mostrar en el panel
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("FALTAN POR LIMPIAR:");

        // Muestra solo los 10 items que quedan
        foreach (string item in items)
        {
            sb.AppendLine($"- {item}");
        }

        missingItemsListText.text = sb.ToString();
    }
}