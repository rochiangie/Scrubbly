using UnityEngine;
using System.Linq;

// 🔴 ESTA CLASE ES CRUCIAL PARA LA MÚSICA DE PERSONAJE
// Debe ser un Singleton que persista entre la escena de Selección y la escena de Gameplay.
// El SpotlightSelector lo usa para guardar el ID. El AudioManager lo usa para leer el ID.
public class CharacterSelection : MonoBehaviour
{
    public static CharacterSelection Instance { get; private set; }

    [Header("Estado Persistente")]
    public string selectedCharacterID = "";
    private const string SELECTED_CHARACTER_KEY = "SelectedCharacter";

    void Awake()
    {
        // Implementación de Singleton estricta y persistente
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Cargar el ID al inicio, en caso de que volvamos a una escena de selección
        LoadSelectedID();

        Debug.Log("[SELECTION] CharacterSelection inicializado y persistente.");
    }

    public void SetSelectedID(string id)
    {
        selectedCharacterID = id;
        PlayerPrefs.SetString(SELECTED_CHARACTER_KEY, id);
        PlayerPrefs.Save();
        Debug.Log($"[SELECTION] ID de personaje guardado en Singleton/PlayerPrefs: {id}");
    }

    private void LoadSelectedID()
    {
        if (PlayerPrefs.HasKey(SELECTED_CHARACTER_KEY))
        {
            selectedCharacterID = PlayerPrefs.GetString(SELECTED_CHARACTER_KEY);
            Debug.Log($"[SELECTION] ID de personaje cargado al inicio: {selectedCharacterID}");
        }
    }
}