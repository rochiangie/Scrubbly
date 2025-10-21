using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Animator))]
public class CleaningController : MonoBehaviour
{
    // ---------------- Refs ----------------
    [Header("Refs")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Animator anim;

    // ---------------- Capas y rangos ----------------
    [Header("Layers & Ranges")]
    [SerializeField] private LayerMask toolsLayer;
    [SerializeField] private float pickupRange = 3.5f;
    [SerializeField] private float dropForce = 1.5f;

    // ---------------- Input ----------------
    [Header("Input (teclas simples)")]
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private KeyCode cleanKey = KeyCode.R;
    [SerializeField] private KeyCode trashKey = KeyCode.F;

    // ---------------- Limpieza ----------------
    [Header("Cleaning")]
    [SerializeField] private float damagePerHit = 1f;
    [SerializeField] private bool requireCorrectTool = true;
    [SerializeField] private string[] validToolIds = { "Mop", "Sponge", "Vacuum", "Escoba" };
    [SerializeField] private string dirtTag = "Dirt";
    [SerializeField] private string trashTag = "Basura"; // Usamos el tag "Basura"

    // ---------------- Animación ----------------
    [Header("Animation Layer")]
    [SerializeField] private string cleaningLayerName = "Clean";
    [SerializeField] private float layerBlendSpeed = 12f;

    // ---------------- Estado ----------------
    public ToolDescriptor CurrentTool { get; private set; }
    private List<DirtSpot> nearbyDirt = new List<DirtSpot>();
    private List<TrashObject> nearbyTrash = new List<TrashObject>();

    private int cleaningLayerIndex = -1;


    // ================== Unity ==================
    private void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (anim)
        {
            cleaningLayerIndex = anim.GetLayerIndex(cleaningLayerName);
            if (cleaningLayerIndex < 0)
            {
                Debug.LogError($"Animator no tiene la capa '{cleaningLayerName}'. La transición de peso de capa fallará.");
            }
        }
    }

    private void Update()
    {
        // ---- PICKUP / DROP ----
        if (Input.GetKeyDown(pickupKey))
        {
            if (CurrentTool) DropCurrentTool();
            else TryPickupTool();
        }

        // ---- ESTADO DE LAS ZONAS DE LIMPIEZA ----
        bool holding = CurrentTool != null;
        bool dirtNearby = nearbyDirt.Count > 0;
        bool trashNearby = nearbyTrash.Count > 0;

        // ---- LIMPIEZA CLÁSICA (R / Clic) ----
        bool cleanPressed = Input.GetKeyDown(cleanKey) || Input.GetButtonDown("Fire1");

        if (holding && cleanPressed && dirtNearby)
        {
            ApplyCleanHit();
        }

        // ---- ELIMINAR BASURA (F) ----
        if (Input.GetKeyDown(trashKey) && trashNearby)
        {
            TryRemoveTrash("Escoba");
        }

        UpdateCleaningLayer(holding && (dirtNearby || trashNearby));
    }

    // ================== MÉTODOS PÚBLICOS DE INTERACCIÓN ==================

    public void RegisterTool(ToolDescriptor tool)
    {
        if (tool == null)
        {
            Debug.LogError("[REGISTER FAIL] Se intentó registrar una herramienta nula.");
            return;
        }

        if (tool.TryGetComponent<Carryable>(out var carryable))
        {
            carryable.IsCarried = true;
        }

        Equip(tool);

        if (anim != null) anim.SetBool("IsHolding", true);
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
        else
        {
            tool.transform.SetParent(null);
            if (tool.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.AddForce(transform.forward * dropForce, ForceMode.Impulse);
            }
        }

        SetAllCollidersTrigger(tool.gameObject, false);
        if (anim != null) anim.SetBool("IsHolding", false);
    }

