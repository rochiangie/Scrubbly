using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameNavigator : MonoBehaviour
{
    // Nombre de la escena a la que quieres volver (Menú Principal)
    [Tooltip("El nombre exacto de la escena de menú principal")]
    public string mainMenuSceneName = "MenuPrincipal";

    /// <summary>
    /// Esta función debe ser llamada por un botón en el Panel de Victoria.
    /// </summary>
    public void QuitOrReturnToMenu()
    {
        // 🛑 PASO 1: Reanudar el tiempo ANTES de cargar la escena
        // (CRÍTICO: Si no haces esto, la próxima escena se cargará congelada).
        Time.timeScale = 1f;

        if (Application.isEditor)
        {
            // Si estás en el Editor de Unity, detenemos el modo Play (simula la salida)
            Debug.Log("[GAME OVER] ¡Cabaña limpia! Simulación de salida / Volver al Menú.");
            // UnityEditor.EditorApplication.isPlaying = false; // Comenta esta línea en el build final

            // Opcional: Cargar la escena de menú, si existe en el Editor
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            // 🛑 PASO 2: Si es un juego compilado, salimos o cargamos el menú
            if (string.IsNullOrEmpty(mainMenuSceneName))
            {
                // Si no se asignó un nombre de escena, cerramos la aplicación
                Application.Quit();
            }
            else
            {
                // Cargamos la escena de menú principal
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }
    }
}