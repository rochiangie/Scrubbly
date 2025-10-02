using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Rigidbody rb;         // del cuerpo (Capsule)
    [SerializeField] Transform playerBody; // para transformar vel. a espacio local (strafe 2D)

    [Header("Tuning")]
    [SerializeField] float speedSmooth = 8f;

    Animator anim;
    float speedParam; // suavizado

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (rb == null)
        {
            // intenta encontrar un Rigidbody en padres
            rb = GetComponentInParent<Rigidbody>();
        }
        if (playerBody == null && rb != null) playerBody = rb.transform;
    }

    void Update()
    {
        UpdateLocomotionParams();
    }

    void UpdateLocomotionParams()
    {
        if (rb == null) return;

        // velocidad en el plano XZ
        Vector3 v = rb.linearVelocity; v.y = 0f;
        float targetSpeed = v.magnitude;

        // suavizado para evitar �parpadeo� de anim
        speedParam = Mathf.Lerp(speedParam, targetSpeed, Time.deltaTime * speedSmooth);
        anim.SetFloat("Speed", speedParam);

        // Si quer�s strafe 2D:
        if (playerBody != null)
        {
            Vector3 localV = playerBody.InverseTransformDirection(v);
            anim.SetFloat("MoveX", localV.x); // izquierda/derecha
            anim.SetFloat("MoveZ", localV.z); // adelante/atr�s
        }
    }

    // Estos los llam�s desde otros scripts:
    public void SetGrounded(bool grounded) => anim.SetBool("IsGrounded", grounded);
    public void TriggerJump() => anim.SetTrigger("Jump");
    public void TriggerLand() => anim.SetTrigger("Land");
    public void SetCleaning(bool cleaning) => anim.SetBool("IsCleaning", cleaning);
    public void SetHolding(bool holding) => anim.SetBool("IsHolding", holding);
    public void TriggerInteract() => anim.SetTrigger("Interact");
}
