using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cargar escenas
using System.Collections; // Necesario para la corrutina (si se usa un temporizador)

public class GameUIController : MonoBehaviour
{
    // [2025-10-16] Recuerda: proporciono la función completa según tu solicitud.
    [Header("Referencias UI")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;

    [Header("Configuración de Carga")]
    [Tooltip("El índice de la escena del Menú Principal en Build Settings (generalmente 0).")]
    public int menuSceneIndex = 0;

    // Opcional: Si quieres un temporizador en lugar de un botón:
    // public float waitTimeBeforeMenu = 5f; 

    void Start()
    {
        // Suscribirse al evento de resultado final
        GameEvents.OnGameResult += ShowFinalScreen;

        // Asegurarse de que los paneles estén ocultos al inicio
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);

        // Asegúrate de que el tiempo esté corriendo al inicio del nivel
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        // Cancelar la suscripción al destruir el objeto
        GameEvents.OnGameResult -= ShowFinalScreen;

        // MUY IMPORTANTE: Asegurar que el tiempo se reanude si la escena de juego se destruye
        Time.timeScale = 1f;
    }

    private void ShowFinalScreen(bool won)
    {
        // 1. Pausar el juego (El panel se muestra y el juego se detiene)
        Time.timeScale = 0f;

        if (won)
        {
            Debug.Log("[UI] Activando Panel de Victoria y pausando el tiempo.");
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
        }
        else
        {
            Debug.Log("[UI] Activando Panel de Derrota y pausando el tiempo.");
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
            }
        }

        // Si quisieras un temporizador en lugar de un botón, descomentarías esto:
        // StartCoroutine(WaitAndLoadMenu(waitTimeBeforeMenu));
    }

    // ===============================================
    // FUNCIÓN CLAVE PARA LOS BOTONES DE LA UI
    // ===============================================

    /// <summary>
    /// Reanuda el tiempo y carga la escena del Menú Principal.
    /// Esta función debe ser llamada por un botón "Continuar" o "Menú Principal" 
    /// en el Panel de Victoria/Derrota.
    /// </summary>
    public void LoadMainMenu()
    {
        // 1. Reanudar el tiempo ANTES de cargar una nueva escena.
        Time.timeScale = 1f;

        // 2. Cargar la escena del menú principal.
        SceneManager.LoadScene(menuSceneIndex);
    }

    /* // Ejemplo de cómo sería la función con temporizador (si no usas botón)
    private IEnumerator WaitAndLoadMenu(float waitTime)
    {
        // Usar Time.unscaledDeltaTime para contar tiempo real mientras Time.timeScale = 0
        float timer = 0f;
        while (timer < waitTime)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // Después del tiempo de espera, reanudar y cargar el menú
        LoadMainMenu(); 
    }
    */
}