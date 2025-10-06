using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonToPrincipal : MonoBehaviour
{
    // Nombre de la escena de selecci�n de personajes
    private const string SELECTION_SCENE_NAME = "Principal";

    // Esta funci�n se debe asignar al evento OnClick() del bot�n
    public void GoToSelectionScene()
    {
        // Carga la escena principal donde se selecciona el personaje.
        // El GameDataController (que contiene el ID persistente) no se destruye.
        SceneManager.LoadScene(SELECTION_SCENE_NAME);

        Debug.Log("[NAVIGATOR] Regresando a la escena principal.");
    }
}