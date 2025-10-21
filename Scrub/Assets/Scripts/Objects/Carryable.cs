// Carryable.cs - FINAL

using UnityEngine;

public class Carryable : MonoBehaviour
{
    // 📢 NUEVO: Propiedad para rastrear el estado de transporte.
    // Esto resuelve el error 'IsCarried' en el CleaningController y PlayerInteraction.
    public bool IsCarried { get; set; } = false;

    [Header("Configuración de Drop")]
    [Tooltip("Fuerza por defecto aplicada al soltar si no se especifica.")]
    public float defaultDropForce = 3f;

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

    /// <summary>
    /// Recoge el objeto, lo adjunta al padre y configura las físicas.
    /// </summary>
    public void PickUp(Transform parent, Collider[] playerColliders)
    {
        playerCollidersReference = playerColliders; // Guardamos la referencia para el Drop

        // 1. Configuración de físicas
        rb.useGravity = false;
        // La detección continua es mejor para objetos kinemáticos que se mueven rápido
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
                // Ignorar colisiones para que el objeto no "empuje" al jugador
                Physics.IgnoreCollision(carryableCollider, playerCol, true);
            }
        }

        // 4. Actualizar estado
        IsCarried = true;
    }

    /// <summary>
    /// Suelta el objeto, restaura físicas y aplica una fuerza.
    /// Es llamado por CleaningController (para soltar herramientas) o PlayerInteraction.
    /// </summary>
    /// <param name="direction">Dirección de la fuerza aplicada.</param>
    /// <param name="force">Magnitud de la fuerza (usualmente dropForce de CleaningController).</param>
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
        // Usamos ForceMode.VelocityChange para un impulso instantáneo y controlado.
        rb.AddForce(direction * force, ForceMode.VelocityChange);

        // 5. Actualizar estado
        IsCarried = false;
      
    }

    /// <summary>
    /// Método DROP ESTÁNDAR (Usado cuando el objeto se suelta sin una dirección/fuerza específica).
    /// </summary>
    public void Drop()
    {
        // 📢 MEJORA: Llama a la versión con parámetros, usando la fuerza por defecto y la dirección "hacia adelante" (simplemente Vector3.forward si se necesita, o cero si no se quiere fuerza).
        // Nota: Si este método es llamado por un objeto sin una dirección clara (como una memoria), es mejor usar fuerza cero.
        Drop(Vector3.zero, 0f);
    }
}