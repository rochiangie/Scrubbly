using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    // === Movimiento y Salto ===
    [Header("Movimiento tipo tanque")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 180f;

    [Header("Salto")]
    public float jumpForce = 6f;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Animación")]
    public Animator animator;

    // El hash es más eficiente para Unity
    private readonly int JumpTriggerHash = Animator.StringToHash("Jump");

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

        // Inicializa el Animator si no está asignado en el Inspector
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Crea el objeto GroundCheck si es nulo
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
        // 1. Detección de suelo: La condición principal para poder saltar
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask);

        // 2. Manejo de Input de salto
        if (isGrounded && Input.GetKeyDown(jumpKey))
        {
            // Programamos el salto para el próximo FixedUpdate
            jumpScheduled = true;
        }
    }

    void FixedUpdate()
    {
        // === Lógica de Salto y Animación ===
        if (jumpScheduled)
        {
            // FÍSICAS: Anular velocidad vertical y aplicar fuerza de impulso
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            // ANIMACIÓN: Activa el Trigger "Jump"
            if (animator != null)
            {
                animator.SetTrigger(JumpTriggerHash);
            }

            jumpScheduled = false; // Resetear la bandera
        }

        // === Lógica de Movimiento existente ===

        // 1) Girar en el lugar con A/D (yaw)
        float turn = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(turn) > 0.001f)
        {
            var yaw = Quaternion.Euler(0f, turn * rotationSpeed * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(rb.rotation * yaw);
        }

        // 2) Avanzar SOLO con W
        bool forwardPressed = Input.GetKey(KeyCode.W);
        Vector3 targetHoriz = forwardPressed ? transform.forward * moveSpeed : Vector3.zero;

        // 3) Proyección y aceleración horizontal
        Vector3 v = rb.linearVelocity;
        Vector3 vertical = Vector3.up * v.y; // Mantiene la gravedad y la velocidad de salto
        Vector3 currentAlongForward = transform.forward * Vector3.Dot(new Vector3(v.x, 0f, v.z), transform.forward);

        float accel = 20f;
        Vector3 newHoriz = Vector3.Lerp(currentAlongForward, targetHoriz, accel * Time.fixedDeltaTime);

        rb.linearVelocity = newHoriz + vertical;
    }
}