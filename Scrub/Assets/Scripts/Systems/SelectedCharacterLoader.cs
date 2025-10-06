using UnityEngine;
using System.Linq;

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
    public string FallbackCharacterID = "1";

    [Header("Available Characters")]
    public CharacterPrefabData[] AvailableCharacters;

    [Header("Referencias de Escena")]
    public Transform SpawnPoint;
    public string PlayerTag = "Player"; // ¡CRÍTICO! La cámara usará esta etiqueta.

    // ===============================================
    // LÓGICA DE CARGA
    // ===============================================

    void Awake()
    {
        // 1. Obtener el ID del personaje persistente
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

        // 3. Instanciar y asignar Tag (sin tocar la cámara)
        InstantiateCharacter(characterPrefab);
    }

    // ... (GetCharacterID y GetPrefabByID - Se mantienen igual) ...

    string GetCharacterID()
    {
        // Usa la función pública del Singleton GameDataController
        if (GameDataController.Instance != null)
        {
            string persistedID = GameDataController.Instance.GetCharacterID();
            Debug.Log($"[LOADER] Usando ID persistente: {persistedID}");
            return persistedID;
        }

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
        GameObject player = Instantiate(prefab, SpawnPoint.position, SpawnPoint.rotation);

        // ¡CRÍTICO! El script de cámara buscará esta etiqueta.
        player.tag = PlayerTag;

        Debug.Log("[LOADER] Personaje instanciado con Tag: " + PlayerTag);
    }
}