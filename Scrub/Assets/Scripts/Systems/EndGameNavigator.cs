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
        // Asegurar que los paneles estén ocultos al inicio
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
    }

    void OnEnable()
    {
        // 🛑 LÍNEA CLAVE: Suscribirse al evento que dispara el TaskManager
        GameEvents.OnGameResult += HandleGameResult;
    }

    void OnDisable()
    {
        GameEvents.OnGameResult -= HandleGameResult;
    }

    /// <summary>
    /// Llamado por el TaskManager cuando el juego termina (limpieza completada + score chequeado).
    /// </summary>
    private void HandleGameResult(bool won)
    {
        Debug.Log($"[EndGameNavigator] Resultado Final Recibido: {(won ? "VICTORIA" : "DERROTA")}. Activando panel.");

        // 1. Despausar el tiempo
        Time.timeScale = 1f;

        // 2. Mostrar la UI de Resultado
        if (won)
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
        }
        else
        {
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
            }
        }

        // Si usas un script para bloquear el mouse, puedes llamarlo aquí:
        // MouseLookController.Instance?.SetControlsActive(false); 
    }

    // Método público para usar en el botón "Volver al Menú Principal"
    public void GoToMainMenu()
    {
        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogError("Nombre de escena de menú no asignado.");
            return;
        }
        SceneManager.LoadScene(menuSceneName);
    }
}