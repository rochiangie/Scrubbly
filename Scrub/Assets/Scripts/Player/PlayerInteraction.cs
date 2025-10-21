// PlayerInteraction.cs
using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic; // Para List y StringBuilder (Debug G)

// ----------------------------------------------------
// INTERFACES (Asumo que existen)
// ----------------------------------------------------
public interface IInteractable { void Interact(); }
public interface IAttackable { void ReceiveAttack(); }

public class PlayerInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public Transform holdPoint;
    public PlayerAnimationController animCtrl;

    // 🚨 REFERENCIAS A LOS GESTORES DE COMPONENTES 🚨
    private ToolItemManager toolItemManager;
    private ToolPanelIdea toolPanelIdea; // Para manejar el panel de Tools/Pausa

    private CleaningController cleaningController;
    private Carryable carried;
    private IInteractable currentDoorInteractable = null;
    private Carryable nearbyCarryable = null;
    private IAttackable nearbyAttackable = null;

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
    [SerializeField] private KeyCode pickupKey = KeyCode.T;
    [Tooltip("Tecla para Atacar/Destruir directamente (Limpieza)")]
    [SerializeField] private KeyCode attackKey = KeyCode.F;
    [Tooltip("Tecla para mostrar/ocultar el panel de puntuación sentimental.")]
    [SerializeField] private KeyCode scorePanelToggleKey = KeyCode.Tab;

    [Header("Ataque Directo (Limpieza)")]
    [Tooltip("Distancia máxima para detectar un objeto atacable/destruible.")]
    public float attackRange = 2.5f;
    public LayerMask attackableLayer;

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
    // FUNCIÓN UPDATE MODIFICADA - SEPARACIÓN DE INPUTS ESC/ENTER
    // =========================================================================

    void Update()
    {
        // -----------------------------------------------------------------
        // 🖱️ LÓGICA DE LIMPIEZA CON CLICK DEL MOUSE 🖱️
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
            }

            if (Input.GetKeyUp(attackKey))
            {
                toolItemManager.DestroyCurrentTool();
            }
        }

        // -----------------------------------------------------------------
        // LÓGICA DE AGARRE/DECISIÓN (TECLA T)
        // -----------------------------------------------------------------
        if (Input.GetKeyDown(pickupKey))
            TryPickup();

        // -----------------------------------------------------------------
        // LÓGICA DE INTERACCIÓN GENERAL (E)
        // -----------------------------------------------------------------
        if (Input.GetKeyDown(generalInteractKey))
            TryGeneralInteract();

        // 🚨 SEPARACIÓN DE INPUTS DE PAUSA/PANEL 🚨

        // 1. PAUSA PRINCIPAL (Escape)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (toolPanelIdea != null)
            {
                // TogglePause() debe congelar el tiempo y mostrar el menú principal de pausa.
                toolPanelIdea.TogglePause();
            }
        }

        // 2. PANEL DE TOOLS / SCORE (Tab o Enter)
        if (Input.GetKeyDown(scorePanelToggleKey) || Input.GetKeyDown(KeyCode.Return))
        {
            if (toolPanelIdea != null)
            {
                // 🚨 CRÍTICO: ToggleToolsPanel() NO debe congelar el tiempo (Time.timeScale = 1).
                // Si esta función no existe en ToolPanelIdea.cs, compilará con error.
                toolPanelIdea.ToggleToolsPanel();
            }
        }

        // DEBUG: LÓGICA DE CONTEO (TECLA G)
        if (Input.GetKeyDown(KeyCode.G))
        {
            LogRemainingItemsCount();
        }
    }

    // =========================================================================
    // 🖱️ FUNCIÓN PARA LIMPIEZA CON CLICK DEL MOUSE
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
                DirtSpot dirtSpot = hitObject.GetComponent<DirtSpot>();
                if (dirtSpot != null)
                {
                    if (cleaningController != null)
                    {
                        ToolDescriptor activeTool = cleaningController.CurrentTool;
                        if (activeTool != null && dirtSpot.CanBeCleanedBy(activeTool.ToolId))
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
    // LÓGICA DE INTERACCIÓN PRINCIPAL (Tecla T)
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
                    Debug.Log("¡Objeto de Memoria recogido! Iniciando proceso de decisión (T).");
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

        Debug.Log("[Interacción Fallida] No hay objeto que soltar ni recoger (T).");
    }

    // =========================================================================
    // FUNCIÓN DE DEBUG (TECLA 'G')
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
            sb.AppendLine($"   -> Basura (Total Inicial): {totalBasura}");
            sb.AppendLine($"   -> Manchas (Total Inicial): {totalManchas}");
            sb.AppendLine($"   -> (Contador interno de Basura Limpiada: {TaskManager.Instance.cleanedTrashItems})");
            sb.AppendLine("-------------------------------------------------");

            sb.AppendLine($"Objetos Pendientes (Total: {faltantes.Count}):");

            if (faltantes.Count > 0)
            {
                foreach (string item in faltantes.Take(10))
                {
                    sb.AppendLine($"   - {item}");
                }
                if (faltantes.Count > 10)
                {
                    sb.AppendLine($"(... {faltantes.Count - 10} más no mostrados)");
                }
            }
            else
            {
                sb.AppendLine("   - ¡Todo limpio!");
            }

            sb.AppendLine("=================================================");
            Debug.Log(sb.ToString());
        }
        else
        {
            Debug.LogError("[DEBUG] TaskManager no está inicializado. No se puede obtener el conteo.");
        }
    }


    // =========================================================================
    // TRIGGERS Y OTRAS FUNCIONES
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

    private void OnTriggerEnter(Collider other)
    {
        Carryable c = other.GetComponent<Carryable>() ?? other.GetComponentInParent<Carryable>();
        if (c != null && carried == null)
        {
            nearbyCarryable = c;
            Debug.Log($"[Proximidad] Carryable detectado: {c.name}");
        }

        IAttackable a = other.GetComponent<IAttackable>() ?? other.GetComponentInParent<IAttackable>();
        if (a != null)
        {
            nearbyAttackable = a;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Carryable c = other.GetComponent<Carryable>() ?? other.GetComponentInParent<Carryable>();
        if (c != null && c == nearbyCarryable)
        {
            nearbyCarryable = null;
            Debug.Log($"[Proximidad] Carryable perdido: {c.name}");
        }

        IAttackable a = other.GetComponent<IAttackable>() ?? other.GetComponentInParent<IAttackable>();
        if (a != null && a == nearbyAttackable)
        {
            nearbyAttackable = null;
        }
    }

    // Funciones de la puerta
    public void SetCurrentInteractable(IInteractable interactable)
    {
        currentDoorInteractable = interactable;
    }
    public void ClearCurrentInteractable()
    {
        currentDoorInteractable = null;
    }
}