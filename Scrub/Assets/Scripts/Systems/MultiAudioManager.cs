using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MultiAudioManager : MonoBehaviour
{
    public static MultiAudioManager Instance;

    [Header("Música por personaje")]
    public List<AudioSource> characterAudioSources = new List<AudioSource>();

    [Header("Nombre exacto de los IDs (ej: '1', '2', '3')")]
    public List<string> characterIDs = new List<string>();

    [Header("Volumen por escena")]
    public float defaultVolume = 1f;
    public float gameplayVolume = 0.1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        StopAll();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string id = PlayerPrefs.GetString("SelectedCharacter", "");

        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[MULTI-AUDIO] ⚠️ No hay ID guardado al cargar escena. Se usará música por defecto.");
            return; // O podés reproducir un default si querés
        }

        Debug.Log($"[MULTI-AUDIO] Escena cargada: {scene.name} - Personaje seleccionado: {id}");
        PlayCharacterByID(id);
    }


    public void PlayCharacterByID(string id)
    {
        StopAll();

        int index = characterIDs.IndexOf(id);
        if (index >= 0 && index < characterAudioSources.Count)
        {
            var source = characterAudioSources[index];
            bool isGameplayScene = SceneManager.GetActiveScene().name == "NombreDeTuEscenaDeGameplay";
            source.volume = isGameplayScene ? gameplayVolume : defaultVolume;
            source.Play();
            Debug.Log($"[MULTI-AUDIO] ▶️ Reproduciendo música de personaje: {id}");
        }
        else
        {
            Debug.LogWarning($"[MULTI-AUDIO] ❌ No se encontró AudioSource para ID: {id}");
        }
    }

    public void StopAll()
    {
        foreach (var src in characterAudioSources)
        {
            if (src != null && src.isPlaying) src.Stop();
        }
    }
}
