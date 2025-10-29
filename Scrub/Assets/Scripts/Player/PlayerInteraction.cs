using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

// ----------------------------------------------------
// INTERFACES (Asumo que existen)
// ----------------------------------------------------
public interface IInteractable { void Interact(); }
public interface IAttackable { void ReceiveAttack(); }

public class PlayerInteraction : MonoBehaviour
{
    // --- VARIABLES ORIGINALES ---
    [Header("Referencias")]
    public Transform holdPoint;
    public PlayerAnimationController animCtrl;

    // 🚨 GESTORES CRÍTICOS
    private HeldItemSlot heldItemSlot;
    private UIPauseController toolPanelIdea;
    private CleaningController cleaningController;
    private Carryable carried;

    public string toolTag = "CleaningTool";

    // ***************************************************************
    // 🗑️ VARIABLES DE DETECCIÓN
    // ***************************************************************
    private IInteractable currentDoorInteractable = null;
    private Carryable nearbyCarryable = null;
    private IAttackable nearbyAttackable = null;
    private GameObject currentRaycastHitObject = null;

    private Rigidbody playerRigidbody;
    private Collider[] playerColliders;

    [Header("Limpieza con Mouse")]
    [SerializeField] private float mouseInteractionDistance = 2.0f;
    [SerializeField] private float clickCleaningRadius = 0.5f;
    [SerializeField] private LayerMask dirtLayer;
    [SerializeField] private ParticleSystem clickCleaningEffect;

    [Header("Input Keys")]
    [SerializeField] private KeyCode generalInteractKey = KeyCode.E;
    [SerializeField] private KeyCode pickupKey = KeyCode.Q;
    [SerializeField] private KeyCode attackKey = KeyCode.F;
    [SerializeField] private KeyCode scorePanelToggleKey = KeyCode.Tab;


    [Header("Validación de Herramientas")]
    [Tooltip("ID del ToolDescriptor que PUEDE destruir objetos con el Tag 'Basura'.")]
    // 🚨 Este ID DEBE ser igual al ToolId de la Escoba (ej. "Escoba").
    [SerializeField] private string trashDestructionToolId = "Escoba";

    [Header("Detección Raycast")]
    public float interactionRange = 3.0f;
    public LayerMask interactableLayer;

    [Header("Tags de Objetos")]
    [SerializeField] private string memorieTag = "Memorie";
    [SerializeField] private string trashTag = "Basura";

    private Camera mainCamera;

    void Awake()
    {
        heldItemSlot = GetComponent<HeldItemSlot>();
        if (heldItemSlot == null)
            Debug.LogError("PlayerInteraction: No se encontró HeldItemSlot. **Adjunte HeldItemSlot al Player.**");

        toolPanelIdea = FindObjectOfType<UIPauseController>();
        cleaningController = GetComponent<CleaningController>();
        if (cleaningController == null)
            Debug.LogError("PlayerInteraction: No se encontró el CleaningController.");

        if (!animCtrl) animCtrl = GetComponentInChildren<PlayerAnimationController>() ?? GetComponent<PlayerAnimationController>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerColliders = GetComponentsInChildren<Collider>();

        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogError("PlayerInteraction: No se encontró la cámara principal.");
    }

    // =========================================================================
    // FUNCIÓN UPDATE
    // =========================================================================

    void Update()
    {
        DetectNearbyObjects();
        HandleMouseClickCleaning();

        HandleAttackAndToolUse();

        if (Input.GetKeyDown(pickupKey))
            TryDropOrDestroy();

        if (Input.GetKeyDown(generalInteractKey))
            TryGeneralInteract();
    }

    // =========================================================================
    // ⚔️ Ataque y Uso de Herramienta (KeyCode.F) ⚔️
    // =========================================================================

