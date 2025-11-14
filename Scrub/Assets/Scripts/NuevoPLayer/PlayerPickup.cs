// PlayerPickup.cs
using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    [Tooltip("Distancia máxima para detectar la basura.")]
    [SerializeField] private float alcanceInteraccion = 2f;

    [Tooltip("Tecla que usará el jugador para intentar recoger el objeto.")]
    [SerializeField] private KeyCode teclaRecoger = KeyCode.E;

    // --- Referencias ---

    // El Tag que identifica los objetos que pueden ser recogidos.
    private const string TrashTag = "Basura";

    void Update()
    {
        // Verifica si el jugador presiona la tecla de recogida.
        if (Input.GetKeyDown(teclaRecoger))
        {
            TryPickupObject();
        }
    }

    private void TryPickupObject()
    {
        RaycastHit hit;

        // 1. Lanza un rayo desde la posición del jugador hacia adelante.
        if (Physics.Raycast(transform.position, transform.forward, out hit, alcanceInteraccion))
        {
            // Opcional: Dibuja el rayo en la escena para debug (solo visible en el editor)
            Debug.DrawRay(transform.position, transform.forward * alcanceInteraccion, Color.yellow, 1f);

            // 2. Verifica si el objeto golpeado tiene el TAG "Basura".
            if (hit.collider.CompareTag(TrashTag))
            {
                // 3. Intenta obtener el script TrashObject.
                TrashObject trashObject = hit.collider.GetComponent<TrashObject>();

                if (trashObject != null)
                {
                    // Lógica principal de recogida:
                    // Puedes añadir puntos, actualizar UI, etc., antes de destruirlo.
                    HandlePickup(trashObject);
                }
                else
                {
                    // Esto es útil para debug si olvidaste añadir el script al objeto con el tag "Basura".
                    Debug.LogWarning($"El objeto '{hit.collider.name}' tiene el tag '{TrashTag}' pero le falta el script TrashObject.");
                }
            }
        }
    }

    private void HandlePickup(TrashObject trash)
    {
        // Llama al método principal de tu TrashObject para iniciar la limpieza,
        // que se encarga de la destrucción, los efectos y la notificación al TaskManager.
        trash.EliminateTrash();

        // Opcionalmente, podrías llamar a CleanTrash() directamente, 
        // pero EliminateTrash() actúa como un buen punto de entrada público.
        // trash.CleanTrash(); 
    }
}