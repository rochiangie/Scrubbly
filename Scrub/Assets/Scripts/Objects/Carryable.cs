// Carryable.cs - FINAL Y CORREGIDO CON AJUSTE DE ESCALA

using UnityEngine;

public class Carryable : MonoBehaviour
{
    // 📢 NUEVO: Propiedad para rastrear el estado de transporte.
    public bool IsCarried { get; set; } = false;

    [Header("Configuración de Drop")]
    [Tooltip("Fuerza por defecto aplicada al soltar si no se especifica.")]
    public float defaultDropForce = 3f;

    // 📢 NUEVO: Factor para reducir la escala del objeto al ser recogido (ej: 0.5 para la mitad del tamaño)
    [Header("Configuración de Escala")]
    [Tooltip("Factor de escala para aplicar al ser recogido. 1.0 = sin cambio.")]
    public float scaleFactorOnPickup = 0.5f; // Ajusta este valor en el Inspector (0.5 es un buen inicio)

    private Rigidbody rb;
    private Collider carryableCollider;
    private CollisionDetectionMode originalMode;
    // Guardaremos los colliders del jugador para deshacer la ignorancia
    private Collider[] playerCollidersReference;

    // 📢 NUEVO: Guardaremos la escala local original.
    private Vector3 originalLocalScale;

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

        // 📢 NUEVO: Guardamos la escala LOCAL inicial.
        originalLocalScale = transform.localScale;
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

        // 📢 CORRECCIÓN DE ESCALA: Aplicamos la nueva escala al recoger.
        transform.localScale = originalLocalScale * scaleFactorOnPickup;

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

        // 📢 CORRECCIÓN DE ESCALA: Restablecer la escala a la original.
        transform.localScale = originalLocalScale;

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
        // Llama a la versión con parámetros, usando la fuerza cero.
        Drop(Vector3.zero, 0f);
    }
}