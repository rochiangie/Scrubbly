using UnityEngine;
using UnityEngine.UI;

public class MusicSettingsInitializer : MonoBehaviour
{
    private Toggle musicToggle;

    void Start()
    {
        // Verificar AudioManager
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

        // 🔴 CORRECCIÓN: Usar los métodos correctos del AudioManager actualizado
        // 1. Obtener el estado de la música
        bool isMusicEnabled = AudioManager.Instance.IsMusicEnabled();

        // 2. Establecer el estado inicial del Toggle
        // isMusicEnabled = true → música ACTIVADA → Toggle MARCADO (true)
        // isMusicEnabled = false → música DESACTIVADA → Toggle DESMARCADO (false)
        musicToggle.isOn = isMusicEnabled;

        // 3. Conectar el evento del Toggle
        musicToggle.onValueChanged.RemoveAllListeners();

        // 🔴 CORRECCIÓN: Usar ToggleMusic en lugar de ToggleMusicMute
        musicToggle.onValueChanged.AddListener(AudioManager.Instance.ToggleMusic);

        Debug.Log($"[UI INITIALIZER] Toggle de música inicializado. Estado: {(musicToggle.isOn ? "ON" : "OFF")}.");
    }
}