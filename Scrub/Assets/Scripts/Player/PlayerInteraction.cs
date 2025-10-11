using UnityEngine;
using System;

public interface IInteractable { void Interact(); }

public class PlayerInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public Transform holdPoint;
    public PlayerAnimationController animCtrl;

    private CleaningController cleaningController;

    private Carryable carried;
    private IInteractable currentDoorInteractable = null;
    private Carryable nearbyCarryable = null;

    private Rigidbody playerRigidbody;
    private Collider[] playerColliders;

    [Header("Input Keys")]
    [Tooltip("Tecla para Interacción General (Puertas, Soltar Objeto)")]
    [SerializeField] private KeyCode generalInteractKey = KeyCode.E;
    [Tooltip("Tecla para Recoger/Agarrar (Pickup) objetos Carryable")]
    [SerializeField] private KeyCode pickupKey = KeyCode.T; // 🛑 NUEVA TECLA PARA AGARRAR

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
        // 🛑 LÓGICA DE AGARRE (TECLA T)
        if (Input.GetKeyDown(pickupKey))
            TryPickup();

        // 🛑 LÓGICA DE INTERACCIÓN GENERAL (TECLA E)
        if (Input.GetKeyDown(generalInteractKey))
            TryGeneralInteract();
    }

    // =========================================================================
    // NUEVA FUNCIÓN: Maneja solo la lógica de AGARRAR/SOLTAR
    // =========================================================================
    void TryPickup()
    {
        // Lógica 1: Soltar objeto (Si se presiona T y tengo algo, suelto)
        if (carried)
        {
            // La lógica de soltar debe ser uniforme, ya sea Carryable o Tool

            // 1. Es una herramienta de limpieza asignada al CleaningController
            if (cleaningController != null && cleaningController.CurrentTool != null &&
                carried.GetComponent<ToolDescriptor>() == cleaningController.CurrentTool)
            {
                // DELEGAR SOLTAR A CLEANING CONTROLLER
                cleaningController.DropCurrentTool();
                Debug.Log("Herramienta de limpieza soltada por CleaningController.");
            }
            else
            {
                // Es un Carryable normal.
                carried.Drop();
                animCtrl?.SetHolding(false);
                Debug.Log("Objeto normal soltado.");
            }

            carried = null; // Reseteamos la referencia local
            animCtrl?.TriggerInteract();
            return;
        }

        // Lógica 2: Recoger Carryable o Tool (Si presiono T y hay algo cerca)
        if (nearbyCarryable != null)
        {
            // Asegurar HoldPoint si no existe (buena práctica)
            if (!holdPoint)
            {
                var hp = new GameObject("HoldPoint").transform;
                hp.SetParent(transform);
                hp.localPosition = new Vector3(0, 1.2f, 0.6f);
                holdPoint = hp;
            }

            // Mover el objeto a la mano (manejado por Carryable.cs)
            nearbyCarryable.PickUp(holdPoint, playerColliders);

            // Verificamos si es una herramienta de limpieza y la registramos
            ToolDescriptor td = nearbyCarryable.GetComponent<ToolDescriptor>() ?? nearbyCarryable.GetComponentInParent<ToolDescriptor>();

            if (td != null && cleaningController != null)
            {
                // DELEGAR ASIGNACIÓN
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
    // FUNCIÓN EXISTENTE: Ahora maneja solo la INTERACCIÓN DE PUERTAS
    // =========================================================================
    void TryGeneralInteract()
    {
        // Lógica 1: Interacción por TRIGGER (Puerta)
        if (currentDoorInteractable != null)
        {
            currentDoorInteractable.Interact();
            animCtrl?.TriggerInteract();
            Debug.Log($"Interacción General (Puerta) ejecutada con {generalInteractKey}.");
            return;
        }

        Debug.Log("[Interacción Fallida] No hay Interacción General (Puerta) activa.");
    }

    // ... (El resto de los métodos OnTriggerEnter/Exit y Set/ClearCurrentInteractable se mantienen igual) ...

    // Detección de proximidad del cubo
    private void OnTriggerEnter(Collider other)
    {
        Carryable c = other.GetComponent<Carryable>();
        if (c == null) c = other.GetComponentInParent<Carryable>();
        if (c != null && carried == null)
        {
            nearbyCarryable = c;
        }
    }
    // Detección de proximidad del cubo
    private void OnTriggerExit(Collider other)
    {
        Carryable c = other.GetComponent<Carryable>();
        if (c == null) c = other.GetComponentInParent<Carryable>();
        if (c != null && c == nearbyCarryable)
        {
            nearbyCarryable = null;
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