using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Configuración de Música")]
    public AudioSource musicSource;
    public AudioClip selectionMusic; // Música para la escena de selección/lore
    public AudioClip gameplayMusic;  // Música para la escena de limpieza

    private void Awake()
    {
        // 1. Si NO existe una instancia, ESTA es la instancia principal.
        if (Instance == null)
        {
            Instance = this;
            // 2. Le decimos a Unity que no lo destruya al cargar la próxima escena.
            DontDestroyOnLoad(gameObject); // << ESTA LÍNEA DEBE EJECUTARSE
        }
        else
        {
            // 3. Si YA existe una instancia, destruimos la nueva (la de esta escena).
            Destroy(gameObject);
            return; // Salimos de Awake para que no siga ejecutando más código.
        }
    }

    /// <summary>
    /// Cambia a una pista de música y comienza a reproducirla.
    /// </summary>
    /// <param name="newClip">El nuevo clip de audio.</param>
    public void PlayMusic(AudioClip newClip)
    {
        if (musicSource.clip == newClip)
        {
            // Ya estamos reproduciendo esta música, no hacemos nada.
            return;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();
    }

    // Opcional: Para cambiar música automáticamente cuando una escena se carga
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "NombreDeTuEscenaDeLimpieza") // Reemplaza con el nombre de tu 3ra escena
        {
            PlayMusic(gameplayMusic);
        }
        else if (scene.name == "NombreDeTuEscenaDeSeleccion") // Reemplaza con el nombre de tu 1ra escena
        {
            PlayMusic(selectionMusic);
        }
        // Nota: La música de selección seguirá sonando en la nueva escena de Lore.
    }
}