using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public enum MovementMode { Strafe, Tank } // Strafe= A/D lateral | Tank= A/D giran el cuerpo
    [Header("Movimiento")]
    public MovementMode movementMode = MovementMode.Strafe;
    public float moveSpeed = 5f;
    public float rotationSpeed = 180f; // usado en modo Tank
    public float jumpForce = 5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.25f;
    public LayerMask groundMask = ~0; // por defecto todo

    [Header("Refs")]
    public PlayerAnimationController animCtrl;

    private Rigidbody rb;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // evitamos vuelcos
        if (!groundCheck)
        {
            var gc = new GameObject("GroundCheck").transform;
            gc.SetParent(transform);
            gc.localPosition = new Vector3(0, -1.0f, 0);
            groundCheck = gc;
        }
        if (!animCtrl) animCtrl = GetComponentInChildren<PlayerAnimationController>();
    }

    void Update()
    {
        // salto
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            animCtrl?.TriggerJump();
        }
    }

    void FixedUpdate()
    {
        // grounded
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask, QueryTriggerInteraction.Ignore);
        animCtrl?.SetGrounded(isGrounded);

        // movimiento / rotaci�n
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (movementMode == MovementMode.Tank)
        {
            // Girar con A/D, avanzar con W/S
            if (Mathf.Abs(x) > 0.01f)
                transform.Rotate(Vector3.up, x * rotationSpeed * Time.fixedDeltaTime);

            Vector3 forward = transform.forward * z * moveSpeed;
            Vector3 vel = new Vector3(forward.x, rb.linearVelocity.y, forward.z);
            rb.linearVelocity = vel;
        }
        else
        {
            // Strafe cl�sico (A/D lateral, W/S adelante/atr�s)
            Vector3 move = (transform.right * x + transform.forward * z) * moveSpeed;
            Vector3 vel = new Vector3(move.x, rb.linearVelocity.y, move.z);
            rb.linearVelocity = vel;
        }
    }

    void OnCollisionEnter(Collision c)
    {
        if (((1 << c.gameObject.layer) & groundMask) != 0 || c.gameObject.CompareTag("Ground"))
        {
            bool wasGrounded = isGrounded;
            isGrounded = true;
            animCtrl?.SetGrounded(true);
            if (!wasGrounded) animCtrl?.TriggerLand();
        }
    }

    void OnCollisionExit(Collision c)
    {
        if (((1 << c.gameObject.layer) & groundMask) != 0 || c.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            animCtrl?.SetGrounded(false);
        }
    }
}
