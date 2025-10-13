using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonToPrincipal : MonoBehaviour
{
    // Nombre de la escena de selección de personajes (se mantiene para la función de retorno)
    private const string SELECTION_SCENE_NAME = "SeleccionPersonaje";

    // Nombre codificado de la escena de juego o "Principal"
    private const string PRINCIPAL_SCENE_NAME = "Principal";

    void Update()
    {
        // 🛑 Lógica CRÍTICA: Detectar la tecla Enter para iniciar el juego.
        HandleKeyboardNavigation();
    }

    private void HandleKeyboardNavigation()
    {
        // 1. Verificar si la tecla Enter/Intro fue presionada.
        // Si este script solo está activo en la escena de pre-juego (Escena 3),
        // no necesitamos verificar el nombre de la escena aquí.
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            GoToPrincipalScene();
        }
    }

    /// <summary>
    /// Función para iniciar el juego: Carga la escena 'Principal'.
    /// (Llamada por botón o por la tecla Enter/Intro).
    /// </summary>
    public void GoToPrincipalScene()
    {
        // Detiene todas las corrutinas que puedan estar corriendo en este GameObject.
        StopAllCoroutines();

        // Carga la escena principal del juego.
        SceneManager.LoadScene(PRINCIPAL_SCENE_NAME);

        Debug.Log($"[NAVIGATOR] Iniciando el juego en la escena: {PRINCIPAL_SCENE_NAME} (vía teclado/botón)");
    }

    /// <summary>
    /// Función para volver: Carga la escena de selección de personajes.
    /// Esta función se debe asignar al evento OnClick() del botón para REGRESAR.
    /// </summary>
    public void GoToSelectionScene()
    {
        StopAllCoroutines();

        // Carga la escena donde se selecciona el personaje.
        SceneManager.LoadScene(SELECTION_SCENE_NAME);

        Debug.Log("[NAVIGATOR] Regresando a la escena de selección.");
    }
}