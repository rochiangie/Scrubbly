using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    // === Movimiento y Salto ===
    [Header("Movimiento (WASD)")]
    public float moveSpeed = 5f;
    // 📢 NUEVO: Velocidad al correr/sprintar
    public float runSpeed = 10f;

    [Header("Sprint")]
    // 📢 NUEVO: Tecla para correr
    public KeyCode runKey = KeyCode.LeftShift;

    [Header("Salto")]
    public float jumpForce = 6f;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Animación")]
    public Animator animator;

    private readonly int JumpTriggerHash = Animator.StringToHash("Jump");
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
    // 📢 NUEVO: Estado de sprint
    bool isRunning = false;

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

        // 📢 NUEVO: Manejo de Input para correr
        isRunning = Input.GetKey(runKey);
    }

    void FixedUpdate()
    {
        // === Lógica de Salto y Animación ===
        if (jumpScheduled)
        {
            // La línea 'rb.linearVelocity' es un error tipográfico en el original, debería ser 'rb.velocity'.
            // Usaremos rb.velocity en lugar de rb.linearVelocity.
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            if (animator != null)
            {
                animator.SetTrigger(JumpTriggerHash);
            }

            jumpScheduled = false;
        }

        // === Lógica de Movimiento: 4 direcciones (WASD) ===

        // 1) Obtener inputs para adelante/atrás y strafe
        float forwardInput = Input.GetAxis("Vertical");
        float strafeInput = Input.GetAxis("Horizontal");

        // 📢 NUEVO: Determinar la velocidad máxima actual (Correr o Caminar)
        float currentMaxSpeed = isRunning ? runSpeed : moveSpeed;
        // Si no hay input, no podemos correr, solo caminar.
        if (Mathf.Abs(forwardInput) < 0.001f && Mathf.Abs(strafeInput) < 0.001f)
        {
            currentMaxSpeed = 0f;
            isRunning = false; // Desactiva el estado de correr si no hay movimiento
        }


        // 2) Calcular la dirección deseada en el espacio del mundo
        Vector3 desiredForward = transform.forward * forwardInput;
        Vector3 desiredStrafe = transform.right * strafeInput;

        // Combinar y aplicar la velocidad máxima (normalizando si se mueve en diagonal)
        Vector3 targetHoriz = (desiredForward + desiredStrafe).normalized * currentMaxSpeed;

        // --- Lógica de Aceleración Suave ---

        Vector3 v = rb.linearVelocity; // Usar rb.velocity
        Vector3 vertical = Vector3.up * v.y;

        // Usamos toda la velocidad horizontal actual para el Lerp
        Vector3 currentHoriz = new Vector3(v.x, 0f, v.z);

        float accel = 20f;
        // Interpolamos la velocidad horizontal actual hacia la velocidad horizontal deseada
        Vector3 newHoriz = Vector3.Lerp(currentHoriz, targetHoriz, accel * Time.fixedDeltaTime);

        // Aplicar la nueva velocidad horizontal + la velocidad vertical
        rb.linearVelocity = newHoriz + vertical; // Usar rb.velocity

        // 3) Actualizar Animación
        if (animator != null)
        {
            // Usamos la magnitud de la nueva velocidad horizontal para el Animator.
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