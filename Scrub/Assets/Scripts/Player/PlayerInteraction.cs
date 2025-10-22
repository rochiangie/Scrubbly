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

    // 🚨 CORRECCIÓN FINAL: Tipo de variable coincidente con el Manager de UI 🚨
    private ToolItemManager toolItemManager;
    private UIPauseController toolPanelIdea;

    private CleaningController cleaningController;
    private Carryable carried;

    // ***************************************************************
    // 🗑️ VARIABLES DE DETECCIÓN (USADAS POR EL RAYCAST) 🚀
    // ***************************************************************
    private IInteractable currentDoorInteractable = null;
    private Carryable nearbyCarryable = null;
    private IAttackable nearbyAttackable = null;
    private GameObject currentRaycastHitObject = null;
    // ***************************************************************

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

    // VALIDACIÓN DE HERRAMIENTA
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
        toolItemManager = GetComponent<ToolItemManager>();
        if (toolItemManager == null)
            Debug.LogError("PlayerInteraction: No se encontró el ToolItemManager. La limpieza por F fallará.", this);

        // 🚨 CRÍTICO: Buscar el tipo correcto: UIPauseController 🚨
        toolPanelIdea = FindObjectOfType<UIPauseController>();
        if (toolPanelIdea == null)
            Debug.LogWarning("PlayerInteraction: No se encontró UIPauseController. La apertura del panel no funcionará.");

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

        if (toolItemManager != null)
        {
            GameObject activeTool = toolItemManager.GetCurrentTool();

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

            if (Input.GetKeyUp(attackKey))
            {
                toolItemManager.DestroyCurrentTool();
            }
        }

        if (Input.GetKeyDown(pickupKey))
            TryPickup();

        if (Input.GetKeyDown(generalInteractKey))
            TryGeneralInteract();

        // 🛑 ELIMINAMOS INPUT.GETKEYDOWN(ESCAPE) DE AQUÍ. SOLO UIPauseController DEBE GESTIONARLO.

        if (Input.GetKeyDown(KeyCode.G))
        {
            LogRemainingItemsCount();
        }
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
                        // 🚨 CRÍTICO: Llamada con la nueva firma de función (Texto + Callback) 🚨
                        toolPanelIdea.ShowToolsPanelAtWorldPosition(mObject.name, mObject.sentimentalValue, mObject.DecideAndNotify);

                        animCtrl?.TriggerInteract();
                        nearbyCarryable = null;
                        Debug.Log("¡Objeto de Memoria recogido con CLICK! Iniciando proceso de decisión.");
                        return;
                    }
                }

                // 2. VERIFICACIÓN CRÍTICA DE HERRAMIENTA
                ToolDescriptor activeTool = cleaningController?.CurrentTool;

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
                        Debug.Log($"🗑️ Basura destruida con herramienta correcta: {activeTool.ToolId}");
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
                    if (cleaningController != null)
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
    }


    // =========================================================================
    // 🚀 FUNCIONES DE RAYCAST Y RECOGIDA (Q) 🚀
    // =========================================================================

    private void DetectNearbyObjects()
    {
        nearbyCarryable = null;
        nearbyAttackable = null;
        currentDoorInteractable = null;
        currentRaycastHitObject = null;

        RaycastHit hit;
        Vector3 rayOrigin = mainCamera.transform.position;
        Vector3 rayDirection = mainCamera.transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionRange, interactableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            currentRaycastHitObject = hitObject;

            Transform rootTransform = hit.collider.transform.root;

            if (carried == null)
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

    void TryPickup()
    {
        if (carried)
        {
            bool isTool = (cleaningController != null &&
                            cleaningController.CurrentTool != null &&
                            carried.GetComponent<ToolDescriptor>() == cleaningController.CurrentTool);

            if (isTool) cleaningController.DropCurrentTool();
            else carried.Drop();

            carried = null;
            animCtrl?.SetHolding(false);
            animCtrl?.TriggerInteract();
            Debug.Log($"Objeto soltado ({pickupKey}).");
            return;
        }

        if (nearbyCarryable != null)
        {
            if (nearbyCarryable.CompareTag(memorieTag))
            {
                MemorieObject mObject = nearbyCarryable.GetComponent<MemorieObject>();
                if (mObject != null && toolPanelIdea != null)
                {
                    // 🚨 CRÍTICO: Llamada con la nueva firma de función (Texto + Callback) 🚨
                    toolPanelIdea.ShowToolsPanelAtWorldPosition(mObject.name, mObject.sentimentalValue, mObject.DecideAndNotify);

                    nearbyCarryable = null;
                    animCtrl?.TriggerInteract();
                    Debug.Log("¡Objeto de Memoria recogido! Iniciando proceso de decisión (Q).");
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

            nearbyCarryable.PickUp(holdPoint, playerColliders);
            ToolDescriptor td = nearbyCarryable.GetComponent<ToolDescriptor>() ?? nearbyCarryable.GetComponentInParent<ToolDescriptor>();

            if (td != null && cleaningController != null)
            {
                cleaningController.RegisterTool(td);
            }

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