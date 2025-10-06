using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    // Singleton para acceso global
    public static AudioManager Instance;

    [Header("Configuración de Música")]
    [Tooltip("El AudioSource que reproducirá la música. Debe estar en este GameObject.")]
    public AudioSource musicSource;

    public AudioClip selectionMusic; // Música para la escena de selección/lore
    public AudioClip gameplayMusic;  // Música para la escena de limpieza

    private void Awake()
    {
        // === 1. Implementación del Singleton Persistente ===
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[AUDIO MANAGER] Persistencia establecida. Este Manager NO se destruirá.");
        }
        else
        {
            // Si ya existe otra instancia, nos destruimos y salimos.
            Destroy(gameObject);
            return;
        }

        // === 2. Inicialización del AudioSource (Si es nulo) ===
        if (musicSource == null)
        {
            // Intentamos obtener el AudioSource automáticamente del mismo objeto.
            musicSource = GetComponent<AudioSource>();
        }

        if (musicSource == null)
        {
            Debug.LogError("[AUDIO MANAGER] NO se encontró el componente AudioSource en este GameObject. ¡El audio fallará!");
            return;
        }

        // 3. Empezar con la música de selección al inicio
        if (selectionMusic != null)
        {
            PlayMusic(selectionMusic);
            Debug.Log("[AUDIO MANAGER] Música de selección iniciada.");
        }
        else
        {
            Debug.LogWarning("[AUDIO MANAGER] El AudioClip 'Selection Music' está vacío. No se puede iniciar la música.");
        }
    }

    // El Audio Manager debe suscribirse al evento de carga de escena para cambiar la música.
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Cambia a una pista de música y comienza a reproducirla.
    /// </summary>
    public void PlayMusic(AudioClip newClip)
    {
        if (musicSource == null)
        {
            Debug.LogError("[AUDIO MANAGER] musicSource es nulo. No se puede reproducir la música.");
            return;
        }

        // Evitar reiniciar la misma pista.
        if (musicSource.clip == newClip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.loop = true; // Aseguramos que repita
        musicSource.Play();
        Debug.Log($"[AUDIO MANAGER] Reproduciendo música: {newClip.name}");
    }

    /// <summary>
    /// Llamado automáticamente cuando se carga una nueva escena.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 🛑 Importante: Reemplaza estos nombres con los nombres EXACTOS de tus escenas.
        if (scene.name == "NombreDeTuEscenaDeLimpieza")
        {
            PlayMusic(gameplayMusic);
        }
        else if (scene.name == "NombreDeTuEscenaDeLore") // Si creas la escena de Lore
        {
            // La música de selección/lore es la misma por defecto.
            PlayMusic(selectionMusic);
        }
    }
}