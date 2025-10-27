using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Animator))]
public class CleaningController : MonoBehaviour
{
    // ---------------- Refs ----------------
    [Header("Refs")]
    [SerializeField] private Transform[] holdPoints = new Transform[1];
    [SerializeField] private Animator anim;
    [SerializeField] private Collider[] playerColliders;

    // ---------------- Capas y rangos ----------------
    [Header("Layers & Ranges")]
    [SerializeField] private LayerMask toolsLayer;
    [SerializeField] private LayerMask carryableLayer;
    [SerializeField] private float pickupRange = 3.5f;
    [SerializeField] private float dropForce = 1.5f;

    // ---------------- Input ----------------
    [Header("Input (teclas simples)")]
    // 📢 CAMBIO: Eliminamos KeyCode.E. Usaremos Input.GetButtonDown("Fire1") para el click.
    [SerializeField] private KeyCode cleanKey = KeyCode.R;
    [SerializeField] private KeyCode disposeKey = KeyCode.F;

    // ---------------- Limpieza ----------------
    [Header("Cleaning")]
    [SerializeField] private float damagePerHit = 1f;
    [SerializeField] private bool requireCorrectTool = true;
    [SerializeField] private string[] validToolIds = { "Mop", "Sponge", "Vacuum", "Escoba" };
    [SerializeField] private string dirtTag = "Dirt";
    [SerializeField] private string carryableTrashTag = "Trash";
    [SerializeField] private string sweepableTrashTag = "Basura";

    // ---------------- Animación ----------------
    [Header("Animation Layer")]
    [SerializeField] private string cleaningLayerName = "Clean";
    [SerializeField] private float layerBlendSpeed = 12f;

    // ---------------- Estado ----------------
    public List<Carryable> CarriedItems { get; private set; } = new List<Carryable>();

    public Carryable CurrentCarryable => CarriedItems.FirstOrDefault();
    public ToolDescriptor CurrentTool => CurrentCarryable?.GetComponent<ToolDescriptor>();

    private List<DirtSpot> nearbyDirt = new List<DirtSpot>();
    private List<TrashObject> nearbyTrash = new List<TrashObject>();
    private List<Carryable> nearbyCarryables = new List<Carryable>();

    private int cleaningLayerIndex = -1;
    private const int MAX_CARRYABLE_ITEMS = 1; // 📢 Límite de 1 objeto

    // ================== Unity ==================
    private void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (anim)
        {
            cleaningLayerIndex = anim.GetLayerIndex(cleaningLayerName);
        }

