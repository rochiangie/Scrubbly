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
// NOTA: Se asume que HeldItemSlot es el nuevo gestor de herramientas.

public class PlayerInteraction : MonoBehaviour
{
    // --- VARIABLES ORIGINALES ---
    [Header("Referencias")]
    public Transform holdPoint;
    public PlayerAnimationController animCtrl;

    // 🚨 GESTORES CRÍTICOS
    private HeldItemSlot heldItemSlot; // Nuevo gestor de herramientas
    private UIPauseController toolPanelIdea;

    private CleaningController cleaningController;
    private Carryable carried; // Mantenemos carried solo para objetos Carryable NO herramientas

    // ***************************************************************
    // 🗑️ VARIABLES DE DETECCIÓN (USADAS POR EL RAYCAST) 
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
    [SerializeField] private string trashDestructionToolId = "GarbageBagTool";

    [Header("Detección Raycast")]
    public float interactionRange = 3.0f;
    public LayerMask interactableLayer;

    [Header("Tags de Objetos")]
    [SerializeField] private string memorieTag = "Memorie";
    [SerializeField] private string trashTag = "Basura";

    private Camera mainCamera;

    void Awake()
    {
        // 🚨 CRÍTICO: Obtener el nuevo gestor de slot
        heldItemSlot = GetComponent<HeldItemSlot>();
        if (heldItemSlot == null)
            Debug.LogError("PlayerInteraction: No se encontró HeldItemSlot. **Adjunte HeldItemSlot al Player.**");

        toolPanelIdea = FindObjectOfType<UIPauseController>();
        if (toolPanelIdea == null)
            Debug.LogWarning("PlayerInteraction: No se encontró UIPauseController.");

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

        // 🚨 CRÍTICO: Verificar si heldItemSlot es nulo antes de usarlo.
        if (heldItemSlot != null)
        {
            ToolDescriptor activeTool = heldItemSlot.CurrentTool;

            if (Input.GetKeyDown(attackKey))
            {
                if (activeTool != null)
                {
                    CleanTool cleanScript = activeTool.GetComponent<CleanTool>();
                    if (cleanScript != null)
                    {
                        cleanScript.Clean();
                        Debug.Log("Limpiando con la herramienta activa (F).");
                    }
                }
                else if (nearbyAttackable != null)
                {
                    nearbyAttackable.ReceiveAttack();
                    Debug.Log($"Ataque directo (F) ejecutado sobre {currentRaycastHitObject.name}.");
                }
            }

            // DESTRUIR LA HERRAMIENTA AL SOLTAR F
            if (Input.GetKeyUp(attackKey))
            {
                if (heldItemSlot.HasTool)
                {
                    heldItemSlot.DestroyCurrentTool();
                }
            }
        }

        if (Input.GetKeyDown(pickupKey))
            TryPickup();

        if (Input.GetKeyDown(generalInteractKey))
            TryGeneralInteract();
    }


    // =========================================================================
    // 🖱️ FUNCIÓN PARA LIMPIEZA CON CLICK DEL MOUSE (Y DECISIÓN) 🖱️
    // =========================================================================

