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
    private HeldItemSlot heldItemSlot; // Gestor de herramientas (donde se corrigió EquipQuickTool)
    private UIPauseController toolPanelIdea;
    private CleaningController cleaningController;
    private Carryable carried;

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
    [SerializeField] private KeyCode attackKey = KeyCode.F; // Tecla para usar herramienta/ataque
    [SerializeField] private KeyCode scorePanelToggleKey = KeyCode.Tab;
    [SerializeField] private KeyCode tool1Key = KeyCode.Alpha1; // Equipar Tool 1
    [SerializeField] private KeyCode tool2Key = KeyCode.Alpha2; // Equipar Tool 2


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
        // 🚨 CRÍTICO: Obtener el gestor de slot/herramientas.
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

        HandleToolEquipInputs();

        // 🚨 AQUÍ ESTÁ LA FUNCIÓN QUE FALTABA
        HandleAttackAndToolUse();

        if (Input.GetKeyDown(pickupKey))
            TryPickupOrDrop();

        if (Input.GetKeyDown(generalInteractKey))
            TryGeneralInteract();
    }

    // =========================================================================
    // ⚔️ FUNCIÓN FALTANTE: Ataque y Uso de Herramienta (KeyCode.F) ⚔️
    // =========================================================================

    /// <summary>
    /// Maneja el Input de Ataque/Uso de Herramienta (KeyCode.F).
    /// </summary>
    private void HandleAttackAndToolUse()
    {
        if (heldItemSlot == null) return;

        // Usamos GetKeyDown para ejecutar la acción una sola vez
        if (!Input.GetKeyDown(attackKey)) return;

        ToolDescriptor activeTool = heldItemSlot.CurrentTool;

        if (activeTool != null)
        {
            // Si hay una herramienta equipada, intentamos USARLA.
            // Nota: Aquí se usa 'CleanTool' como ejemplo de tu código original. 
            // Si CleaningTool usa otro método, se añade la lógica.

            CleanTool cleanScript = activeTool.GetComponent<CleanTool>();

            if (cleanScript != null)
            {
                // Si la herramienta tiene un CleanTool, la usamos.
                // Esto reemplaza la lógica que tenías en el Update original.
                cleanScript.Clean();
                Debug.Log($"Limpiando con CleanTool (F) - {activeTool.name}.");
            }
            else
            {
                // Lógica genérica o para CleaningTool, asumiendo que tiene un método TryUse()
                activeTool.TryUse();
                Debug.Log($"Usando la herramienta activa (F) - {activeTool.name}.");
            }
        }
        else if (nearbyAttackable != null)
        {
            // Si no hay herramienta, atacamos al objetivo cercano detectado por el raycast.
            nearbyAttackable.ReceiveAttack();
            Debug.Log($"Ataque directo (F) ejecutado sobre {currentRaycastHitObject.name}.");
        }
        else
        {
            Debug.Log("Tecla de ataque (F) presionada, pero no hay herramienta activa ni objetivo atacable cerca.");
        }

        // Eliminamos la lógica de KeyUp que destruía la herramienta al soltar F. 
        // La destrucción/soltar se maneja con la tecla Q en TryPickupOrDrop().
    }

    // =========================================================================
    // 🆕 NUEVAS FUNCIONES PARA MANEJAR INPUTS DE HERRAMIENTAS (1 y 2) 
    // =========================================================================

    // EN PlayerInteraction.cs
    private void HandleToolEquipInputs()
    {
        if (heldItemSlot == null) return;

        if (Input.GetKeyDown(tool1Key))
        {
            // 🚨 DEBUG CRÍTICO: Confirma que la tecla 1 funciona
            Debug.Log($"[INPUT CHECK] Tecla {tool1Key} presionada. Intentando equipar Tool 1.");

            heldItemSlot.EquipQuickTool(1);
            animCtrl?.TriggerInteract();
        }

        if (Input.GetKeyDown(tool2Key))
        {
            // 🚨 DEBUG CRÍTICO: Confirma que la tecla 2 funciona
            Debug.Log($"[INPUT CHECK] Tecla {tool2Key} presionada. Intentando equipar Tool 2.");

            heldItemSlot.EquipQuickTool(2);
            animCtrl?.TriggerInteract();
        }
    }

    // =========================================================================
    // 🖱️ FUNCIÓN PARA LIMPIEZA CON CLICK DEL MOUSE (Y DECISIÓN) 🖱️
    // =========================================================================

    private void HandleMouseClickCleaning()
    {
        // ... (El resto de la lógica de HandleMouseClickCleaning permanece igual) ...
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
                            // TaskManager.Instance.NotifyTrashCleaned(hitObject.name); // Asumo que TaskManager existe
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
                    // Asumo que CanBeCleanedBy y CleanHit existen
                    // if (dirtSpot.CanBeCleanedBy(activeTool.ToolId)) 
                    // {
                    //     float damage = activeTool.ToolPower;
                    //     dirtSpot.CleanHit(damage);
                    //     activeTool.TryUse();
                    //     if (clickCleaningEffect != null)
                    //     {
                    //         Instantiate(clickCleaningEffect, hit.point, Quaternion.identity);
                    //     }
                    // }
                    // return;
                }
            }
        }
    }


    // =========================================================================
    // 🚀 FUNCIÓN DE RECOGIDA Y SUELTA (Q) 🚀
    // =========================================================================

    void TryPickupOrDrop()
    {
        if (heldItemSlot == null) return;

        bool hasToolEquipped = heldItemSlot.HasTool;

        // 1. Lógica: SOLTAR OBJETO (Carryable O herramienta)
        if (carried != null)
        {
            carried.Drop();
            carried = null;
            animCtrl?.SetHolding(false);
            animCtrl?.TriggerInteract();
            Debug.Log($"Objeto Carryable soltado ({pickupKey}).");
            return;
        }

        if (hasToolEquipped)
        {
            heldItemSlot.DestroyCurrentTool();
            animCtrl?.SetHolding(false);
            animCtrl?.TriggerInteract();
            Debug.Log($"Herramienta destruida/desequipada ({pickupKey}).");
            return;
        }


        // 2. Lógica: RECOGER OBJETO (Solo si no lleva nada)
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

                Destroy(nearbyCarryable.gameObject);

                nearbyCarryable = null;
                animCtrl?.SetHolding(true);
                animCtrl?.TriggerInteract();
                Debug.Log($"Herramienta '{td.name}' equipada al recoger con {pickupKey}.");
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
            carried = nearbyCarryable;
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
    // ... (Estas funciones permanecen igual) ...
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