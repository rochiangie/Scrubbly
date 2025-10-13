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
    [Range(0f, 1f)] public float cleanSFXVolumeMultiplier = 0.6f;
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
            if (!isMenuMusic)
            {
                Debug.Log("[AUDIO] 🔄 Transición a Menú.");
                PlayMenuMusic();
            }
        }
        else if (sceneName.Contains("lore") || sceneName.Contains("principal") || sceneName.Contains("gameplay"))
        {
            StopAllCoroutines();
            StartCoroutine(CheckForCharacterMusicDelayed());
        }
    }

    private System.Collections.IEnumerator CheckForCharacterMusicDelayed()
    {
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
            sfxSource.volume = sfxVolumeGlobal;
        }
    }

    private void MapCharacterMusic()
    {
        characterMusicMap.Clear();
        foreach (var pair in characterMusicList)
        {
            if (!string.IsNullOrEmpty(pair.characterID) && pair.musicClip != null)
            {
                if (!characterMusicMap.ContainsKey(pair.characterID))
                    characterMusicMap.Add(pair.characterID, pair.musicClip);
                else
                    Debug.LogWarning($"[AUDIO] ID de personaje duplicado: {pair.characterID}. Ignorando.");
            }
            else
            {
                Debug.LogWarning("[AUDIO] Par de música de personaje inválido (ID vacío o Clip nulo).");
            }
        }
    }

    public string GetSelectedCharacterID()
    {
        // 1. Buscar en CharacterSelection (Singleton)
        if (CharacterSelection.Instance != null && !string.IsNullOrEmpty(CharacterSelection.Instance.selectedCharacterID))
        {
            string id = CharacterSelection.Instance.selectedCharacterID;
            PlayerPrefs.SetString(SELECTED_CHARACTER_KEY, id);
            PlayerPrefs.Save();
            return id;
        }

        // 2. Buscar en PlayerPrefs (Fallback/Persistencia)
        if (PlayerPrefs.HasKey(SELECTED_CHARACTER_KEY))
        {
            string characterID = PlayerPrefs.GetString(SELECTED_CHARACTER_KEY);
            if (!string.IsNullOrEmpty(characterID))
            {
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
        if (musicSource == null || menuMusic == null)
        {
            Debug.LogError("[AUDIO] ❌ No se puede reproducir música de menú. Source o Clip nulo.");
            return;
        }

        if (musicSource.clip == menuMusic && musicSource.isPlaying && !musicSource.mute)
        {
            if (musicSource.volume != menuMusicVolume) musicSource.volume = menuMusicVolume;
            return;
        }

        // Si está muteado, solo asignar clip y volumen base.
        if (musicSource.mute)
        {
            musicSource.clip = menuMusic;
            musicSource.volume = menuMusicVolume;
            isMenuMusic = true;
            currentCharacterID = "";
            Debug.Log("[AUDIO] 🎵 Música de menú asignada pero silenciada (Mute: True).");
            return;
        }

        // Reproducción normal
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
        if (musicSource == null)
        {
            Debug.LogError("[AUDIO] ❌ No se puede reproducir música de personaje. Source nulo.");
            return;
        }

        if (string.IsNullOrEmpty(characterID))
        {
            PlayMenuMusic();
            return;
        }

        if (characterID == currentCharacterID && musicSource.isPlaying && !isMenuMusic && !musicSource.mute)
        {
            return;
        }

        if (!characterMusicMap.TryGetValue(characterID, out AudioClip clipToPlay) || clipToPlay == null)
        {
            Debug.LogError($"[AUDIO] ❌ No hay música asignada/encontrada para: {characterID}. Fallback a menú.");
            PlayMenuMusic();
            return;
        }

        // Si está muteado, solo asignar el clip/volumen para que esté listo.
        if (musicSource.mute)
        {
            musicSource.clip = clipToPlay;
            musicSource.volume = gameplayMusicVolume;
            isMenuMusic = false;
            currentCharacterID = characterID;
            Debug.Log($"[AUDIO] 🎵 Música de personaje '{characterID}' asignada pero silenciada (Mute: True).");
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
    // SFX (Públicos) CON DEBUG DE FALLO
    // ===============================================

    public void PlayCleanSFX()
    {
        float finalVolume = sfxVolumeGlobal * cleanSFXVolumeMultiplier;

        if (sfxSource == null)
        {
            Debug.LogError("[AUDIO-SFX] ❌ FALLO DE SFX Limpieza: sfxSource es NULO. Asegura que AudioManager tiene el componente AudioSource.");
            return;
        }
        if (cleanObjectSFX == null)
        {
            Debug.LogError("[AUDIO-SFX] ❌ FALLO DE SFX Limpieza: cleanObjectSFX es NULO. ¡Asigna el Clip en el Inspector!");
            return;
        }
        if (sfxSource.volume * cleanSFXVolumeMultiplier <= 0.001f)
        {
            Debug.LogWarning($"[AUDIO-SFX] ⚠️ SFX Limpieza silenciado. Volumen final ({finalVolume:F2}) es casi cero. Ajusta sfxVolumeGlobal o cleanSFXVolumeMultiplier.");
            return;
        }

        sfxSource.PlayOneShot(cleanObjectSFX, finalVolume);
        Debug.Log($"[AUDIO-SFX] ✅ Limpieza OK (Vol: {finalVolume:F2}, Global: {sfxVolumeGlobal:F2}, Multi: {cleanSFXVolumeMultiplier:F2})");
    }

    public void PlayPickupSFX()
    {
        float finalVolume = sfxVolumeGlobal * pickupSFXVolumeMultiplier;

        if (sfxSource == null)
        {
            Debug.LogError("[AUDIO-SFX] ❌ FALLO DE SFX Recogida: sfxSource es NULO. Asegura que AudioManager tiene el componente AudioSource.");
            return;
        }
        if (pickupSFX == null)
        {
            Debug.LogError("[AUDIO-SFX] ❌ FALLO DE SFX Recogida: pickupSFX es NULO. ¡Asigna el Clip en el Inspector!");
            return;
        }
        if (sfxSource.volume * pickupSFXVolumeMultiplier <= 0.001f)
        {
            Debug.LogWarning($"[AUDIO-SFX] ⚠️ SFX Recogida silenciado. Volumen final ({finalVolume:F2}) es casi cero.");
            return;
        }

        sfxSource.PlayOneShot(pickupSFX, finalVolume);
        Debug.Log($"[AUDIO-SFX] ✅ Recogida OK (Vol: {finalVolume:F2})");
    }

    public void PlayDropSFX()
    {
        float finalVolume = sfxVolumeGlobal * dropSFXVolumeMultiplier;

        if (sfxSource == null)
        {
            Debug.LogError("[AUDIO-SFX] ❌ FALLO DE SFX Soltar: sfxSource es NULO. Asegura que AudioManager tiene el componente AudioSource.");
            return;
        }
        if (dropSFX == null)
        {
            Debug.LogError("[AUDIO-SFX] ❌ FALLO DE SFX Soltar: dropSFX es NULO. ¡Asigna el Clip en el Inspector!");
            return;
        }
        if (sfxSource.volume * dropSFXVolumeMultiplier <= 0.001f)
        {
            Debug.LogWarning($"[AUDIO-SFX] ⚠️ SFX Soltar silenciado. Volumen final ({finalVolume:F2}) es casi cero.");
            return;
        }

        sfxSource.PlayOneShot(dropSFX, finalVolume);
        Debug.Log($"[AUDIO-SFX] ✅ Soltar OK (Vol: {finalVolume:F2})");
    }

    // ===============================================
    // TOGGLE/MUTE Y DEBUG
    // ===============================================

    private void LoadSavedSettings()
    {
        if (musicSource != null)
        {
            // El valor por defecto 0 significa ON (no muteado). 1 significa OFF (muteado).
            bool isMuted = PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 1;
            musicSource.mute = isMuted;
            Debug.Log($"[AUDIO] Ajuste de inicio. Música Muted: {isMuted}");

            // Si comienza muteado, aseguramos que el volumen percibido sea 0
            if (isMuted) musicSource.volume = 0f;
        }
    }

    public void ToggleMusic(bool musicOn)
    {
        if (musicSource == null) return;

        musicSource.mute = !musicOn;
        PlayerPrefs.SetInt(MUSIC_TOGGLE_KEY, musicSource.mute ? 1 : 0);
        PlayerPrefs.Save();

        if (musicOn)
        {
            // 1. Música ON (Desmutear)
            float targetVolume = isMenuMusic ? menuMusicVolume : gameplayMusicVolume;
            musicSource.volume = targetVolume;

            // Si no está sonando, forzar el inicio
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }

            Debug.Log($"[AUDIO] Toggle Música: ON. Mute Status: {musicSource.mute}. Música reanudada con Vol: {targetVolume:F2}");
        }
        else // musicOn == false (Mutear/OFF)
        {
            // 2. Música OFF (Mutear)
            musicSource.volume = 0f;
            Debug.Log($"[AUDIO] Toggle Música: OFF. Mute Status: {musicSource.mute}. Música silenciada.");
        }
    }

    public bool IsMusicEnabled()
    {
        // El valor 0 significa que NO está muteado (ON).
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
        Debug.Log($"Is Menu Music: {isMenuMusic}");
        Debug.Log($"Current Character ID: {currentCharacterID}");

        Debug.Log($"--- Character Selection Check ---");
        if (CharacterSelection.Instance != null)
        {
            Debug.Log($"Selected Character (Instance): {CharacterSelection.Instance.selectedCharacterID}");
        }
        else
        {
            Debug.Log($"Selected Character (Instance): ❌ CharacterSelection.Instance es NULO");
        }
        Debug.Log($"Selected Character (PlayerPrefs): {PlayerPrefs.GetString(SELECTED_CHARACTER_KEY, "N/A")}");
        Debug.Log($"Music Muted (PlayerPrefs): {PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 1}");
        Debug.Log($"=====================================\n");
    }
}