    private void HandleMouseClickCleaning()
    {
        if (Input.GetMouseButtonDown(0) && Time.timeScale > 0)
        {
            if (mainCamera == null) return;

            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, mouseInteractionDistance))
            {
                GameObject hitObject = hit.collider.gameObject;

                // 1. COMPROBACIÓN DE DECISIÓN (Memorie Tag)
                if (hitObject.CompareTag(memorieTag))
                {
                    MemorieObject mObject = hitObject.GetComponentInParent<MemorieObject>();

                    if (mObject != null && toolPanelIdea != null)
                    {
                        mObject.StartDecisionProcess(toolPanelIdea);

                        animCtrl?.TriggerInteract();
                        nearbyCarryable = null;
                        return;
                    }
                }

                // 2. VERIFICACIÓN CRÍTICA DE HERRAMIENTA
                if (heldItemSlot == null)
                {
                    Debug.LogWarning("[Click Cleaning] No se puede limpiar: HeldItemSlot es nulo.");
                    return;
                }

                ToolDescriptor activeTool = heldItemSlot.CurrentTool;

                if (activeTool == null)
                {
                    Debug.LogWarning("[Click Cleaning] No se puede limpiar/destruir. Necesitas una herramienta equipada.");
                    return;
                }

                // 3. INTENTAR DESTRUIR BASURA (Tag: Basura) 
                if (hitObject.CompareTag(trashTag))
                {
                    if (activeTool.ToolId == trashDestructionToolId)
                    {
                        if (TaskManager.Instance != null)
                        {
                            TaskManager.Instance.NotifyTrashCleaned(hitObject.name);
                        }
                        if (clickCleaningEffect != null)
                        {
                            Instantiate(clickCleaningEffect, hit.point, Quaternion.identity);
                        }

                        Destroy(hitObject);
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"[Click Cleaning] No se puede destruir la basura con {activeTool.ToolId}. Se requiere: {trashDestructionToolId}");
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
                    }
                    return;
                }
            }
        }
    }


    // =========================================================================
    // 🚀 FUNCIONES DE RAYCAST Y RECOGIDA (Q) 🚀
    // =========================================================================

    void TryPickup()
    {
        // 🚨 CRÍTICO: Chequeo de nulidad para heldItemSlot.
        if (heldItemSlot == null) return;

        bool hasToolEquipped = heldItemSlot.HasTool;

        // 1. Lógica: SOLTAR OBJETO (Carryable O herramienta)
        if (carried != null)
        {
            // Solo objetos Carryable (no herramientas)
            carried.Drop();
            carried = null;
            animCtrl?.SetHolding(false);
            animCtrl?.TriggerInteract();
            Debug.Log($"Objeto Carryable soltado ({pickupKey}).");
            return;
        }

        // Si el jugador tiene una herramienta equipada, también se suelta/destruye
        if (hasToolEquipped)
        {
            heldItemSlot.DestroyCurrentTool();
            animCtrl?.SetHolding(false);
            animCtrl?.TriggerInteract();
            Debug.Log($"Herramienta destruida al soltar ({pickupKey}).");
            return;
        }


        // 2. Lógica: RECOGER OBJETO
        if (nearbyCarryable != null)
        {
            // A. Si es objeto de memoria
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

            // B. Si es una Herramienta (ToolDescriptor)
            ToolDescriptor td = nearbyCarryable.GetComponent<ToolDescriptor>() ?? nearbyCarryable.GetComponentInParent<ToolDescriptor>();

            if (td != null)
            {
                // Equipamos la herramienta 
                heldItemSlot.EquipToolPrefab(td.gameObject);

                // Destruir el objeto del mundo que acabamos de "recoger"
                Destroy(nearbyCarryable.gameObject);

                nearbyCarryable = null;
                animCtrl?.SetHolding(true);
                animCtrl?.TriggerInteract();
                return;
            }

            // C. Si es un Carryable (NO herramienta)
            if (!holdPoint)
            {
                var hp = new GameObject("HoldPoint").transform;
                hp.SetParent(transform);
                hp.localPosition = new Vector3(0, 1.2f, 0.6f);
                holdPoint = hp;
            }

            nearbyCarryable.PickUp(holdPoint, playerColliders);
            carried = nearbyCarryable; // Almacenar como objeto Carryable
            nearbyCarryable = null;
            animCtrl?.SetHolding(true);
            animCtrl?.TriggerInteract();

            Debug.Log($"¡Objeto {carried.name} recogido con la tecla {pickupKey}!");
            return;
        }

        Debug.Log("[Interacción Fallida] No hay objeto que soltar ni recoger (Q).");
    }

    void TryGeneralInteract()
    {
        if (currentDoorInteractable != null)
        {
            currentDoorInteractable.Interact();
            animCtrl?.TriggerInteract();
            Debug.Log($"Interacción General (Puerta) ejecutada con {generalInteractKey}.");
            return;
        }
        Debug.Log("[Interacción Fallida] No hay Interacción General (Puerta) activa (E).");
    }

    // =========================================================================
    // Lógica de Detección (Corregida) 
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

            // 🚨 SOLUCIÓN: Verificar heldItemSlot y su estado.
            bool isToolEquipped = (heldItemSlot != null && heldItemSlot.HasTool);

            // Solo buscar Carryable si no llevamos nada.
            if (carried == null && !isToolEquipped)
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
        if (TaskManager.Instance != null)
        {
            int remaining = TaskManager.Instance.GetRemainingCleanableItemsCount();
            Debug.Log($"[DEBUG CONTEO 'G'] Ítems Faltantes: {remaining}");
        }
    }
}