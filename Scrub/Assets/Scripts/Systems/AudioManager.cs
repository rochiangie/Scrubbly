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
        // 1. Singleton: Si ya existe una instancia, nos destruimos.
        if (Instance == null)
        {
            Instance = this;
            // 2. Persistencia: Marca este objeto para que NO se destruya al cargar una nueva escena.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 3. Inicializar y empezar con la música de selección
        if (musicSource != null && selectionMusic != null)
        {
            musicSource.clip = selectionMusic;
            musicSource.loop = true; // Queremos que se repita la música
            musicSource.Play();
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