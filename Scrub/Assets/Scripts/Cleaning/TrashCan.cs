// TrashCan.cs

using UnityEngine;

public class TrashCan : MonoBehaviour
{
    private CleaningManager manager;

    void Start()
    {
        // 1. Busca el Manager de limpieza en la escena.
        manager = FindObjectOfType<CleaningManager>();

        if (manager == null)
        {
            Debug.LogError("El objeto 'Basurero' no puede encontrar el 'CleaningManager' en la escena. Asegúrate de que existe.");
        }

        // 2. Verifica que el objeto tenga un Collider con Is Trigger para funcionar.
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("El componente TrashCan debe estar en un objeto con un Collider marcado como 'Is Trigger' para detectar entradas.");
        }
    }

    /// <summary>
    /// Se llama cuando otro Collider entra en el trigger del basurero.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // 1. Verifica si el objeto tiene el Tag de basura transportable ("Trash").
        if (other.CompareTag("Trash"))
        {
            // 2. Busca el componente Carryable en el objeto que colisionó.
            Carryable carryable = other.GetComponent<Carryable>() ?? other.GetComponentInParent<Carryable>();

            if (carryable != null && manager != null)
            {
                // 3. Lógica CLAVE: Solo contamos la basura si *ya no está siendo transportada*.
                if (!carryable.IsCarried)
                {
                    Debug.Log($"🗑️ TrashCan detectó objeto suelto: {other.name}. Notificando al Manager.");
                    // 4. Notificamos al Manager (El Manager verifica si ya lo contó y luego lo destruye).
                    manager.TrashDeposited(other.gameObject);
                }
                else if (carryable.IsCarried)
                {
                    // Debug.Log para saber que el objeto no fue contado porque el jugador sigue sujetándolo.
                    Debug.Log($"🗑️ TrashCan detectó objeto, pero sigue siendo transportado ({other.name}). Debe ser soltado primero.");
                }
            }
        }
    }
}