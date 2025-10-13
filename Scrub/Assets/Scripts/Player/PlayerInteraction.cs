using UnityEngine;
using System;
using System.Linq;

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
    [Tooltip("Tecla para Interacción General (Puertas)")]
    [SerializeField] private KeyCode generalInteractKey = KeyCode.E;
    [Tooltip("Tecla para Recoger/Agarrar y Soltar objetos Carryable/Tool")]
    [SerializeField] private KeyCode pickupKey = KeyCode.T;

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
        // LÓGICA DE AGARRE (TECLA T)
        if (Input.GetKeyDown(pickupKey))
            TryPickup();

        // LÓGICA DE INTERACCIÓN GENERAL (TECLA E)
        if (Input.GetKeyDown(generalInteractKey))
            TryGeneralInteract();
    }

    // =========================================================================
    // FUNCIÓN PRINCIPAL: AGARRAR/SOLTAR con SFX
    // =========================================================================
    void TryPickup()
    {
        // Lógica 1: Soltar objeto (Si se presiona T y tengo algo, suelto)
        if (carried)
        {
            // Determinar si es una herramienta o un Carryable normal.
            bool isTool = (cleaningController != null && cleaningController.CurrentTool != null &&
                           carried.GetComponent<ToolDescriptor>() == cleaningController.CurrentTool);

            if (isTool)
            {
                // DELEGAR SOLTAR A CLEANING CONTROLLER (Este método YA dispara el SFX de soltar)
                cleaningController.DropCurrentTool();
                Debug.Log("Herramienta de limpieza soltada por CleaningController.");
            }
            else
            {
                // Es un Carryable normal.
                carried.Drop();
                animCtrl?.SetHolding(false);
                Debug.Log("Objeto normal soltado.");

                // 🔥 DISPARAR SFX DE SOLTAR (Solo si es un Carryable normal, si es herramienta, lo hizo CleaningController)
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayDropSFX();
                }
            }

            carried = null;
            animCtrl?.TriggerInteract();
            return;
        }

        // Lógica 2: Recoger Carryable o Tool (Si presiono T y hay algo cerca)
        if (nearbyCarryable != null)
        {
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

            // Verificamos si es una herramienta de limpieza y la registramos
            ToolDescriptor td = nearbyCarryable.GetComponent<ToolDescriptor>() ?? nearbyCarryable.GetComponentInParent<ToolDescriptor>();

            if (td != null && cleaningController != null)
            {
                // DELEGAR ASIGNACIÓN a CleaningController.
                // 🔴 IMPORTANTE: cleaningController.RegisterTool(td) ya llama a AudioManager.Instance.PlayPickupSFX();
                cleaningController.RegisterTool(td);
                carried = nearbyCarryable; // Asignar carried después del registro.
            }
            else
            {
                // Si no es una herramienta de limpieza
                carried = nearbyCarryable;

                // 🔥 DISPARAR SFX DE RECOGER (Solo si NO es una herramienta de limpieza, si lo es, lo hizo CleaningController)
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayPickupSFX();
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
    // FUNCIÓN EXISTENTE: Maneja solo la INTERACCIÓN DE PUERTAS
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

    // =========================================================================
    // TRIGGERS DE PROXIMIDAD
    // =========================================================================

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