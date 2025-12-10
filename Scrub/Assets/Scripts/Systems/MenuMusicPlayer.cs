using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuMusicPlayer : MonoBehaviour
{
    // NO necesitamos que sea Singleton ni DontDestroyOnLoad si AudioManager ya lo es.
    // Lo simplificamos para que solo sirva de "disparador" si es necesario.

    [Header("Música del Menú")]
    public float menuMusicVolume = 0.3f; // Este valor ya no se usa, AudioManager tiene el control

    // El AudioSource y AudioClip son redundantes si AudioManager los maneja.

    void Start()
    {
        // 🚨 CORRECCIÓN: Llamamos al AudioManager Singleton para reproducir la música.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
            // Ya no necesitamos suscribirnos a sceneLoaded aquí, AudioManager ya lo hace.
        }
        else
        {
            Debug.LogError("[MENU MUSIC PLAYER] ❌ AudioManager.Instance es nulo. La música no se reproducirá.");
        }
    }

    // Eliminamos Awake, OnDestroy, InitializeAudio, OnSceneLoaded y StopMenuMusic 
    // ya que AudioManager maneja todos esos eventos y lógica de volumen.

    // Si solo quieres que el objeto exista y NO haga nada (lo cual es redundante):

    /*
    public void StopMenuMusic()
    {
        if (AudioManager.Instance != null)
        {
            // Nota: Asume que existe un método StopMusic en AudioManager
            // De lo contrario, usa StopCharacterMusic o PlayMenuMusic para forzar el cambio
            // AudioManager.Instance.StopMusic(); 
        }
    }
    */
}