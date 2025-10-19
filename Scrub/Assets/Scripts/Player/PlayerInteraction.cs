using UnityEngine;
using System;
using System.Linq;

// ----------------------------------------------------
// INTERFACES 
// ----------------------------------------------------
public interface IInteractable { void Interact(); }
public interface IAttackable { void ReceiveAttack(); }
// NOTA: Asumo que tienes una clase GameEvents para el ToggleScorePanel

public class PlayerInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public Transform holdPoint;
    public PlayerAnimationController animCtrl;

    // 🚨 REFERENCIA AL TOOL HANDLER (Declaración es correcta)
    private ToolHandler toolHandler;

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
        toolHandler = GetComponent<ToolHandler>();

        if (toolHandler == null)
        {
            Debug.LogError("ToolHandler no se pudo encontrar en este objeto.", this);
        }
        
        cleaningController = GetComponent<CleaningController>();
        if (cleaningController == null)
            Debug.LogError("PlayerInteraction: No se encontró el CleaningController.");

        if (!animCtrl) animCtrl = GetComponentInChildren<PlayerAnimationController>() ?? GetComponent<PlayerAnimationController>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerColliders = GetComponentsInChildren<Collider>();
    }

    void Update()
    {
        // LÓGICA DE AGARRE/DECISIÓN (TECLA T)
        if (Input.GetKeyDown(pickupKey))
            TryPickup();

        // LÓGICA DE INTERACCIÓN GENERAL (TECLA E)
        if (Input.GetKeyDown(generalInteractKey))
            TryGeneralInteract();

        // TOGGLE DEL PANEL DE PUNTUACIÓN
        if (Input.GetKeyDown(scorePanelToggleKey))
        {
            // Asumo que tienes una clase estática GameEvents
            // GameEvents.ToggleScorePanel(); 
        }

        // -----------------------------------------------------------------
        // ➡️ LÓGICA DE APERTURA DEL PANEL DE HERRAMIENTAS (TECLA ENTER/RETURN)
        // -----------------------------------------------------------------
        if (Input.GetKeyDown(KeyCode.Return)) // Tecla Enter (Return)
        {
            // El debug ya verificó que la tecla se presiona, ahora verifica la referencia
            if (toolHandler != null)
            {
                toolHandler.ToggleToolSelectionPanel();
            }
            else
            {
                // El error ya se detectó en Awake, pero lo mantenemos para feedback inmediato
                Debug.LogError("Error FATAL: La referencia 'toolHandler' es NULL en PlayerInteraction. No se puede abrir el panel.");
            }
        }
    }

    // =========================================================================
    // EL RESTO DE TUS FUNCIONES (TryPickup, TryGeneralInteract, Triggers) 
    // ESTÁN CORRECTAS Y SE MANTIENEN IGUALES
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

            // Si es una herramienta activa, la destruye/suelta de forma controlada
            if (isTool)
            {
                // NOTA: El ToolHandler se encarga ahora de la instanciación/destrucción de la herramienta activa.
                // Esta lógica parece ser para herramientas NO instanciadas por el panel, sino recogidas del suelo.
                // Se mantiene la lógica original de tu script.
                cleaningController.DropCurrentTool();
            }
            else
            {
                carried.Drop();
                animCtrl?.SetHolding(false);
                // Lógica de SFX...
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