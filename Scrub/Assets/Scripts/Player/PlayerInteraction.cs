using UnityEngine;
using System;
using System.Linq;

// ----------------------------------------------------
// INTERFACES (Mantengo las tuyas)
// ----------------------------------------------------
public interface IInteractable { void Interact(); }
public interface IAttackable { void ReceiveAttack(); } // Interfaz para destrucción (opcional, pero útil)

public class PlayerInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public Transform holdPoint;
    public PlayerAnimationController animCtrl;

    private CleaningController cleaningController;
    private Carryable carried;
    private IInteractable currentDoorInteractable = null;
    private Carryable nearbyCarryable = null;
    private IAttackable nearbyAttackable = null; // Nuevo: Para objetos destruibles directos

    private Rigidbody playerRigidbody;
    private Collider[] playerColliders;

    [Header("Input Keys")]
    [Tooltip("Tecla para Interacción General (Puertas)")]
    [SerializeField] private KeyCode generalInteractKey = KeyCode.E;
    [Tooltip("Tecla para Recoger/Agarrar y Soltar objetos Carryable/Tool")]
    [SerializeField] private KeyCode pickupKey = KeyCode.T;
    [Tooltip("Tecla para Atacar/Destruir directamente (Limpieza)")]
    [SerializeField] private KeyCode attackKey = KeyCode.F; // NUEVO INPUT

    [Header("Ataque Directo (Limpieza)")]
    [Tooltip("Distancia máxima para detectar un objeto atacable/destruible.")]
    public float attackRange = 2.5f;
    public LayerMask attackableLayer; // Layer de los objetos destructibles (Ej: Suciedad)

    [Header("Tags de Objetos")]
    [Tooltip("Tag para objetos que inician el proceso de decisión sentimental.")]
    [SerializeField] private string memorieTag = "Memorie"; // El Tag de los objetos de memoria

    void Awake()
    {
        cleaningController = GetComponent<CleaningController>();
        if (cleaningController == null)
            Debug.LogError("PlayerInteraction: No se encontró el CleaningController.");

        if (!animCtrl) animCtrl = GetComponentInChildren<PlayerAnimationController>() ?? GetComponent<PlayerAnimationController>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerColliders = GetComponentsInChildren<Collider>();
    }

    void Update()
    {
        // LÓGICA DE ATAQUE/DESTRUCCIÓN (TECLA F) <--- ¡NUEVO!
        if (Input.GetKeyDown(attackKey))
            TryAttack();

        // LÓGICA DE AGARRE/DECISIÓN (TECLA T)
        if (Input.GetKeyDown(pickupKey))
            TryPickup();

        // LÓGICA DE INTERACCIÓN GENERAL (TECLA E)
        if (Input.GetKeyDown(generalInteractKey))
            TryGeneralInteract();
    }

    // =========================================================================
    // NUEVA FUNCIÓN: ATAQUE DIRECTO (Tecla F) - Para limpieza/destrucción inmediata
    // =========================================================================
    void TryAttack()
    {
        // Dispara la animación de ataque (Asume que tienes un Trigger "Attack" en tu Animator)
        animCtrl?.TriggerInteract();

        // 1. Raycast para detectar el objeto atacable
        RaycastHit hit;
        // Lanzamos un rayo hacia adelante
        if (Physics.Raycast(transform.position, transform.forward, out hit, attackRange, attackableLayer))
        {
            // Verificamos si el objeto golpeado tiene el script de ataque (por ejemplo, DirtSpot o SentimentalObject de limpieza)
            IAttackable attackable = hit.collider.GetComponent<IAttackable>();
            if (attackable == null)
            {
                // Intenta buscar en el padre por si el collider es hijo
                attackable = hit.collider.GetComponentInParent<IAttackable>();
            }

            if (attackable != null)
            {
                // Llama al método de ataque/destrucción del objeto
                attackable.ReceiveAttack();
                Debug.Log($"Objeto {hit.collider.name} atacado/limpiado con la tecla {attackKey}!");
                return;
            }
        }

        Debug.Log("[Ataque Fallido (F)] No se detectó IAttackable en el rango.");
    }

    // =========================================================================
    // FUNCIÓN PRINCIPAL: AGARRAR/SOLTAR/DECIDIR (Tecla T)
    // =========================================================================
    void TryPickup()
    {
        // Lógica 1: Soltar objeto (Si se presiona T y tengo algo, suelto)
        if (carried)
        {
            bool isTool = (cleaningController != null && cleaningController.CurrentTool != null &&
                            carried.GetComponent<ToolDescriptor>() == cleaningController.CurrentTool);

            if (isTool)
            {
                cleaningController.DropCurrentTool();
            }
            else
            {
                carried.Drop();
                animCtrl?.SetHolding(false);

                if (AudioManager.Instance != null)
                {
                    // Asume que tienes un AudioManager con PlayDropSFX
                    // AudioManager.Instance.PlayDropSFX(); 
                }
            }

            carried = null;
            animCtrl?.TriggerInteract();
            Debug.Log("Objeto soltado (T).");
            return;
        }

        // Lógica 2: Recoger Carryable, Tool o Memorie
        if (nearbyCarryable != null)
        {
            // ----------------------------------------------------
            // NUEVO: DECISIÓN DE MEMORIA
            // ----------------------------------------------------
            if (nearbyCarryable.CompareTag(memorieTag))
            {
                MemorieObject mObject = nearbyCarryable.GetComponent<MemorieObject>();
                if (mObject != null)
                {
                    // Se inicia el proceso de decisión, que luego destruirá el objeto.
                    mObject.StartDecisionProcess();

                    // Asegurarse de que el objeto ya no sea detectable para evitar loops.
                    nearbyCarryable = null;
                    animCtrl?.TriggerInteract();
                    Debug.Log("¡Objeto de Memoria recogido! Iniciando proceso de decisión (T).");
                    return;
                }
            }

            // ----------------------------------------------------
            // LÓGICA NORMAL DE RECOGER HERRAMIENTA O CARRYABLE (Si no es Memorie)
            // ----------------------------------------------------

            // Asegurar HoldPoint si no existe 
            if (!holdPoint)
            {
                var hp = new GameObject("HoldPoint").transform;
                hp.SetParent(transform);
                hp.localPosition = new Vector3(0, 1.2f, 0.6f);
                holdPoint = hp;
            }

            // Mover el objeto a la mano (manejado por Carryable.cs)
            nearbyCarryable.PickUp(holdPoint, playerColliders);

            ToolDescriptor td = nearbyCarryable.GetComponent<ToolDescriptor>() ?? nearbyCarryable.GetComponentInParent<ToolDescriptor>();

            if (td != null && cleaningController != null)
            {
                cleaningController.RegisterTool(td);
                carried = nearbyCarryable;
            }
            else
            {
                carried = nearbyCarryable;
                if (AudioManager.Instance != null)
                {
                    // AudioManager.Instance.PlayPickupSFX();
                }
            }

            nearbyCarryable = null;
            animCtrl?.SetHolding(true);
            animCtrl?.TriggerInteract();

            Debug.Log($"¡Objeto {carried.name} recogido con la tecla {pickupKey}!");
            return;
        }

        Debug.Log("[Interacción Fallida] No hay objeto que soltar ni recoger (T).");
    }

    // =========================================================================
    // FUNCIÓN EXISTENTE: Maneja la INTERACCIÓN GENERAL (Tecla E)
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
    // TRIGGERS DE PROXIMIDAD (Detecta objetos Carryable y IAttackable)
    // =========================================================================

    private void OnTriggerEnter(Collider other)
    {
        // 1. Detección de Carryable (para la tecla T)
        Carryable c = other.GetComponent<Carryable>();
        if (c == null) c = other.GetComponentInParent<Carryable>();
        if (c != null && carried == null)
        {
            nearbyCarryable = c;
            Debug.Log($"[Proximidad] Carryable detectado: {c.name}");
        }

        // 2. Detección de IAttackable (opcional si usas Raycast, pero útil para Feedback)
        IAttackable a = other.GetComponent<IAttackable>();
        if (a == null) a = other.GetComponentInParent<IAttackable>();
        if (a != null)
        {
            nearbyAttackable = a;
            // Debug.Log($"[Proximidad] IAttackable detectado: {other.name}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 1. Limpieza de Carryable
        Carryable c = other.GetComponent<Carryable>();
        if (c == null) c = other.GetComponentInParent<Carryable>();
        if (c != null && c == nearbyCarryable)
        {
            nearbyCarryable = null;
            Debug.Log($"[Proximidad] Carryable perdido: {c.name}");
        }

        // 2. Limpieza de IAttackable
        IAttackable a = other.GetComponent<IAttackable>();
        if (a == null) a = other.GetComponentInParent<IAttackable>();
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