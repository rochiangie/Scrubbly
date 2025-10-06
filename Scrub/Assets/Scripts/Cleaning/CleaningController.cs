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
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private KeyCode cleanKey = KeyCode.R;

    [Header("Cleaning")]
    // NOTA: 'damagePerHit' es ahora un multiplicador del poder de la herramienta
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
    // NOTA: Asumimos que ToolDescriptor tiene el método 'bool TryUse()'
    public ToolDescriptor CurrentTool { get; private set; }
    private List<DirtSpot> nearbyDirt = new List<DirtSpot>();

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
        bool cleanPressed = Input.GetKeyDown(cleanKey);
        bool dirtNearby = nearbyDirt.Count > 0;

        // Logging de input
        if (cleanPressed && debugLogs)
        {
            string toolID = holding ? CurrentTool.toolId : "NONE";
            DLog($"[INPUT TEST] Tecla 'R' PRESIONADA. Holding={holding}, Tool={toolID}, DirtNearby={dirtNearby}");
        }

        UpdateCleaningLayer(holding && dirtNearby && cleanPressed); // Animación solo si hay input

        // Solo golpeamos si hay herramienta, se presionó el input y hay suciedad
        if (holding && cleanPressed && dirtNearby)
        {
            ApplyCleanHit();
        }
    }

    // ================== Detección por Trigger (Suciedad) ==================
    // (Lógica de detección y remoción de nearbyDirt se mantiene igual, es correcta)
    // ... (Métodos OnTriggerEnter y OnTriggerExit se mantienen) ...
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

    // ... (Método RegisterTool se mantiene) ...
    public void RegisterTool(ToolDescriptor tool)
    {
        if (tool == null)
        {
            Debug.LogError("[REGISTER FAIL] Script externo intentó registrar una herramienta nula.");
            return;
        }
        Equip(tool);
        DLog($"[EXTERNAL REGISTER] Herramienta '{tool.name}' registrada correctamente.");
    }

    // ... (Método DropCurrentTool se mantiene) ...
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

        DLog("[Pickup] DROP realizado por CleaningController.");
    }

    // ================== Lógica Interna de Limpieza (EL CAMBIO CRÍTICO) ==================

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

        // CRÍTICO: 1. Intentar usar la herramienta. 
        // Si TryUse devuelve false, la herramienta se destruyó en ese llamado.
        bool successfullyUsed = CurrentTool.TryUse();

        if (!successfullyUsed)
        {
            DLogWarning($"[Clean HIT FAIL] Herramienta '{CurrentTool.toolId}' se gastó. Ya no está equipada.");
            CurrentTool = null; // Limpiamos la referencia localmente.
            return;
        }

        // Si la herramienta pudo usarse, aplicamos el golpe a todos los spots cercanos
        float damage = damageMultiplier * CurrentTool.toolPower;

        for (int i = nearbyDirt.Count - 1; i >= 0; i--)
        {
            DirtSpot dirt = nearbyDirt[i];

            if (dirt == null)
            {
                nearbyDirt.RemoveAt(i);
                continue;
            }

            // 2. Comprobación de herramienta correcta
            if (requireCorrectTool && !dirt.CanBeCleanedBy(CurrentTool.toolId))
            {
                DLogWarning($"[Clean FAIL 1: Tool Mismatch] Herramienta '{CurrentTool.toolId}' no limpia {dirt.name}.");
                continue;
            }

            // 3. Aplicamos el daño
            dirt.CleanHit(damage);

            DLog($"[Clean HIT OK] Aplicando {damage:F2} de daño a {dirt.name}.");
        }
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