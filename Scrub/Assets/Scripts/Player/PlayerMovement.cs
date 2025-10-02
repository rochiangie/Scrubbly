using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{

    [Header("Movimiento tipo tanque")]
    public float moveSpeed = 5f;          // velocidad al mantener W
    public float rotationSpeed = 180f;    // grados/seg con A/D

    [Header("Ground Check (opcional)")]
    public Transform groundCheck;
    public float groundRadius = 0.25f;
    public LayerMask groundMask = ~0;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (!groundCheck)
        {
            var gc = new GameObject("GroundCheck").transform;
            gc.SetParent(transform);
            gc.localPosition = new Vector3(0f, -1.0f, 0f);
            groundCheck = gc;
        }
    }

    void FixedUpdate()
    {
        // 1) Girar en el lugar con A/D (yaw)
        float turn = Input.GetAxisRaw("Horizontal"); // A=-1, D=1
        if (Mathf.Abs(turn) > 0.001f)
        {
            var yaw = Quaternion.Euler(0f, turn * rotationSpeed * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(rb.rotation * yaw);
        }

        // 2) Avanzar SOLO con W (S no hace nada)
        bool forwardPressed = Input.GetKey(KeyCode.W);
        Vector3 targetHoriz = forwardPressed ? transform.forward * moveSpeed : Vector3.zero;

        // 3) Forzar que la velocidad horizontal NO tenga componente lateral (sin strafe)
        //    Proyectamos la velocidad actual sobre el "forward" y la llevamos hacia el objetivo.
        Vector3 v = rb.linearVelocity;
        Vector3 vertical = Vector3.up * v.y;
        Vector3 currentAlongForward = transform.forward * Vector3.Dot(new Vector3(v.x, 0f, v.z), transform.forward);

        // acelera/desacelera suave hacia el objetivo (sin lateral)
        float accel = 20f; // subí/bajá para responsividad
        Vector3 newHoriz = Vector3.Lerp(currentAlongForward, targetHoriz, accel * Time.fixedDeltaTime);

        rb.linearVelocity = newHoriz + vertical;
    }
}
