using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
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

    // 📢 NUEVO: Punto de agarre personalizado (opcional)
    [Header("Configuración de Agarre")]
    [Tooltip("Asigna un objeto hijo vacío que represente dónde debe agarrarse este objeto.")]
    [SerializeField] private Transform customGripPoint;

    private Rigidbody rb;
    private Collider[] carryableColliders;
    private CollisionDetectionMode originalMode;
    // Guardaremos los colliders del jugador para deshacer la ignorancia
    private Collider[] playerCollidersReference;

    // 📢 NUEVO: Guardaremos la escala local original.
    private Vector3 originalLocalScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Check is technically redundant due to RequireComponent, but good for safety if component was removed in editor somehow
        if (rb == null)
        {
            Debug.LogWarning($"⚠️ Carryable en {gameObject.name} no tenía Rigidbody. Se añadió automáticamente.");
            rb = gameObject.AddComponent<Rigidbody>();
        }

        carryableColliders = GetComponentsInChildren<Collider>();
        originalMode = rb.collisionDetectionMode;

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

        // 📢 CORRECCIÓN DE ESCALA: Aplicamos la nueva escala al recoger.
        transform.localScale = originalLocalScale * scaleFactorOnPickup;

        // 3. Posicionamiento (Usando Custom Grip Point si existe)
        if (customGripPoint != null)
        {
            // Queremos que el customGripPoint coincida con el parent (HoldPoint) en posición y rotación.
            // Primero alineamos la rotación
            transform.rotation = parent.rotation * Quaternion.Inverse(customGripPoint.localRotation);
            
            // Luego alineamos la posición: movemos el objeto para que el grip point esté en el origen del padre
            // Calculamos la diferencia entre el objeto y su grip point en espacio mundial
            Vector3 gripOffset = customGripPoint.position - transform.position;
            transform.position = parent.position - gripOffset;
        }
        else
        {
            // Comportamiento por defecto: centrar en el pivote
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        // 4. Desactivar todos los Colliders (para evitar colisiones mientras se transporta)
        foreach (Collider col in carryableColliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }

        // 5. Actualizar estado
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
        // 1. Reactivar todos los Colliders
        foreach (Collider col in carryableColliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }
        playerCollidersReference = null;

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