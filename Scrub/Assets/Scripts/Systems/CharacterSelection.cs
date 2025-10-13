using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelection : MonoBehaviour
{
    public static CharacterSelection Instance { get; private set; }

    public string SelectedCharacterID { get; private set; } = "";

    [SerializeField] private string preGameplaySceneName = "NombreDeTuEscenaPreJuego";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetSelectedID(string characterID)
    {
        SelectedCharacterID = characterID;
        PlayerPrefs.SetString("SelectedCharacter", characterID);
        PlayerPrefs.Save();

        Debug.Log($"[SELECTION] ✅ Guardado ID: {characterID}");

        // 🔥 NUEVO: Cambiar música inmediatamente al seleccionar personaje
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCharacterMusicImmediate(characterID);
        }
        else
        {
            Debug.LogWarning("[SELECTION] AudioManager no encontrado");
        }
    }

    public void ConfirmAndLoadGame()
    {
        if (string.IsNullOrEmpty(SelectedCharacterID))
        {
            Debug.LogError("[SELECTION] No hay personaje seleccionado.");
            return;
        }

        PlayerPrefs.SetString("SelectedCharacter", SelectedCharacterID);
        PlayerPrefs.Save();

        // 🔥 Asegurar que la música esté actualizada antes de cambiar escena
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCharacterMusicImmediate(SelectedCharacterID);
        }

        SceneManager.LoadScene(preGameplaySceneName);
    }

    public string GetSelectedID()
    {
        return SelectedCharacterID;
    }
}