using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class TaskManager : MonoBehaviour
{
    private int totalDirt = 0;
    private int cleanedCount = 0;

    [Header("UI y Paneles")]
    public TimedUIPanel notificationPanel;

    [Tooltip("El GameObject del panel de 'Ganaste' (debe estar inactivo al inicio).")]
    public GameObject winPanel;

    private const string WIN_PANEL_ERROR = "[TASK MANAGER] El Panel de Victoria (Win Panel) no está asignado. La escena no continuará.";

    void Awake()
    {
        // Encontrar objetos activos E inactivos, por si la suciedad se desactiva/reactiva.
        DirtSpot[] allDirtSpots = FindObjectsOfType<DirtSpot>(true);
        totalDirt = allDirtSpots.Length;

        // 🚨 CORRECCIÓN: Contar los spots que tienen IsCleaned = true. 
        // ESTO SÓLO FUNCIONA SI YA AGREGÓ LA PROPIEDAD 'IsCleaned' EN DirtSpot.cs.
        // Si no está seguro, póngalo a cero (cleanedCount = 0;).
        cleanedCount = allDirtSpots.Count(d => d.IsCleaned);

        Debug.Log($"[TASK MANAGER] Inicializado. Suciedad Total: {totalDirt}. Suciedad ya limpia: {cleanedCount}.");
    }

    void Start()
    {
        // ... (El resto del código de Start permanece igual) ...
        if (winPanel == null)
        {
            Debug.LogError(WIN_PANEL_ERROR);
        }
        else if (winPanel.activeSelf)
        {
            winPanel.SetActive(false);
        }

        GameEvents.OnAnyDirtCleaned += HandleCleaned;
        GameEvents.Progress(cleanedCount, totalDirt);

        if (notificationPanel != null)
        {
            notificationPanel.ShowAndHide();
        }
        else
        {
            Debug.LogWarning("[TASK MANAGER] Notification Panel es nulo, no se mostrará el mensaje inicial.");
        }

        if (totalDirt == 0)
        {
            Debug.LogWarning("[TASK MANAGER] No se encontró suciedad en la escena. Terminando inmediatamente.");
            HandleWinCondition();
        }
    }

    void OnDestroy()
    {
        GameEvents.OnAnyDirtCleaned -= HandleCleaned;
    }

    void HandleCleaned()
    {
        cleanedCount++;

        Debug.Log($"[TASK MANAGER] Suciedad limpiada: {cleanedCount} / {totalDirt}.");
        GameEvents.Progress(cleanedCount, totalDirt);

        if (cleanedCount >= totalDirt)
        {
            HandleWinCondition();
        }
    }

    private void HandleWinCondition()
    {
        GameEvents.OnAnyDirtCleaned -= HandleCleaned;

        // CORRECCIÓN: Llamar a AllDone sin argumentos, según GameEvents.cs
        GameEvents.AllDone();

        if (winPanel != null)
        {
            Debug.Log("[TASK MANAGER] 🎉 ¡Todas las tareas de limpieza completadas! Esperando veredicto sentimental...");
            // winPanel.SetActive(true); // Opcional: Desactivar esto. Dejar que SentimentalScoreManager muestre el panel de V/D.
        }
        else
        {
            Debug.LogError(WIN_PANEL_ERROR);
        }
    }
}