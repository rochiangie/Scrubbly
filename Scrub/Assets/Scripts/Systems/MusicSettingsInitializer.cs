using UnityEngine;
using UnityEngine.UI; // Necesario para acceder al componente Toggle
using UnityEngine.Events; // Necesario para UnityAction (para la conexi�n de eventos)

public class MusicSettingsInitializer : MonoBehaviour
{
    private Toggle musicToggle;

    void Start()
    {
        // El AudioManager debe estar cargado, ya que este es un script de UI persistente.
        if (AudioManager.Instance == null)
        {
            Debug.LogError("El AudioManager no se ha cargado. No se puede inicializar la configuraci�n de m�sica.");
            return;
        }

        musicToggle = GetComponent<Toggle>();
        if (musicToggle == null)
        {
            Debug.LogError("Este GameObject requiere un componente Toggle para funcionar.");
            return;
        }

        // 1. Obtener el estado de silencio guardado.
        bool isMutedInPrefs = AudioManager.Instance.IsMusicMuted();

        // 2. Establecer el estado inicial del Toggle (Visual)
        // La l�gica del ToggleMusicMute es: TRUE = M�sica ON.
        // Si la m�sica est� silenciada (isMutedInPrefs es TRUE), el Toggle debe estar DESMARCADO (FALSE).
        musicToggle.isOn = !isMutedInPrefs;


        // 3. Conectar el evento del Toggle al AudioManager de forma segura.
        // Es crucial quitar listeners anteriores antes de a�adir el nuevo para evitar llamadas duplicadas.
        musicToggle.onValueChanged.RemoveAllListeners();

        // A�ade el listener. El estado 'isOn' del Toggle se pasar� autom�ticamente como 'bool'
        // a la funci�n ToggleMusicMute(bool isOn).
        musicToggle.onValueChanged.AddListener(AudioManager.Instance.ToggleMusicMute);

        Debug.Log($"[UI INITIALIZER] Toggle de m�sica inicializado. Estado: {(musicToggle.isOn ? "ON" : "OFF")}.");
    }
}