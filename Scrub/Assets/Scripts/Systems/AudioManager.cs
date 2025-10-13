using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    // Singleton para acceso global
    public static AudioManager Instance;

    [Header("Configuración de Audio")]
    [Tooltip("El AudioSource que reproducirá la música.")]
    public AudioSource musicSource;
    [Tooltip("El AudioSource que reproducirá los efectos de sonido (SFX).")]
    public AudioSource sfxSource; // Fuente dedicada a SFX cortos (para permitir el corte)

    // Constantes para las claves de PlayerPrefs
    private const string MUSIC_TOGGLE_KEY = "MusicMuted";
    private const string SFX_TOGGLE_KEY = "SfxMuted";

    [Header("Música del Juego")]
    [Tooltip("Música por defecto para Menús, Selección, etc.")]
    public AudioClip defaultMusic;
    public List<CharacterMusicPair> characterMusicList = new List<CharacterMusicPair>();
    private Dictionary<string, AudioClip> characterMusicMap = new Dictionary<string, AudioClip>();

    [Header("Efectos de Sonido (SFX)")]
    [Tooltip("SFX al limpiar un objeto.")]
    public AudioClip cleanObjectSFX;
    [Tooltip("SFX al recoger un objeto.")]
    public AudioClip pickupSFX;
    [Tooltip("SFX al soltar un objeto.")]
    public AudioClip dropSFX;

    [Range(0f, 1f)]
    [Tooltip("Volumen específico para el SFX de limpieza.")]
    public float cleanSFXVolume = 1.0f;

    // CONTROL DE VOLUMEN POR ESCENA
    [Range(0f, 1f)]
    [Tooltip("Volumen deseado para la música en la escena de Gameplay (ej: 0.5 para bajar el volumen).")]
    public float gameplayMusicVolume = 0.5f;

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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 2. Inicialización de AudioSources (Corregido: añade sfxSource si falta)
        if (musicSource == null) musicSource = GetComponent<AudioSource>();

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        if (musicSource == null || sfxSource == null)
        {
            Debug.LogError("[AUDIO MANAGER] Falta(n) AudioSource(s).");
        }

        // 3. Rellenar diccionario de personajes
        foreach (var pair in characterMusicList)
        {
            if (!characterMusicMap.ContainsKey(pair.characterID))
                characterMusicMap.Add(pair.characterID, pair.musicClip);
        }

        // 4. Aplicar configuración guardada (Volumen inicial 1.0f)
        LoadSavedSettings();
    }

    // ===========================================
    // CONTROL DE MÚSICA Y ESCENAS
    // ===========================================
    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 🛑 Lógica de control de volumen por escena y cambio de música
        if (scene.name == "NombreDeTuEscenaDeGameplay")
        {
            musicSource.volume = gameplayMusicVolume;
            PlayCharacterMusic();
        }
        else
        {
            musicSource.volume = 1.0f; // Volumen máximo para Menús/Selección
            PlayMusic(defaultMusic);
        }
    }

    public void PlayMusic(AudioClip newClip)
    {
        if (musicSource == null || newClip == null) return;

        // Corregido: Compara el volumen actual con el volumen objetivo de la escena
        float targetVolume = SceneManager.GetActiveScene().name == "NombreDeTuEscenaDeGameplay" ? gameplayMusicVolume : 1.0f;
        if (musicSource.clip == newClip && musicSource.isPlaying && Mathf.Approximately(musicSource.volume, targetVolume)) return;

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.volume = targetVolume; // Aplica el volumen antes de reproducir
        musicSource.Play();
        Debug.Log($"[AUDIO MANAGER] Reproduciendo música: {newClip.name}");
    }

    private void PlayCharacterMusic()
    {
        // CLAVE: Asegura que esta ID se lea correctamente desde PlayerPrefs (Ej: "SelectedCharacter")
        string selectedCharacterID = PlayerPrefs.GetString("SelectedCharacter", "DEFAULT");
        AudioClip characterClip;

        if (characterMusicMap.TryGetValue(selectedCharacterID, out characterClip))
        {
            PlayMusic(characterClip);
        }
        else
        {
            Debug.LogWarning($"[AUDIO MANAGER] No se encontró música para el personaje: {selectedCharacterID}. Usando por defecto.");
            PlayMusic(defaultMusic);
        }
    }

    // ===========================================
    // CONTROL DE SFX (Corte de Sonido Añadido)
    // ===========================================

    /// <summary>
    /// Detiene el sonido actual del sfxSource y reproduce uno nuevo.
    /// </summary>
    private void StopAndPlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null || clip == null) return;

        // CORRECCIÓN CLAVE: Detiene el sonido anterior inmediatamente.
        sfxSource.Stop();

        sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayCleanSFX()
    {
        StopAndPlaySFX(cleanObjectSFX, cleanSFXVolume);
    }

    public void PlayPickupSFX()
    {
        StopAndPlaySFX(pickupSFX);
    }

    public void PlayDropSFX()
    {
        StopAndPlaySFX(dropSFX);
    }

    // ===========================================
    // LÓGICA DE SILENCIADO
    // ===========================================

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) ToggleMusicMuteKeyboard();
    }

    private void LoadSavedSettings()
    {
        musicSource.mute = PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 1;
        sfxSource.mute = PlayerPrefs.GetInt(SFX_TOGGLE_KEY, 0) == 1;
        // Inicializa el volumen de la música en 1.0 (volumen predeterminado de menú)
        musicSource.volume = 1.0f;
    }

    public void ToggleMusicMuteKeyboard()
    {
        ApplyMusicMuteState(!musicSource.mute);
    }

    public void ToggleMusicMute(bool isOn)
    {
        ApplyMusicMuteState(!isOn);
    }

    private void ApplyMusicMuteState(bool shouldBeMuted)
    {
        musicSource.mute = shouldBeMuted;
        PlayerPrefs.SetInt(MUSIC_TOGGLE_KEY, shouldBeMuted ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[AUDIO MANAGER] Música silenciada: {shouldBeMuted}.");
    }

    public bool IsMusicMuted() { return PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 1; }
    public bool IsSfxMuted() { return PlayerPrefs.GetInt(SFX_TOGGLE_KEY, 0) == 1; }

    public void ToggleSfxMute(bool isOn)
    {
        sfxSource.mute = !isOn;
        PlayerPrefs.SetInt(SFX_TOGGLE_KEY, !isOn ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[AUDIO MANAGER] SFX silenciados: {!isOn}.");
    }
}