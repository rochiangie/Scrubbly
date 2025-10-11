using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    // Singleton para acceso global
    public static AudioManager Instance;

    [Header("Configuración de Audio")]
    [Tooltip("El AudioSource que reproducirá la música. Debe estar en este GameObject.")]
    public AudioSource musicSource;

    // Constante para la clave de PlayerPrefs
    private const string MUSIC_TOGGLE_KEY = "MusicMuted";

    [Header("Música del Juego")]
    [Tooltip("Música por defecto para Menús, Selección, etc.")]
    public AudioClip defaultMusic;

    [Tooltip("La clave es el nombre/ID del personaje. El valor es el AudioClip de su música.")]
    // Lista para que los pares Personaje/Música sean editables en el Inspector
    public List<CharacterMusicPair> characterMusicList = new List<CharacterMusicPair>();
    private Dictionary<string, AudioClip> characterMusicMap = new Dictionary<string, AudioClip>();

    // Clase auxiliar para la visibilidad en el Inspector
    [System.Serializable]
    public class CharacterMusicPair
    {
        public string characterID;
        public AudioClip musicClip;
    }

    // ===========================================
    // AWAKE & CONFIGURACIÓN INICIAL
    // ===========================================
    private void Awake()
    {
        // 1. Implementación del Singleton Persistente
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ¡Permite que persista entre escenas!
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 2. Inicialización y chequeo del AudioSource
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        if (musicSource == null)
        {
            Debug.LogError("[AUDIO MANAGER] NO se encontró AudioSource. ¡El audio fallará!");
            return;
        }

        // 3. Rellenar el diccionario de personajes (para acceso rápido)
        foreach (var pair in characterMusicList)
        {
            if (!characterMusicMap.ContainsKey(pair.characterID))
            {
                characterMusicMap.Add(pair.characterID, pair.musicClip);
            }
        }

        // 4. Aplicar configuración guardada
        LoadSavedSettings();

        // 5. Empezar con la música por defecto
        if (defaultMusic != null)
        {
            PlayMusic(defaultMusic);
        }
    }

    private void LoadSavedSettings()
    {
        // Carga si la música estaba silenciada la última vez (1 = Silenciada, 0 = Activa)
        bool isMutedFromPrefs = PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 1;

        // Aplicamos el valor guardado al AudioSource
        musicSource.mute = isMutedFromPrefs;
        Debug.Log($"[AUDIO MANAGER] Configuración cargada: Música Silenciada = {isMutedFromPrefs}");
    }

    // ===========================================
    // SUSCRIPCIÓN DE ESCENAS
    // ===========================================
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Llamado automáticamente cuando se carga una nueva escena.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 🛑 Reemplaza "NombreDeTuEscenaDeGameplay" con el nombre real.
        if (scene.name == "NombreDeTuEscenaDeGameplay")
        {
            PlayCharacterMusic();
        }
        else // Asumimos que cualquier otra escena (Menú, Selección) usa la música por defecto
        {
            PlayMusic(defaultMusic);
        }
    }

    // ===========================================
    // LÓGICA DE REPRODUCCIÓN
    // ===========================================

    /// <summary>
    /// Cambia a una pista de música y comienza a reproducirla.
    /// </summary>
    public void PlayMusic(AudioClip newClip)
    {
        if (musicSource == null || newClip == null) return;

        // Evitar reiniciar la misma pista
        if (musicSource.clip == newClip && musicSource.isPlaying) return;

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.loop = true; // Aseguramos que repita
        musicSource.Play();
        Debug.Log($"[AUDIO MANAGER] Reproduciendo música: {newClip.name}");
    }

    /// <summary>
    /// Carga la música basada en el personaje seleccionado (lee PlayerPrefs).
    /// </summary>
    private void PlayCharacterMusic()
    {
        // 🛑 Clave de PlayerPrefs. Debe coincidir con la que usas para guardar la selección.
        string selectedCharacterID = PlayerPrefs.GetString("SelectedCharacter", "DEFAULT");

        AudioClip characterClip;
        if (characterMusicMap.TryGetValue(selectedCharacterID, out characterClip))
        {
            PlayMusic(characterClip);
        }
        else
        {
            Debug.LogWarning($"[AUDIO MANAGER] No se encontró música para el personaje: {selectedCharacterID}. Usando música por defecto.");
            PlayMusic(defaultMusic);
        }
    }

    // ===========================================
    // FUNCIONALIDAD DEL BOTÓN DE AJUSTES (SETTINGS)
    // ===========================================

    /// <summary>
    /// Alterna el estado de silenciado de la música y guarda la preferencia.
    /// Esta función debe conectarse al evento On Value Changed (Boolean) del componente Toggle.
    /// </summary>
    /// <param name="isOn">El valor booleano pasado por el Toggle. True = Marcado/Música ON.</param>
    public void ToggleMusicMute(bool isOn)
    {
        // 1. Invertir la lógica: Si el Toggle está 'isOn' (marcado), la música NO debe estar mute.
        bool shouldBeMuted = !isOn;

        musicSource.mute = shouldBeMuted;

        // 2. Guardar la preferencia
        // Guardamos el estado del silencio: 1 si está silenciado, 0 si está activo.
        int muteValue = shouldBeMuted ? 1 : 0;
        PlayerPrefs.SetInt(MUSIC_TOGGLE_KEY, muteValue);

        PlayerPrefs.Save();

        Debug.Log($"[AUDIO MANAGER] Música silenciada: {shouldBeMuted}. Preferencia guardada.");
    }

    /// <summary>
    /// Devuelve el estado de silenciado (1 = Silenciado, 0 = Activo). 
    /// Útil para inicializar el estado del Toggle de la UI.
    /// </summary>
    public bool IsMusicMuted()
    {
        // Devuelve TRUE si el valor guardado es 1 (Silenciado)
        return PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 1;
    }
}