using UnityEngine;

public class InteractionTrigger : MonoBehaviour
{
    private IInteractable interactableObject;

    void Awake()
    {
        // Obtiene el script de la puerta (ej: DoorInteraction) que implementa IInteractable
        interactableObject = GetComponent<IInteractable>();

        if (interactableObject == null)
        {
            Debug.LogError($"¡ERROR! '{gameObject.name}' tiene un InteractionTrigger, pero NO implementa IInteractable.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // CRÍTICO: El Player debe tener el Tag "Player" en su GameObject raíz.
        if (other.CompareTag("Player"))
        {
            // Busca PlayerInteraction en el objeto raíz del Player (donde está el Tag).
            // Si el collider está en un hijo, GetComponentInParent sube para encontrarlo.
            PlayerInteraction playerInteraction = other.GetComponentInParent<PlayerInteraction>();

            if (playerInteraction != null && interactableObject != null)
            {
                // Informa al PlayerInteraction que la puerta está activa.
                playerInteraction.SetCurrentInteractable(interactableObject);
                Debug.Log("Trigger: Jugador entró al área de la puerta.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInteraction playerInteraction = other.GetComponentInParent<PlayerInteraction>();

            if (playerInteraction != null)
            {
                // Informa al PlayerInteraction que la puerta ya no está activa.
                playerInteraction.ClearCurrentInteractable();
                Debug.Log("Trigger: Interacción de puerta finalizada.");
            }
        }
    }
}