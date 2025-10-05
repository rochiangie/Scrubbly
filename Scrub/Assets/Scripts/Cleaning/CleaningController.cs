// CleaningController.cs - FINAL Y SIN CONFLICTOS (Versión Completa)

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
    [SerializeField] private float damagePerHit = 1f;
    [SerializeField] private bool requireCorrectTool = true;
    [SerializeField] private string[] validToolIds = { "Mop", "Sponge", "Vacuum" };
    [SerializeField] private string dirtTag = "Dirt";

    [Header("Animation Layer")]
    [SerializeField] private string cleaningLayerName = "Clean";
    [SerializeField] private float layerBlendSpeed = 12f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false; // Controla si DLog se imprime

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
        bool cleanPressed = Input.GetKeyDown(cleanKey);
        bool dirtNearby = nearbyDirt.Count > 0;

        // Log de diagnóstico para el input 'R'
        if (cleanPressed)
        {
            string toolID = holding && CurrentTool != null ? CurrentTool.toolId : "NONE";
            Debug.Log($"[INPUT TEST] Tecla 'R' PRESIONADA. Holding={holding}, Tool={toolID}, DirtNearby={dirtNearby}");
        }

        UpdateCleaningLayer(holding && dirtNearby);

        if (holding && cleanPressed && dirtNearby)
        {
            ApplyCleanHit();
        }
    }

    // ================== Detección por Trigger (Suciedad) ==================

    // NOTA: Para que esto funcione, el Player DEBE tener un Collider marcado como 'Is Trigger'.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(dirtTag))
        {
            DirtSpot dirt = other.GetComponent<DirtSpot>() ?? other.GetComponentInParent<DirtSpot>();

            // Asegúrate de que no es nulo y que no lo tenemos ya
            if (dirt != null && !nearbyDirt.Contains(dirt))
            {
                nearbyDirt.Add(dirt);
                // Log de detección verde
                Debug.Log($"[Clean Trigger] 🟢 Detectado: {dirt.name}. Ahora hay {nearbyDirt.Count} spots cerca.");
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
                // Log de detección rojo
                Debug.Log($"[Clean Trigger] 🔴 Dejado: {dirt.name}. Quedan {nearbyDirt.Count} spots.");
            }
        }
    }

    // ================== Métodos Públicos de Interacción (Llamados por PlayerInteraction) ==================

    public void RegisterTool(ToolDescriptor tool)
    {
        if (tool == null)
        {
            Debug.LogError("[REGISTER FAIL] Script externo intentó registrar una herramienta nula.");
            return;
        }
        Equip(tool);
        Debug.Log($"[EXTERNAL REGISTER] Herramienta '{tool.name}' registrada correctamente.");
    }

    public void DropCurrentTool()
    {
        if (!CurrentTool) return;

        var tool = CurrentTool;
        CurrentTool = null;

        // 1. Llama al Carryable.Drop() para que maneje físicas y colisiones.
        if (tool.TryGetComponent<Carryable>(out var carryable))
        {
            // Usamos la versión con dirección y fuerza para darle el empujón
            carryable.Drop(transform.forward, dropForce);
        }

        // 2. Quitamos el Trigger del Collider 
        SetAllCollidersTrigger(tool.gameObject, false);

        if (anim != null) anim.SetBool("IsHolding", false);

        DLog("[Pickup] DROP realizado por CleaningController.");
    }

    // ================== Lógica Interna de Limpieza ==================

    private void Equip(ToolDescriptor tool)
    {
        CurrentTool = tool;

        // Aseguramos que sea Trigger para evitar empujar
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

        // --- LOG DE DIAGNÓSTICO CRÍTICO PARA EL BUG DE LIMPIEZA ---
        Debug.Log($"[CLEAN HIT] Intentando golpear. ToolID: {CurrentTool.toolId}. DirtCount: {nearbyDirt.Count}");
        // --------------------------------------------------------

        for (int i = nearbyDirt.Count - 1; i >= 0; i--)
        {
            DirtSpot dirt = nearbyDirt[i];

            if (dirt == null)
            {
                nearbyDirt.RemoveAt(i);
                continue;
            }

            if (requireCorrectTool && !dirt.CanBeCleanedBy(CurrentTool.toolId))
            {
                // Este log te dirá si el problema es que la herramienta no coincide con la suciedad
                Debug.LogWarning($"[Clean FAIL 1: Tool Mismatch] Herramienta '{CurrentTool.toolId}' no limpia {dirt.name}.");
                continue;
            }

            // Si llegamos aquí, la herramienta es correcta o no requerida
            float damage = damagePerHit * CurrentTool.toolPower;
            dirt.CleanHit(damage);

            Debug.Log($"[Clean HIT OK] Aplicando {damage:F2} de daño a {dirt.name}.");
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
    private void DLogWarn(string m) { if (debugLogs) Debug.LogWarning(m); }
}