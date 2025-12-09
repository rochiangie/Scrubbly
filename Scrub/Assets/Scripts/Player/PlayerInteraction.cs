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
    
    // 📢 NUEVO: Lista para apilar orgánicos
    private List<Carryable> organicStack = new List<Carryable>();

    [Header("Tacho Recolector")]
    [Tooltip("Prefab de la bolsa de basura que se genera al llenar el tacho.")]
    public GameObject trashBagPrefab;
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
    [SerializeField] private string trashDestructionToolId = "Escoba";

    [Header("Detección Raycast")]
    public float interactionRange = 3.0f;
    public LayerMask interactableLayer;

    [Header("Puntero Raycast 3D")]
    [Tooltip("GameObject con SpriteRenderer que se mostrará donde apunta el raycast")]
    [SerializeField] private GameObject raycastPointer;
    [Tooltip("Distancia del puntero desde la superficie")]
    [SerializeField] private float pointerOffset = 0.02f;
    [Tooltip("Escala del puntero")]
    [SerializeField] private float pointerScale = 0.15f;
    
    [Header("Colores Generales")]
    [SerializeField] private Color memorieColor = new Color(1f, 0f, 1f); // Magenta
    [SerializeField] private Color toolColor = Color.cyan;
    [SerializeField] private Color interactableColor = Color.blue;
    [SerializeField] private Color defaultColor = Color.white;

    [Header("Colores de Residuos")]
    [SerializeField] private Color glassColor = new Color(0f, 1f, 1f); // Cyan/Celeste
    [SerializeField] private Color plasticColor = new Color(1f, 1f, 0f); // Amarillo
    [SerializeField] private Color paperColor = new Color(0.6f, 0.4f, 0.2f); // Marrón claro
    [SerializeField] private Color hazardousColor = new Color(1f, 0f, 0f); // Rojo
    [SerializeField] private Color organicColor = new Color(0.4f, 1f, 0.4f); // Verde claro (Organico)
    [SerializeField] private Color bagsColor = new Color(0.8f, 0.8f, 0.8f); // Gris claro (Bolsas)
    [SerializeField] private Color dirtSpotColor = new Color(0.5f, 0.25f, 0f); // Marrón oscuro (Manchas)

    [Header("Tags de Objetos")]
    [SerializeField] private string memorieTag = "Memorie";
    [SerializeField] private string glassTag = "Vidrio";
    [SerializeField] private string plasticTag = "Plastico";
    [SerializeField] private string paperTag = "Papeles";
    [SerializeField] private string hazardousTag = "Peligrosos";
    [SerializeField] private string organicTag = "Organico";
    [SerializeField] private string bagsTag = "Bolsas";

    private Camera mainCamera;

    // 🔴 VARIABLE PARA OUTLINE
    private Outline currentOutline;
    
    // 🎨 COMPONENTE DEL PUNTERO
    private SpriteRenderer pointerSpriteRenderer;

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

        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogError("PlayerInteraction: No se encontró la cámara principal.");
        
        // Inicializar componente del puntero
        if (raycastPointer != null)
        {
            pointerSpriteRenderer = raycastPointer.GetComponent<SpriteRenderer>();
            raycastPointer.SetActive(false); // Desactivar al inicio
        }
    }

    void Update()
    {
        // 🛡️ SEGURIDAD: Si el objeto que llevamos fue destruido externamente (ej. por un basurero), limpiar la referencia
        if (carried != null && carried.gameObject == null) 
        {
            carried = null;
            // Limpiar también el stack si el principal murió
            organicStack.RemoveAll(x => x == null || x.gameObject == null);
            if (organicStack.Count == 0) animCtrl?.SetHolding(false);
            else carried = organicStack[organicStack.Count - 1]; // Recuperar el siguiente si queda alguno
        }
        // Limpieza extra del stack por seguridad
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
    // 🔍 LÓGICA DE DETECCIÓN (RAYCAST + OUTLINE)
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

        // 🔴 VISUALIZACIÓN DEBUG: Dibuja una línea roja en la ventana Scene
        Debug.DrawRay(rayOrigin, rayDirection * interactionRange, Color.red);

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionRange, interactableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            currentRaycastHitObject = hitObject;

            // 🎯 ACTUALIZAR PUNTERO
            UpdateRaycastPointer(hit, hitObject);

            // 🔴 LÓGICA DE OUTLINE
            HandleOutline(hitObject);

            Transform rootTransform = hit.collider.transform.root;
            
            // Siempre detectamos carryables cercanos
            Carryable c = rootTransform.GetComponent<Carryable>() ?? hitObject.GetComponentInParent<Carryable>();
            if (c != null) nearbyCarryable = c;

            IAttackable a = hitObject.GetComponentInParent<IAttackable>();
            if (a != null) nearbyAttackable = a;

            IInteractable i = hitObject.GetComponentInParent<IInteractable>();
            if (i != null) currentDoorInteractable = i;
        }
        else
        {
            // Si no golpeamos nada, posicionar el puntero a distancia fija
            if (raycastPointer != null)
            {
                if (!raycastPointer.activeSelf)
                {
                    raycastPointer.SetActive(true);
                }
                
                // Posicionar a distancia fija en la dirección de la cámara
                Vector3 targetPosition = rayOrigin + rayDirection * interactionRange;
                raycastPointer.transform.position = targetPosition;
                
                // Hacer que mire hacia la cámara
                if (mainCamera != null)
                {
                    raycastPointer.transform.LookAt(mainCamera.transform);
                    raycastPointer.transform.Rotate(0, 180, 0);
                }
                
                // Aplicar escala y color por defecto
                raycastPointer.transform.localScale = Vector3.one * pointerScale;
                if (pointerSpriteRenderer != null)
                {
                    pointerSpriteRenderer.color = defaultColor;
                }
            }

            // Si no golpeamos nada, limpiar outline
            if (currentOutline != null)
            {
                currentOutline.enabled = false;
                currentOutline = null;
            }
        }
    }

    private void HandleOutline(GameObject hitObject)
    {
        // Buscar componente Outline en el objeto golpeado o sus padres
        Outline outline = hitObject.GetComponent<Outline>() ?? hitObject.GetComponentInParent<Outline>();

        // Si el objeto golpeado es diferente al que ya tenemos resaltado
        if (currentOutline != outline)
        {
            // Desactivar el anterior si existe
            if (currentOutline != null)
            {
                currentOutline.enabled = false;
            }

            // Activar el nuevo si existe
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
    }

    // =========================================================================
    // 🎯 ACTUALIZACIÓN DEL PUNTERO RAYCAST
    // =========================================================================
    private void UpdateRaycastPointer(RaycastHit hit, GameObject hitObject)
    {
        if (raycastPointer == null) return;
        
        // Activar el puntero
        if (!raycastPointer.activeSelf)
        {
            raycastPointer.SetActive(true);
        }
        
        // Posicionar el puntero en el punto de impacto con offset
        raycastPointer.transform.position = hit.point + hit.normal * pointerOffset;
        
        // Hacer que el puntero mire hacia la cámara (billboard)
        if (mainCamera != null)
        {
            raycastPointer.transform.LookAt(mainCamera.transform);
            raycastPointer.transform.Rotate(0, 180, 0);
        }
        
        // Aplicar escala
        raycastPointer.transform.localScale = Vector3.one * pointerScale;
        
        // 🎨 DETERMINAR Y APLICAR COLOR SEGÚN EL TIPO DE OBJETO
        Color targetColor = defaultColor;
        
        // 1. Recuerdos (Prioridad Alta)
        if (hitObject.CompareTag(memorieTag))
        {
            targetColor = memorieColor;
        }
        // 2. Tipos de Basura Específicos
        else if (hitObject.CompareTag(glassTag)) targetColor = glassColor;
        else if (hitObject.CompareTag(plasticTag)) targetColor = plasticColor;
        else if (hitObject.CompareTag(paperTag)) targetColor = paperColor;
        else if (hitObject.CompareTag(hazardousTag)) targetColor = hazardousColor;
        else if (hitObject.CompareTag(organicTag)) targetColor = organicColor;
        else if (hitObject.CompareTag(bagsTag)) targetColor = bagsColor;
        
        // 3. Manchas de Suciedad
        else if (hitObject.GetComponent<DirtSpot>() != null)
        {
            targetColor = dirtSpotColor;
        }
        // 4. Otros Objetos
        else
        {
            Carryable carryable = hitObject.GetComponentInParent<Carryable>();
            if (carryable != null)
            {
                ToolDescriptor tool = carryable.GetComponent<ToolDescriptor>() ?? carryable.GetComponentInParent<ToolDescriptor>();
                if (tool != null)
                {
                    targetColor = toolColor;
                }
                // Si es un carryable genérico que no cayó en los tags de basura anteriores
                else
                {
                    targetColor = defaultColor; 
                }
            }
            else
            {
                IInteractable interactable = hitObject.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    targetColor = interactableColor;
                }
            }
        }
        
        // Aplicar color al sprite
        if (pointerSpriteRenderer != null)
        {
            pointerSpriteRenderer.color = targetColor;
        }
    }

    // =========================================================================
    // 🖱️ LIMPIEZA CON CLICK
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

                // 1. Prioridad: Recoger Objeto o Herramienta
                
                Carryable clickCarryable = hitObject.GetComponentInParent<Carryable>();

                // Restaurar variables necesarias
                bool isHandsFree = (carried == null && (heldItemSlot == null || !heldItemSlot.HasTool));
                bool isOrganicStackable = false;

                if (carried != null && clickCarryable != null)
                {
                    bool carriedIsOrganic = carried.CompareTag(organicTag);
                    bool newIsOrganic = clickCarryable.CompareTag(organicTag);

                    if (carriedIsOrganic && newIsOrganic)
                    {
                        if (organicStack.Count < 5)
                        {
                            isOrganicStackable = true;
                        }
                        else
                        {
                            Debug.Log("Stack lleno (Max 5).");
                        }
                    }
                }

                if (isHandsFree || isOrganicStackable)
                {
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
                                if (toolPanelIdea != null && isPaused) toolPanelIdea.SetIsPaused(false);
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
                            // Las herramientas NO se apilan
                            if (organicStack.Count > 0) 
                            {
                                Debug.Log("No puedes recoger herramienta con manos ocupadas.");
                                return; 
                            }
                            
                            heldItemSlot.EquipToolPrefab(td.gameObject, holdPoint);
                            Destroy(clickCarryable.gameObject);
                        }
                        else
                        {
                            // RECOGER OBJETO (Normal o Stack)
                            clickCarryable.PickUp(holdPoint, playerColliders);
                            
                            if (isOrganicStackable)
                            {
                                // Posicionamiento visual del stack (un poco aleatorio o hacia arriba para que se vea el bulto)
                                float stackOffset = organicStack.Count * 0.15f; // 15cm más arriba por cada item
                                clickCarryable.transform.localPosition = new Vector3(0, stackOffset, stackOffset * 0.5f);
                                clickCarryable.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0);
                            }
                            else
                            {
                                // Primer objeto
                                organicStack.Clear(); // Asegurar limpieza
                            }

                            organicStack.Add(clickCarryable);
                            carried = clickCarryable; // Carried siempre es el último recogido (o el principal, no importa mucho mientras no sea null)
                            Debug.Log($"Recogido: {clickCarryable.name}. Stack actual: {organicStack.Count}");
                        }

                        if (toolPanelIdea != null && isPaused) toolPanelIdea.SetIsPaused(false);

                        animCtrl?.SetHolding(td != null || carried != null);
                        animCtrl?.TriggerInteract();
                        return;
                    }
                }

                // 2. Herramienta Activa
                if (heldItemSlot == null) return;
                ToolDescriptor activeTool = heldItemSlot.CurrentTool;
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
            // Soltar el ÚLTIMO objeto añadido (LIFO)
            Carryable itemToDrop = organicStack[organicStack.Count - 1];
            
            if (itemToDrop != null)
            {
                itemToDrop.Drop();
                Debug.Log($"Objeto {itemToDrop.name} soltado del stack.");
            }
            
            organicStack.RemoveAt(organicStack.Count - 1);

            // Actualizar 'carried'
            if (organicStack.Count > 0)
            {
                carried = organicStack[organicStack.Count - 1]; // El nuevo 'carried' es el anterior en la pila
            }
            else
            {
                carried = null; // Manos vacías
                animCtrl?.SetHolding(false);
            }
            
            animCtrl?.TriggerInteract();
            return;
        }
        // Fallback por si carried existe pero no está en stack (ej. otros objetos)
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
        if (carried != null && carried.CompareTag("Tacho"))
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
                if (mainCamera == null) return;
                Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                RaycastHit hit;

                // Usamos interactionRange (3.0f) para mayor alcance
                if (Physics.Raycast(ray, out hit, interactionRange))
                {
                    GameObject hitObject = hit.collider.gameObject;
                    // Debug.Log($"[Tacho] Raycast golpeó: {hitObject.name} (Tag: {hitObject.tag})");
                    
                    Carryable clickCarryable = hitObject.GetComponentInParent<Carryable>();

                    if (clickCarryable != null && clickCarryable.CompareTag(organicTag))
                    {
                        // 1. "Meter" orgánico en el tacho
                        Destroy(clickCarryable.gameObject);
                        currentOrganicInBin++;

                        Debug.Log($"[Tacho] ♻️ Orgánico recolectado. Total: {currentOrganicInBin}/5");

                        // 2. Feedback
                        if (clickCleaningEffect != null) Instantiate(clickCleaningEffect, hit.point, Quaternion.identity);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayPickupSFX();
                        animCtrl?.TriggerInteract();

                        // 3. Generar Bolsa
                        if (currentOrganicInBin >= 5)
                        {
                            if (trashBagPrefab != null)
                            {
                                Vector3 spawnPos = transform.position + transform.forward * 1.0f + Vector3.up * 0.5f;
                                Instantiate(trashBagPrefab, spawnPos, Quaternion.identity);
                                Debug.Log("[Tacho] 🎒 ¡Bolsa Generada!");
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
