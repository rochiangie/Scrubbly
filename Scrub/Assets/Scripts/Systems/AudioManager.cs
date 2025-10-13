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
    public AudioClip defaultMusic;
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

    private const string MUSIC_TOGGLE_KEY = "MusicMuted";
    private const string SFX_TOGGLE_KEY = "SfxMuted";
    private const string CHARACTER_KEY = "SelectedCharacter";

    [System.Serializable]
    public class CharacterMusicPair
    {
        public string characterID;
        public AudioClip musicClip;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Autoasignación (Asume que MusicSource es el primer hijo y SfxSource el segundo)
        var sources = GetComponentsInChildren<AudioSource>();
        if (musicSource == null && sources.Length > 0) musicSource = sources[0];
        if (sfxSource == null && sources.Length > 1) sfxSource = sources[1];

        if (musicSource == null) { Debug.LogError("[AUDIO] No se encontró Music Source."); return; }
        if (sfxSource == null) { sfxSource = musicSource; Debug.LogWarning("[AUDIO] Usando Music Source para SFX."); }

        foreach (var pair in characterMusicList)
        {
            if (!characterMusicMap.ContainsKey(pair.characterID))
                characterMusicMap.Add(pair.characterID, pair.musicClip);
        }

        LoadSavedSettings();
        musicSource.volume = menuMusicVolume;

        // CORRECCIÓN: Iniciar música en la primera escena
        if (defaultMusic != null) PlayMusic(defaultMusic);
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Usamos la corrutina para asegurar que el PlayerPrefs esté disponible en el frame siguiente
        StopAllCoroutines();
        StartCoroutine(ExecuteMusicChangeNextFrame(scene));
    }

    private IEnumerator ExecuteMusicChangeNextFrame(Scene scene)
    {
        yield return null;

        string sceneName = scene.name.ToLower();
        string selectedID = PlayerPrefs.GetString(CHARACTER_KEY, "");

        float targetVolume;
        AudioClip targetClip;

        // 1. Menú/Selección (Volumen de Menú)
        if (sceneName.Contains("menu") || sceneName.Contains("seleccion"))
        {
            targetVolume = menuMusicVolume;
            targetClip = defaultMusic;
        }
        // 2. Gameplay/Lore (Volumen Bajo, Música de Personaje/Default)
        else if (sceneName.Contains("lore") || sceneName.Contains("principal") || sceneName.Contains("gameplay"))
        {
            targetVolume = gameplayMusicVolume;

            if (!string.IsNullOrEmpty(selectedID) && characterMusicMap.TryGetValue(selectedID, out AudioClip clip) && clip != null)
            {
                targetClip = clip;
            }
            else
            {
                targetClip = defaultMusic;
            }
        }
        else
        {
            targetVolume = menuMusicVolume;
            targetClip = defaultMusic;
        }

        // Aplicar el volumen ANTES de la reproducción y forzar el cambio.
        musicSource.volume = targetVolume;

        // Si la música ya fue iniciada por CharacterSelection, PlayMusic lo manejará sin superposición.
        PlayMusic(targetClip);
    }

    // ===============================================
    // FUNCIONES DE REPRODUCCIÓN Y CONTROL DE AUDIO
    // ===============================================

    /// <summary>
    /// Función para cambiar la música. FUERZA la detención para evitar superposición.
    /// </summary>
    // EN AudioManager.cs

    /// <summary>
    /// Función para cambiar la música. FUERZA la detención para evitar superposición.
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null)
        {
            Debug.LogWarning("[AUDIO] Clip nulo o MusicSource no disponible.");
            return;
        }

        // Si el clip que se pide es el mismo que está sonando, no hacemos nada.
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        // 🛑 CORRECCIÓN CRÍTICA: Detener SIEMPRE lo que esté sonando.
        // Esto mata la música anterior que se haya quedado pegada.
        musicSource.Stop();

        musicSource.clip = clip;
        musicSource.loop = true;

        // El volumen ya está configurado previamente.
        musicSource.Play();
        Debug.Log($"[AUDIO] 🎵 Reproduciendo: {clip.name} (volumen: {musicSource.volume})");
    }
    /// <summary>
    /// Función llamada desde CharacterSelection al confirmar. Fuerza el volumen bajo.
    /// </summary>
    public void PlayCharacterMusicImmediate(string characterID, float targetVolume)
    {
        musicSource.volume = targetVolume;

        if (characterMusicMap.TryGetValue(characterID, out AudioClip clip) && clip != null)
        {
            PlayMusic(clip);
        }
        else
        {
            PlayMusic(defaultMusic);
        }
    }

    // --- SFX Functions ---
    private void StopAndPlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.Stop();
        sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayCleanSFX() { StopAndPlaySFX(cleanObjectSFX, cleanSFXVolume); }
    public void PlayPickupSFX() { StopAndPlaySFX(pickupSFX, pickupSFXVolume); }
    public void PlayDropSFX() { StopAndPlaySFX(dropSFX, dropSFXVolume); }

    // ===============================================
    // LÓGICA DE SILENCIADO
    // ===============================================

    void Update() { if (Input.GetKeyDown(KeyCode.M)) ToggleMusicMuteKeyboard(); }

    private void LoadSavedSettings()
    {
        // Se asegura que los sources existan antes de mutear.
        if (musicSource != null) musicSource.mute = PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 1;
        if (sfxSource != null) sfxSource.mute = PlayerPrefs.GetInt(SFX_TOGGLE_KEY, 0) == 1;
        // Asume el volumen inicial de menú.
        musicSource.volume = 1.0f;
    }

    public bool IsMusicMuted() { return PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 1; }

    public void ToggleMusicMuteKeyboard() { ApplyMuteState(!musicSource.mute); }
    public void ToggleMusicMute(bool isOn) { ApplyMuteState(!isOn); }

    private void ApplyMuteState(bool shouldBeMuted)
    {
        if (musicSource == null) return;
        musicSource.mute = shouldBeMuted;
        PlayerPrefs.SetInt(MUSIC_TOGGLE_KEY, shouldBeMuted ? 1 : 0);
        PlayerPrefs.Save();
    }
}