// PlayerInteraction.cs - FINAL

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
    private Collider[] playerColliders; // Necesario para el Carryable.PickUp

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
        if (Input.GetKeyDown(KeyCode.E))
            TryInteract();
    }

    void TryInteract()
    {
        // Lógica 1: Soltar objeto
        if (carried)
        {
            bool isToolInHand = false;

            if (cleaningController != null && cleaningController.CurrentTool != null)
            {
                // Es una Tool que está asignada en el CleaningController
                ToolDescriptor toolInHand = carried.GetComponent<ToolDescriptor>() ?? carried.GetComponentInParent<ToolDescriptor>();

                if (toolInHand != null && toolInHand == cleaningController.CurrentTool)
                {
                    // **DELEGAR SOLTAR**
                    cleaningController.DropCurrentTool();
                    isToolInHand = true;
                    Debug.Log("Herramienta de limpieza soltada por CleaningController.");
                }
            }

            if (!isToolInHand)
            {
                // Es un Carryable normal.
                carried.Drop(); // Llama al Carryable.Drop() para restaurar físicas y colisiones
                animCtrl?.SetHolding(false);
                Debug.Log("Objeto normal soltado.");
            }

            carried = null; // Reseteamos la referencia local
            animCtrl?.TriggerInteract();
            return;
        }

        // Lógica 2: Recoger Carryable o Tool
        if (nearbyCarryable != null)
        {
            if (!holdPoint)
            {
                var hp = new GameObject("HoldPoint").transform;
                hp.SetParent(transform);
                hp.localPosition = new Vector3(0, 1.2f, 0.6f);
                holdPoint = hp;
            }

            // Mover el objeto a la mano (esto es manejado por Carryable.cs)
            nearbyCarryable.PickUp(holdPoint, playerColliders);

            // Verificamos si es una herramienta de limpieza
            ToolDescriptor td = nearbyCarryable.GetComponent<ToolDescriptor>() ?? nearbyCarryable.GetComponentInParent<ToolDescriptor>();

            if (td != null && cleaningController != null)
            {
                // **DELEGAR ASIGNACIÓN**
                cleaningController.RegisterTool(td);
            }

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