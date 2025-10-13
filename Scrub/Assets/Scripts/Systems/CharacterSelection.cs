using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelection : MonoBehaviour
{
    public static CharacterSelection Instance { get; private set; }

    public string SelectedCharacterID { get; private set; } = "";

    [SerializeField] private string preGameplaySceneName = "NombreDeTuEscenaPreJuego";
    private const string CHARACTER_KEY = "SelectedCharacter";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }

        Instance = this;

        // Carga el ID guardado al despertar, si existe (para iniciar el Spotlight)
        SelectedCharacterID = PlayerPrefs.GetString(CHARACTER_KEY, "");
    }

    /// <summary>
    /// Guarda el ID localmente, en PlayerPrefs, y notifica al AudioManager.
    /// Esta función es llamada por SpotlightSelector.
    /// </summary>
    public void SetSelectedID(string characterID)
    {
        SelectedCharacterID = characterID;

        // 1. Guardar la ID en el disco para la siguiente escena.
        PlayerPrefs.SetString(CHARACTER_KEY, characterID);
        PlayerPrefs.Save();

        Debug.Log($"[SELECTION] ✅ Personaje guardado: {characterID}");

        // 2. Notificar al AudioManager para un cambio de música inmediato.
        /*if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCharacterMusicImmediate(characterID);
        }*/
    }

    /// <summary>
    /// Función de transición: solo verifica y carga la siguiente escena (Confirmar).
    /// </summary>
    public void ConfirmAndLoadGame()
    {
        if (string.IsNullOrEmpty(SelectedCharacterID))
        {
            Debug.LogError("[SELECTION] No hay personaje seleccionado. No se puede cargar el juego.");
            return;
        }

        // La ID ya está guardada. Simplemente carga la escena.
        SceneManager.LoadScene(preGameplaySceneName);
    }

    public string GetSelectedID()
    {
        return SelectedCharacterID;
    }
}