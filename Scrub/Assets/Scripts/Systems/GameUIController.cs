using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameUIController : MonoBehaviour
{
    // [2025-10-16] Recuerda: proporciono la función completa según tu solicitud.
    [Header("Referencias UI")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;

    [Header("Configuración de Carga")]
    [Tooltip("El índice de la escena del Menú Principal en Build Settings (generalmente 0).")]
    public int menuSceneIndex = 0;

    [Tooltip("Tiempo de espera en segundos antes de cargar el menú principal (ej: 3.0 segundos).")]
    public float waitTimeBeforeMenu = 3.0f;

    void Start()
    {
        GameEvents.OnGameResult += ShowFinalScreen;

        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);

        // Aseguramos que el tiempo esté corriendo al inicio
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        GameEvents.OnGameResult -= ShowFinalScreen;
        // Siempre reanudar el tiempo al destruir la escena
        Time.timeScale = 1f;
    }

    // ===============================================
    // FUNCIÓN PRINCIPAL DE PANTALLA FINAL
    // ===============================================
    private void ShowFinalScreen(bool won)
    {
        // 1. Pausar el juego (Permite que el panel se vea estático)
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

        // 2. Iniciar la corrutina para esperar y cargar el menú
        StartCoroutine(WaitAndLoadMenu(waitTimeBeforeMenu));
    }

    // ===============================================
    // CORRUTINA DE ESPERA Y CARGA
    // ===============================================

    /// <summary>
    /// Espera un tiempo real, despausa el juego y carga la escena del menú.
    /// </summary>
    private IEnumerator WaitAndLoadMenu(float waitTime)
    {
        // 1. Esperar el tiempo configurado (contando en tiempo real, ignorando Time.timeScale = 0)
        yield return new WaitForSecondsRealtime(waitTime);

        // 2. Despausar el juego
        Time.timeScale = 1f;
        Debug.Log("[UI] Tiempo reanudado. Cargando Menú Principal.");

        // 3. Cargar la escena del menú principal.
        SceneManager.LoadScene(menuSceneIndex);
    }

    /// <summary>
    /// Función de utilidad, aunque ahora la carga es automática.
    /// Mantenida por si se usa en un botón 'Saltar espera'.
    /// </summary>
    public void LoadMainMenu()
    {
        // Si se llama desde un botón, detiene la corrutina y carga inmediatamente.
        StopAllCoroutines();

        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneIndex);
    }
}