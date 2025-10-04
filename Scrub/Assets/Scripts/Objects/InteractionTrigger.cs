using UnityEngine;

public class InteractionTrigger : MonoBehaviour
{
    private IInteractable interactableObject;

    void Start()
    {
        // Obtiene el script que implementa la funci�n Interact() (ej: Door.cs)
        interactableObject = GetComponent<IInteractable>();

        if (interactableObject == null)
        {
            Debug.LogError("El GameObject " + gameObject.name + " tiene un InteractionTrigger, pero no implementa IInteractable (falta el script de Puerta, Bot�n, etc.).", this);
        }

        // �Importante! Aseg�rate de que este GameObject tiene un Collider marcado como Is Trigger.
    }

    void OnTriggerEnter(Collider other)
    {
        // Verificamos si el objeto que entr� en el Trigger es el Player
        if (other.CompareTag("Player"))
        {
            PlayerInteraction playerInteraction = other.GetComponent<PlayerInteraction>();

            if (playerInteraction != null && interactableObject != null)
            {
                // Le decimos al Player que la interacci�n con este objeto ahora es posible.
                playerInteraction.SetCurrentInteractable(interactableObject);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInteraction playerInteraction = other.GetComponent<PlayerInteraction>();

            if (playerInteraction != null)
            {
                // Le decimos al Player que la interacci�n con este objeto ya no es posible.
                playerInteraction.ClearCurrentInteractable();
            }
        }
    }
}