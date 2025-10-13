using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Animator))]
public class CleaningController : MonoBehaviour
{
    // ================== Variables ==================

    [Header("Refs")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Animator anim;

    [Header("Layers & Ranges")]
    [SerializeField] private LayerMask toolsLayer;
    [SerializeField] private float pickupRange = 3.5f;
    [SerializeField] private float dropForce = 1.5f;

    [Header("Input (teclas simples)")]
    [SerializeField] private KeyCode generalInteractKey = KeyCode.E;
    [SerializeField] private KeyCode cleanKey = KeyCode.R;

    [Header("Cleaning")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private bool requireCorrectTool = true;
    [SerializeField] private string[] validToolIds = { "Mop", "Sponge", "Vacuum" };
    [SerializeField] private string dirtTag = "Dirt";

    [Header("Animation Layer")]
    [SerializeField] private string cleaningLayerName = "Clean";
    [SerializeField] private float layerBlendSpeed = 12f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    // ESTADO CRÍTICO
    public ToolDescriptor CurrentTool { get; private set; }
    private List<DirtSpot> nearbyDirt = new List<DirtSpot>();
    private bool isCleaningInputHeld = false;
    private int cleaningLayerIndex = -1;

    // ================== Unity Lifecycle ==================

    private void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (anim)
        {
            cleaningLayerIndex = anim.GetLayerIndex(cleaningLayerName);
        }
    }

    private void Update()
    {
        bool holding = CurrentTool != null;
        bool dirtNearby = nearbyDirt.Count > 0;

        // Input de limpieza
        isCleaningInputHeld = Input.GetKey(cleanKey) || Input.GetMouseButton(0);

        // Log de diagnóstico
        if (isCleaningInputHeld && debugLogs)
        {
            string toolID = holding ? CurrentTool.toolId : "NONE";
            DLog($"[INPUT TEST] Tecla/Clic MANTENIDO. Holding={holding}, Tool={toolID}, DirtNearby={dirtNearby}");
        }

        // Animación
        UpdateCleaningLayer(holding && isCleaningInputHeld);

        // Aplicar limpieza cuando se presiona la tecla/clic
        bool cleanInputDown = Input.GetKeyDown(cleanKey) || Input.GetMouseButtonDown(0);

        if (holding && cleanInputDown && dirtNearby)
        {
            ApplyCleanHit();
        }

        // 🔥 NUEVO: SFX de limpieza CONTINUO mientras se mantiene la tecla
        if (holding && isCleaningInputHeld && dirtNearby)
        {
            PlayContinuousCleaningSFX();
        }
    }

    // ================== SFX INTEGRATION ==================

    /// <summary>
    /// Reproduce SFX de limpieza continua mientras se mantiene la tecla
    /// </summary>
    private void PlayContinuousCleaningSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCleanSFX();
        }
    }

    /// <summary>
    /// Reproduce SFX cuando se recoge una herramienta
    /// </summary>
    private void PlayPickupSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPickupSFX();
        }
    }

    /// <summary>
    /// Reproduce SFX cuando se suelta una herramienta
    /// </summary>
    private void PlayDropSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDropSFX();
        }
    }

    // ================== Detección por Trigger (Suciedad) ==================

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(dirtTag))
        {
            DirtSpot dirt = other.GetComponent<DirtSpot>() ?? other.GetComponentInParent<DirtSpot>();

            if (dirt != null && !nearbyDirt.Contains(dirt))
            {
                nearbyDirt.Add(dirt);
                DLog($"[Clean Trigger] 🟢 Detectado: {dirt.name}. Ahora hay {nearbyDirt.Count} spots cerca.");
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
                DLog($"[Clean Trigger] 🔴 Dejado: {dirt.name}. Quedan {nearbyDirt.Count} spots.");
            }
        }
    }

    // ================== Métodos Públicos de Interacción ==================

    public void RegisterTool(ToolDescriptor tool)
    {
        if (tool == null)
        {
            Debug.LogError("[REGISTER FAIL] Script externo intentó registrar una herramienta nula.");
            return;
        }
        Equip(tool);

        // 🔥 SFX DE RECOGER HERRAMIENTA
        PlayPickupSFX();

        DLog($"[EXTERNAL REGISTER] Herramienta '{tool.name}' registrada correctamente.");
    }

    public void DropCurrentTool()
    {
        if (!CurrentTool) return;

        var tool = CurrentTool;
        CurrentTool = null;

        if (tool.TryGetComponent<Carryable>(out var carryable))
        {
            carryable.Drop(transform.forward, dropForce);
        }

        SetAllCollidersTrigger(tool.gameObject, false);

        if (anim != null) anim.SetBool("IsHolding", false);

        // 🔥 SFX DE SOLTAR HERRAMIENTA
        PlayDropSFX();

        DLog("[Pickup] DROP realizado por CleaningController.");
    }

    // ================== Lógica Interna de Limpieza ==================

    private void Equip(ToolDescriptor tool)
    {
        CurrentTool = tool;

        SetAllCollidersTrigger(tool.gameObject, true);

        var t = tool.transform;
        if (holdPoint != null)
        {
            t.SetParent(holdPoint, true);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
        }
        DLog($"[Pickup] EQUIP: {tool.name}. Asignación CurrentTool exitosa.");
    }

    private void ApplyCleanHit()
    {
        if (CurrentTool == null) return;

        // Limpiar lista de referencias nulas
        nearbyDirt.RemoveAll(dirt => dirt == null);

        if (nearbyDirt.Count == 0) return;

        // Encontrar dirt spot más cercano
        DirtSpot closestDirt = nearbyDirt
            .OrderBy(dirt => Vector3.Distance(transform.position, dirt.transform.position))
            .FirstOrDefault();

        if (closestDirt == null) return;

        // Intentar usar la herramienta
        bool successfullyUsed = CurrentTool.TryUse();

        if (!successfullyUsed)
        {
            DLogWarning($"[Clean HIT FAIL] Herramienta '{CurrentTool.toolId}' se gastó. Ya no está equipada.");
            CurrentTool = null;
            return;
        }

        float damage = damageMultiplier * CurrentTool.toolPower;

        // Comprobación de herramienta correcta
        if (requireCorrectTool && !closestDirt.CanBeCleanedBy(CurrentTool.toolId))
        {
            DLogWarning($"[Clean FAIL 1: Tool Mismatch] Herramienta '{CurrentTool.toolId}' no limpia {closestDirt.name}.");
            return;
        }

        // Aplicar daño
        closestDirt.CleanHit(damage);

        // 🔥 SFX DE IMPACTO DE LIMPIEZA (adicional al continuo)
        // Esto da un "golpe" sonoro adicional al impacto
        PlayContinuousCleaningSFX();

        DLog($"[Clean HIT OK] Aplicando {damage:F2} de daño SOLAMENTE a {closestDirt.name} (el más cercano).");
    }

    // ================== Utilities ==================

    private static void SetAllCollidersTrigger(GameObject go, bool isTrigger)
    {
        var cols = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.isTrigger = isTrigger;
    }

    private void UpdateCleaningLayer(bool shouldUseCleaning)
    {
        if (anim == null) return;
        anim.SetBool("IsHolding", CurrentTool != null);
        anim.SetBool("IsCleaning", shouldUseCleaning);

        if (cleaningLayerIndex >= 0)
        {
            float cur = anim.GetLayerWeight(cleaningLayerIndex);
            float tgt = shouldUseCleaning ? 1f : 0f;
            anim.SetLayerWeight(cleaningLayerIndex, Mathf.MoveTowards(cur, tgt, Time.deltaTime * layerBlendSpeed));
        }
    }

    private void DLog(string m) { if (debugLogs) Debug.Log(m); }
    private void DLogWarning(string m) { if (debugLogs) Debug.LogWarning(m); }
}