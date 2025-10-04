// CleaningController.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Animator))]
public class CleaningController : MonoBehaviour
{
    // ---------------- Refs ----------------
    [Header("Refs")]
    [SerializeField] private Transform holdPoint;  // Dónde va la herramienta (mano)
    [SerializeField] private Animator anim;

    // ---------------- Capas y rangos ----------------
    [Header("Layers & Ranges")]
    [SerializeField] private LayerMask toolsLayer;
    [SerializeField] private float pickupRange = 3.5f; // Usado para el Raycast de recogida
    [SerializeField] private float dropForce = 1.5f;

    // ---------------- Input ----------------
    [Header("Input (teclas simples)")]
    [SerializeField] private KeyCode pickupKey = KeyCode.E; // Recoger/Soltar
    [SerializeField] private KeyCode cleanKey = KeyCode.R;  // Limpiar

    // ---------------- Limpieza ----------------
    [Header("Cleaning")]
    [SerializeField] private float baseCleanRate = 1f;
    [SerializeField] private bool requireCorrectTool = true;
    [SerializeField] private string[] validToolIds = { "Mop", "Sponge", "Vacuum" };
    [SerializeField] private string dirtTag = "Dirt"; // Tag para la detección de suciedad

    // ---------------- Animación (capa "Clean") ----------------
    [Header("Animation Layer")]
    [SerializeField] private string cleaningLayerName = "Clean";
    [SerializeField] private float layerBlendSpeed = 12f;
    [SerializeField] private bool useCrossFade = false;
    [SerializeField] private string cleaningStateName = "Clean_Loop";
    [SerializeField] private string upperIdleStateName = "UpperBody_Idle";

    // ---------------- Debug ----------------
    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    // ---------------- Estado ----------------
    public ToolDescriptor CurrentTool { get; private set; }

    // LISTA DE OBJETOS DE SUCIEDAD DENTRO DEL TRIGGER DEL JUGADOR
    private List<DirtSpot> nearbyDirt = new List<DirtSpot>();

    private int cleaningLayerIndex = -1;
    private bool prevCleaning = false;
    private int cleanHash = 0, idleHash = 0;
    private bool loggedCleanMissing = false, loggedIdleMissing = false;

    // ================== Unity ==================
    private void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (anim)
        {
            cleaningLayerIndex = anim.GetLayerIndex(cleaningLayerName);
            if (cleaningLayerIndex < 0) DLogWarn($"[Anim] No encontré la capa '{cleaningLayerName}'.");
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
        // Solo limpiamos si tenemos herramienta, input presionado Y suciedad cerca
        bool shouldUseCleaning = holding && cleaningInput && nearbyDirt.Count > 0;

        UpdateCleaningLayer(shouldUseCleaning);

        if (shouldUseCleaning)
            ApplyCleanToNearbyDirt();
    }

    // ================== DETECCIÓN POR TRIGGER ==================

