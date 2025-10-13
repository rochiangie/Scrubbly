using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Música")]
    public AudioClip menuMusic; // Música del menú inicial
    public List<CharacterMusicPair> characterMusicList = new List<CharacterMusicPair>();
    private Dictionary<string, AudioClip> characterMusicMap = new Dictionary<string, AudioClip>();

    [Range(0f, 1f)] public float menuMusicVolume = 0.3f;
    [Range(0f, 1f)] public float gameplayMusicVolume = 0.15f;

    [Header("SFX")]
    public AudioClip cleanObjectSFX;
    public AudioClip pickupSFX;
    public AudioClip dropSFX;

    [Range(0f, 1f)] public float cleanSFXVolume = 0.8f;
    [Range(0f, 1f)] public float pickupSFXVolume = 0.7f;
    [Range(0f, 1f)] public float dropSFXVolume = 0.6f;

    // Control de estado
    private string currentCharacterID = "";
    private bool characterMusicStarted = false;
    private AudioClip currentMusicClip = null;

    // Constantes para PlayerPrefs
    private const string MUSIC_TOGGLE_KEY = "MusicMuted";

    [System.Serializable]
    public class CharacterMusicPair
    {
        public string characterID;
        public AudioClip musicClip;
    }

    void Awake()
    {
        // 🔴 SOLUCIÓN: Manejar Singleton correctamente
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Inicializar AudioSources
        if (musicSource == null) musicSource = GetComponent<AudioSource>();
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();

        // Configurar audio source
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        // Mapear música de personajes
        foreach (var pair in characterMusicList)
        {
            if (!characterMusicMap.ContainsKey(pair.characterID))
                characterMusicMap.Add(pair.characterID, pair.musicClip);
        }

        // Cargar configuración
        LoadSavedSettings();

        Debug.Log("[AUDIO] AudioManager inicializado correctamente");
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[AUDIO] Suscrito a SceneManager");
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Debug.Log("[AUDIO] Desuscrito de SceneManager");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name.ToLower();
        Debug.Log($"[AUDIO] Escena cargada: {sceneName}, characterMusicStarted: {characterMusicStarted}");

        // 🔴 SOLUCIÓN: Lógica simplificada y robusta
        if (sceneName.Contains("menu"))
        {
            // Reset al volver al menú
            characterMusicStarted = false;
            currentCharacterID = "";
            StartMenuMusic();
        }
        else if (sceneName.Contains("seleccion") || sceneName.Contains("select"))
        {
            // Selección - continuar música menú si no hay personaje
            if (!characterMusicStarted)
            {
                StartMenuMusic();
            }
        }
        else if (sceneName.Contains("lore") || sceneName.Contains("principal") || sceneName.Contains("gameplay"))
        {
            // Gameplay - usar música de personaje si está seleccionado
            if (characterMusicStarted && !string.IsNullOrEmpty(currentCharacterID))
            {
                // Reforzar música actual
                musicSource.volume = gameplayMusicVolume;
                if (!musicSource.isPlaying)
                {
                    PlayCharacterMusic(currentCharacterID);
                }
            }
        }

        // 🔴 SOLUCIÓN: Eliminar AudioListeners duplicados
        RemoveDuplicateAudioListeners();
    }

    // 🔴 NUEVO: Eliminar AudioListeners duplicados
    private void RemoveDuplicateAudioListeners()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length > 1)
        {
            Debug.LogWarning($"[AUDIO] Encontrados {listeners.Length} AudioListeners, eliminando duplicados...");

            // Mantener solo el primero, eliminar los demás
            for (int i = 1; i < listeners.Length; i++)
            {
                Destroy(listeners[i]);
                Debug.Log($"[AUDIO] Eliminado AudioListener duplicado: {listeners[i].gameObject.name}");
            }
        }
    }

    // ===============================================
    // CONTROL DE MÚSICA - SIMPLIFICADO
    // ===============================================

    public void StartMenuMusic()
    {
        if (menuMusic == null)
        {
            Debug.LogError("[AUDIO] MenuMusic no asignado en Inspector");
            return;
        }

        musicSource.volume = menuMusicVolume;
        PlayMusicInternal(menuMusic);
        characterMusicStarted = false;
        Debug.Log("[AUDIO] 🎵 Música de menú iniciada");
    }

    public void PlayCharacterMusic(string characterID)
    {
        if (string.IsNullOrEmpty(characterID))
        {
            Debug.LogError("[AUDIO] characterID es nulo o vacío");
            return;
        }

        AudioClip clipToPlay = null;

        if (characterMusicMap.TryGetValue(characterID, out AudioClip clip))
        {
            clipToPlay = clip;
        }
        else
        {
            Debug.LogWarning($"[AUDIO] No se encontró música para: {characterID}");
            return;
        }

        currentCharacterID = characterID;
        characterMusicStarted = true;
        musicSource.volume = gameplayMusicVolume;

        PlayMusicInternal(clipToPlay);
        Debug.Log($"[AUDIO] 🎵 Música de personaje: {characterID}");
    }

    private void PlayMusicInternal(AudioClip clip)
    {
        if (clip == null || musicSource == null)
        {
            Debug.LogError("[AUDIO] Clip o MusicSource nulo");
            return;
        }

        // Solo cambiar si es diferente
        if (musicSource.clip != clip || !musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.Play();
            currentMusicClip = clip;
            Debug.Log($"[AUDIO] Reproduciendo: {clip.name}");
        }
    }

    // ===============================================
    // SFX FUNCTIONS
    // ===============================================

    public void PlayCleanSFX()
    {
        if (sfxSource != null && cleanObjectSFX != null)
        {
            sfxSource.PlayOneShot(cleanObjectSFX, cleanSFXVolume);
            Debug.Log("[AUDIO] SFX Limpieza reproducido");
        }
    }

    public void PlayPickupSFX()
    {
        if (sfxSource != null && pickupSFX != null)
        {
            sfxSource.PlayOneShot(pickupSFX, pickupSFXVolume);
            Debug.Log("[AUDIO] SFX Pickup reproducido");
        }
    }

    public void PlayDropSFX()
    {
        if (sfxSource != null && dropSFX != null)
        {
            sfxSource.PlayOneShot(dropSFX, dropSFXVolume);
            Debug.Log("[AUDIO] SFX Drop reproducido");
        }
    }

    // ===============================================
    // TOGGLE/MUTE FUNCTIONS - CORREGIDO
    // ===============================================

    private void LoadSavedSettings()
    {
        if (musicSource != null)
        {
            // 0 = no muteado, 1 = muteado
            bool isMuted = PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 1;
            musicSource.mute = isMuted;
            Debug.Log($"[AUDIO] Configuración cargada: Mute = {isMuted}");
        }
    }

    // 🔴 CORRECCIÓN: Toggle funcionando correctamente
    public void ToggleMusic(bool musicOn)
    {
        // musicOn = true → música ACTIVADA → mute = false
        // musicOn = false → música DESACTIVADA → mute = true
        if (musicSource != null)
        {
            musicSource.mute = !musicOn;
            PlayerPrefs.SetInt(MUSIC_TOGGLE_KEY, musicSource.mute ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[AUDIO] Música: {(musicOn ? "ACTIVADA" : "DESACTIVADA")}");
        }
    }

    public bool IsMusicEnabled()
    {
        return PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 0;
    }
}