    private void HandleAttackAndToolUse()
    {
        if (heldItemSlot == null) return;

        if (!Input.GetKeyDown(attackKey)) return;

        ToolDescriptor activeTool = heldItemSlot.CurrentTool;

        if (activeTool != null)
        {
            CleanTool cleanScript = activeTool.GetComponent<CleanTool>();

            if (cleanScript != null)
            {
                cleanScript.Clean();
                Debug.Log($"Limpiando con CleanTool (F) - {activeTool.name}.");
            }
            else
            {
                activeTool.TryUse();
                Debug.Log($"Usando la herramienta activa (F) - {activeTool.name}.");
            }
        }
        else if (nearbyAttackable != null)
        {
            nearbyAttackable.ReceiveAttack();
            Debug.Log($"Ataque directo (F) ejecutado sobre {currentRaycastHitObject.name}.");
        }
        else
        {
            Debug.Log("Tecla de ataque (F) presionada, pero no hay herramienta activa ni objetivo atacable cerca.");
        }
    }

    // =========================================================================
    // 🖱️ FUNCIÓN PARA LIMPIEZA/RECOGIDA CON CLICK DEL MOUSE 🖱️
    // =========================================================================

    private void HandleMouseClickCleaning()
    {
        bool isPaused = toolPanelIdea != null && Time.timeScale == 0;

        if (Input.GetMouseButtonDown(0) && Time.timeScale > 0)
        {
            if (mainCamera == null) return;

            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, mouseInteractionDistance))
            {
                GameObject hitObject = hit.collider.gameObject;

                // 1. Prioridad: Recoger Objeto o Herramienta (SOLO si la mano está vacía)
                if (carried == null && !heldItemSlot.HasTool)
                {
                    Carryable clickCarryable = hitObject.GetComponentInParent<Carryable>();

                    if (clickCarryable != null)
                    {
                        ToolDescriptor td = clickCarryable.GetComponent<ToolDescriptor>() ?? clickCarryable.GetComponentInParent<ToolDescriptor>();

                        if (clickCarryable.CompareTag(memorieTag))
                        {
                            MemorieObject mObject = clickCarryable.GetComponentInParent<MemorieObject>();
                            if (mObject != null && toolPanelIdea != null)
                            {
                                mObject.StartDecisionProcess(toolPanelIdea);
                                animCtrl?.TriggerInteract();
                                if (toolPanelIdea != null && isPaused)
                                {
                                    toolPanelIdea.SetIsPaused(false);
                                }
                                return;
                            }
                        }

                        if (!holdPoint)
                        {
                            var hp = new GameObject("HoldPoint").transform;
                            hp.SetParent(transform);
                            hp.localPosition = new Vector3(0, 1.2f, 0.6f);
                            holdPoint = hp;
                        }

                        if (td != null)
                        {
                            heldItemSlot.EquipToolPrefab(td.gameObject, holdPoint);
                            Destroy(clickCarryable.gameObject);
                        }
                        else
                        {
                            clickCarryable.PickUp(holdPoint, playerColliders);
                            carried = clickCarryable;
                        }

                        // 🚨 Doble seguro de cierre de panel después de recoger con Click
                        if (toolPanelIdea != null && isPaused)
                        {
                            toolPanelIdea.SetIsPaused(false);
                            Debug.Log("Panel cerrado tras recoger con CLICK.");
                        }

                        animCtrl?.SetHolding(td != null || carried != null);
                        animCtrl?.TriggerInteract();
                        return;
                    }
                }

                // 2. Verificación de Herramienta Activa (Limpieza/Destrucción)
                if (heldItemSlot == null) return;
                ToolDescriptor activeTool = heldItemSlot.CurrentTool;
                if (activeTool == null) return;

                // 3. INTENTAR DESTRUIR BASURA (Tag: Basura) 
                if (hitObject.CompareTag(trashTag))
                {
                    string activeId = activeTool.ToolId;
                    string requiredId = trashDestructionToolId;

                    if (activeId == requiredId)
                    {
                        if (clickCleaningEffect != null)
                        {
                            Instantiate(clickCleaningEffect, hit.point, Quaternion.identity);
                        }
                        activeTool.TryUse();
                        Destroy(hitObject);
                        Debug.Log($"[Basura SUCCESS] Basura destruida. ID Activo: '{activeId}'.");
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"[Basura FAILED] Herramienta incorrecta. Activa: '{activeId}' | Requerida: '{requiredId}'.");
                        return;
                    }
                }

