using UnityEngine;

// Asegura que este GameObject siempre tenga un Rigidbody.
[RequireComponent(typeof(Rigidbody))]
public class Movimiento : MonoBehaviour
{
    // --- Variables de Configuración ---

    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad máxima de movimiento del personaje.")]
    [SerializeField] private float velocidadMovimiento = 5f;

    [Tooltip("Fuerza aplicada para el salto.")]
    [SerializeField] private float fuerzaSalto = 7f;

    [Header("Detección de Suelo")]
    [Tooltip("Distancia para el raycast de detección de suelo.")]
    [SerializeField] private float distanciaDeteccionSuelo = 0.2f;
    [Tooltip("Máscara de capa para considerar qué es suelo.")]
    [SerializeField] private LayerMask capaSuelo;

    // --- Referencias ---

    private Rigidbody rb;
    // La dirección que calculamos en Update para usar en FixedUpdate
    private Vector3 _direccionMovimiento = Vector3.zero;

    // --- Métodos de Ciclo de Vida ---

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // La rotación debe ser congelada ya que el MouseLookController maneja la rotación del cuerpo.
        rb.freezeRotation = true;
    }

    void Update()
    {
        // 1. Manejo de Input de Movimiento
        float inputX = Input.GetAxisRaw("Horizontal"); // A/D o Flechas Izq/Der
        float inputZ = Input.GetAxisRaw("Vertical");   // W/S o Flechas Arr/Aba

        // La dirección es LOCAL. Z es adelante/atrás, X es derecha/izquierda.
        // No la normalizamos aquí, ya que la velocidad final se normaliza en FixedUpdate.
        Vector3 direccionLocal = new Vector3(inputX, 0f, inputZ);

        // Guardamos la dirección para usarla en FixedUpdate
        AplicarMovimiento(direccionLocal);

        // 2. Manejo de Input de Salto
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            Saltar();
        }
    }

    void FixedUpdate()
    {
        // Aseguramos que el movimiento sea independiente de la frecuencia de fotogramas.
        // Obtenemos la dirección deseada basada en la rotación actual del cuerpo (Rigidbody).
        Vector3 movimientoDeseado = transform.TransformDirection(_direccionMovimiento);

        // Aplicamos la velocidad. Usamos .normalized si inputX e inputZ son mayores a 1.
        Vector3 velocidadTarget = movimientoDeseado.normalized * velocidadMovimiento;

        // Mantenemos la velocidad vertical (gravedad/salto) intacta.
        // NOTA: Usando .velocity si no estás seguro de la versión de Unity.
        // Si usas rb.linearVelocity, reemplaza rb.velocity por rb.linearVelocity.
        rb.linearVelocity = new Vector3(velocidadTarget.x, rb.linearVelocity.y, velocidadTarget.z);
    }

    // --- Lógica de Movimiento ---

    private void AplicarMovimiento(Vector3 direccion)
    {
        // Guardamos la dirección de entrada (local) para aplicarla en FixedUpdate.
        _direccionMovimiento = direccion;
    }

    private void Saltar()
    {
        // Aplica una fuerza instantánea hacia arriba.
        rb.AddForce(Vector3.up * fuerzaSalto, ForceMode.Impulse);
    }

    // --- Detección de Suelo (Método de Raycast más robusto) ---

    private bool IsGrounded()
    {
        // Lanza un rayo desde el fondo del personaje hacia abajo.
        // Esto es más robusto que solo OnCollisionEnter para el movimiento FPS.
        return Physics.Raycast(transform.position, Vector3.down, distanciaDeteccionSuelo, capaSuelo);
    }

    // Si prefieres usar la detección de colisión simple, puedes mantener la versión anterior:
    /*
    private void OnCollisionStay(Collision collision)
    {
        // Si tienes la detección de Raycast, esto ya no es estrictamente necesario,
        // pero puedes usarlo como respaldo.
    }
    */

    // Opcional: Para dibujar el raycast en el editor para debug.
    private void OnDrawGizmos()
    {
        Gizmos.color = IsGrounded() ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * distanciaDeteccionSuelo);
    }
}