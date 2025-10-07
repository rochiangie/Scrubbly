using UnityEngine;

public class VictoryPanelController : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Arrastra aquí el GameObject raíz del Panel de Victoria/Fin de Juego.")]
    public GameObject victoryPanel;

    [Tooltip("Arrastra aquí el GameObject que contiene tu barra/Slider de progreso (HUD).")]
    public GameObject progressSliderObject;

    private void Start()
    {
        // Asegurarse de que el panel esté oculto al inicio
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("¡El Panel de Victoria no está asignado! Arrastra el panel al script.");
        }
    }

    // Esta función se llamará cuando se complete la condición de victoria (por el Dirt Manager).
    public void ShowVictoryPanel()
    {
        if (victoryPanel != null)
        {
            // 1. Ocultar el Slider/HUD
            if (progressSliderObject != null)
            {
                progressSliderObject.SetActive(false);
            }

            // 2. Mostrar el panel de victoria
            victoryPanel.SetActive(true);

            // 3. Pausar el juego y desbloquear el cursor
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Función para reanudar el juego (útil si la asignas a un botón "Reiniciar")
    public void RestartGame()
    {
        // Si tienes un botón de Reiniciar, asegúrate de reactivar el tiempo y recargar la escena
        Time.timeScale = 1f;
        // Unity.SceneManagement.SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }
}