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

    // 🚨 REFERENCIAS A LOS GESTORES DE COMPONENTES 🚨
    private ToolItemManager toolItemManager;
    private ToolPanelIdea toolPanelIdea;
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
    [Tooltip("Tecla para Interacción General (Puertas)")]
    [SerializeField] private KeyCode generalInteractKey = KeyCode.E;

    [Tooltip("Tecla para Recoger/Agarrar y Soltar objetos Carryable/Tool")]
    [SerializeField] private KeyCode pickupKey = KeyCode.Q;

    [Tooltip("Tecla para Atacar/Destruir directamente (Limpieza)")]
    [SerializeField] private KeyCode attackKey = KeyCode.F;

    [Tooltip("Tecla para mostrar/ocultar el panel de puntuación sentimental.")]
    [SerializeField] private KeyCode scorePanelToggleKey = KeyCode.Tab;

    [Header("Detección Raycast")]
    [Tooltip("Distancia máxima para detectar objetos recolectables y generales.")]
    public float interactionRange = 3.0f;
    [Tooltip("Capas de objetos generales (Puertas, Carryable, Memorie, Atacable)")]
    public LayerMask interactableLayer;

    [Header("Tags de Objetos")]
    [Tooltip("Tag para objetos que inician el proceso de decisión sentimental.")]
    [SerializeField] private string memorieTag = "Memorie";
    [SerializeField] private string trashTag = "Basura";

    private Camera mainCamera;

    void Awake()
    {
        toolItemManager = GetComponent<ToolItemManager>();
        if (toolItemManager == null)
            Debug.LogError("PlayerInteraction: No se encontró el ToolItemManager. La limpieza por F fallará.", this);

        toolPanelIdea = FindObjectOfType<ToolPanelIdea>();
        if (toolPanelIdea == null)
            Debug.LogWarning("PlayerInteraction: No se encontró ToolPanelIdea. La apertura del panel no funcionará.");

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

        // --- Lógica de Input y Limpieza ---
        HandleMouseClickCleaning();

        if (toolItemManager != null)
        {
            GameObject activeTool = toolItemManager.GetCurrentTool();

            // Lógica de Ataque (F)
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

        // Lógica de Agarre/Decisión (Q)
        if (Input.GetKeyDown(pickupKey))
            TryPickup();

        // Lógica de Interacción General (E)
        if (Input.GetKeyDown(generalInteractKey))
            TryGeneralInteract();

        // Lógica de Pausa/Panel
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            toolPanelIdea?.TogglePause();
        }

        if (Input.GetKeyDown(scorePanelToggleKey) || Input.GetKeyDown(KeyCode.Return))
        {
            toolPanelIdea?.ToggleToolsPanel();
        }

        // DEBUG: LÓGICA DE CONTEO (TECLA G)
        if (Input.GetKeyDown(KeyCode.G))
        {
            LogRemainingItemsCount();
        }
    }


    // =========================================================================
    // 🚀 FUNCIÓN CORREGIDA: DETECCIÓN CENTRALIZADA CON RAYCAST 🚀
    // *************************************************************************
    // Ahora prioriza la búsqueda de componentes en el objeto raíz, donde se 
    // encuentran Carryable.cs y MemorieObject.cs en jerarquías complejas.
    // *************************************************************************

    /// <summary>
    /// Lanza un Raycast para detectar el objeto más cercano y actualiza las referencias de interacción.
    /// </summary>
    private void DetectNearbyObjects()
    {
        // 1. Limpiar todas las referencias en cada frame
        nearbyCarryable = null;
        nearbyAttackable = null;
        currentDoorInteractable = null;
        currentRaycastHitObject = null;

        RaycastHit hit;
        Vector3 rayOrigin = mainCamera.transform.position;
        Vector3 rayDirection = mainCamera.transform.forward;

        // 2. Lanzar Raycast
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionRange, interactableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            currentRaycastHitObject = hitObject;

            // Referencia al GameObject principal/raíz de la jerarquía del objeto golpeado.
            // Esto es CRÍTICO si los colliders están en objetos hijos, pero los scripts
            // (Carryable.cs, IInteractable, etc.) están en el objeto raíz.
            Transform rootTransform = hit.collider.transform.root;

            // 3. DETECCIÓN DE CARRYABLE (TECLA Q)
            if (carried == null)
            {
                // Buscamos el Carryable directamente en la raíz y en el objeto golpeado.
                Carryable c = rootTransform.GetComponent<Carryable>() ?? hitObject.GetComponentInParent<Carryable>();

                if (c != null)
                {
                    nearbyCarryable = c;
                    // Debug para confirmar que la detección de Q funciona.
                    Debug.Log($"[Raycast OK] Carryable detectado: {c.name}. Listo para recoger (Q).");
                }
            }

            // 4. DETECCIÓN DE ATAQUE (IAttackable - Tecla F)
            // Usamos GetComponentInParent para buscar hacia arriba desde el objeto golpeado.
            IAttackable a = hitObject.GetComponentInParent<IAttackable>();
            if (a != null)
            {
                nearbyAttackable = a;
            }

            // 5. DETECCIÓN DE INTERACCIÓN GENERAL (IInteractable - Tecla E)
            // Usamos GetComponentInParent para buscar hacia arriba desde el objeto golpeado.
            IInteractable i = hitObject.GetComponentInParent<IInteractable>();
            if (i != null)
            {
                currentDoorInteractable = i;
            }

            // Opcional: Si el OutlineController usa una interfaz como IHighlightable
            // y está en el objeto raíz, puedes activarlo aquí.
            // Ejemplo: rootTransform.GetComponent<IHighlightable>()?.Highlight();
        }
        // Si no golpeamos nada, el OutlineController debería encargarse de desactivar el outline.
    }

    // El resto del script PlayerInteraction.cs permanece igual...

    // =========================================================================
    // 🖱️ FUNCIÓN PARA LIMPIEZA CON CLICK DEL MOUSE (NO CAMBIA)
    // =========================================================================

    private void HandleMouseClickCleaning()
    {
        // Solo interactuamos si el juego no está en pausa (Time.timeScale > 0)
        if (Input.GetMouseButtonDown(0) && Time.timeScale > 0)
        {
            if (mainCamera == null) return;

            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, mouseInteractionDistance))
            {
                GameObject hitObject = hit.collider.gameObject;

                // 🚨 VERIFICACIÓN CRÍTICA DE HERRAMIENTA 🚨
                ToolDescriptor activeTool = cleaningController?.CurrentTool;

                if (activeTool == null)
                {
                    Debug.LogWarning("[Click Cleaning] No se puede limpiar/destruir. Necesitas una herramienta equipada.");
                    return;
                }

                // 1. INTENTAR DESTRUIR BASURA (Tag: Basura)
                if (hitObject.CompareTag(trashTag))
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

                // 2. INTENTAR LIMPIAR MANCHAS (Clase: DirtSpot)
                // Se asume que DirtSpot.cs está en el objeto que colisiona.
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
    // LÓGICA DE INTERACCIÓN PRINCIPAL (Tecla Q)
    // =========================================================================

    void TryPickup()
    {
        // Lógica 1: Soltar objeto (Si tengo algo, suelto)
        if (carried)
        {
            bool isTool = (cleaningController != null &&
                            cleaningController.CurrentTool != null &&
                            carried.GetComponent<ToolDescriptor>() == cleaningController.CurrentTool);

            if (isTool)
            {
                cleaningController.DropCurrentTool();
            }
            else
            {
                carried.Drop();
                animCtrl?.SetHolding(false);
            }

            carried = null;
            animCtrl?.TriggerInteract();
            Debug.Log($"Objeto soltado ({pickupKey}).");
            return;
        }

        // Lógica 2: Recoger Carryable, Tool o Memorie
        if (nearbyCarryable != null)
        {
            // Lógica de MemorieObject...
            if (nearbyCarryable.CompareTag(memorieTag))
            {
                MemorieObject mObject = nearbyCarryable.GetComponent<MemorieObject>();
                if (mObject != null)
                {
                    mObject.StartDecisionProcess();
                    nearbyCarryable = null;
                    animCtrl?.TriggerInteract();
                    Debug.Log("¡Objeto de Memoria recogido! Iniciando proceso de decisión (Q).");
                    return;
                }
            }

            // LÓGICA NORMAL DE RECOGER HERRAMIENTA O CARRYABLE
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

    // =========================================================================
    // LÓGICA DE INTERACCIÓN GENERAL (Tecla E)
    // =========================================================================

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
    // FUNCIONES DE COMPATIBILIDAD
    // =========================================================================

    public void SetCurrentInteractable(IInteractable interactable)
    {
        currentDoorInteractable = interactable;
    }
    public void ClearCurrentInteractable()
    {
        currentDoorInteractable = null;
    }

    // =========================================================================
    // FUNCIÓN DE DEBUG (TECLA 'G') - NO CAMBIA
    // =========================================================================

    private void LogRemainingItemsCount()
    {
        if (TaskManager.Instance != null)
        {
            int remaining = TaskManager.Instance.GetRemainingCleanableItemsCount();
            int totalBasura = TaskManager.Instance.totalTrashItems;
            int totalManchas = TaskManager.Instance.totalDirtSpots;

            List<string> faltantes = TaskManager.Instance.remainingItemNames;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=================================================");
            sb.AppendLine($"[DEBUG CONTEO 'G'] Ítems Faltantes: {remaining}");
            sb.AppendLine($"   -> Basura (Total Inicial): {totalBasura}");
            sb.AppendLine($"   -> Manchas (Total Inicial): {totalManchas}");
            sb.AppendLine($"   -> (Contador interno de Basura Limpiada: {TaskManager.Instance.cleanedTrashItems})");
            sb.AppendLine("-------------------------------------------------");

            sb.AppendLine($"Objetos Pendientes (Total: {faltantes.Count}):");

            if (faltantes.Count > 0)
            {
                foreach (string item in faltantes.Take(10))
                {
                    sb.AppendLine($"   - {item}");
                }
                if (faltantes.Count > 10)
                {
                    sb.AppendLine($"(... {faltantes.Count - 10} más no mostrados)");
                }
            }
            else
            {
                sb.AppendLine("   - ¡Todo limpio!");
            }

            sb.AppendLine("=================================================");
            Debug.Log(sb.ToString());
        }
        else
        {
            Debug.LogError("[DEBUG] TaskManager no está inicializado. No se puede obtener el conteo.");
        }
    }
}