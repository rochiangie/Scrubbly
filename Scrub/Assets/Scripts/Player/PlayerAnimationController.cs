using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Rigidbody rb;         // del cuerpo (Capsule)
    [SerializeField] Transform playerBody; // para convertir vel. a local

    [Header("Tuning")]
    [SerializeField] float speedSmooth = 8f;

    Animator anim;
    float speedParam; // suavizado

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (!rb) rb = GetComponentInParent<Rigidbody>();
        if (!playerBody && rb) playerBody = rb.transform;
    }

    void Update()
    {
        UpdateLocomotionParams();
    }

    void UpdateLocomotionParams()
    {
        if (!rb) return;

        Vector3 v = rb.linearVelocity; v.y = 0f;
        float targetSpeed = v.magnitude;

        speedParam = Mathf.Lerp(speedParam, targetSpeed, Time.deltaTime * speedSmooth);
        anim.SetFloat("Speed", speedParam);

        if (playerBody)
        {
            Vector3 localV = playerBody.InverseTransformDirection(v);
            anim.SetFloat("MoveX", localV.x);
            anim.SetFloat("MoveZ", localV.z);
        }
    }

    public void SetGrounded(bool grounded) => anim.SetBool("IsGrounded", grounded);
    public void TriggerJump() => anim.SetTrigger("Jump");
    public void TriggerLand() => anim.SetTrigger("Land");
    public void SetCleaning(bool cleaning) => anim.SetBool("IsCleaning", cleaning);
    public void SetHolding(bool holding) => anim.SetBool("IsHolding", holding);
    public void TriggerInteract() => anim.SetTrigger("Interact");
}
