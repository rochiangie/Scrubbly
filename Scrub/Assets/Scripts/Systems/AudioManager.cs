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

    [Range(0f, 1f)] public float gameplayMusicVolume = 0.1f;

    [Header("SFX")]
    public AudioClip cleanObjectSFX;
    public AudioClip pickupSFX;
    public AudioClip dropSFX;

    [Range(0f, 1f)] public float cleanSFXVolume = 1.0f;

    private const string MUSIC_TOGGLE_KEY = "MusicMuted";
    private const string SFX_TOGGLE_KEY = "SfxMuted";

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

        // Autoasignación si no se asignaron manualmente
        if (musicSource == null || sfxSource == null)
        {
            var sources = GetComponentsInChildren<AudioSource>();
            if (sources.Length > 0) musicSource = sources[0];
            if (sources.Length > 1) sfxSource = sources[1];
        }

        if (musicSource == null)
        {
            Debug.LogError("[AUDIO] No se encontró el AudioSource para música.");
            return;
        }

        if (sfxSource == null)
        {
            Debug.LogWarning("[AUDIO] No se encontró sfxSource. Usando musicSource para SFX.");
            sfxSource = musicSource;
        }

        // Inicializar el diccionario de música
        foreach (var pair in characterMusicList)
        {
            if (!characterMusicMap.ContainsKey(pair.characterID))
            {
                characterMusicMap.Add(pair.characterID, pair.musicClip);
                Debug.Log($"[AUDIO DEBUG] Registrado personaje: {pair.characterID} → {pair.musicClip?.name}");
            }
        }

        LoadSavedSettings();

        // 🔥 REPRODUCIR MÚSICA INICIAL INMEDIATAMENTE
        PlayMusic(defaultMusic);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[AUDIO] Escena cargada: {scene.name}");
        StopAllCoroutines();
        StartCoroutine(ExecuteMusicChangeNextFrame(scene));
    }

    private IEnumerator ExecuteMusicChangeNextFrame(Scene scene)
    {
        yield return null; // Esperar un frame para que todo esté inicializado

        string selectedID = PlayerPrefs.GetString("SelectedCharacter", "");
        Debug.Log($"[AUDIO] Cambiando música para escena: {scene.name} - Personaje: {selectedID}");

        // 🔥 LÓGICA MEJORADA PARA DETERMINAR QUÉ MÚSICA REPRODUCIR
        if (scene.name == "MainMenu" || string.IsNullOrEmpty(selectedID))
        {
            // Menú principal o sin personaje seleccionado → Música default
            musicSource.volume = 1.0f;
            PlayMusic(defaultMusic);
            Debug.Log($"[AUDIO] Reproduciendo música DEFAULT para escena: {scene.name}");
        }
        else
        {
            // Gameplay con personaje seleccionado → Música del personaje
            if (characterMusicMap.TryGetValue(selectedID, out AudioClip clip) && clip != null)
            {
                musicSource.volume = gameplayMusicVolume;
                PlayMusic(clip);
                Debug.Log($"[AUDIO] Reproduciendo música de PERSONAJE: {selectedID} para escena: {scene.name}");
            }
            else
            {
                Debug.LogWarning($"[AUDIO] Música no encontrada para {selectedID}. Usando default.");
                musicSource.volume = gameplayMusicVolume;
                PlayMusic(defaultMusic);
            }
        }
    }

    // 🔥 NUEVA FUNCIÓN: Cambiar música inmediatamente sin esperar carga de escena
    public void PlayCharacterMusicImmediate(string characterID)
    {
        if (string.IsNullOrEmpty(characterID))
        {
            Debug.LogWarning("[AUDIO] ID de personaje vacío");
            return;
        }

        Debug.Log($"[AUDIO] Solicitado cambio inmediato de música para: {characterID}");

        if (characterMusicMap.TryGetValue(characterID, out AudioClip clip) && clip != null)
        {
            musicSource.volume = gameplayMusicVolume;
            PlayMusic(clip);
            Debug.Log($"[AUDIO] 🎵 Música cambiada inmediatamente para: {characterID}");
        }
        else
        {
            Debug.LogWarning($"[AUDIO] No se encontró música para ID: {characterID}");
            // Si no encuentra la música del personaje, reproducir default pero con volumen de gameplay
            musicSource.volume = gameplayMusicVolume;
            PlayMusic(defaultMusic);
        }
    }

    // 🔥 FUNCIÓN COMPATIBILIDAD CON MULTIAUDIOMANAGER
    public void PlayCharacterByID(string characterID)
    {
        PlayCharacterMusicImmediate(characterID);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null)
        {
            Debug.LogWarning("[AUDIO] Clip de música nulo o musicSource no disponible");
            return;
        }

        // Solo cambiar si es diferente o no se está reproduciendo
        if (musicSource.clip != clip || !musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
            Debug.Log($"[AUDIO] 🎵 Reproduciendo: {clip.name}");
        }
        else
        {
            Debug.Log($"[AUDIO] La música {clip.name} ya se está reproduciendo");
        }
    }

    public void PlayCharacterMusic()
    {
        string selectedID = PlayerPrefs.GetString("SelectedCharacter", "");

        if (!string.IsNullOrEmpty(selectedID) && characterMusicMap.TryGetValue(selectedID, out AudioClip clip))
        {
            musicSource.volume = gameplayMusicVolume;
            PlayMusic(clip);
        }
        else
        {
            PlayMusic(defaultMusic);
        }
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

    private void StopAndPlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null || clip == null) return;

        sfxSource.Stop();
        sfxSource.PlayOneShot(clip, volume);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMusicMuteKeyboard();
        }
    }

    private void LoadSavedSettings()
    {
        musicSource.mute = PlayerPrefs.GetInt(MUSIC_TOGGLE_KEY, 0) == 1;
        sfxSource.mute = PlayerPrefs.GetInt(SFX_TOGGLE_KEY, 0) == 1;
        musicSource.volume = 1.0f;
    }

    public void ToggleMusicMuteKeyboard()
    {
        ApplyMuteState(!musicSource.mute);
    }

    public void ToggleMusicMute(bool isOn)
    {
        ApplyMuteState(!isOn);
    }

    private void ApplyMuteState(bool shouldBeMuted)
    {
        if (musicSource == null) return;

        musicSource.mute = shouldBeMuted;
        PlayerPrefs.SetInt(MUSIC_TOGGLE_KEY, shouldBeMuted ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[AUDIO] Música silenciada: {shouldBeMuted}");
    }

    public bool IsMusicMuted()
    {
        return musicSource != null && musicSource.mute;
    }

    public bool IsSfxMuted()
    {
        return sfxSource != null && sfxSource.mute;
    }

    public void ToggleSfxMute(bool isOn)
    {
        if (sfxSource == null) return;

        sfxSource.mute = !isOn;
        PlayerPrefs.SetInt(SFX_TOGGLE_KEY, !isOn ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[AUDIO] SFX silenciados: {!isOn}");
    }
}