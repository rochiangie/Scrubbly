// PlayerInteraction.cs
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

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
    private ToolPanelIdea toolPanelIdea; // Para abrir el panel con TogglePause()

    private CleaningController cleaningController;
    private Carryable carried;
    private IInteractable currentDoorInteractable = null;
    private Carryable nearbyCarryable = null;
    private IAttackable nearbyAttackable = null;

    private Rigidbody playerRigidbody;
    private Collider[] playerColliders;

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

    void Awake()
    {
        // 1. Obtener la referencia al ToolItemManager
        toolItemManager = GetComponent<ToolItemManager>();
        if (toolItemManager == null)
            Debug.LogError("PlayerInteraction: No se encontró el ToolItemManager. La limpieza por F fallará.", this);

        // 2. Obtener la referencia al ToolPanelIdea (Apertura del Panel/Pausa)
        toolPanelIdea = GetComponent<ToolPanelIdea>();
        if (toolPanelIdea == null)
            Debug.LogWarning("PlayerInteraction: No se encontró ToolPanelIdea. La apertura del panel (Enter) no funcionará.");

        // 3. Obtener el resto de componentes...
        cleaningController = GetComponent<CleaningController>();
        if (cleaningController == null)
            Debug.LogError("PlayerInteraction: No se encontró el CleaningController.");

        if (!animCtrl) animCtrl = GetComponentInChildren<PlayerAnimationController>() ?? GetComponent<PlayerAnimationController>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerColliders = GetComponentsInChildren<Collider>();
    }

    // =========================================================================
    // FUNCIÓN UPDATE MODIFICADA
    // =========================================================================

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            LogRemainingItemsCount();
        }
        // -----------------------------------------------------------------
        // 🚨 LÓGICA DE USO Y DESTRUCCIÓN DE LA HERRAMIENTA (TECLA F) 🚨
        // -----------------------------------------------------------------
        if (toolItemManager != null)
        {
            GameObject activeTool = toolItemManager.GetCurrentTool();

            // Lógica de Limpieza: Presionar F
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

            // Lógica de Destrucción: Soltar F
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
        // LÓGICA DE APERTURA DE PANELES (Enter)
        // -----------------------------------------------------------------
        if (Input.GetKeyDown(generalInteractKey))
            TryGeneralInteract();

        if (Input.GetKeyDown(scorePanelToggleKey))
        {
            // GameEvents.ToggleScorePanel(); 
        }

        if (Input.GetKeyDown(KeyCode.Return)) // Tecla Enter (Return)
        {
            if (toolPanelIdea != null)
            {
                // 🚨 LLAMADA A LA FUNCIÓN DE APERTURA DEL PANEL 🚨
                toolPanelIdea.TogglePause();
            }
        }
    }


    // =========================================================================
    // EL RESTO DE TUS FUNCIONES SE MANTIENEN IGUALES
    // =========================================================================

    // FUNCIÓN PRINCIPAL: AGARRAR/SOLTAR/DECIDIR (Tecla T)
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
            Debug.Log("Objeto soltado (T).");
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

    // FUNCIÓN EXISTENTE: Maneja la INTERACCIÓN GENERAL (Tecla E)
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

    // TRIGGERS DE PROXIMIDAD (Detecta objetos Carryable y IAttackable)
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
    /// <summary>
    /// Muestra la cuenta de ítems restantes para limpiar.
    /// </summary>
    /// <summary>
    /// Muestra la cuenta de ítems restantes para limpiar, incluyendo los nombres de los pendientes.
    /// </summary>
    private void LogRemainingItemsCount()
    {
        if (TaskManager.Instance != null)
        {
            // Se asume que GetRemainingCleanableItemsCount() es público en TaskManager
            int remaining = TaskManager.Instance.GetRemainingCleanableItemsCount();
            int totalBasura = TaskManager.Instance.totalTrashItems;
            int totalManchas = TaskManager.Instance.totalDirtSpots;

            // 🚨 Acceso a la lista de pendientes 🚨
            List<string> faltantes = TaskManager.Instance.remainingItemNames;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=================================================");
            sb.AppendLine($"[DEBUG CONTEO 'G'] Ítems Faltantes: {remaining}");
            sb.AppendLine($"   -> Basura (Total Inicial): {totalBasura}");
            sb.AppendLine($"   -> Manchas (Total Inicial): {totalManchas}");
            sb.AppendLine($"   -> (Contador interno de Basura Limpiada: {TaskManager.Instance.cleanedTrashItems})");
            sb.AppendLine("-------------------------------------------------");

            // 🚨 Lógica para listar los objetos pendientes 🚨
            sb.AppendLine($"Objetos Pendientes (Total: {faltantes.Count}):");

            if (faltantes.Count > 0)
            {
                // Muestra solo los primeros 10 ítems para evitar saturar la consola
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