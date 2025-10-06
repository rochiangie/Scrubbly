using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Linq; // Necesario para .FirstOrDefault()

[System.Serializable]
public class CharacterPrefabData
{
    public string ID;               // Ej: "Player_Female"
    public GameObject Prefab;
}

public class SelectedCharacterLoader : MonoBehaviour
{
    [Header("Simulación / Fallback")]
    [Tooltip("ID a usar si se inicia la escena principal directamente.")]
    public string FallbackCharacterID = "Player_Female";
    [Tooltip("Lista de prefabs disponibles para spawnear.")]
    public CharacterPrefabData[] AvailableCharacters; // ¡Asigna tus prefabs aquí!

    [Header("Referencias de Escena")]
    [SerializeField] Transform spawnPoint;
    [SerializeField] string playerTag = "Player";
    [SerializeField] CinemachineCamera vcam;

    [Header("Configuración de Cámara")]
    [SerializeField] string[] anchorNames = { "CameraAnchor", "head", "mixamorig:Head" };

    void Awake()
    {
        if (!vcam) vcam = FindObjectOfType<CinemachineCamera>();
        if (!spawnPoint) spawnPoint = transform; // Usa este objeto si no hay punto de spawn
    }

    IEnumerator Start()
    {
        // 1. LIMPIEZA INICIAL: Destruir el Player anterior (si existe) y Focos
        foreach (var go in GameObject.FindGameObjectsWithTag(playerTag)) Destroy(go);

        // DESTRUIR FOCOS DE LA ESCENA ANTERIOR
        foreach (var light in FindObjectsOfType<Light>())
        {
            if (light.type == LightType.Spot) Destroy(light.gameObject);
        }

        // 🛑 Lógica eliminada: if (CharacterSelection.Instance == null) yield break;
        // 🛑 Lógica eliminada: if (CharacterSelection.Instance.SelectedPrefab) CharacterSelection.Instance.DestroyPersistedIfAny();

        // 2. SPAWNEAR PLAYER (Usando la lógica de simulación)

        // Determinar qué ID cargar (simulado o persistente)
        string charID = GetCharacterID();

        // Spawnea y obtiene el GameObject
        var player = SpawnCharacter(charID);

        if (!player)
        {
            Debug.LogError($"[LOADER] No se pudo spawnear el personaje con ID: {charID}. Verifica la lista 'Available Characters'.");
            yield break;
        }

        player.tag = playerTag;

        // Snap al piso 1 frame después (por colliders)
        yield return new WaitForFixedUpdate();
        SnapToGround(player.transform);

        // 3. CONFIGURAR CÁMARA
        if (vcam)
        {
            var anchor = FindAnchor(player.transform) ?? player.transform;
            vcam.Target.TrackingTarget = anchor;
            vcam.Target.LookAtTarget = anchor;

            var tpf = vcam.GetComponent<CinemachineThirdPersonFollow>();
            if (tpf)
            {
                // CRÍTICO: Damping a cero para el anclaje inicial
                tpf.Damping = Vector3.zero;

                tpf.VerticalArmLength = 0f;
                tpf.CameraDistance = 3.5f;
            }
        }
    }

    // ================== LÓGICA DE SELECCIÓN/SPAWN ==================

    /// <summary>
    /// Obtiene el ID del personaje, usando GameDataController (persistente) si existe, o el Fallback (simulación) si no.
    /// </summary>
    string GetCharacterID()
    {
        // Si tienes un GameDataController persistente, úsalo. (Se asume que GameDataController es el nuevo Singleton)
        if (GameDataController.Instance != null)
        {
            string persistedID = GameDataController.Instance.SelectedCharacterID;
            Debug.Log($"[LOADER] Usando ID persistente: {persistedID}");
            return persistedID;
        }

        // Si no, usar el ID de simulación
        Debug.LogWarning($"[LOADER] GameDataController no encontrado. Usando ID de Fallback: {FallbackCharacterID}");
        return FallbackCharacterID;
    }

    /// <summary>
    /// Spawnea el prefab en la posición definida.
    /// </summary>
    GameObject SpawnCharacter(string charID)
    {
        // Usamos LINQ para buscar el prefab por ID
        var charData = AvailableCharacters.FirstOrDefault(c => c.ID == charID);

        if (charData != null && charData.Prefab != null)
        {
            Vector3 pos = spawnPoint.position;
            Quaternion rot = spawnPoint.rotation;

            return Instantiate(charData.Prefab, pos, rot);
        }

        return null;
    }

    // ================== UTILITIES (Lógica de anclaje y Snap) ==================

    void SnapToGround(Transform t, float skin = 0.02f)
    {
        // (Lógica de SnapToGround sin cambios)
        Bounds b = default; bool has = false;
        foreach (var c in t.GetComponentsInChildren<Collider>())
        {
            if (!c.enabled) continue;
            if (!has) { b = c.bounds; has = true; }
            else b.Encapsulate(c.bounds);
        }
        if (!has)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }
        }
        if (!has) return;

        Vector3 origin = new Vector3(b.center.x, b.center.y + 50f, b.center.z);
        if (Physics.Raycast(origin, Vector3.down, out var hit, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            float deltaY = hit.point.y + skin - b.min.y;
            t.position += new Vector3(0f, deltaY, 0f);
        }
    }

    Transform FindAnchor(Transform root)
    {
        foreach (var n in anchorNames)
        {
            var f = FindDeep(root, n);
            if (f) return f;
        }
        return null;
    }

    Transform FindDeep(Transform r, string name)
    {
        if (r.name.Contains(name)) return r;
        for (int i = 0; i < r.childCount; i++)
        {
            var f = FindDeep(r.GetChild(i), name);
            if (f) return f;
        }
        return null;
    }
}