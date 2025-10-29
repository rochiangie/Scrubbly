using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement; // ⬅️ Necesitas esto para cargar escenas

// 🔴 ESTA CLASE ES CRUCIAL PARA LA MÚSICA DE PERSONAJE
// Debe ser un Singleton que persista entre la escena de Selección y la escena de Gameplay.
// El SpotlightSelector lo usa para guardar el ID. El AudioManager lo usa para leer el ID.
public class CharacterSelection : MonoBehaviour
{
    public static CharacterSelection Instance { get; private set; }

    [Header("Estado Persistente")]
    public string selectedCharacterID = "";
    private const string SELECTED_CHARACTER_KEY = "SelectedCharacter";

    // 🟢 NUEVAS CONSTANTES para el mapeo de ID a Escena
    // Define los IDs de los dos personajes jugables y el nombre de sus escenas de destino.
    private const string ID_CHARACTER_PRINCIPAL = "9"; // ⬅️ Ejemplo: El ID del personaje que va a Principal
    private const string SCENE_PRINCIPAL = "Principal";

    private const string ID_CHARACTER_CHICK = "1"; // ⬅️ Ejemplo: El ID del personaje que va a CasaChick
    private const string SCENE_CASA_CHICK = "CasaChick";

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

    // 🌟 FUNCIÓN CLAVE: Lógica para cargar la escena basada en el personaje.
    /// <summary>
    /// Lee el ID seleccionado y carga la escena de juego correspondiente (Principal o CasaChick).
    /// Si el ID no coincide con un personaje jugable, no hace nada (asumiendo que los 7 restantes son "apagados").
    /// </summary>
    public void GoToGameScene()
    {
        string sceneToLoad = "";

        // Lógica de ruteo:
        if (selectedCharacterID == ID_CHARACTER_PRINCIPAL)
        {
            sceneToLoad = SCENE_PRINCIPAL;
            Debug.Log($"[SELECTION] Cargando escena: {SCENE_PRINCIPAL} para el personaje {ID_CHARACTER_PRINCIPAL}.");
        }
        else if (selectedCharacterID == ID_CHARACTER_CHICK)
        {
            sceneToLoad = SCENE_CASA_CHICK;
            Debug.Log($"[SELECTION] Cargando escena: {SCENE_CASA_CHICK} para el personaje {ID_CHARACTER_CHICK}.");
        }
        else
        {
            // Este es el caso de los 7 personajes "apagados"
            Debug.LogWarning($"[SELECTION] El personaje con ID: {selectedCharacterID} no tiene ruta de escena definida (personaje deshabilitado).");
            return; // No cargamos nada si es uno de los 7 restantes.
        }

        // Cargar la escena
        SceneManager.LoadScene(sceneToLoad);
    }
}