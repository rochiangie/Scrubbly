using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class EndGameNavigator : MonoBehaviour
{
    // === Asigna estos GameObjects en el Inspector ===
    [Header("Referencias de UI Final")]
    [Tooltip("Panel completo que se muestra al ganar.")]
    public GameObject victoryPanel;
    [Tooltip("Panel completo que se muestra al perder.")]
    public GameObject defeatPanel;

    [Header("Configuración de Escena")]
    [Tooltip("Nombre de la escena a la que regresar (ej: 'MainMenu').")]
    public string menuSceneName = "MainMenu";

    void Awake()
    {
        Debug.Log("[EndGameNavigator] Awake - Inicializando...");
        // Asegurar que los paneles estén ocultos al inicio
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
    }

    void OnEnable()
    {
        // 🛑 LÍNEA CLAVE: Suscribirse al evento que dispara el TaskManager
        Debug.Log("[EndGameNavigator] Suscribiéndose a GameEvents.OnGameResult");
        GameEvents.OnGameResult += HandleGameResult;
    }

    void OnDisable()
    {
        Debug.Log("[EndGameNavigator] Desuscribiéndose de GameEvents.OnGameResult");
        GameEvents.OnGameResult -= HandleGameResult;
    }

    /// <summary>
    /// Llamado por el TaskManager cuando el juego termina (limpieza completada + score chequeado).
    /// </summary>
    private void HandleGameResult(bool won)
    {
        Debug.Log($"[EndGameNavigator] ========== RESULTADO: {(won ? "VICTORIA ✅" : "DERROTA ❌")} ==========");

        // 1. Pausar el juego
        Time.timeScale = 0f;
        Debug.Log("[EndGameNavigator] ⏸️ Juego pausado");

        // 2. Mostrar el panel correspondiente
        if (won)
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
                Debug.Log("[EndGameNavigator] ✅ Panel de VICTORIA activado");
                Debug.Log("[EndGameNavigator] ⏳ Esperando que el usuario presione un botón...");
            }
            else
            {
                Debug.LogError("[EndGameNavigator] ❌ victoryPanel no está asignado!");
            }
        }
        else
        {
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
                Debug.Log("[EndGameNavigator] ❌ Panel de DERROTA activado");
                Debug.Log("[EndGameNavigator] ⏳ Esperando que el usuario presione un botón...");
            }
            else
            {
                Debug.LogError("[EndGameNavigator] ❌ defeatPanel no está asignado!");
            }
        }

        // 🛑 NO SE CARGA NINGUNA ESCENA AUTOMÁTICAMENTE
        // Los botones en los paneles deben configurarse para llamar a:
        // - EndGameNavigator.GoToCredits() para ir a créditos
        // - EndGameNavigator.GoToMainMenu() para volver al menú
    }

    /// <summary>
    /// Método público para volver al menú principal (llamar desde un botón)
    /// </summary>
    public void GoToMainMenu()
    {
        Debug.Log("[EndGameNavigator] 🏠 Volviendo al menú principal...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    /// <summary>
    /// Método público para ir a créditos (llamar desde un botón)
    /// </summary>
    public void GoToCredits()
    {
        Debug.Log("[EndGameNavigator] 🎬 Yendo a créditos...");
        Time.timeScale = 1f;
        SceneManager.LoadScene("Credits");
    }
}