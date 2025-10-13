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
    [Tooltip("El ID que se usará si no se encuentra la clave en PlayerPrefs (ej: '1').")]
    public string FallbackCharacterID = "1";

    // 🔥 Clave CRÍTICA: Debe coincidir con la usada en CharacterSelection.cs
    private const string CHARACTER_KEY = "SelectedCharacter";

    [Header("Available Characters")]
    public CharacterPrefabData[] AvailableCharacters;

    [Header("Referencias de Escena")]
    [Tooltip("Punto donde se colocará el personaje instanciado.")]
    public Transform SpawnPoint;
    [Tooltip("Etiqueta asignada al personaje instanciado. (CRÍTICO para la cámara).")]
    public string PlayerTag = "Player";

    // ===============================================
    // LÓGICA DE CARGA
    // ===============================================

    void Awake()
    {
        // 1. Obtener el ID del personaje directamente de PlayerPrefs
        string characterId = GetCharacterID();

        // 2. Buscar el Prefab
        GameObject characterPrefab = GetPrefabByID(characterId);

        if (characterPrefab == null)
        {
            Debug.LogError($"[LOADER] No se encontró Prefab para el ID: {characterId}. Cargando Fallback ID.");

            // Intentamos cargar el Fallback
            characterPrefab = GetPrefabByID(FallbackCharacterID);

            if (characterPrefab == null)
            {
                Debug.LogError("[LOADER] El Prefab de Fallback tampoco se encontró. La carga falló.");
                return;
            }
        }

        // 3. Instanciar y asignar Tag
        InstantiateCharacter(characterPrefab);
    }

    /// <summary>
    /// 🔥 FUNCIÓN CORREGIDA: Obtiene el ID directamente de PlayerPrefs.
    /// </summary>
    string GetCharacterID()
    {
        // Lee la ID guardada por CharacterSelection, usando el Fallback si no hay nada.
        string persistedID = PlayerPrefs.GetString(CHARACTER_KEY, FallbackCharacterID);

        Debug.Log($"[LOADER] Usando ID persistente: {persistedID}");
        return persistedID;
    }

    private GameObject GetPrefabByID(string id)
    {
        // Busca el primer elemento que coincida con el ID
        var data = AvailableCharacters.FirstOrDefault(c => c.ID == id);
        return data?.Prefab;
    }

    private void InstantiateCharacter(GameObject prefab)
    {
        // Si no se asignó un punto de Spawn, usa la posición del Loader.
        Transform targetSpawn = SpawnPoint != null ? SpawnPoint : transform;

        GameObject player = Instantiate(prefab, targetSpawn.position, targetSpawn.rotation);

        // ¡CRÍTICO! El script de cámara buscará esta etiqueta.
        player.tag = PlayerTag;

        Debug.Log("[LOADER] Personaje instanciado con Tag: " + PlayerTag);
    }
}