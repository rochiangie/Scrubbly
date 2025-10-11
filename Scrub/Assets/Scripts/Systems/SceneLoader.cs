using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para gestionar escenas

public class SceneLoader : MonoBehaviour
{
    // Funci�n p�blica que se puede llamar desde el evento OnClick() de un bot�n
    // Acepta el nombre de la escena a la que queremos ir.
    public void CargarEscena(string nombreDeEscena)
    {
        Debug.Log("Cargando escena: " + nombreDeEscena);

        // Carga la escena por su nombre. 
        // El nombre debe coincidir EXACTAMENTE con el de la escena.
        SceneManager.LoadScene("SeleccionPersonaje");
    }
}