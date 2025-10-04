using UnityEngine;

public class InteractionTrigger : MonoBehaviour
{
    private IInteractable interactableObject;

    void Start()
    {
        // Obtiene el script que implementa la función Interact() (ej: Door.cs)
        interactableObject = GetComponent<IInteractable>();

        if (interactableObject == null)
        {
            Debug.LogError("El GameObject " + gameObject.name + " tiene un InteractionTrigger, pero no implementa IInteractable (falta el script de Puerta, Botón, etc.).", this);
        }

        // ¡Importante! Asegúrate de que este GameObject tiene un Collider marcado como Is Trigger.
    }

    void OnTriggerEnter(Collider other)
    {
        // Verificamos si el objeto que entró en el Trigger es el Player
        if (other.CompareTag("Player"))
        {
            PlayerInteraction playerInteraction = other.GetComponent<PlayerInteraction>();

            if (playerInteraction != null && interactableObject != null)
            {
                // Le decimos al Player que la interacción con este objeto ahora es posible.
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
                // Le decimos al Player que la interacción con este objeto ya no es posible.
                playerInteraction.ClearCurrentInteractable();
            }
        }
    }
}