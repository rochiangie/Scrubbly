// PlayerInteraction.cs

using UnityEngine;
using System;

// Interfaz para la puerta. Puedes dejarla aquí o en su propio archivo IInteractable.cs.
public interface IInteractable { void Interact(); }

public class PlayerInteraction : MonoBehaviour
{
    [Header("Referencias")]
    public Transform holdPoint;
    public PlayerAnimationController animCtrl;

    private Carryable carried;
    private IInteractable currentDoorInteractable = null;
    private Carryable nearbyCarryable = null;

    private Rigidbody playerRigidbody;
    private Collider[] playerColliders; // Array de Colliders del Player para Ignorar Colisión

    void Awake()
    {
        if (!animCtrl) animCtrl = GetComponentInChildren<PlayerAnimationController>() ?? GetComponent<PlayerAnimationController>();
        playerRigidbody = GetComponent<Rigidbody>();

        // Obtener TODOS los colliders del Player, incluyendo hijos.
        playerColliders = GetComponentsInChildren<Collider>();

        if (playerColliders.Length == 0)
        {
            Debug.LogError("PlayerInteraction: No se encontraron Colliders del Player. La prevención de empuje fallará.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            TryInteract();
    }

    void TryInteract()
    {
        // Lógica 1: Soltar objeto
        if (carried)
        {
            carried.Drop();
            carried = null;
            animCtrl?.SetHolding(false);
            animCtrl?.TriggerInteract();
            Debug.Log("Objeto soltado.");
            return;
        }

        // Lógica 2: Recoger Cubo (si está cerca)
        if (nearbyCarryable != null)
        {
            if (!holdPoint)
            {
                var hp = new GameObject("HoldPoint").transform;
                hp.SetParent(transform);
                hp.localPosition = new Vector3(0, 1.2f, 0.6f);
                holdPoint = hp;
            }

            // CRÍTICO: Recoger y pasar el ARRAY de Colliders del Player.
            nearbyCarryable.PickUp(holdPoint, playerColliders);

            carried = nearbyCarryable;
            nearbyCarryable = null;
            animCtrl?.SetHolding(true);
            animCtrl?.TriggerInteract();
            Debug.Log("¡Objeto recogido con la tecla E!");
            return;
        }

        // Lógica 3: Interacción por TRIGGER (Puerta)
        if (currentDoorInteractable != null)
        {
            currentDoorInteractable.Interact();
            animCtrl?.TriggerInteract();
            return;
        }

        Debug.Log("[Interacción Fallida] No hay objeto que soltar, recoger (E) ni Puerta activa.");
    }

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