    private void OnTriggerEnter(Collider other)
    {
        // 🛑 CRÍTICO: Chequeo de Tag para ser eficiente y enfocarnos en la suciedad
        if (other.CompareTag(dirtTag))
        {
            DirtSpot dirt = other.GetComponent<DirtSpot>() ?? other.GetComponentInParent<DirtSpot>();

            if (dirt != null && !nearbyDirt.Contains(dirt))
            {
                nearbyDirt.Add(dirt);
                Debug.Log($"[Clean Trigger] 🟢 Detectado DirtTag en: {dirt.name}. Suciedad cerca ({nearbyDirt.Count} spots).");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(dirtTag))
        {
            DirtSpot dirt = other.GetComponent<DirtSpot>() ?? other.GetComponentInParent<DirtSpot>();

            if (dirt != null && nearbyDirt.Contains(dirt))
            {
                nearbyDirt.Remove(dirt);
                Debug.Log($"[Clean Trigger] 🔴 Dejado DirtTag en: {dirt.name}. Quedan ({nearbyDirt.Count} spots).");
            }
        }
    }


    // ================== CLEAN LOGIC ==================

    private void ApplyCleanToNearbyDirt()
    {
        if (CurrentTool == null) return;

        // Limpiamos la lista de objetos nulos (suciedad que fue destruida previamente)
        // Iteramos al revés para eliminar sin problemas
        for (int i = nearbyDirt.Count - 1; i >= 0; i--)
        {
            DirtSpot dirt = nearbyDirt[i];

            if (dirt == null)
            {
                nearbyDirt.RemoveAt(i);
                continue;
            }

            // 1. Validación de herramienta
            if (requireCorrectTool && !dirt.CanBeCleanedBy(CurrentTool.toolId))
            {
                continue; // Saltar a la siguiente suciedad
            }

            // 2. Validación de ID de herramienta
            if (!ToolAllowed(CurrentTool.toolId))
            {
                DLog($"[Clean] '{CurrentTool.toolId}' no permitida por la configuración.");
                continue;
            }

            // 3. Aplicar limpieza
            float work = baseCleanRate * CurrentTool.toolPower * Time.deltaTime;
            dirt.CleanTick(work);

            // Si CleanTick destruye el objeto, el 'if (dirt == null)' al inicio del bucle lo limpiará
        }
    }

    // ================== PICKUP / DROP LOGIC (Mantiene Raycast/Overlap para recoger) ==================

    private void TryPickupTool()
    {
        Camera rayCamera = Camera.main;
        if (!rayCamera) { DLogErr("[Pickup] No hay Camera.main en escena."); return; }

        Vector3 origin = rayCamera.transform.position + rayCamera.transform.forward * 0.15f;
        Vector3 dir = rayCamera.transform.forward;

        // 1. Raycast para una detección precisa (usando la capa Tools)
        if (Physics.Raycast(origin, dir, out RaycastHit rayHit, pickupRange, toolsLayer, QueryTriggerInteraction.Ignore))
        {
            var td = rayHit.collider.GetComponentInParent<ToolDescriptor>();
            if (td != null && !rayHit.collider.transform.IsChildOf(transform))
            {
                DLog($"[Pickup] EQUIP (raycast): {td.name}");
                Equip(td);
                return;
            }
        }

        // 2. Fallback: Overlap (por si estás MUY cerca)
        Vector3 probe = transform.position + transform.forward * 1.0f;
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
    }

    private void Equip(ToolDescriptor tool)
    {
        CurrentTool = tool;

        if (tool.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        SetAllCollidersTrigger(tool.gameObject, true); // Haz la herramienta Trigger

        var t = tool.transform;
        if (holdPoint != null)
        {
            t.SetParent(holdPoint, true);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
        }
        DLog($"[Pickup] EQUIP: {tool.name} (ID {tool.toolId})");
    }

    private void DropCurrentTool()
    {
        if (!CurrentTool) return;

        var tool = CurrentTool;
        CurrentTool = null;

        SetAllCollidersTrigger(tool.gameObject, false); // Vuelve la herramienta a sólido

        if (tool.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.AddForce(transform.forward * dropForce, ForceMode.VelocityChange);
        }

        tool.transform.SetParent(null, true);
        DLog("[Pickup] DROP");
    }

    // ================== ANIM & INPUT helpers ==================

    private void UpdateCleaningLayer(bool shouldUseCleaning)
    {
        if (anim == null) return;

        anim.SetBool("IsHolding", CurrentTool != null);
        anim.SetBool("IsCleaning", shouldUseCleaning);

        if (cleaningLayerIndex >= 0 && cleaningLayerIndex < anim.layerCount)
        {
            float cur = anim.GetLayerWeight(cleaningLayerIndex);
            float tgt = shouldUseCleaning ? 1f : 0f;
            anim.SetLayerWeight(cleaningLayerIndex, Mathf.MoveTowards(cur, tgt, Time.deltaTime * layerBlendSpeed));
        }

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

    // ================== UTILS ==================
    private static void SetAllCollidersTrigger(GameObject go, bool isTrigger)
    {
        var cols = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.isTrigger = isTrigger;
    }

    private bool ToolAllowed(string id)
    {
        if (validToolIds == null || validToolIds.Length == 0) return true;
        return validToolIds.Contains(id);
    }

    private void DLog(string m) { if (debugLogs) Debug.Log(m); }
    private void DLogWarn(string m) { if (debugLogs) Debug.LogWarning(m); }
    private void DLogErr(string m) { if (debugLogs) Debug.LogError(m); }
}