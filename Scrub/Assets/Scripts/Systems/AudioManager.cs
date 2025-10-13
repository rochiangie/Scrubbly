using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

// 🔊 Gestiona toda la reproducción de audio (Música y SFX) y persiste entre escenas.
public class AudioManager : MonoBehaviour
{
    // Singleton - Acceso global simple y seguro
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("Fuente para la música (debe ser loop: true)")]
    public AudioSource musicSource;
    [Tooltip("Fuente para efectos de sonido (PlayOneShot)")]
    public AudioSource sfxSource;

    [Header("Música")]
    public AudioClip menuMusic; // Música del menú/Selección
    public List<CharacterMusicPair> characterMusicList = new List<CharacterMusicPair>();
    private Dictionary<string, AudioClip> characterMusicMap = new Dictionary<string, AudioClip>();

    [Header("Volúmenes (Editor)")]
    [Range(0f, 1f)] public float menuMusicVolume = 0.3f;
    [Range(0f, 1f)] public float gameplayMusicVolume = 0.15f;
    [Range(0f, 1f)] public float sfxVolumeGlobal = 1.0f; // Volumen base para SFX

    [Header("SFX")]
    public AudioClip cleanObjectSFX;
    public AudioClip pickupSFX;
    public AudioClip dropSFX;

    // Volúmenes relativos de SFX (Multiplicadores)
    [Range(0f, 1f)] public float cleanSFXVolumeMultiplier = 0.4f;
    [Range(0f, 1f)] public float pickupSFXVolumeMultiplier = 0.7f;
    [Range(0f, 1f)] public float dropSFXVolumeMultiplier = 0.6f;

    // Control de estado
    private string currentCharacterID = "";
    private bool isMenuMusic = false;

    // Constantes
    private const string MUSIC_TOGGLE_KEY = "MusicMuted";
    private const string SELECTED_CHARACTER_KEY = "SelectedCharacter"; // Llave para PlayerPrefs
    private const float CHECK_CHARACTER_DELAY = 0.2f; // Espera para que otros Singletons carguen

    [System.Serializable]
    public class CharacterMusicPair
    {
        public string characterID;
        public AudioClip musicClip;
    }

    void Awake()
    {
        // 🚀 Implementación de Singleton estricta
        if (Instance != null)
        {
            if (Instance != this)
            {
                Destroy(gameObject);
                Debug.LogWarning("[AUDIO] ⛔ Instancia duplicada de AudioManager destruida.");
            }
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 🛠️ Inicializar AudioSources si faltan
        InitializeAudioSources();

        // 🗺️ Mapear música de personajes
        MapCharacterMusic();

        LoadSavedSettings();

        // Asignar el volumen global de SFX al sfxSource (puede cambiar en runtime)
        sfxSource.volume = sfxVolumeGlobal;

        Debug.Log("[AUDIO] ✅ AudioManager inicializado y persistente.");
    }

    void Start()
    {
        // 🎶 Iniciar con música de menú, asumiendo que la primera escena es un menú.
        PlayMenuMusic();
    }

    // ===============================================
    // GESTIÓN DE EVENTOS DE ESCENA
    // ===============================================

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name.ToLower();
        Debug.Log($"[AUDIO] Escena cargada: {sceneName}");

        if (sceneName.Contains("menu") || sceneName.Contains("seleccion"))
        {
            // Escenas de menú o selección - Siempre reproducir música de menú
            if (!isMenuMusic)
            {
                Debug.Log("[AUDIO] 🔄 Transición a Menú.");
                PlayMenuMusic();
            }
        }
        else if (sceneName.Contains("lore") || sceneName.Contains("principal") || sceneName.Contains("gameplay"))
        {
            // Escenas de Gameplay - Esperar y cargar música de personaje
            StopAllCoroutines();
            StartCoroutine(CheckForCharacterMusicDelayed());
        }
    }

    private System.Collections.IEnumerator CheckForCharacterMusicDelayed()
    {
        // Espera mínima para permitir que scripts como CharacterSelection inicialicen sus variables
        yield return new WaitForSeconds(CHECK_CHARACTER_DELAY);

        string characterID = GetSelectedCharacterID();

        if (!string.IsNullOrEmpty(characterID))
        {
            Debug.Log($"[AUDIO] 🔥 Intentando música del personaje: {characterID}");
            PlayCharacterMusic(characterID);
        }
        else
        {
            Debug.LogWarning("[AUDIO] ⚠️ No se pudo encontrar personaje seleccionado. Fallback a música de menú.");
            PlayMenuMusic();
        }
    }

    // ===============================================
    // FUNCIONES DE SOPORTE PRIVADAS
    // ===============================================

    private void InitializeAudioSources()
    {
        // Inicializar Music Source
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        // Inicializar SFX Source
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    private void MapCharacterMusic()
    {
        characterMusicMap.Clear();
        foreach (var pair in characterMusicList)
        {
            if (!characterMusicMap.ContainsKey(pair.characterID))
                characterMusicMap.Add(pair.characterID, pair.musicClip);
            else
                Debug.LogWarning($"[AUDIO] ID de personaje duplicado: {pair.characterID}. Ignorando.");
        }
    }

    private string GetSelectedCharacterID()
    {
        // 1. Buscar en CharacterSelection (Singleton)
        // Se asume la existencia de la clase CharacterSelection con una instancia estática 'Instance'
        // y una variable 'selectedCharacterID'.
        // ESTO ES CLAVE para el flujo de escenas.
        if (CharacterSelection.Instance != null && !string.IsNullOrEmpty(CharacterSelection.Instance.selectedCharacterID))
        {
            string id = CharacterSelection.Instance.selectedCharacterID;
            Debug.Log($"[AUDIO] Personaje de CharacterSelection (Actual): {id}");
            return id;
        }

        // 2. Buscar en PlayerPrefs (Fallback/Persistencia)
        if (PlayerPrefs.HasKey(SELECTED_CHARACTER_KEY))
        {
            string characterID = PlayerPrefs.GetString(SELECTED_CHARACTER_KEY);
            if (!string.IsNullOrEmpty(characterID))
            {
                Debug.Log($"[AUDIO] Personaje de PlayerPrefs (Fallback): {characterID}");
                return characterID;
            }
        }

        return null;
    }

    // ===============================================
    // CONTROL DE MÚSICA (Públicos)
    // ===============================================

    public void PlayMenuMusic()
    {
        if (musicSource == null || menuMusic == null) return;

        if (musicSource.clip == menuMusic && musicSource.isPlaying)
        {
            // Solo ajusta el volumen si no es el correcto, evita Stop/Play innecesarios
            if (musicSource.volume != menuMusicVolume) musicSource.volume = menuMusicVolume;
            return;
        }

        musicSource.Stop();
        musicSource.clip = menuMusic;
        musicSource.volume = menuMusicVolume;
        musicSource.Play();
        isMenuMusic = true;
        currentCharacterID = "";

        Debug.Log("[AUDIO] 🎵 Música de menú iniciada.");
    }

    public void PlayCharacterMusic(string characterID)
    {
        if (musicSource == null) return;
        if (string.IsNullOrEmpty(characterID))
        {
            PlayMenuMusic();
            return;
        }

        // Si ya está sonando la música correcta, salir
        if (characterID == currentCharacterID && musicSource.isPlaying && !isMenuMusic)
        {
            return;
        }

        // Busca el clip en el diccionario
        if (!characterMusicMap.TryGetValue(characterID, out AudioClip clipToPlay) || clipToPlay == null)
        {
            Debug.LogError($"[AUDIO] ❌ No hay música asignada/encontrada para: {characterID}. Fallback a menú.");
            PlayMenuMusic();
            return;
        }

        musicSource.Stop();
        musicSource.clip = clipToPlay;
        musicSource.volume = gameplayMusicVolume;
        musicSource.Play();
        isMenuMusic = false;
        currentCharacterID = characterID;

        Debug.Log($"[AUDIO] 🎵 Música de personaje iniciada: {characterID}");
    }

    // ===============================================
    // SFX (Públicos)
    // ===============================================

    public void PlayCleanSFX()
    {
        if (sfxSource != null && cleanObjectSFX != null)
        {
            float finalVolume = sfxVolumeGlobal * cleanSFXVolumeMultiplier;
            sfxSource.PlayOneShot(cleanObjectSFX, finalVolume);
            Debug.Log($"[AUDIO] SFX Limpieza (Vol: {finalVolume:F2})");
        }
    }

    public void PlayPickupSFX()
    {
        if (sfxSource != null && pickupSFX != null)
        {
            float finalVolume = sfxVolumeGlobal * pickupSFXVolumeMultiplier;
            sfxSource.PlayOneShot(pickupSFX, finalVolume);
            Debug.Log($"[AUDIO] SFX Pickup (Vol: {finalVolume:F2})");
        }
    }

    public void PlayDropSFX()
    {
        if (sfxSource != null && dropSFX != null)
        {
            float finalVolume = sfxVolumeGlobal * dropSFXVolumeMultiplier;
            sfxSource.PlayOneShot(dropSFX, finalVolume);
            Debug.Log($"[AUDIO] SFX Drop (Vol: {finalVolume:F2})");
        }
    }

    // ===============================================
    // TOGGLE/MUTE Y DEBUG
    // ===============================================

    private void LoadSavedSettings()
    {
        if (musicSource != null)
        {
            bool isMuted = PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 1;
            musicSource.mute = isMuted;
        }
    }

    public void ToggleMusic(bool musicOn)
    {
        if (musicSource != null)
        {
            musicSource.mute = !musicOn;
            PlayerPrefs.SetInt(MUSIC_TOGGLE_KEY, musicSource.mute ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[AUDIO] Toggle Música: {(musicOn ? "ON" : "OFF")}");
        }
    }

    public bool IsMusicEnabled()
    {
        // Por defecto (0) está ON.
        return PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 0;
    }

    // 🔴 Debug (útil para diagnosticar fallos de sonido)
    public void DebugAudioStatus()
    {
        Debug.Log($"\n\n=== AUDIO DEBUG STATUS ({Time.time:F2}) ===");
        Debug.Log($"Clip: {(musicSource?.clip != null ? musicSource.clip.name : "Ninguno")}");
        Debug.Log($"Is Playing: {musicSource?.isPlaying ?? false}");
        Debug.Log($"Volume (Music): {musicSource?.volume:F2} | SFX Global: {sfxVolumeGlobal:F2}");
        Debug.Log($"Mute: {musicSource?.mute ?? false}");
        Debug.Log($"Current Character ID: {currentCharacterID}");

        Debug.Log($"--- Character Selection Check ---");
        if (CharacterSelection.Instance != null)
        {
            Debug.Log($"Selected Character (Instance): {CharacterSelection.Instance.selectedCharacterID}");
        }
        Debug.Log($"Selected Character (PlayerPrefs): {PlayerPrefs.GetString(SELECTED_CHARACTER_KEY, "N/A")}");
        Debug.Log($"=====================================\n");
    }
}