using UnityEngine;

public class CharacterSelection : MonoBehaviour
{
    public static CharacterSelection Instance { get; private set; }

    public string selectedCharacterID;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[CHARACTER] ✅ Singleton creado: " + gameObject.name);
        }
        else if (Instance != this)
        {
            Debug.Log("[CHARACTER] ❌ Destruyendo duplicado: " + gameObject.name);
            Destroy(gameObject);
        }
    }

    public void SetSelectedID(string characterID)
    {
        if (string.IsNullOrEmpty(characterID))
        {
            Debug.LogError("[CHARACTER] ❌ ID de personaje inválido");
            return;
        }

        selectedCharacterID = characterID;
        PlayerPrefs.SetString("SelectedCharacter", characterID);
        PlayerPrefs.Save();

        Debug.Log($"[CHARACTER] ✅ Personaje guardado: {characterID}");

        // 🔴 OPCIONAL: Debug inmediato
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.DebugAudioStatus();
        }
    }
}