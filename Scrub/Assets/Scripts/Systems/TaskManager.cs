using UnityEngine;

public class TaskManager : MonoBehaviour
{
    int totalDirt;
    int cleaned;

    [Header("UI y Paneles")]
    public TimedUIPanel notificationPanel;

    [Tooltip("El GameObject del panel de 'Ganaste' (donde está el script de temporizador).")]
    // Referencia al Panel de Victoria (debe estar inactivo al inicio)
    public GameObject winPanel;

    void Start()
    {
        // Seguridad: El panel de victoria debe estar desactivado al inicio.
        if (winPanel != null && winPanel.activeSelf)
        {
            winPanel.SetActive(false);
        }

        // Inicialización de la lógica
        totalDirt = FindObjectsOfType<DirtSpot>(true).Length;
        cleaned = 0;

        GameEvents.Progress(cleaned, totalDirt);
        GameEvents.OnAnyDirtCleaned += HandleCleaned;

        // Muestra la notificación inicial
        notificationPanel.ShowAndHide();
    }

    void OnDestroy()
    {
        GameEvents.OnAnyDirtCleaned -= HandleCleaned;
    }

    void HandleCleaned()
    {
        cleaned++;
        GameEvents.Progress(cleaned, totalDirt);

        if (cleaned >= totalDirt)
        {
            // 1. Llama al evento de finalización
            GameEvents.AllDone();

            // 2. Activa el Panel de Victoria
            // Esto dispara la función OnEnable() del script WinPanelController, 
            // iniciando la Corrutina de 10 segundos para cargar la escena de Menú.
            if (winPanel != null)
            {
                winPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("[TASK MANAGER] El Panel de Victoria (Win Panel) no está asignado. La escena no continuará.");
            }

            // 3. Desuscribirse para evitar que se ejecute de nuevo si se encontrara más suciedad (seguridad)
            GameEvents.OnAnyDirtCleaned -= HandleCleaned;
        }
    }
}