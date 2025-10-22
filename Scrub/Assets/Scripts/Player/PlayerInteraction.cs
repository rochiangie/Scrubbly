using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic; // Para List y StringBuilder (Debug G)

// ----------------------------------------------------
// INTERFACES (Asumo que existen)
// ----------------------------------------------------
// NOTA: Para que esto funcione, asegúrate de que todos los objetos
// (Puertas, Carryable, MemorieObject, TrashObject) usen estas interfaces.
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
    private ToolPanelIdea toolPanelIdea; // Para manejar el panel de Tools/Pausa

    private CleaningController cleaningController;
    private Carryable carried;

    // ***************************************************************
    // 🗑️ VARIABLES DE DETECCIÓN (AHORA USADAS POR EL RAYCAST) 🚀
    // ***************************************************************
    private IInteractable currentDoorInteractable = null; // Puertas/General (E)
    private Carryable nearbyCarryable = null;             // Carryable/Tool/Memorie (Q)
    private IAttackable nearbyAttackable = null;          // Objeto Atacable/Destruible (F)

    // Almacena el último objeto para evitar llamadas GetComponent innecesarias en el Raycast
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

        // Usamos FindObjectOfType, ya que el componente ToolPanelIdea suele estar en un Canvas distinto.
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
    // FUNCIÓN UPDATE MODIFICADA - AHORA LLAMA A LA DETECCIÓN POR RAYCAST
    // =========================================================================

    void Update()
    {
        // 🚀 NUEVA LÓGICA: Detección centralizada por Raycast 🚀
        DetectNearbyObjects();

        // -----------------------------------------------------------------
        // 🖱️ LÓGICA DE LIMPIEZA CON CLICK DEL MOUSE (NO CAMBIA) 🖱️
        // -----------------------------------------------------------------
        HandleMouseClickCleaning();

        // -----------------------------------------------------------------
        // 🚨 LÓGICA DE USO Y DESTRUCCIÓN DE LA HERRAMIENTA (TECLA F) 🚨
        // -----------------------------------------------------------------
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
                    // Si no tiene herramienta, ataca al objeto atacable detectado por el Raycast
                    nearbyAttackable.ReceiveAttack();
                    Debug.Log($"Ataque directo (F) ejecutado sobre {currentRaycastHitObject.name}.");
                }
            }

            if (Input.GetKeyUp(attackKey))
            {
                toolItemManager.DestroyCurrentTool();
            }
        }

        // -----------------------------------------------------------------
        // LÓGICA DE AGARRE/DECISIÓN (TECLA Q)
        // -----------------------------------------------------------------
        if (Input.GetKeyDown(pickupKey))
            TryPickup();

        // -----------------------------------------------------------------
        // LÓGICA DE INTERACCIÓN GENERAL (E)
        // -----------------------------------------------------------------
        if (Input.GetKeyDown(generalInteractKey))
            TryGeneralInteract();

        // 🚨 PAUSA/PANEL LÓGICA (NO CAMBIA) 🚨

        // 1. PAUSA PRINCIPAL (Escape)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            toolPanelIdea?.TogglePause();
        }

        // 2. PANEL DE TOOLS / SCORE (Tab o Enter)
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
    // 🚀 NUEVA FUNCIÓN: DETECCIÓN CENTRALIZADA CON RAYCAST 🚀
    // =========================================================================

    /// <summary>
    /// Lanza un Raycast para detectar el objeto más cercano y actualiza las referencias de interacción.
    /// </summary>
    private void DetectNearbyObjects()
    {
        // Limpiar todas las referencias en cada frame
        nearbyCarryable = null;
        nearbyAttackable = null;
        currentDoorInteractable = null;
        currentRaycastHitObject = null;

        RaycastHit hit;
        Vector3 rayOrigin = mainCamera.transform.position;
        Vector3 rayDirection = mainCamera.transform.forward;

        // Raycast lanzado desde el centro de la cámara
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionRange, interactableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            currentRaycastHitObject = hitObject;

            // 1. Comprobar si es Carryable (incluye Tools y MemorieObjects)
            Carryable c = hitObject.GetComponent<Carryable>() ?? hitObject.GetComponentInParent<Carryable>();
            if (c != null && carried == null)
            {
                nearbyCarryable = c;
                Debug.Log($"[Raycast] Carryable detectado: {c.name}");
            }

            // 2. Comprobar si es IAttackable (Basura/Destructible - usado con F sin herramienta)
            IAttackable a = hitObject.GetComponent<IAttackable>() ?? hitObject.GetComponentInParent<IAttackable>();
            if (a != null)
            {
                nearbyAttackable = a;
                // Debug.Log($"[Raycast] Atacable detectado: {hitObject.name}");
            }

            // 3. Comprobar si es IInteractable (General/Puertas - usado con E)
            IInteractable i = hitObject.GetComponent<IInteractable>() ?? hitObject.GetComponentInParent<IInteractable>();
            if (i != null)
            {
                // Se asume que este es el principal uso del GeneralInteractKey (E)
                currentDoorInteractable = i;
                // Debug.Log($"[Raycast] Interacción General detectada: {hitObject.name}");
            }
        }
    }


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

                    // 🛑 Si llegamos aquí, se tenía una herramienta y se destruye el objeto.
                    Destroy(hitObject);
                    return;
                }

                // 2. INTENTAR LIMPIAR MANCHAS (Clase: DirtSpot)
                DirtSpot dirtSpot = hitObject.GetComponent<DirtSpot>();
                if (dirtSpot != null)
                {
                    if (cleaningController != null)
                    {
                        // Si la herramienta es la correcta para esta mancha (CanBeCleanedBy)
                        if (dirtSpot.CanBeCleanedBy(activeTool.ToolId))
                        {
                            float damage = activeTool.ToolPower;
                            dirtSpot.CleanHit(damage);
                            activeTool.TryUse(); // Consume durabilidad

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
    // LÓGICA DE INTERACCIÓN PRINCIPAL (Tecla Q) - NO CAMBIA, AHORA USA nearbyCarryable DEL RAYCAST
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
    // LÓGICA DE INTERACCIÓN GENERAL (Tecla E) - NO CAMBIA, AHORA USA currentDoorInteractable DEL RAYCAST
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
    // 🛑 TRIGGERS DE COLISIÓN (ELIMINADOS Y REEMPLAZADOS POR DetectNearbyObjects) 🛑
    // =========================================================================
    // private void OnTriggerEnter(Collider other) {}
    // private void OnTriggerExit(Collider other) {}


    // =========================================================================
    // FUNCIONES DE PUERTA (Mantienen su funcionalidad para ser llamadas desde otros scripts)
    // =========================================================================

    // Estas funciones aún son necesarias si otros scripts, como un 'DoorController', 
    // manejan su propia lógica de trigger/proximidad y le notifican al PlayerInteraction.
    // Sin embargo, si la detección de puertas ahora es SÓLO por Raycast, estas funciones
    // pueden volverse redundantes, pero las mantendremos por compatibilidad.
    public void SetCurrentInteractable(IInteractable interactable)
    {
        // Se asume que esto es usado por un script de puerta para forzar una interacción.
        currentDoorInteractable = interactable;
    }
    public void ClearCurrentInteractable()
    {
        currentDoorInteractable = null;
    }

    // =========================================================================
    // FUNCIÓN DE DEBUG (TECLA 'G') - NO CAMBIA
    // =========================================================================

    /// <summary>
    /// Muestra la cuenta de ítems restantes para limpiar.
    /// </summary>
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