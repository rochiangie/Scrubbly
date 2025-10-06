using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonToPrincipal : MonoBehaviour
{
    // Nombre de la escena de selección de personajes (es una constante codificada)
    private const string SELECTION_SCENE_NAME = "SeleccionPersonaje";

    // 🛑 CAMBIO: Nombre codificado de la escena de juego o "Principal"
    private const string PRINCIPAL_SCENE_NAME = "Principal";

    // Hemos eliminado la variable pública TargetPrincipalSceneName

    /// <summary>
    /// Función para iniciar el juego: Carga la escena 'Principal'.
    /// Esta función se debe asignar al evento OnClick() del botón para INICIAR el juego.
    /// </summary>
    public void GoToPrincipalScene()
    {
        // Carga la escena principal del juego usando la constante.
        SceneManager.LoadScene(PRINCIPAL_SCENE_NAME);

        Debug.Log($"[NAVIGATOR] Iniciando el juego en la escena: {PRINCIPAL_SCENE_NAME}");
    }

    /// <summary>
    /// Función para volver: Carga la escena de selección de personajes.
    /// Esta función se debe asignar al evento OnClick() del botón para REGRESAR.
    /// </summary>
    public void GoToSelectionScene()
    {
        // Carga la escena donde se selecciona el personaje.
        SceneManager.LoadScene(SELECTION_SCENE_NAME);

        Debug.Log("[NAVIGATOR] Regresando a la escena de selección.");
    }
}