    // ================== DETECCIÓN POR TRIGGER ==================

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(dirtTag))
        {
            DirtSpot dirt = other.GetComponent<DirtSpot>() ?? other.GetComponentInParent<DirtSpot>();
            if (dirt != null && !nearbyDirt.Contains(dirt))
            {
                nearbyDirt.Add(dirt);
            }
        }

        if (other.CompareTag(trashTag))
        {
            TrashObject trash = other.GetComponent<TrashObject>() ?? other.GetComponentInParent<TrashObject>();
            if (trash != null && !nearbyTrash.Contains(trash))
            {
                nearbyTrash.Add(trash);
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
            }
        }

        if (other.CompareTag(trashTag))
        {
            TrashObject trash = other.GetComponent<TrashObject>() ?? other.GetComponentInParent<TrashObject>();
            if (trash != null && nearbyTrash.Contains(trash))
            {
                nearbyTrash.Remove(trash);
            }
        }
    }

    // ================== LÓGICA DE LIMPIEZA CLÁSICA ==================

    private void ApplyCleanHit()
    {
        if (CurrentTool == null) return;

        nearbyDirt.RemoveAll(dirt => dirt == null);
        if (nearbyDirt.Count == 0) return;

        DirtSpot closestDirt = nearbyDirt
            .OrderBy(dirt => Vector3.Distance(transform.position, dirt.transform.position))
            .FirstOrDefault();

        if (closestDirt == null) return;

        bool successfullyUsed = CurrentTool.TryUse();
        if (!successfullyUsed) { CurrentTool = null; return; }

        float damage = damagePerHit * CurrentTool.ToolPower;

        if (requireCorrectTool && !closestDirt.CanBeCleanedBy(CurrentTool.ToolId)) { return; }

        closestDirt.CleanHit(damage);

        // 🛑 CORRECCIÓN 1: Notifica que se limpió un Spot (DirtSpot.cs ya llama a NotifySpotCleaned en HandleDestruction)
        // Por lo general, esta llamada no es necesaria aquí si CleanHit() es lo que destruye el objeto.
        // Si tu lógica requiere una llamada aquí:
        // if (TaskManager.Instance != null) { TaskManager.Instance.NotifySpotCleaned(); }
        // Si no, confiar en que el DirtSpot lo hace al destruirse.

        if (AudioManager.Instance != null)
        {
            // AudioManager.Instance.PlayCleanSFX();
        }
    }

    // ================== LÓGICA DE BASURA (TECLA F) ==================

    private void TryRemoveTrash(string requiredToolId)
    {
        // Verificación de herramienta y disponibilidad (Lógica intacta)
        if (CurrentTool == null || CurrentTool.ToolId != requiredToolId)
        {
            Debug.LogWarning($"[Trash] Necesitas la herramienta '{requiredToolId}' (Escoba) para barrer.");
            return;
        }

        nearbyTrash.RemoveAll(t => t == null);
        if (nearbyTrash.Count == 0) return;

        TrashObject closestTrash = nearbyTrash
            .OrderBy(t => Vector3.Distance(transform.position, t.transform.position))
            .FirstOrDefault();

        if (closestTrash == null) return;

        if (!CurrentTool.TryUse())
        {
            CurrentTool = null;
            return;
        }

        // 🛑 CORRECCIÓN CLAVE: Notificar al TaskManager ANTES de la destrucción
        if (TaskManager.Instance != null)
        {
            // 🚨 AHORA SE ENVÍA EL NOMBRE DEL OBJETO DE BASURA, NO DEL JUGADOR/CONTROLADOR 🚨
            TaskManager.Instance.NotifyTrashCleaned(closestTrash.gameObject.name);

            Debug.Log($"[Trash Removal] Enviando a TaskManager: {closestTrash.gameObject.name}");
        }

        // Ejecuta la eliminación del objeto de basura
        closestTrash.EliminateTrash();

        // Eliminar de la lista de proximidad
        nearbyTrash.Remove(closestTrash);
    }

    // ================== LÓGICA INTERNA DE EQUIPO ==================

    private void TryPickupTool()
    {
        Camera rayCamera = Camera.main;
        if (!rayCamera) return;

        Vector3 origin = rayCamera.transform.position + rayCamera.transform.forward * 0.15f;
        Vector3 dir = rayCamera.transform.forward;

        ToolDescriptor td = null;

        // 1. Raycast
        if (Physics.Raycast(origin, dir, out RaycastHit rayHit, pickupRange, toolsLayer, QueryTriggerInteraction.Ignore))
        {
            td = rayHit.collider.GetComponentInParent<ToolDescriptor>();
        }

        // 2. Fallback Overlap
        if (td == null)
        {
            Vector3 probe = transform.position + transform.forward * 1.0f;
            var around = Physics.OverlapSphere(probe, 0.85f, toolsLayer, QueryTriggerInteraction.Collide);
            foreach (var c in around)
            {
                if (c.transform.IsChildOf(transform)) continue;
                td = c.GetComponentInParent<ToolDescriptor>();
                if (td != null) break;
            }
        }

        if (td != null)
        {
            if (td.TryGetComponent<Carryable>(out var carryable))
            {
                // El 'null' asume que el PickUp en Carryable no necesita los colliders aquí
                carryable.PickUp(holdPoint, null);
            }

            RegisterTool(td);
        }
    }

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
    }

    // ================== UTILITIES Y ANIMACIÓN ==================

    private static void SetAllCollidersTrigger(GameObject go, bool isTrigger)
    {
        var cols = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.isTrigger = isTrigger;
    }

    private void UpdateCleaningLayer(bool shouldUseCleaning)
    {
        if (anim == null) return;

        anim.SetBool("IsCleaning", shouldUseCleaning);
        anim.SetBool("IsHolding", CurrentTool != null);

        if (cleaningLayerIndex >= 0)
        {
            float cur = anim.GetLayerWeight(cleaningLayerIndex);
            float tgt = shouldUseCleaning ? 1f : 0f;

            anim.SetLayerWeight(cleaningLayerIndex, Mathf.MoveTowards(cur, tgt, Time.deltaTime * layerBlendSpeed));
        }
    }
}