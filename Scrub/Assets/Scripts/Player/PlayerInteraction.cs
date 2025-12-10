using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

// ----------------------------------------------------
// INTERFACES
// ----------------------------------------------------
public interface IInteractable { void Interact(); }
public interface IAttackable { void ReceiveAttack(); }

public class PlayerInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public Transform holdPoint;
    public PlayerAnimationController animCtrl;

    // 🚨 GESTORES CRÍTICOS
    private HeldItemSlot heldItemSlot;
    private UIPauseController toolPanelIdea;
    private CleaningController cleaningController;
    private Carryable carried;
    
    // 📢 NUEVO: Lista para apilar orgánicos (Deshabilitada por diseño actual, pero mantenida por compatibilidad)
    private List<Carryable> organicStack = new List<Carryable>();

    [Header("Tacho Recolector")]
    [Tooltip("Prefab de la bolsa de basura que se genera al llenar el tacho.")]
    public GameObject trashBagPrefab;
    [Tooltip("Punto donde aparecerá la bolsa generada. Si es null, aparece frente al jugador.")]
    public Transform trashBagSpawnPoint; // ✅ NUEVO
    private int currentOrganicInBin = 0;

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
    [SerializeField] private float interactionRange = 3.0f; // Unificado
    [SerializeField] private float clickCleaningRadius = 0.5f;
    [SerializeField] private LayerMask dirtLayer;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private ParticleSystem clickCleaningEffect;

    [Header("Input Keys")]
    [SerializeField] private KeyCode generalInteractKey = KeyCode.E;
    [SerializeField] private KeyCode pickupKey = KeyCode.Q;
    [SerializeField] private KeyCode attackKey = KeyCode.F;
    [SerializeField] private KeyCode scorePanelToggleKey = KeyCode.Tab;

    [Header("Validación de Herramientas")]
    [Tooltip("ID del ToolDescriptor que PUEDE destruir objetos con el Tag 'Basura'.")]
    [SerializeField] private string trashDestructionToolId = "Escoba";

    [Header("Tags")]
    [SerializeField] private string organicTag = "Organico";
    [SerializeField] private string glassTag = "Vidrio";
    [SerializeField] private string plasticTag = "Plastico";
    [SerializeField] private string paperTag = "Papeles";
    [SerializeField] private string hazardousTag = "Peligrosos";
    [SerializeField] private string bagsTag = "Bolsas";

    // Puntero Visual
    [Header("Visual Feedback")]
    [SerializeField] private GameObject raycastPointer;
    [SerializeField] private float pointerScale = 0.1f;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color interactColor = Color.green;
    [SerializeField] private Color attackColor = Color.red;
    private SpriteRenderer pointerSpriteRenderer;
    private Outline currentOutline;

    // =========================================================================
    // AWAKE & UPDATE
    // =========================================================================
    void Awake()
    {
        heldItemSlot = GetComponent<HeldItemSlot>();
        if (heldItemSlot == null)
            Debug.LogError("PlayerInteraction: No se encontró HeldItemSlot. **Adjunte HeldItemSlot al Player.**");

        toolPanelIdea = FindObjectOfType<UIPauseController>();
        cleaningController = GetComponent<CleaningController>();
        
        if (!animCtrl) animCtrl = GetComponentInChildren<PlayerAnimationController>() ?? GetComponent<PlayerAnimationController>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerColliders = GetComponentsInChildren<Collider>();

        if (raycastPointer != null)
        {
            pointerSpriteRenderer = raycastPointer.GetComponent<SpriteRenderer>();
            raycastPointer.SetActive(false);
        }
    }

    void Update()
    {
        // 🛡️ SEGURIDAD: Si el objeto que llevamos fue destruido externamente
        if (carried != null && carried.gameObject == null) 
        {
            carried = null;
            organicStack.RemoveAll(x => x == null || x.gameObject == null);
            if (organicStack.Count == 0) animCtrl?.SetHolding(false);
            else carried = organicStack[organicStack.Count - 1];
        }
        
        if (organicStack.Count > 0)
        {
            organicStack.RemoveAll(x => x == null || x.gameObject == null);
            if (organicStack.Count == 0 && carried == null) animCtrl?.SetHolding(false);
        }

        DetectNearbyObjects();
        HandleMouseClickCleaning();
        HandleRightClickInteraction(); // 🖱️ Click Derecho (Tacho)
        HandleAttackAndToolUse();

        if (Input.GetKeyDown(pickupKey))
            TryDropOrDestroy();

        if (Input.GetKeyDown(generalInteractKey))
            TryGeneralInteract();
    }

    // =========================================================================
    // 🔍 DETECCIÓN Y RAYCAST
    // =========================================================================
    private void DetectNearbyObjects()
    {
        nearbyCarryable = null;
        nearbyAttackable = null;
        currentDoorInteractable = null;
        currentRaycastHitObject = null;

        if (Camera.main == null) return;

        RaycastHit hit;
        Vector3 rayOrigin = Camera.main.transform.position;
        Vector3 rayDirection = Camera.main.transform.forward;

        Debug.DrawRay(rayOrigin, rayDirection * interactionRange, Color.red);

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionRange, interactableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            currentRaycastHitObject = hitObject;

            UpdateRaycastPointer(hit, hitObject);
            HandleOutline(hitObject);

            Transform rootTransform = hit.collider.transform.root;
            
            Carryable c = rootTransform.GetComponent<Carryable>() ?? hitObject.GetComponentInParent<Carryable>();
            if (c != null) nearbyCarryable = c;

            IAttackable a = hitObject.GetComponentInParent<IAttackable>();
            if (a != null) nearbyAttackable = a;

            IInteractable i = hitObject.GetComponentInParent<IInteractable>();
            if (i != null) currentDoorInteractable = i;
        }
        else
        {
            if (raycastPointer != null)
            {
                if (!raycastPointer.activeSelf) raycastPointer.SetActive(true);
                
                Vector3 targetPosition = rayOrigin + rayDirection * interactionRange;
                raycastPointer.transform.position = targetPosition;
                raycastPointer.transform.LookAt(Camera.main.transform);
                raycastPointer.transform.Rotate(0, 180, 0);
                raycastPointer.transform.localScale = Vector3.one * pointerScale;
                if (pointerSpriteRenderer != null) pointerSpriteRenderer.color = defaultColor;
            }

            if (currentOutline != null)
            {
                currentOutline.enabled = false;
                currentOutline = null;
            }
        }
    }

    private void UpdateRaycastPointer(RaycastHit hit, GameObject hitObject)
    {
        if (raycastPointer == null) return;

        raycastPointer.SetActive(true);
        raycastPointer.transform.position = hit.point;
        raycastPointer.transform.LookAt(Camera.main.transform);
        raycastPointer.transform.Rotate(0, 180, 0);

        float distance = hit.distance;
        float scale = pointerScale * (1 + distance * 0.1f);
        raycastPointer.transform.localScale = Vector3.one * scale;

        if (pointerSpriteRenderer != null)
        {
            if (hitObject.GetComponent<IInteractable>() != null || hitObject.GetComponentInParent<IInteractable>() != null)
                pointerSpriteRenderer.color = interactColor;
            else if (hitObject.GetComponent<IAttackable>() != null || hitObject.GetComponentInParent<IAttackable>() != null)
                pointerSpriteRenderer.color = attackColor;
            else
                pointerSpriteRenderer.color = defaultColor;
        }
    }

    private void HandleOutline(GameObject hitObject)
    {
        Outline outline = hitObject.GetComponent<Outline>() ?? hitObject.GetComponentInParent<Outline>();
        
        if (currentOutline != null && currentOutline != outline)
        {
            currentOutline.enabled = false;
        }

        if (outline != null)
        {
            outline.enabled = true;
            currentOutline = outline;
        }
        else
        {
            currentOutline = null;
        }
    }

    // =========================================================================
    // 🖱️ CLICK IZQUIERDO (RECOGER / LIMPIAR)
    // =========================================================================
    private void HandleMouseClickCleaning()
    {
        bool isPaused = toolPanelIdea != null && Time.timeScale == 0;

        if (Input.GetMouseButtonDown(0) && Time.timeScale > 0)
        {
            if (Camera.main == null) return;

            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionRange))
            {
                GameObject hitObject = hit.collider.gameObject;
                
                Carryable clickCarryable = hitObject.GetComponentInParent<Carryable>();

                // Verificar si las manos están realmente libres (incluyendo CleaningController)
                bool isCleaningControllerBusy = cleaningController != null && cleaningController.CarriedItems.Count > 0;
                bool isHandsFree = (carried == null && (heldItemSlot == null || !heldItemSlot.HasTool) && !isCleaningControllerBusy);
                
                // El usuario solicitó explícitamente NO permitir recoger más de un objeto a la vez.
                // La excepción es el Tacho, pero eso se maneja con Click Derecho.
                bool isOrganicStackable = false; 

                if (isHandsFree || isOrganicStackable)
                {
                    if (clickCarryable != null)
                    {
                        // ✨ SI EL OBJETO TIENE PANEL, SE OCULTA
                        ObjetoInfo info = clickCarryable.GetComponentInChildren<ObjetoInfo>();
                        if (info != null)
                            info.OcultarPanel();
                        // 1.1 Si es herramienta -> Equipar
                        ToolDescriptor td = clickCarryable.GetComponent<ToolDescriptor>() ?? clickCarryable.GetComponentInParent<ToolDescriptor>();
                        if (td != null)
                        {
                            if (heldItemSlot != null)
                            {
                                heldItemSlot.EquipExistingTool(td.gameObject, holdPoint);
                                animCtrl?.SetHolding(true);
                                animCtrl?.TriggerInteract();
                            }
                            

                            return;
                        }

                        // 1.2 Si es objeto normal -> Recoger
                        if (cleaningController != null)
                        {
                            cleaningController.RegisterCarryable(clickCarryable);
                            // Sincronizar estado local
                            carried = clickCarryable; 
                            if (isOrganicStackable) organicStack.Add(clickCarryable);
                            else organicStack.Clear(); // Reset si no es stackable
                        }
                        
                        else
                        {
                            // Fallback simple si no hay CleaningController
                            clickCarryable.PickUp(holdPoint, playerColliders);
                            carried = clickCarryable;
                        }
                        
                        animCtrl?.SetHolding(true);
                        animCtrl?.TriggerInteract();
                        return;

                    }
                }

                // 2. Interacción con Memorias
                MemorieObject memorie = hitObject.GetComponent<MemorieObject>();
                if (memorie != null)
                {
                    if (toolPanelIdea != null)
                    {
                        memorie.StartDecisionProcess(toolPanelIdea);
                        animCtrl?.TriggerInteract();
                    }
                    return;
                }

                // Si tenemos herramienta activa, intentar usarla
                ToolDescriptor activeTool = null;
                if (heldItemSlot != null && heldItemSlot.HasTool) activeTool = heldItemSlot.CurrentTool;
                
                if (activeTool == null) return;

                // 3. Destruir Basura
                if (hitObject.CompareTag(organicTag) || hitObject.CompareTag(glassTag) || 
                    hitObject.CompareTag(plasticTag) || hitObject.CompareTag(paperTag) || 
                    hitObject.CompareTag(hazardousTag) || hitObject.CompareTag(bagsTag))
                {
                    if (activeTool.ToolId == trashDestructionToolId)
                    {
                        if (clickCleaningEffect != null) Instantiate(clickCleaningEffect, hit.point, Quaternion.identity);
                        activeTool.TryUse();
                        Destroy(hitObject);
                        Debug.Log($"[Basura] Destruida con {activeTool.ToolId}");
                        return;
                    }
                }

                // 4. Limpiar Manchas
                DirtSpot dirtSpot = hitObject.GetComponent<DirtSpot>();
                if (dirtSpot != null)
                {
                    if (dirtSpot.CanBeCleanedBy(activeTool.ToolId))
                    {
                        dirtSpot.CleanHit(activeTool.ToolPower);
                        activeTool.TryUse();
                        if (clickCleaningEffect != null) Instantiate(clickCleaningEffect, hit.point, Quaternion.identity);
                    }
                    return;
                }
            }
        }
    }

    // =========================================================================
    // ⚔️ ATAQUE Y USO DE HERRAMIENTA (F)
    // =========================================================================
    private void HandleAttackAndToolUse()
    {
        if (heldItemSlot == null) return;
        if (!Input.GetKeyDown(attackKey)) return;

        ToolDescriptor activeTool = heldItemSlot.CurrentTool;

        if (activeTool != null)
        {
            CleanTool cleanScript = activeTool.GetComponent<CleanTool>();
            if (cleanScript != null) cleanScript.Clean();
            else activeTool.TryUse();
        }
        else if (nearbyAttackable != null)
        {
            nearbyAttackable.ReceiveAttack();
        }
    }

    // =========================================================================
    // 🚀 SOLTAR / DESTRUIR (Q)
    // =========================================================================
    void TryDropOrDestroy()
    {
        if (heldItemSlot == null && carried == null && organicStack.Count == 0) return;

        // 1. Destruir Herramienta
        if (heldItemSlot != null && heldItemSlot.HasTool)
        {
            heldItemSlot.DestroyCurrentTool();
            animCtrl?.SetHolding(false);
            animCtrl?.TriggerInteract();
            carried = null;
            return;
        }

        // 2. Soltar Objeto (Manejo de Stack)
        if (organicStack.Count > 0)
        {
            Carryable itemToDrop = organicStack[organicStack.Count - 1];
            
            if (itemToDrop != null)
            {
                itemToDrop.Drop();
                Debug.Log($"Objeto {itemToDrop.name} soltado del stack.");
            }
            
            organicStack.RemoveAt(organicStack.Count - 1);

            if (organicStack.Count > 0)
            {
                carried = organicStack[organicStack.Count - 1];
            }
            else
            {
                carried = null;
                animCtrl?.SetHolding(false);
            }
            
            animCtrl?.TriggerInteract();
            return;
        }
        else if (carried != null)
        {
                carried.Drop(); 
                Debug.Log($"Objeto {carried.name} soltado.");
                carried = null;
                animCtrl?.SetHolding(false);
                animCtrl?.TriggerInteract();
        }
    }

    // =========================================================================
    // 🖱️ INTERACCIÓN CLICK DERECHO (TACHO)
    // =========================================================================
    private void HandleRightClickInteraction()
    {
        // 1. Determinar si tenemos un Tacho (ya sea como Carryable o como Tool)
        bool isHoldingTacho = false;

        // Caso A: Es un objeto normal (Carryable)
        if (carried != null && (carried.CompareTag("Tacho") || carried.CompareTag("Tacho2")))
        {
            isHoldingTacho = true;
        }
        // Caso B: Es una herramienta equipada (HeldItemSlot)
        else if (heldItemSlot != null && heldItemSlot.HasTool)
        {
            // DEBUG: Ver qué herramienta tenemos realmente
            if (Input.GetMouseButtonDown(1))
            {
                 Debug.Log($"[RightClick] Herramienta actual (HeldItemSlot): '{heldItemSlot.CurrentTool.name}', Tag: '{heldItemSlot.CurrentTool.tag}'");
            }

            if (heldItemSlot.CurrentTool.CompareTag("Tacho"))
            {
                isHoldingTacho = true;
            }
        }
        // Caso C: Es un objeto gestionado por CleaningController (Conflict Resolution)
        else if (cleaningController != null && cleaningController.CarriedItems.Count > 0)
        {
             Carryable ccItem = cleaningController.CurrentCarryable;
             
             // DEBUG: Ver qué tiene CleaningController
             if (Input.GetMouseButtonDown(1))
             {
                 Debug.Log($"[RightClick] Objeto en CleaningController: '{ccItem.name}', Tag: '{ccItem.tag}'");
             }

             if (ccItem != null && ccItem.CompareTag("Tacho"))
             {
                 isHoldingTacho = true;
             }
        }

        // DEBUG: Diagnóstico al hacer click derecho
        if (Input.GetMouseButtonDown(1))
        {
            if (isHoldingTacho) Debug.Log("[RightClick] ✅ Tacho detectado en mano.");
            else Debug.Log("[RightClick] ❌ No se detectó Tacho (ni como objeto, ni herramienta, ni en CleaningController).");
        }

        // 2. Ejecutar lógica si tenemos el Tacho
        if (isHoldingTacho)
        {
            if (Input.GetMouseButtonDown(1)) // Click Derecho
            {
                if (Camera.main == null) return;
                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                RaycastHit hit;

                // Usamos interactionRange (3.0f) para mayor alcance
                if (Physics.Raycast(ray, out hit, interactionRange))
                {
                    GameObject hitObject = hit.collider.gameObject;
                    
                    Carryable clickCarryable = hitObject.GetComponentInParent<Carryable>();

                    if (clickCarryable != null && clickCarryable.CompareTag(organicTag))
                    {
                        // 1. Notificar al TaskManager ANTES de destruir (para que cuente como limpiado)
                        if (TaskManager.Instance != null)
                        {
                            TaskManager.Instance.NotifyTrashCleaned(clickCarryable.gameObject);
                        }

                        // 2. "Meter" orgánico en el tacho
                        Destroy(clickCarryable.gameObject);
                        currentOrganicInBin++;

                        Debug.Log($"[Tacho] ♻️ Orgánico recolectado. Total: {currentOrganicInBin}/5");

                        // 3. Feedback
                        if (clickCleaningEffect != null) Instantiate(clickCleaningEffect, hit.point, Quaternion.identity);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayPickupSFX();
                        animCtrl?.TriggerInteract();

                        // 4. Generar Bolsa
                        // Condición: Llegamos a 5 O es el último orgánico del nivel
                        bool isLastOrganic = false;
                        if (TaskManager.Instance != null)
                        {
                            isLastOrganic = (TaskManager.Instance.cleanedOrganic >= TaskManager.Instance.totalOrganic);
                        }

                        if (currentOrganicInBin >= 5 || (isLastOrganic && currentOrganicInBin > 0))
                        {
                            if (trashBagPrefab != null)
                            {
                                // Determinar posición de spawn
                                Vector3 spawnPos;
                                if (trashBagSpawnPoint != null)
                                {
                                    spawnPos = trashBagSpawnPoint.position;
                                }
                                else
                                {
                                    // Fallback: Frente al jugador
                                    spawnPos = transform.position + transform.forward * 1.0f + Vector3.up * 0.5f;
                                }

                                GameObject newBag = Instantiate(trashBagPrefab, spawnPos, Quaternion.identity);
                                
                                // ✅ FIX: Asegurar que el objeto tenga el nombre y tag correctos para que TaskManager lo detecte como Bolsa
                                newBag.name = "Bolsa_Residuo_Generada_" + System.Guid.NewGuid().ToString().Substring(0, 4);
                                if (string.IsNullOrEmpty(newBag.tag) || newBag.tag == "Untagged" || newBag.tag == bagsTag)
                                {
                                    newBag.tag = "RTrash"; // Forzamos RTrash como pidió el usuario
                                }
                                
                                // Registrar la nueva bolsa en el TaskManager
                                if (TaskManager.Instance != null)
                                {
                                    TaskManager.Instance.RegisterNewTrashItem(newBag);
                                }

                                Debug.Log($"[Tacho] 🎒 ¡Bolsa Generada en {(trashBagSpawnPoint != null ? "Punto Fijo" : "Frente al Jugador")}! (Items: {currentOrganicInBin})");
                            }
                            currentOrganicInBin = 0;
                        }
                    }
                }
            }
        }
    }

    // =========================================================================
    // 💡 INTERACCIÓN GENERAL (E)
    // =========================================================================
    void TryGeneralInteract()
    {
        // 1. Interacción General (Puertas, Interruptores, etc)
        if (currentDoorInteractable != null)
        {
            currentDoorInteractable.Interact();
            animCtrl?.TriggerInteract();
        }
    }

    public void SetCurrentInteractable(IInteractable interactable)
    {
        currentDoorInteractable = interactable;
    }

    public void ClearCurrentInteractable()
    {
        currentDoorInteractable = null;
    }
}
