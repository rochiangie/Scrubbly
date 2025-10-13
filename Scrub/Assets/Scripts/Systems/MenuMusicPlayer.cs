using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuMusicPlayer : MonoBehaviour
{
    public static MenuMusicPlayer Instance;

    [Header("Música del Menú")]
    public AudioClip menuMusic;
    public float menuMusicVolume = 0.3f;

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayMenuMusic();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitializeAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = menuMusicVolume;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name.ToLower();
        Debug.Log($"[MENU MUSIC] Escena cargada: {sceneName}");

        // Detener música de menú en gameplay
        if (sceneName.Contains("lore") || sceneName.Contains("principal") || sceneName.Contains("gameplay"))
        {
            Debug.Log("[MENU MUSIC] 🛑 Deteniendo música de menú");
            StopMenuMusic();
            Destroy(gameObject, 1f);
        }
        // Continuar en menú/selección
        else if ((sceneName.Contains("menu") || sceneName.Contains("seleccion")) && !audioSource.isPlaying)
        {
            PlayMenuMusic();
        }
    }

    public void PlayMenuMusic()
    {
        if (menuMusic == null) return;

        if (audioSource.clip != menuMusic || !audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.clip = menuMusic;
            audioSource.Play();
            Debug.Log("[MENU MUSIC] 🎵 Reproduciendo música de menú");
        }
    }

    public void StopMenuMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("[MENU MUSIC] Música de menú detenida");
        }
    }
}