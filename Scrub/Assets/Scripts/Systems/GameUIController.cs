using UnityEngine;

public class GameUIController : MonoBehaviour
{
    // [2025-10-16] Recuerda: proporciono la función completa según tu solicitud.
    [Header("Referencias UI")]
    // 📢 Asegúrate de arrastrar el GameObject de tu Panel de Victoria aquí.
    public GameObject victoryPanel;

    // 📢 Opcional: El panel de Derrota.
    public GameObject defeatPanel;

    void Start()
    {
        // 1. Suscribirse al evento de resultado final
        GameEvents.OnGameResult += ShowFinalScreen;

        // 2. Asegurarse de que los paneles estén ocultos al inicio
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
    }

    void OnDestroy()
    {
        // Cancelar la suscripción al destruir el objeto para evitar errores
        GameEvents.OnGameResult -= ShowFinalScreen;
    }

    /// <summary>
    /// Método llamado cuando el TaskManager determina el resultado final.
    /// </summary>
    private void ShowFinalScreen(bool won)
    {
        // Opcional: Pausar el juego para que el jugador interactúe con el panel.
        Time.timeScale = 0f;

        if (won)
        {
            Debug.Log("[UI] Activando Panel de Victoria.");
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
        }
        else
        {
            Debug.Log("[UI] Activando Panel de Derrota.");
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
            }
        }
    }
}