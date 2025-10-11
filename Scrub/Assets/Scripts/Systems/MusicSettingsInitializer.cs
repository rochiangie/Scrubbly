using UnityEngine;
using UnityEngine.UI; // Necesario para acceder al componente Toggle
using UnityEngine.Events; // Necesario para UnityAction (para la conexión de eventos)

public class MusicSettingsInitializer : MonoBehaviour
{
    private Toggle musicToggle;

    void Start()
    {
        // El AudioManager debe estar cargado, ya que este es un script de UI persistente.
        if (AudioManager.Instance == null)
        {
            Debug.LogError("El AudioManager no se ha cargado. No se puede inicializar la configuración de música.");
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
        // La lógica del ToggleMusicMute es: TRUE = Música ON.
        // Si la música está silenciada (isMutedInPrefs es TRUE), el Toggle debe estar DESMARCADO (FALSE).
        musicToggle.isOn = !isMutedInPrefs;


        // 3. Conectar el evento del Toggle al AudioManager de forma segura.
        // Es crucial quitar listeners anteriores antes de añadir el nuevo para evitar llamadas duplicadas.
        musicToggle.onValueChanged.RemoveAllListeners();

        // Añade el listener. El estado 'isOn' del Toggle se pasará automáticamente como 'bool'
        // a la función ToggleMusicMute(bool isOn).
        musicToggle.onValueChanged.AddListener(AudioManager.Instance.ToggleMusicMute);

        Debug.Log($"[UI INITIALIZER] Toggle de música inicializado. Estado: {(musicToggle.isOn ? "ON" : "OFF")}.");
    }
}