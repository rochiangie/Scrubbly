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
    public AudioClip menuMusic; // Música del menú
    public List<CharacterMusicPair> characterMusicList = new List<CharacterMusicPair>();
    private Dictionary<string, AudioClip> characterMusicMap = new Dictionary<string, AudioClip>();

    [Header("Volúmenes")]
    [Range(0f, 1f)] public float menuMusicVolume = 0.3f;
    [Range(0f, 1f)] public float gameplayMusicVolume = 0.15f;

    [Header("SFX")]
    public AudioClip cleanObjectSFX;
    public AudioClip pickupSFX;
    public AudioClip dropSFX;

    [Range(0f, 1f)] public float cleanSFXVolume = 0.4f;
    [Range(0f, 1f)] public float pickupSFXVolume = 0.7f;
    [Range(0f, 1f)] public float dropSFXVolume = 0.6f;

    // Control
    private string currentCharacterID = "";
    private bool isMenuMusic = true;

    // Constantes
    private const string MUSIC_TOGGLE_KEY = "MusicMuted";

    [System.Serializable]
    public class CharacterMusicPair
    {
        public string characterID;
        public AudioClip musicClip;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Inicializar AudioSources
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        // Mapear música de personajes
        foreach (var pair in characterMusicList)
        {
            if (!characterMusicMap.ContainsKey(pair.characterID))
                characterMusicMap.Add(pair.characterID, pair.musicClip);
        }

        LoadSavedSettings();

        Debug.Log("[AUDIO] AudioManager inicializado");
    }

    void Start()
    {
        // Iniciar con música de menú
        PlayMenuMusic();
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name.ToLower();
        Debug.Log($"[AUDIO] Escena cargada: {sceneName}");

        if (sceneName.Contains("menu") || sceneName.Contains("seleccion"))
        {
            // Menú o selección - música de menú
            if (!isMenuMusic)
            {
                PlayMenuMusic();
            }
        }
        else if (sceneName.Contains("lore") || sceneName.Contains("principal"))
        {
            // Gameplay - música de personaje
            StartCoroutine(CheckForCharacterMusicDelayed());
        }
    }

    private System.Collections.IEnumerator CheckForCharacterMusicDelayed()
    {
        yield return new WaitForSeconds(0.1f); // Pequeña espera

        string characterID = GetSelectedCharacterID();

        if (!string.IsNullOrEmpty(characterID))
        {
            Debug.Log($"[AUDIO] 🔥 Reproduciendo música del personaje: {characterID}");
            PlayCharacterMusic(characterID);
        }
        else
        {
            Debug.LogError("[AUDIO] ❌ No se pudo encontrar personaje seleccionado");
            // Fallback: continuar con música de menú
            PlayMenuMusic();
        }
    }

    private string GetSelectedCharacterID()
    {
        // 1. Buscar en CharacterSelection
        if (CharacterSelection.Instance != null)
        {
            Debug.Log($"[AUDIO] CharacterSelection encontrado: {CharacterSelection.Instance.selectedCharacterID}");
            if (!string.IsNullOrEmpty(CharacterSelection.Instance.selectedCharacterID))
            {
                return CharacterSelection.Instance.selectedCharacterID;
            }
        }

        // 2. Buscar en PlayerPrefs
        if (PlayerPrefs.HasKey("SelectedCharacter"))
        {
            string characterID = PlayerPrefs.GetString("SelectedCharacter");
            if (!string.IsNullOrEmpty(characterID))
            {
                Debug.Log($"[AUDIO] Personaje de PlayerPrefs: {characterID}");
                return characterID;
            }
        }

        Debug.LogWarning("[AUDIO] No se encontró characterID en ninguna fuente");
        return null;
    }

    // ===============================================
    // CONTROL DE MÚSICA
    // ===============================================

    public void PlayMenuMusic()
    {
        if (menuMusic == null)
        {
            Debug.LogError("[AUDIO] MenuMusic no asignado");
            return;
        }

        if (musicSource.clip == menuMusic && musicSource.isPlaying)
        {
            Debug.Log("[AUDIO] Música de menú ya está sonando");
            return;
        }

        musicSource.Stop();
        musicSource.clip = menuMusic;
        musicSource.volume = menuMusicVolume;
        musicSource.Play();
        isMenuMusic = true;
        currentCharacterID = "";

        Debug.Log("[AUDIO] 🎵 Música de menú iniciada");
    }

    public void PlayCharacterMusic(string characterID)
    {
        if (string.IsNullOrEmpty(characterID))
        {
            Debug.LogError("[AUDIO] characterID es nulo o vacío");
            PlayMenuMusic(); // Fallback
            return;
        }

        AudioClip clipToPlay = null;

        if (characterMusicMap.TryGetValue(characterID, out AudioClip clip))
        {
            clipToPlay = clip;
        }
        else
        {
            Debug.LogError($"[AUDIO] ❌ No hay música asignada para: {characterID}");
            // Mostrar qué characterIDs están disponibles
            Debug.Log("[AUDIO] CharacterIDs disponibles: " + string.Join(", ", characterMusicMap.Keys));
            PlayMenuMusic(); // Fallback
            return;
        }

        if (musicSource.clip == clipToPlay && musicSource.isPlaying)
        {
            Debug.Log($"[AUDIO] Música de {characterID} ya está sonando");
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
    // SFX
    // ===============================================

    public void PlayCleanSFX()
    {
        if (sfxSource != null && cleanObjectSFX != null)
        {
            sfxSource.PlayOneShot(cleanObjectSFX, cleanSFXVolume);
            Debug.Log("[AUDIO] SFX Limpieza");
        }
    }

    public void PlayPickupSFX()
    {
        if (sfxSource != null && pickupSFX != null)
        {
            sfxSource.PlayOneShot(pickupSFX, pickupSFXVolume);
            Debug.Log("[AUDIO] SFX Pickup");
        }
    }

    public void PlayDropSFX()
    {
        if (sfxSource != null && dropSFX != null)
        {
            sfxSource.PlayOneShot(dropSFX, dropSFXVolume);
            Debug.Log("[AUDIO] SFX Drop");
        }
    }

    // ===============================================
    // TOGGLE/MUTE
    // ===============================================

    private void LoadSavedSettings()
    {
        if (musicSource != null)
        {
            bool isMuted = PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 1;
            musicSource.mute = isMuted;
            Debug.Log($"[AUDIO] Mute: {isMuted}");
        }
    }

    public void ToggleMusic(bool musicOn)
    {
        if (musicSource != null)
        {
            musicSource.mute = !musicOn;
            PlayerPrefs.SetInt(MUSIC_TOGGLE_KEY, musicSource.mute ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[AUDIO] Música: {(musicOn ? "ON" : "OFF")}");
        }
    }

    public bool IsMusicEnabled()
    {
        return PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 0;
    }

    // 🔴 NUEVO: Método para debug
    public void DebugAudioStatus()
    {
        Debug.Log($"=== AUDIO DEBUG ===");
        Debug.Log($"Music Source: {musicSource}");
        Debug.Log($"Clip: {musicSource?.clip?.name}");
        Debug.Log($"Is Playing: {musicSource?.isPlaying}");
        Debug.Log($"Volume: {musicSource?.volume}");
        Debug.Log($"Mute: {musicSource?.mute}");
        Debug.Log($"Current Character ID: {currentCharacterID}");
        Debug.Log($"Is Menu Music: {isMenuMusic}");
        Debug.Log($"CharacterSelection Instance: {CharacterSelection.Instance != null}");
        if (CharacterSelection.Instance != null)
        {
            Debug.Log($"Selected Character: {CharacterSelection.Instance.selectedCharacterID}");
        }
    }
}