using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class SelectedCharacterLoader : MonoBehaviour
{
    [SerializeField] Transform spawnPoint;
    [SerializeField] string playerTag = "Player";
    [SerializeField] CinemachineCamera vcam; // asigná tu vcam (CM3)

    // Ajusta si es necesario, pero "head" debería funcionar con la búsqueda por patrón
    [SerializeField] string[] anchorNames = { "CameraAnchor", "head", "mixamorig:Head" };

    void Awake() { if (!vcam) vcam = FindObjectOfType<CinemachineCamera>(); }

    IEnumerator Start()
    {
        // 1. LIMPIEZA INICIAL: Destruir el Player anterior y el Spot Light de la escena de selección
        foreach (var go in GameObject.FindGameObjectsWithTag(playerTag)) Destroy(go);

        // 🛑 NUEVO: DESTRUIR FOCOS DE LA ESCENA ANTERIOR
        // Esto previene interferencias. Busca cualquier luz tipo Spot y la destruye.
        foreach (var light in FindObjectsOfType<Light>())
        {
            if (light.type == LightType.Spot) Destroy(light.gameObject);
        }

        if (CharacterSelection.Instance == null) yield break;
        if (CharacterSelection.Instance.SelectedPrefab) CharacterSelection.Instance.DestroyPersistedIfAny();

        // 2. SPAWNEAR PLAYER
        Vector3 pos = spawnPoint ? spawnPoint.position : Vector3.zero;
        Quaternion rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        var player = CharacterSelection.Instance.GetOrSpawn(pos, rot);
        if (!player) yield break;

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
                // 🛑 CAMBIO CRÍTICO: DAMPING A CERO
                // Esto elimina el efecto de "flote" o "balanceo" al hacer que el seguimiento sea instantáneo (rígido).
                tpf.Damping = Vector3.zero;

                tpf.VerticalArmLength = 0f;
                tpf.CameraDistance = 3.5f;
            }
        }
    }

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

    // FUNCIÓN EDITADA: Busca por patrón (Contains) en lugar de coincidencia exacta (==)
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