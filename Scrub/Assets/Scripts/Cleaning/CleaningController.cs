using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necesario para usar OrderBy y FirstOrDefault

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
    [SerializeField] private KeyCode generalInteractKey = KeyCode.E; // (De PlayerInteraction)
    [SerializeField] private KeyCode cleanKey = KeyCode.R; // 🛑 Tu tecla de limpieza (por defecto R)

    [Header("Cleaning")]
    // NOTA: 'damageMultiplier' es el multiplicador del poder de la herramienta
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

        // 🛑 CAMBIO CRÍTICO: Chequea la tecla (R) O el clic izquierdo (Fire1)
        bool cleanInputPressed = Input.GetKeyDown(cleanKey) || Input.GetButtonDown("Fire1");

        // Log de diagnóstico para el input
        if (cleanInputPressed && debugLogs)
        {
            string toolID = holding ? CurrentTool.toolId : "NONE";
            DLog($"[INPUT TEST] Tecla/Clic PRESIONADO. Holding={holding}, Tool={toolID}, DirtNearby={dirtNearby}");
        }

        // Animación solo si hay input
        UpdateCleaningLayer(holding && dirtNearby && cleanInputPressed);

        // Solo golpeamos si hay herramienta, se presionó el input (tecla o clic) y hay suciedad
        if (holding && cleanInputPressed && dirtNearby)
        {
            ApplyCleanHit();
        }
    }

    // ================== Detección por Trigger (Suciedad) ==================
    // ... (Estas funciones se mantienen sin cambios, ya que solo manejan la lista de objetos en rango) ...
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

    // ================== Métodos Públicos de Interacción (sin cambios) ==================

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

        // 1. Limpiamos la lista de referencias nulas y eliminadas
        nearbyDirt.RemoveAll(dirt => dirt == null);

        if (nearbyDirt.Count == 0) return;

        // 2. 🛑 ENCONTRAR Y SELECCIONAR EL DIRT SPOT MÁS CERCANO 🛑

        // Usamos LINQ para ordenar los DirtSpots por la distancia desde la posición de este objeto.
        DirtSpot closestDirt = nearbyDirt
            .OrderBy(dirt => Vector3.Distance(transform.position, dirt.transform.position))
            .FirstOrDefault();

        if (closestDirt == null) return;

        // 3. Intentar usar la herramienta (consumo de durabilidad)
        bool successfullyUsed = CurrentTool.TryUse();

        if (!successfullyUsed)
        {
            DLogWarning($"[Clean HIT FAIL] Herramienta '{CurrentTool.toolId}' se gastó. Ya no está equipada.");
            CurrentTool = null; // Limpiamos la referencia localmente.
            return;
        }

        // Si la herramienta pudo usarse, aplicamos el golpe SOLAMENTE al spot más cercano
        float damage = damageMultiplier * CurrentTool.toolPower;

        // 4. Comprobación de herramienta correcta
        if (requireCorrectTool && !closestDirt.CanBeCleanedBy(CurrentTool.toolId))
        {
            DLogWarning($"[Clean FAIL 1: Tool Mismatch] Herramienta '{CurrentTool.toolId}' no limpia {closestDirt.name}.");
            return; // No aplicamos daño
        }

        // 5. Aplicamos el daño
        closestDirt.CleanHit(damage);

        DLog($"[Clean HIT OK] Aplicando {damage:F2} de daño SOLAMENTE a {closestDirt.name} (el más cercano).");

        // Opcional: Si el DirtSpot se destruye inmediatamente después del golpe final, 
        // se eliminará de la lista en la siguiente actualización del TriggerExit/OnTriggerEnter, 
        // o por la limpieza de nulos al inicio de esta función.
    }

    // ================== Utilities (sin cambios) ==================

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