        if (holdPoints.Length < MAX_CARRYABLE_ITEMS)
        {
            Debug.LogError($"El CleaningController necesita al menos {MAX_CARRYABLE_ITEMS} punto de agarre asignado en el Inspector.");
        }
    }

    private void Update()
    {
        // ---- INTERACCIÓN PRINCIPAL (CLICK / Fire1) ----
        // 📢 CAMBIO: Usamos el botón del ratón (Fire1) para la interacción de recoger/soltar.
        if (Input.GetButtonDown("Fire1"))
        {
            // Si sostenemos algo, lo soltamos (Drop), sino, intentamos recoger (Pickup).
            if (CarriedItems.Count > 0)
            {
                DropAllHeldItems();
            }
            else
            {
                TryPickupObject();
            }
        }

        // ---- LIMPIEZA CLÁSICA (Tecla R) ----
        bool cleanPressed = Input.GetKeyDown(cleanKey);

        if (CurrentTool != null && cleanPressed && nearbyDirt.Count > 0)
        {
            ApplyCleanHit();
        }

        // ---- DISPOSICIÓN / BARRIDO (Tecla F) ----
        if (Input.GetKeyDown(disposeKey))
        {
            if (CurrentTool != null && CurrentTool.ToolId == "Escoba" && nearbyTrash.Count > 0)
            {
                TryRemoveTrash("Escoba");
            }
        }

        UpdateCleaningLayer(CarriedItems.Count > 0 && (nearbyDirt.Count > 0 || nearbyTrash.Count > 0));
    }

    // ================== MÉTODOS PÚBLICOS DE INTERACCIÓN ==================

    public void RegisterCarryable(Carryable carryableObject)
    {
        // Verifica el límite de 1
        if (carryableObject == null || CarriedItems.Count >= MAX_CARRYABLE_ITEMS) return;

        // 1. Asignar el punto de agarre (slot 0)
        int slotIndex = 0;
        Transform parentPoint = holdPoints[slotIndex];

        // 2. Registrar el objeto
        CarriedItems.Add(carryableObject);

        // 3. Llama al PickUp del Carryable
        carryableObject.PickUp(parentPoint, playerColliders);

        // 4. Configurar colisionadores y animación
        SetAllCollidersTrigger(carryableObject.gameObject, true);
        if (anim != null) anim.SetBool("IsHolding", true);

        Debug.Log($"🛠️ Objeto recogido: {carryableObject.name} en slot {slotIndex}");
    }

    public void DropAllHeldItems()
    {
        float totalItems = CarriedItems.Count;

        foreach (var carryable in CarriedItems.ToList())
        {
            if (carryable != null)
            {
                // Dirección de soltado simple
                carryable.Drop(transform.forward, dropForce);

                SetAllCollidersTrigger(carryable.gameObject, false);
            }
        }

        CarriedItems.Clear();
        if (anim != null) anim.SetBool("IsHolding", false);

        Debug.Log($"Soltados {totalItems} objetos.");
    }

    // ================== LÓGICA INTERNA DE EQUIPO ==================

    private void TryPickupObject()
    {
        // El límite de recolección a 1 está aquí
        if (CarriedItems.Count >= MAX_CARRYABLE_ITEMS)
        {
            Debug.LogWarning("Ya estás sosteniendo el máximo de objetos.");
            return;
        }

        Camera rayCamera = Camera.main;
        if (!rayCamera) return;

        LayerMask targetLayer = toolsLayer | carryableLayer;

        Carryable targetCarryable = null;

        // 1. Raycast (Búsqueda más precisa)
        // Raycast es mejor para interacciones de 'Click', ya que mira donde apunta la cámara.
        Vector3 origin = rayCamera.transform.position + rayCamera.transform.forward * 0.15f;
        Vector3 dir = rayCamera.transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit rayHit, pickupRange, targetLayer, QueryTriggerInteraction.Ignore))
        {
            targetCarryable = rayHit.collider.GetComponentInParent<Carryable>();
        }

        // 2. Fallback OverlapSphere (Si el Raycast falla, busca alrededor)
        if (targetCarryable == null)
        {
            Vector3 probe = transform.position + transform.forward * 1.0f;
            float currentRange = pickupRange * 0.5f;
            var nearbyColliders = Physics.OverlapSphere(probe, currentRange, targetLayer, QueryTriggerInteraction.Collide);

            foreach (var c in nearbyColliders)
            {
                if (!c.transform.IsChildOf(transform) && c.TryGetComponent<Carryable>(out var carryable) && !CarriedItems.Contains(carryable))
                {
                    targetCarryable = carryable;
                    break; // Rompe al encontrar el primer objeto válido.
                }
            }
        }

        // 3. Registrar el objeto (Si se encontró un ÚNICO objeto válido)
        if (targetCarryable != null)
        {
            if (!CarriedItems.Contains(targetCarryable))
            {
                RegisterCarryable(targetCarryable);
            }
        }

        if (targetCarryable == null)
        {
            Debug.Log("No se encontraron objetos Carryable o herramientas cercanas para recoger.");
        }
    }

    // ================== LÓGICA DE LIMPIEZA Y BARRIDO ==================

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
        if (!successfullyUsed)
        {
            CurrentTool.TryGetComponent<Carryable>(out var brokenCarryable);
            if (brokenCarryable != null)
            {
                CarriedItems.Remove(brokenCarryable);
                Destroy(brokenCarryable.gameObject);
                if (CarriedItems.Count == 0) UpdateCleaningLayer(false);
            }
            return;
        }

        float damage = damagePerHit * CurrentTool.ToolPower;

        if (requireCorrectTool && !closestDirt.CanBeCleanedBy(CurrentTool.ToolId))
        {
            Debug.LogWarning($"[Clean Hit] Herramienta incorrecta: {CurrentTool.ToolId} para {closestDirt.name}");
            return;
        }

        closestDirt.CleanHit(damage);
    }

    private void TryRemoveTrash(string requiredToolId)
    {
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
            return;
        }

        if (closestTrash != null)
        {
            Destroy(closestTrash.gameObject);
            Debug.Log($"🗑️ Basura eliminada: {closestTrash.name}");
            nearbyTrash.Remove(closestTrash);
        }
    }

    // ================== DETECCIÓN Y UTILIDADES ==================

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(dirtTag))
        {
            DirtSpot dirt = other.GetComponent<DirtSpot>() ?? other.GetComponentInParent<DirtSpot>();
            if (dirt != null && !nearbyDirt.Contains(dirt)) nearbyDirt.Add(dirt);
        }

        if (other.GetComponent<Carryable>() != null)
        {
            Carryable carryable = other.GetComponent<Carryable>() ?? other.GetComponentInParent<Carryable>();
            if (carryable != null && !nearbyCarryables.Contains(carryable)) nearbyCarryables.Add(carryable);
        }

        if (other.CompareTag(sweepableTrashTag))
        {
            TrashObject trash = other.GetComponent<TrashObject>() ?? other.GetComponentInParent<TrashObject>();
            if (trash != null && !nearbyTrash.Contains(trash)) nearbyTrash.Add(trash);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(dirtTag))
        {
            DirtSpot dirt = other.GetComponent<DirtSpot>() ?? other.GetComponentInParent<DirtSpot>();
            if (dirt != null && nearbyDirt.Contains(dirt)) nearbyDirt.Remove(dirt);
        }

        if (other.GetComponent<Carryable>() != null)
        {
            Carryable carryable = other.GetComponent<Carryable>() ?? other.GetComponentInParent<Carryable>();
            if (carryable != null && nearbyCarryables.Contains(carryable)) nearbyCarryables.Remove(carryable);
        }

        if (other.CompareTag(sweepableTrashTag))
        {
            TrashObject trash = other.GetComponent<TrashObject>() ?? other.GetComponentInParent<TrashObject>();
            if (trash != null && nearbyTrash.Contains(trash)) nearbyTrash.Remove(trash);
        }
    }

    private void UpdateCleaningLayer(bool shouldUseCleaning)
    {
        if (anim == null) return;

        anim.SetBool("IsCleaning", shouldUseCleaning);
        anim.SetBool("IsHolding", CarriedItems.Count > 0);

        if (cleaningLayerIndex >= 0)
        {
            float cur = anim.GetLayerWeight(cleaningLayerIndex);
            float tgt = shouldUseCleaning ? 1f : 0f;

            anim.SetLayerWeight(cleaningLayerIndex, Mathf.MoveTowards(cur, tgt, Time.deltaTime * layerBlendSpeed));
        }
    }

    private static void SetAllCollidersTrigger(GameObject go, bool isTrigger)
    {
        var cols = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.isTrigger = isTrigger;
    }

    [ContextMenu("Debug Nearby Objects")]
    public void DebugNearbyObjects()
    {
        Debug.Log($"=== 🎯 DEBUG NEARBY OBJECTS ===");
        Debug.Log($"Objetos transportados: {CarriedItems.Count} / {MAX_CARRYABLE_ITEMS}");
        Debug.Log($"Herramienta activa: {(CurrentTool != null ? CurrentTool.ToolId : "Ninguna")}");
    }
}