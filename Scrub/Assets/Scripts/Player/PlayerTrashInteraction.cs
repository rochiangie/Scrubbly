using UnityEngine;

public class PlayerTrashInteraction : MonoBehaviour
{
    [Header("Configuración de Interacción de Basura")]
    [Tooltip("Radio de proximidad para detectar la basura con el Tag 'Basura' al presionar 'F'.")]
    public float detectionRadius = 2.5f; // Valor por defecto

    private const string TrashTag = "Basura";

    // Nota: El PlayerCamera e InteractableLayer no son necesarios para esta lógica.

    void Update()
    {
        // Comprueba si se presiona la tecla 'F' y si el panel de decisión no está activo
        if (Input.GetKeyDown(KeyCode.F) && !SentimentalScoreManager.IsDecisionActive)
        {
            TryEliminateTrash();
        }
    }

    private void TryEliminateTrash()
    {
        // 1. Detectar todos los colliders cercanos en un radio
        // Usamos la posición del Player (transform.position) como centro
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);

        TrashObject closestTrash = null;
        // Usamos la distancia cuadrada para optimizar las comparaciones
        float minDistanceSqr = detectionRadius * detectionRadius;

        // 2. Iterar, filtrar por Tag y encontrar el objeto más cercano
        foreach (var hitCollider in hitColliders)
        {
            // Filtrar por el Tag "Basura"
            if (hitCollider.CompareTag(TrashTag))
            {
                // Calcular la distancia cuadrada (más rápido que la raíz cuadrada)
                float distanceSqr = (hitCollider.transform.position - transform.position).sqrMagnitude;

                // Si es el objeto más cercano encontrado hasta ahora
                if (distanceSqr < minDistanceSqr)
                {
                    TrashObject currentTrash = hitCollider.GetComponent<TrashObject>();

                    if (currentTrash != null)
                    {
                        // Actualizar el objeto más cercano y la distancia mínima
                        closestTrash = currentTrash;
                        minDistanceSqr = distanceSqr;
                    }
                }
            }
        }

        // 3. Eliminar la basura más cercana si se encontró una
        if (closestTrash != null)
        {
            closestTrash.EliminateTrash();
        }
        else
        {
            Debug.Log("No se encontró basura con el Tag 'Basura' en el radio de detección.");
        }
    }

    // Opcional: Para depuración, dibuja el radio de detección en el editor de Unity.
    private void OnDrawGizmosSelected()
    {
        // Dibuja el radio solo si el script está en el objeto actual (el jugador)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}