using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    // === Movimiento y Salto ===
    [Header("Movimiento (WASD)")]
    public float moveSpeed = 5f;
    // 🛑 Eliminamos rotationSpeed ya que no rotaremos con A/D

    [Header("Salto")]
    public float jumpForce = 6f;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Animación")]
    public Animator animator;

    // El hash es más eficiente para Unity
    private readonly int JumpTriggerHash = Animator.StringToHash("Jump");
    // 🛑 NUEVO: Hash para la velocidad del Animator
    private readonly int SpeedFloatHash = Animator.StringToHash("Speed");

    // === Ground Check ===
    [Header("Ground Check (opcional)")]
    public Transform groundCheck;
    public float groundRadius = 0.25f;
    public LayerMask groundMask = ~0;

    // === Variables privadas ===
    Rigidbody rb;
    bool isGrounded;
    bool jumpScheduled = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (!groundCheck)
        {
            var gc = new GameObject("GroundCheck").transform;
            gc.SetParent(transform);
            gc.localPosition = new Vector3(0f, -1.0f, 0f);
            groundCheck = gc;
        }
    }

    void Update()
    {
        // 1. Detección de suelo
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask);

        // 2. Manejo de Input de salto
        if (isGrounded && Input.GetKeyDown(jumpKey))
        {
            jumpScheduled = true;
        }
    }

    void FixedUpdate()
    {
        // === Lógica de Salto y Animación ===
        if (jumpScheduled)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            if (animator != null)
            {
                animator.SetTrigger(JumpTriggerHash);
            }

            jumpScheduled = false;
        }

        // === Lógica de Movimiento: 4 direcciones (WASD) ===

        // 🛑 1) Obtener inputs para adelante/atrás y strafe
        float forwardInput = Input.GetAxis("Vertical");
        float strafeInput = Input.GetAxis("Horizontal");

        // 🛑 2) Calcular la dirección deseada en el espacio del mundo
        Vector3 desiredForward = transform.forward * forwardInput;
        Vector3 desiredStrafe = transform.right * strafeInput;

        // Combinar y aplicar la velocidad máxima (normalizando si se mueve en diagonal)
        Vector3 targetHoriz = (desiredForward + desiredStrafe).normalized * moveSpeed;

        // Si no hay input, el vector deseado es cero.
        if (Mathf.Abs(forwardInput) < 0.001f && Mathf.Abs(strafeInput) < 0.001f)
        {
            targetHoriz = Vector3.zero;
        }

        // --- Lógica de Aceleración Suave (mantenida) ---

        Vector3 v = rb.linearVelocity;
        Vector3 vertical = Vector3.up * v.y; // Mantiene la gravedad

        // Usamos toda la velocidad horizontal actual para el Lerp
        Vector3 currentHoriz = new Vector3(v.x, 0f, v.z);

        float accel = 20f;
        // Interpolamos la velocidad horizontal actual hacia la velocidad horizontal deseada
        Vector3 newHoriz = Vector3.Lerp(currentHoriz, targetHoriz, accel * Time.fixedDeltaTime);

        // Aplicar la nueva velocidad horizontal + la velocidad vertical
        rb.linearVelocity = newHoriz + vertical;

        // 🛑 3) Actualizar Animación
        if (animator != null)
        {
            // Usamos la magnitud de la velocidad horizontal para el Animator.
            animator.SetFloat(SpeedFloatHash, newHoriz.magnitude);
        }
    }

    // Opcional: Para depuración, dibuja el radio de detección del suelo en el editor de Unity.
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }
}