// Carryable.cs - FINAL

using UnityEngine;

public class Carryable : MonoBehaviour
{
    private Rigidbody rb;
    private Collider carryableCollider;
    private CollisionDetectionMode originalMode;
    // Guardaremos los colliders del jugador para deshacer la ignorancia
    private Collider[] playerCollidersReference;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        carryableCollider = GetComponent<Collider>();
        if (rb != null)
        {
            originalMode = rb.collisionDetectionMode;
        }
        else
        {
            Debug.LogError($"Carryable en {gameObject.name} requiere un Rigidbody.");
        }
    }

    public void PickUp(Transform parent, Collider[] playerColliders)
    {
        playerCollidersReference = playerColliders; // Guardamos la referencia para el Drop

        // 1. Configuración de físicas
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.isKinematic = true;

        // 2. Jerarquía
        transform.SetParent(parent, true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // 3. Ignorar Colisiones entre Player y Carryable
        if (carryableCollider != null && playerCollidersReference != null)
        {
            foreach (var playerCol in playerCollidersReference)
            {
                Physics.IgnoreCollision(carryableCollider, playerCol, true);
            }
        }
    }

    // Método de Drop con parámetros (usado internamente o por otros sistemas)
    public void Drop(Vector3 direction, float force)
    {
        // 1. Deshacer Ignorar Colisiones
        if (carryableCollider != null && playerCollidersReference != null)
        {
            foreach (var playerCol in playerCollidersReference)
            {
                Physics.IgnoreCollision(carryableCollider, playerCol, false);
            }
            playerCollidersReference = null;
        }

        // 2. Restaurar físicas
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = originalMode;

        // 3. Quitar jerarquía
        transform.SetParent(null);

        // 4. Aplicar fuerza
        rb.AddForce(direction * force, ForceMode.VelocityChange);
    }

    // MÉTODO DROP ESTÁNDAR (Lo que PlayerInteraction llama cuando suelta un objeto normal)
    public void Drop()
    {
        // Llama a la versión completa con fuerza cero.
        Drop(Vector3.zero, 0f);
    }
}