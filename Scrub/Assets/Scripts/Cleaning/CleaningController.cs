// CleaningController.cs - CORREGIDO PARA DISTINTOS TIPOS DE BASURA
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

    // 📢 REFERENCIA DEL PLAYER
    [SerializeField] private Collider[] playerColliders;

    // ---------------- Capas y rangos ----------------
    [Header("Layers & Ranges")]
    [SerializeField] private LayerMask toolsLayer;
    [SerializeField] private LayerMask carryableLayer; // 📢 Capa para objetos Carryable/Trash
    [SerializeField] private float pickupRange = 3.5f;
    [SerializeField] private float dropForce = 1.5f;

    // ---------------- Input ----------------
    [Header("Input (teclas simples)")]
    [SerializeField] private KeyCode pickupDropKey = KeyCode.E;
    [SerializeField] private KeyCode cleanKey = KeyCode.R;
    [SerializeField] private KeyCode disposeKey = KeyCode.F; // Usado para barrer basura no transportable

    // ---------------- Limpieza ----------------
    [Header("Cleaning")]
    [SerializeField] private float damagePerHit = 1f;
    [SerializeField] private bool requireCorrectTool = true;
    [SerializeField] private string[] validToolIds = { "Mop", "Sponge", "Vacuum", "Escoba" };
    [SerializeField] private string dirtTag = "Dirt";
    // 📢 TAG para objetos transportables (bolsas, etc.) que se depositan en el basurero
    [SerializeField] private string carryableTrashTag = "Trash";
    // 📢 TAG para basura pequeña que se barre (puede ser "TrashObject" o similar)
    [SerializeField] private string sweepableTrashTag = "Basura";

    // ---------------- Animación ----------------
    [Header("Animation Layer")]
    [SerializeField] private string cleaningLayerName = "Clean";
    [SerializeField] private float layerBlendSpeed = 12f;

    // ---------------- Estado ----------------
    public ToolDescriptor CurrentTool { get; private set; }
    public Carryable CurrentCarryable { get; private set; }
    private List<DirtSpot> nearbyDirt = new List<DirtSpot>();
    // Lista para basura que se barre (usando el tag sweepableTrashTag)
    private List<TrashObject> nearbyTrash = new List<TrashObject>();
    // Lista para objetos Carryable (herramientas O basura transportable)
    private List<Carryable> nearbyCarryables = new List<Carryable>();

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
        // ---- PICKUP / DROP (Tecla E) ----
        if (Input.GetKeyDown(pickupDropKey))
        {
            if (CurrentTool != null || CurrentCarryable != null)
            {
                DropHeldObject();
            }
            else
            {
                TryPickupObject();
            }
        }

        // ---- DISPOSICIÓN / BARRIDO (Tecla F) ----
        if (Input.GetKeyDown(disposeKey))
        {
            // La lógica de "depositar" una bolsa de basura está ahora en DropHeldObject,
            // que llama a Carryable.Drop(), y el TrashCan.cs se encarga del resto.

            // Si llevamos la escoba y hay basura que barrer, barremos.
            if (CurrentTool != null && CurrentTool.ToolId == "Escoba" && nearbyTrash.Count > 0)
            {
                TryRemoveTrash("Escoba");
            }
            // Si no llevamos nada, podemos intentar recoger basura pequeña (sweepableTrashTag)
            // Esto es solo un ejemplo de funcionalidad, el "barrido" se asume que elimina la basura pequeña.
        }

        bool holding = CurrentCarryable != null;
        bool dirtNearby = nearbyDirt.Count > 0;
        bool trashNearby = nearbyTrash.Count > 0;
        bool cleanPressed = Input.GetKeyDown(cleanKey) || Input.GetButtonDown("Fire1");

        if (CurrentTool != null && cleanPressed && dirtNearby)
        {
            ApplyCleanHit();
        }

        UpdateCleaningLayer(holding && (dirtNearby || trashNearby));
    }

    // ================== MÉTODOS PÚBLICOS DE INTERACCIÓN ==================

    // 📢 MÉTODO MODIFICADO
    public void RegisterCarryable(Carryable carryableObject)
    {
        if (carryableObject == null || CurrentCarryable != null) return;

        // 1. Verifica si es una herramienta (ToolDescriptor)
        ToolDescriptor tool = carryableObject.GetComponent<ToolDescriptor>();
        if (tool != null)
        {
            CurrentTool = tool;
        }

        // 2. Establece el objeto transportado
        CurrentCarryable = carryableObject;

        // 3. Llama al PickUp del Carryable, pasándole los colliders del jugador para ignorar colisiones
        carryableObject.PickUp(holdPoint, playerColliders);

        SetAllCollidersTrigger(carryableObject.gameObject, true);
        if (anim != null) anim.SetBool("IsHolding", true);

        Debug.Log($"🛠️ Objeto recogido: {carryableObject.name} (Tipo: {(tool != null ? "Herramienta" : "Basura")})");
    }

    // 📢 MÉTODO MODIFICADO
    public void DropHeldObject()
    {
        Carryable carryable = CurrentCarryable;
        if (carryable == null) return;

        // Limpieza de referencias
        CurrentCarryable = null;
        CurrentTool = null;

        // El Carryable se encarga de restaurar la cinemática, la gravedad y la escala.
        carryable.Drop(transform.forward, dropForce);

        // Restaurar colisionadores y animación
        SetAllCollidersTrigger(carryable.gameObject, false);
        if (anim != null) anim.SetBool("IsHolding", false);
    }

    // ================== LÓGICA INTERNA DE EQUIPO ==================

    // 📢 MÉTODO CLAVE MODIFICADO
    private void TryPickupObject()
    {
        Camera rayCamera = Camera.main;
        if (!rayCamera) return;

        Vector3 origin = rayCamera.transform.position + rayCamera.transform.forward * 0.15f;
        Vector3 dir = rayCamera.transform.forward;

        Carryable targetCarryable = null;
        ToolDescriptor targetTool = null;

        // Combinar capas de herramientas y objetos carryable (basura)
        LayerMask targetLayer = toolsLayer | carryableLayer;

        // 1. Raycast
        if (Physics.Raycast(origin, dir, out RaycastHit rayHit, pickupRange, targetLayer, QueryTriggerInteraction.Ignore))
        {
            targetCarryable = rayHit.collider.GetComponentInParent<Carryable>();
            targetTool = targetCarryable?.GetComponent<ToolDescriptor>();
        }

        // 2. Fallback Overlap
        if (targetCarryable == null)
        {
            Vector3 probe = transform.position + transform.forward * 1.0f;
            var around = Physics.OverlapSphere(probe, 0.85f, targetLayer, QueryTriggerInteraction.Collide);
            foreach (var c in around)
            {
                if (c.transform.IsChildOf(transform)) continue;
                targetCarryable = c.GetComponentInParent<Carryable>();
                if (targetCarryable != null)
                {
                    targetTool = targetCarryable.GetComponent<ToolDescriptor>();
                    break;
                }
            }
        }

        if (targetCarryable != null)
        {
            // 📢 Lógica de Prioridad de Recogida:
            // Prioridad 1: Si es una Herramienta O una Basura Transportable (Carryable + Tag)
            if (targetTool != null || targetCarryable.CompareTag(carryableTrashTag))
            {
                RegisterCarryable(targetCarryable);
            }
        }
    }


    // ================== DETECCIÓN POR TRIGGER ==================

    // 📢 MÉTODO MODIFICADO (Añadimos Carryable/Trash y corregimos Tags)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(dirtTag))
        {
            DirtSpot dirt = other.GetComponent<DirtSpot>() ?? other.GetComponentInParent<DirtSpot>();
            if (dirt != null && !nearbyDirt.Contains(dirt))
            {
                nearbyDirt.Add(dirt);
                Debug.Log($"🧹 DirtSpot detectado: {dirt.name}");
            }
        }

        // Detección de objetos transportables (Carryable, que pueden ser Trash o Tool)
        if (other.GetComponent<Carryable>() != null)
        {
            Carryable carryable = other.GetComponent<Carryable>() ?? other.GetComponentInParent<Carryable>();
            if (carryable != null && !nearbyCarryables.Contains(carryable))
            {
                nearbyCarryables.Add(carryable);
            }
        }

        // Detección de basura que se barre (Sweepable Trash)
        if (other.CompareTag(sweepableTrashTag))
        {
            TrashObject trash = other.GetComponent<TrashObject>() ?? other.GetComponentInParent<TrashObject>();
            if (trash != null && !nearbyTrash.Contains(trash))
            {
                nearbyTrash.Add(trash);
                Debug.Log($"🗑️ TrashObject (destruible) detectado: {trash.name}");
            }
        }
    }

    // 📢 MÉTODO MODIFICADO
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

        if (other.GetComponent<Carryable>() != null)
        {
            Carryable carryable = other.GetComponent<Carryable>() ?? other.GetComponentInParent<Carryable>();
            if (carryable != null && nearbyCarryables.Contains(carryable))
            {
                nearbyCarryables.Remove(carryable);
            }
        }

        if (other.CompareTag(sweepableTrashTag))
        {
            TrashObject trash = other.GetComponent<TrashObject>() ?? other.GetComponentInParent<TrashObject>();
            if (trash != null && nearbyTrash.Contains(trash))
            {
                nearbyTrash.Remove(trash);
            }
        }
    }

    // ================== LÓGICA DE BARRIDO ==================

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
            CurrentTool = null;
            DropHeldObject();
            return;
        }

        if (closestTrash != null)
        {
            // La lógica de eliminación de basura (barrer) va aquí.
            Destroy(closestTrash.gameObject);

            Debug.Log($"🗑️ Basura eliminada: {closestTrash.name}");
            nearbyTrash.Remove(closestTrash);
        }
    }

    // Resto de métodos de utilidad (ApplyCleanHit, UpdateCleaningLayer, SetAllCollidersTrigger, DebugNearbyObjects)
    // se mantienen igual que en el código anterior.

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
            CurrentTool = null;
            DropHeldObject();
            return;
        }

        float damage = damagePerHit * CurrentTool.ToolPower;

        if (requireCorrectTool && !closestDirt.CanBeCleanedBy(CurrentTool.ToolId))
        {
            Debug.LogWarning($"[Clean Hit] Herramienta incorrecta: {CurrentTool.ToolId} para {closestDirt.name}");
            return;
        }

        closestDirt.CleanHit(damage);
        Debug.Log($"🧹 Aplicando {damage} de daño a {closestDirt.name}");

        // if (AudioManager.Instance != null)
        // {
        //     // AudioManager.Instance.PlayCleanSFX();
        // }
    }

    private void UpdateCleaningLayer(bool shouldUseCleaning)
    {
        if (anim == null) return;

        anim.SetBool("IsCleaning", shouldUseCleaning);
        anim.SetBool("IsHolding", CurrentCarryable != null);

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
        Debug.Log($"Dirt Spots cercanos: {nearbyDirt.Count}");
        Debug.Log($"Carryable Objects cercanos: {nearbyCarryables.Count}");
        Debug.Log($"Trash Objects (Destruibles) cercanos: {nearbyTrash.Count}");
        Debug.Log($"Objeto/Herramienta actual: {(CurrentCarryable != null ? CurrentCarryable.name : "Ninguno")}");
    }
}