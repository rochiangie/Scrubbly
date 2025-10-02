using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // soporte opcional para el nuevo Input System
#endif

[RequireComponent(typeof(Animator))]
public class CleaningController : MonoBehaviour
{
    // ---------------- Refs ----------------
    [Header("Refs")]
    [SerializeField] private Camera rayCamera;     // Main Camera
    [SerializeField] private Transform holdPoint;  // mano del player (origen visual)
    [SerializeField] private Animator anim;        // Animator del Player

    // ---------------- Capas y rangos ----------------
    [Header("Layers & Ranges")]
    [SerializeField] private LayerMask toolsLayer; // capa de herramientas (Tools)
    [SerializeField] private LayerMask dirtLayer;  // capa de suciedad (Interactable/Dirt)
    [SerializeField] private float pickupRange = 3.5f;
    [SerializeField] private float cleanRange = 2.5f;

    // ---------------- Input ----------------
    [Header("Input (teclas simples)")]
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private KeyCode cleanKey = KeyCode.R;

    // ---------------- Limpieza ----------------
    [Header("Cleaning")]
    [SerializeField] private float baseCleanRate = 1f;     // trabajo/seg
    [SerializeField] private bool requireCorrectTool = false; // valida DirtSpot.requiredToolId
    [SerializeField] private string[] validToolIds;        // opcional: restringir herramientas válidas

    // ---------------- Aim Assist para superficies bajas ----------------
    [Header("Aim assist (low surfaces)")]
    [SerializeField] private bool aimFromHand = true;       // origen desde la mano (si hay)
    [SerializeField, Range(0f, 0.75f)]
    private float downBias = 0.35f;                          // inclinación hacia abajo (0 = recto)
    [SerializeField] private float handForwardOffset = 0.12f;// empuja el origen hacia adelante
    [SerializeField] private float sphereRadius = 0.22f;     // grosor del ray al limpiar
    [SerializeField] private float overlapRadius = 0.28f;    // radio de búsqueda cercana

    // ---------------- Animación (capa "Clean") ----------------
    [Header("Animation Layer")]
    [SerializeField] private string cleaningLayerName = "Clean"; // nombre EXACTO de la capa
    [SerializeField] private float layerBlendSpeed = 12f;        // suavizado del peso
    [SerializeField] private bool useCrossFade = false;         // si NO tenés transiciones en la capa
    [SerializeField] private string cleaningStateName = "Clean_Loop";      // estado de limpiar en capa Clean
    [SerializeField] private string upperIdleStateName = "UpperBody_Idle"; // estado idle en capa Clean

    // ---------------- Debug ----------------
    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    // ---------------- Estado ----------------
    public ToolDescriptor CurrentTool { get; private set; }
    private int cleaningLayerIndex = -1;
    private bool prevCleaning = false;
    private int cleanHash = 0, idleHash = 0;
    private bool loggedCleanMissing = false, loggedIdleMissing = false;

    // ================== Unity ==================
    private void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (!rayCamera && Camera.main) rayCamera = Camera.main;

