using UnityEngine;
using UnityEngine.SceneManagement;

public class BotonStart : MonoBehaviour
{
    [Tooltip("Nombre de la escena a cargar al presionar el botón.")]
    [SerializeField] private string nombreEscena = "CasaChick";

    /// <summary>
    /// Método para asignar al evento OnClick del botón en el Inspector.
    /// </summary>
    public void CargarEscena()
    {
        Debug.Log($"Iniciando juego... Cargando escena: {nombreEscena}");
        SceneManager.LoadScene(nombreEscena);
    }

    /// <summary>
    /// Método opcional para salir del juego (por si quieres reusar el script para un botón de Salir).
    /// </summary>
    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}
