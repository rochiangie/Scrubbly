using UnityEngine;
using System;

// Mantener la interfaz IInteractable aquí o en un archivo separado (IInteractable.cs)
public interface IInteractable { void Interact(); }

public class PlayerInteraction : MonoBehaviour
{
    // Las variables de Raycast se eliminan.

    [Header("Referencias")]
    public Transform holdPoint;
    public PlayerAnimationController animCtrl;

    private Carryable carried;
    private IInteractable currentInteractable = null;
    private Rigidbody playerRigidbody; // Lo necesitamos para la comprobación del Tag

    void Awake()
    {
        // ... (Inicialización)
        playerRigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Solo verificamos la tecla E para SOLTAR o para la PUERTA.
        if (Input.GetKeyDown(KeyCode.E))
            TryInteract();
    }

    void TryInteract()
    {
        // Lógica 1: Soltar objeto (PRIORIDAD MÁXIMA)
        if (carried)
        {
            carried.Drop();
            carried = null;
            animCtrl?.SetHolding(false);
            animCtrl?.TriggerInteract();
            return;
        }

        // Lógica 2: Interacción por TRIGGER (Puerta)
        if (currentInteractable != null)
        {
            currentInteractable.Interact();
            animCtrl?.TriggerInteract();
            return;
        }

        // Ya no hay lógica de Raycast aquí.
        Debug.Log("[Interacción Fallida] No hay Trigger de Puerta activo.");
    }

    // 🛑 LÓGICA DE RECOGIDA DE CUBOS POR CONTACTO
    private void OnTriggerEnter(Collider other)
    {
        // 1. Si ya estamos llevando algo, no hacemos nada.
        if (carried != null)
        {
            return;
        }

        // 2. Intentamos encontrar el script Carryable en el objeto que tocamos.
        Carryable c = other.GetComponent<Carryable>();
        if (c == null) c = other.GetComponentInParent<Carryable>();

        if (c != null)
        {
            // 3. ¡Recoger objeto!
            if (!holdPoint)
            {
                // Asegúrate de que HoldPoint existe (lógica para crearlo si es nulo)
                var hp = new GameObject("HoldPoint").transform;
                hp.SetParent(transform);
                hp.localPosition = new Vector3(0, 1.2f, 0.6f);
                holdPoint = hp;
            }
            c.PickUp(holdPoint);
            carried = c;
            animCtrl?.SetHolding(true);
            animCtrl?.TriggerInteract();
            Debug.Log("¡Objeto recogido por contacto!");
        }
    }

    // Funciones de la puerta (Llamadas por InteractionTrigger.cs)
    public void SetCurrentInteractable(IInteractable interactable)
    {
        currentInteractable = interactable;
    }

    public void ClearCurrentInteractable()
    {
        currentInteractable = null;
    }
}