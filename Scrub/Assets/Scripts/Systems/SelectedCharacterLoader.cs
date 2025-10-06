using UnityEngine;
//using Cinemachine; // Importación necesaria para CinemachineVirtualCamera
using System.Linq; // Necesario para la función .FirstOrDefault()

public class SelectedCharacterLoader : MonoBehaviour
{
    // ===============================================
    // ESTRUCTURAS DE DATOS
    // ===============================================

    [System.Serializable]
    public class CharacterPrefabData
    {
        public string ID;
        public GameObject Prefab;
    }

    // ===============================================
    // VARIABLES DEL INSPECTOR
    // ===============================================

    [Header("Simulación / Fallback")]
    [Tooltip("ID a usar si GameDataController no se encuentra (ej: al iniciar en esta escena directamente).")]
    public string FallbackCharacterID = "1";

    [Header("Available Characters")]
    [Tooltip("Lista de todos los prefabs de personaje con sus IDs.")]
    public CharacterPrefabData[] AvailableCharacters;

    [Header("Referencias de Escena")]
    [Tooltip("El Transform donde aparecerá el personaje.")]
    public Transform SpawnPoint;
    public string PlayerTag = "Player";

    //[Header("Referencias de Cámara")]
    [Tooltip("La cámara virtual de Cinemachine que debe seguir al nuevo personaje.")]
    //public CinemachineVirtualCamera Vcam;

    // ===============================================
    // LÓGICA DE CARGA
    // ===============================================

    void Awake()
    {
        // 1. Obtener el ID del personaje persistente o el Fallback
        string characterId = GetCharacterID();

        // 2. Buscar el Prefab
        GameObject characterPrefab = GetPrefabByID(characterId);

        if (characterPrefab == null)
        {
            Debug.LogError($"[LOADER] No se encontró Prefab para el ID: {characterId}. Cargando Fallback.");
            characterPrefab = GetPrefabByID(FallbackCharacterID);
            if (characterPrefab == null)
            {
                Debug.LogError("[LOADER] El Prefab de Fallback tampoco se encontró. La carga falló.");
                return;
            }
        }

        // 3. Instanciar y configurar
        InstantiateCharacter(characterPrefab);
    }

    /// <summary>
    /// Obtiene el ID del personaje, usando GameDataController (persistente) si existe.
    /// </summary>
    string GetCharacterID()
    {
        // Usa la función pública del Singleton
        if (GameDataController.Instance != null)
        {
            string persistedID = GameDataController.Instance.GetCharacterID();
            Debug.Log($"[LOADER] Usando ID persistente: {persistedID}");
            return persistedID;
        }

        // Si el Singleton no se ha cargado (ej: iniciando en esta escena), usa el ID de simulación
        Debug.LogWarning($"[LOADER] GameDataController no encontrado. Usando ID de Fallback: {FallbackCharacterID}");
        return FallbackCharacterID;
    }

    private GameObject GetPrefabByID(string id)
    {
        // Busca el primer elemento que coincida con el ID
        var data = AvailableCharacters.FirstOrDefault(c => c.ID == id);
        return data?.Prefab;
    }

    private void InstantiateCharacter(GameObject prefab)
    {
        // Instancia el personaje en la posición del SpawnPoint
        GameObject player = Instantiate(prefab, SpawnPoint.position, SpawnPoint.rotation);

        // Asignar el Tag (si es necesario)
        player.tag = PlayerTag;

        // Configurar la cámara para que siga al nuevo personaje
        /*if (Vcam != null)
        {
            Vcam.Follow = player.transform;
            Vcam.LookAt = player.transform;
        }*/
    }
}