        if (anim)
        {
            cleaningLayerIndex = anim.GetLayerIndex(cleaningLayerName);
            if (cleaningLayerIndex < 0)
                DLogWarn($"[Anim] No encontré la capa '{cleaningLayerName}'. Verificá el nombre en el Animator.");
            else
                DLog($"[Anim] Capa '{cleaningLayerName}' index={cleaningLayerIndex}");
        }
    }

    private void Update()
    {
        // ---- PICKUP / DROP ----
        if (PickupPressedThisFrame())
        {
            if (CurrentTool) DropCurrentTool();
            else TryPickupTool();
        }

        // ---- LIMPIEZA + ANIM ----
        bool holding = CurrentTool != null;
        bool cleaningInput = CleanHeld();
        bool shouldUseCleaning = holding && cleaningInput;

        UpdateCleaningLayer(shouldUseCleaning);

        if (shouldUseCleaning)
            TryCleanTick();
    }

    // ================== PICKUP ==================
    private void TryPickupTool()
    {
        if (!rayCamera) { DLogErr("[Pickup] No hay Camera."); return; }

        Vector3 origin = rayCamera.transform.position + rayCamera.transform.forward * 0.15f; // evita auto-hit
        Vector3 dir = rayCamera.transform.forward;
        Debug.DrawRay(origin, dir * pickupRange, Color.red);

        // 1) RaycastAll (ordenado) respetando obstáculos
        var hits = Physics.RaycastAll(origin, dir, pickupRange, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        bool blocked = false;
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue; // salto al Player

            var td = h.collider.GetComponentInParent<ToolDescriptor>();
            if (td != null)
            {
                if (!blocked)
                {
                    DLog($"[Pickup] EQUIP (ray): {td.name}");
                    Equip(td);
                    return;
                }
                else
                {
                    DLog("[Pickup] Hay un obstáculo delante de la herramienta, no equipo.");
                    break;
                }
            }

            // marcar oclusión si es sólido y no está en Tools
            if (!h.collider.isTrigger && !InMask(h.collider.gameObject.layer, toolsLayer))
                blocked = true;
        }

        // 2) Fallback: Overlap (solo Tools), por si estás MUY cerca
        Vector3 probe = origin + dir * 1.0f;
        var around = Physics.OverlapSphere(probe, 0.85f, toolsLayer, QueryTriggerInteraction.Collide);
        foreach (var c in around)
        {
            if (c.transform.IsChildOf(transform)) continue;
            var td = c.GetComponentInParent<ToolDescriptor>();
            if (td != null)
            {
                DLog($"[Pickup] EQUIP (overlap): {td.name}");
                Equip(td);
                return;
            }
        }

        DLog("[Pickup] No encontré herramientas.");
    }

    private void Equip(ToolDescriptor tool)
    {
        CurrentTool = tool;

        if (tool.TryGetComponent<Rigidbody>(out var rb))
        {
            // primero detengo, luego seteo kinematic (evita warnings)
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        SetAllCollidersTrigger(tool.gameObject, true);

        var t = tool.transform;
        t.SetParent(holdPoint, true);     // mantiene escala mundial
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        DLog($"[Pickup] EQUIP: {tool.name} (ID {tool.toolId})");
    }

    private void DropCurrentTool()
    {
        if (!CurrentTool) return;

        var tool = CurrentTool;
        CurrentTool = null;

        SetAllCollidersTrigger(tool.gameObject, false);

        if (tool.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.AddForce(transform.forward * 1.5f, ForceMode.VelocityChange);
        }

        tool.transform.SetParent(null, true);
        DLog("[Pickup] DROP");
    }

    // ================== CLEAN ==================
    private void TryCleanTick()
    {
        if (!rayCamera) return;
        if (!CurrentTool) return;

        Transform o = (aimFromHand && holdPoint != null) ? holdPoint : rayCamera.transform;
        Vector3 origin = o.position + rayCamera.transform.forward * handForwardOffset;
        Vector3 dir = (rayCamera.transform.forward + Vector3.down * downBias).normalized;

        Debug.DrawRay(origin, dir * cleanRange, Color.cyan);

        // 1) SphereCast (gordo)
        if (Physics.SphereCast(origin, sphereRadius, dir, out var sphHit, cleanRange, dirtLayer, QueryTriggerInteraction.Collide))
        {
            if (TryApplyClean(sphHit.collider)) return;
        }

        // 2) Raycast fino
        if (Physics.Raycast(origin, dir, out var hit, cleanRange, dirtLayer, QueryTriggerInteraction.Collide))
        {
            if (TryApplyClean(hit.collider)) return;
        }

        // 3) RaycastAll con oclusión
        var all = Physics.RaycastAll(origin, dir, cleanRange, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(all, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in all)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;

            bool isTools = InMask(h.collider.gameObject.layer, toolsLayer);
            bool isDirt = InMask(h.collider.gameObject.layer, dirtLayer);
            var dirt = h.collider.GetComponentInParent<DirtSpot>();

            if (isDirt && dirt != null) { ApplyClean(dirt); return; }
            if (dirt != null && !isDirt) { ApplyClean(dirt); return; } // collider hijo en otra capa

            if (!isTools && !h.collider.isTrigger) break; // obstáculo sólido
        }

        // 4) Overlap cercano
        Vector3 probe = origin + dir * Mathf.Min(1.0f, cleanRange * 0.5f);
        var around = Physics.OverlapSphere(probe, overlapRadius, dirtLayer, QueryTriggerInteraction.Collide);
        foreach (var c in around)
        {
            var dirt = c.GetComponentInParent<DirtSpot>();
            if (dirt != null) { ApplyClean(dirt); return; }
        }
    }


    private bool TryApplyClean(Collider col)
    {
        if (col.TryGetComponent(out DirtSpot d)) { ApplyClean(d); return true; }
        var parent = col.GetComponentInParent<DirtSpot>();
        if (parent != null) { ApplyClean(parent); return true; }

        DLog($"[Clean] Impacté '{col.name}' pero no tiene DirtSpot.");
        return false;
    }

    private void ApplyClean(DirtSpot dirt)
    {
        if (requireCorrectTool && !dirt.CanBeCleanedBy(CurrentTool.toolId)) { DLog("[Clean] Herramienta incorrecta."); return; }
        if (!ToolAllowed(CurrentTool.toolId)) { DLog($"[Clean] '{CurrentTool.toolId}' no permitida."); return; }

        float work = baseCleanRate * CurrentTool.toolPower * Time.deltaTime;
        dirt.CleanTick(work);
    }

    // ================== ANIM ==================
    private void UpdateCleaningLayer(bool shouldUseCleaning)
    {
        // booleans por si tu Animator los usa
        anim.SetBool("IsHolding", CurrentTool != null);
        anim.SetBool("IsCleaning", shouldUseCleaning);

        // peso de la capa
        if (cleaningLayerIndex >= 0 && cleaningLayerIndex < anim.layerCount)
        {
            float cur = anim.GetLayerWeight(cleaningLayerIndex);
            float tgt = shouldUseCleaning ? 1f : 0f;
            anim.SetLayerWeight(cleaningLayerIndex, Mathf.MoveTowards(cur, tgt, Time.deltaTime * layerBlendSpeed));
        }

        // (Opcional) Forzar estado con CrossFade para evitar depender de transiciones
        if (!useCrossFade || cleaningLayerIndex < 0) { prevCleaning = shouldUseCleaning; return; }

        if (cleanHash == 0 && !string.IsNullOrEmpty(cleaningStateName)) cleanHash = Animator.StringToHash(cleaningStateName);
        if (idleHash == 0 && !string.IsNullOrEmpty(upperIdleStateName)) idleHash = Animator.StringToHash(upperIdleStateName);

        if (shouldUseCleaning && !prevCleaning)
        {
            if (cleanHash != 0 && anim.HasState(cleaningLayerIndex, cleanHash))
                anim.CrossFade(cleanHash, 0.08f, cleaningLayerIndex, 0f);
            else if (!loggedCleanMissing) { Debug.LogWarning("[Anim] Estado de limpieza no encontrado en capa Clean."); loggedCleanMissing = true; }
        }
        else if (!shouldUseCleaning && prevCleaning)
        {
            if (idleHash != 0 && anim.HasState(cleaningLayerIndex, idleHash))
                anim.CrossFade(idleHash, 0.08f, cleaningLayerIndex, 0f);
            else if (!loggedIdleMissing) { Debug.LogWarning("[Anim] Estado idle de brazos no encontrado en capa Clean."); loggedIdleMissing = true; }
        }

        prevCleaning = shouldUseCleaning;
    }

    // ================== INPUT helpers ==================
    private bool PickupPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (pickupKey == KeyCode.E && Keyboard.current.eKey.wasPressedThisFrame) return true;
        }
#endif
        return Input.GetKeyDown(pickupKey);
    }

    private bool CleanHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (cleanKey == KeyCode.R && Keyboard.current.rKey.isPressed) return true;
        }
#endif
        return Input.GetKey(cleanKey);
    }

    // ================== Utils ==================
    private static void SetAllCollidersTrigger(GameObject go, bool isTrigger)
    {
        var cols = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.isTrigger = isTrigger;
    }
    private static bool InMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    private bool ToolAllowed(string id)
    {
        if (validToolIds == null || validToolIds.Length == 0) return true;
        for (int i = 0; i < validToolIds.Length; i++) if (validToolIds[i] == id) return true;
        return false;
    }

    private void DLog(string m) { if (debugLogs) Debug.Log(m); }
    private void DLogWarn(string m) { if (debugLogs) Debug.LogWarning(m); }
    private void DLogErr(string m) { if (debugLogs) Debug.LogError(m); }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (!rayCamera && Camera.main) rayCamera = Camera.main;
        if (anim) cleaningLayerIndex = anim.GetLayerIndex(cleaningLayerName);
    }
#endif
}
