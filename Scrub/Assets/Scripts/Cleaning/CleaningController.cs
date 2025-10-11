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
    [SerializeField] private KeyCode generalInteractKey = KeyCode.E;
    [SerializeField] private KeyCode cleanKey = KeyCode.R; // 🛑 Tu tecla de limpieza (por defecto R)

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

    // NUEVA VAR: Indica si se está manteniendo el input de limpieza
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

        // 🛑 CAMBIO CRÍTICO: MANEJO DE INPUT DE LIMPIEZA
        // Usamos GetKey/GetButton para detectar si la tecla está MANTENIDA (animación continua)
        isCleaningInputHeld = Input.GetKey(cleanKey) || Input.GetButton("Fire1");

        // Log de diagnóstico para el input
        if (isCleaningInputHeld && debugLogs)
        {
            string toolID = holding ? CurrentTool.toolId : "NONE";
            DLog($"[INPUT TEST] Tecla/Clic MANTENIDO. Holding={holding}, Tool={toolID}, DirtNearby={dirtNearby}");
        }

        // Animación se activa si se está sosteniendo la herramienta Y se mantiene el input de limpieza
        UpdateCleaningLayer(holding && isCleaningInputHeld);

        // Solo aplicamos el HIT una vez (GetKeyDown/GetButtonDown) para evitar daño excesivo por frame
        // Si tu animación es un ciclo, puedes usar GetKey/GetButton y controlar el daño por tiempo (cooldown)
        bool cleanInputDown = Input.GetKeyDown(cleanKey) || Input.GetButtonDown("Fire1");

        if (holding && cleanInputDown && dirtNearby)
        {
            // Si quieres daño constante mientras mantienes la tecla:
            // Aplica un golpe fuerte o llama a un método de Coroutine.
            // Aquí se usa el enfoque simple de un golpe por pulsación.
            ApplyCleanHit();
        }
    }

    // ================== Detección por Trigger (Suciedad) ==================
    // ... (Mantén OnTriggerEnter y OnTriggerExit sin cambios) ...
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

            // **IMPORTANTE**: La suciedad DEBE estar limpia (destruida) para que este trigger se resuelva. 
            // Si el objeto 'dirt' no es nulo, significa que sigue existiendo en el Trigger.

            if (dirt != null && nearbyDirt.Contains(dirt))
            {
                nearbyDirt.Remove(dirt);
                DLog($"[Clean Trigger] 🔴 Dejado: {dirt.name}. Quedan {nearbyDirt.Count} spots.");
            }
            // NOTA: Si un DirtSpot se destruye, Unity automáticamente llama a OnTriggerExit. 
            // Si el objeto ya fue destruido, 'dirt' será nulo, y la limpieza de la lista se hace en ApplyCleanHit.
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

        // 1. Limpiamos la lista de referencias nulas (objetos destruidos)
        nearbyDirt.RemoveAll(dirt => dirt == null);

        if (nearbyDirt.Count == 0) return;

        // 2. ENCONTRAR Y SELECCIONAR EL DIRT SPOT MÁS CERCANO

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

        // 5. Aplicamos el daño. Este método contiene la lógica de destrucción y partículas.
        closestDirt.CleanHit(damage);

        DLog($"[Clean HIT OK] Aplicando {damage:F2} de daño SOLAMENTE a {closestDirt.name} (el más cercano).");
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

        // El booleano IsCleaning ahora se mantiene mientras el jugador mantiene la tecla/clic
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