                // 4. INTENTAR LIMPIAR MANCHAS (Clase: DirtSpot)
                DirtSpot dirtSpot = hitObject.GetComponent<DirtSpot>();
                if (dirtSpot != null)
                {
                    if (dirtSpot.CanBeCleanedBy(activeTool.ToolId))
                    {
                        float damage = activeTool.ToolPower;
                        dirtSpot.CleanHit(damage);
                        activeTool.TryUse();

                        if (clickCleaningEffect != null)
                        {
                            Instantiate(clickCleaningEffect, hit.point, Quaternion.identity);
                        }
                        Debug.Log($"[DirtSpot SUCCESS] DirtSpot limpiado. ID Activo: '{activeTool.ToolId}'.");
                    }
                    else
                    {
                        Debug.LogWarning($"[DirtSpot FAILED] DirtSpot requiere otra herramienta. Activa: '{activeTool.ToolId}'.");
                    }
                    return;
                }
            }
        }
    }


    // =========================================================================
    // 🚀 FUNCIÓN DE SOLTAR/DESTRUIR (Q) 🚀
    // 📢 CORRECCIÓN: SOLO DESTRUYE LA TOOL, LOS CARRYABLES NORMALES SE SUELTAN.
    // =========================================================================
    void TryDropOrDestroy()
    {
        if (heldItemSlot == null && carried == null) return;

        // 1. Lógica: DESTRUIR HERRAMIENTA (Prioridad alta si existe)
        // Asumimos que heldItemSlot.DestroyCurrentTool() destruye el objeto.
        if (heldItemSlot != null && heldItemSlot.HasTool)
        {
            heldItemSlot.DestroyCurrentTool();
            animCtrl?.SetHolding(false);
            animCtrl?.TriggerInteract();
            Debug.Log($"Herramienta destruida/desequipada ({pickupKey}).");

            // **IMPORTANTE:** Si la Tool es destruida, también limpiamos la referencia de Carryable.
            // Asumiendo que ToolDescriptor y Carryable están en el mismo GameObject.
            carried = null;

            return;
        }

        // 2. Lógica: SOLTAR OBJETO Carryable (que NO es Tool)
        // Solo si 'carried' tiene valor, y no fue destruido como Tool en el paso anterior.
        if (carried != null)
        {
            // 📢 NUEVO: Asumimos que carried tiene un método Drop() o una función para soltarlo.
            // Si el carried es un objeto que debe ser soltado (ej. bolsa de basura),
            // necesitas una función para liberarlo del transform.

            // Lógica de Soltar (reemplaza la destrucción)
            // **IMPORTANTE:** Reemplaza esta línea con el método de tu Carryable para soltar.
            // Por ejemplo: carried.GetComponent<Carryable>()?.Drop(transform.forward, dropForce);
            // Si no tienes una función Drop, solo libera la jerarquía y activa las físicas:

            carried.transform.SetParent(null);
            if (carried.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                // Opcional: rb.AddForce(transform.forward * dropForce, ForceMode.Impulse);
            }

            Debug.Log($"Objeto Carryable {carried.name} soltado ({pickupKey}).");
            carried = null;
            animCtrl?.SetHolding(false);
            animCtrl?.TriggerInteract();
            return;
        }

        Debug.Log("[Interacción Fallida] No hay objeto que destruir o soltar (Q).");
    }

    // =========================================================================
    // 💡 INTERACCIÓN GENERAL (E) - Recoger y Abrir Paneles 💡
    // =========================================================================
    void TryGeneralInteract()
    {
        if (heldItemSlot == null) return;

        bool isPaused = toolPanelIdea != null && Time.timeScale == 0;

        // 🚨 PRIORIDAD 1: RECOGER/EQUIPAR 🚨
        if (nearbyCarryable != null && carried == null && !heldItemSlot.HasTool)
        {
            ToolDescriptor td = nearbyCarryable.GetComponent<ToolDescriptor>() ?? nearbyCarryable.GetComponentInParent<ToolDescriptor>();

            if (nearbyCarryable.CompareTag(memorieTag))
            {
                MemorieObject mObject = nearbyCarryable.GetComponent<MemorieObject>();
                if (mObject != null && toolPanelIdea != null)
                {
                    mObject.StartDecisionProcess(toolPanelIdea);
                    nearbyCarryable = null;
                    animCtrl?.TriggerInteract();
                    return;
                }
            }

            if (td != null || !nearbyCarryable.CompareTag(memorieTag))
            {
                if (!holdPoint)
                {
                    var hp = new GameObject("HoldPoint").transform;
                    hp.SetParent(transform);
                    hp.localPosition = new Vector3(0, 1.2f, 0.6f);
                    holdPoint = hp;
                }

                if (td != null)
                {
                    heldItemSlot.EquipToolPrefab(td.gameObject, holdPoint);
                    Destroy(nearbyCarryable.gameObject);
                    Debug.Log($"Herramienta '{td.name}' equipada al recoger con E.");
                }
                else
                {
                    nearbyCarryable.PickUp(holdPoint, playerColliders);
                    carried = nearbyCarryable;
                    Debug.Log($"Objeto {carried.name} recogido con E.");
                }

                nearbyCarryable = null;
                animCtrl?.SetHolding(td != null || carried != null);
                animCtrl?.TriggerInteract();

                // 🚨 Cierre de panel después de recoger con E
                if (toolPanelIdea != null && isPaused)
                {
                    toolPanelIdea.SetIsPaused(false);
                    Debug.Log("Panel de herramientas cerrado tras equipar/recoger objeto con E.");
                }

                return;
            }
        }

        // 🚨 PRIORIDAD 2: INTERACCIÓN GENERAL (Puertas, Estaciones, etc.)
        if (currentDoorInteractable != null)
        {
            currentDoorInteractable.Interact();
            animCtrl?.TriggerInteract();
            Debug.Log($"Interacción General ejecutada con {generalInteractKey}.");
            return;
        }

        Debug.Log("[Interacción Fallida] No hay objeto que recoger ni Interacción General activa (E).");
    }

    // =========================================================================
    // Lógica de Detección 
    // =========================================================================
    private void DetectNearbyObjects()
    {
        nearbyCarryable = null;
        nearbyAttackable = null;
        currentDoorInteractable = null;
        currentRaycastHitObject = null;

        if (mainCamera == null) return;

        RaycastHit hit;
        Vector3 rayOrigin = mainCamera.transform.position;
        Vector3 rayDirection = mainCamera.transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionRange, interactableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            currentRaycastHitObject = hitObject;

            Transform rootTransform = hit.collider.transform.root;

            bool isHoldingSomething = (carried != null || (heldItemSlot != null && heldItemSlot.HasTool));

            if (!isHoldingSomething)
            {
                Carryable c = rootTransform.GetComponent<Carryable>() ?? hitObject.GetComponentInParent<Carryable>();

                if (c != null)
                {
                    nearbyCarryable = c;
                }
            }

            IAttackable a = hitObject.GetComponentInParent<IAttackable>();
            if (a != null)
            {
                nearbyAttackable = a;
            }

            IInteractable i = hitObject.GetComponentInParent<IInteractable>();
            if (i != null)
            {
                currentDoorInteractable = i;
            }
        }
    }

    // --- FUNCIONES DE COMPATIBILIDAD Y DEBUG ---
    public void SetCurrentInteractable(IInteractable interactable)
    {
        currentDoorInteractable = interactable;
    }
    public void ClearCurrentInteractable()
    {
        currentDoorInteractable = null;
    }

    private void LogRemainingItemsCount()
    {
        // ...
    }
}