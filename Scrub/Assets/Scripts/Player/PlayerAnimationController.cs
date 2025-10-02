using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Rigidbody rb;                 // del cuerpo (Capsule)
    [SerializeField] Transform playerBody;         // para convertir vel. a local
    [SerializeField] HeldItemSlot heldItemSlot;    // slot de herramienta en mano

    [Header("Locomotion")]
    [SerializeField] float speedSmooth = 8f;

    [Header("Upper Body (Cleaning Layer)")]
    [SerializeField] int cleaningLayerIndex = 1;   // índice de la capa con Avatar Mask (brazos)
    [SerializeField] KeyCode cleanKey = KeyCode.R; // tecla para limpiar
    [SerializeField] float layerBlendSpeed = 10f;  // qué tan rápido sube/baja el peso
    [SerializeField] string[] validToolIds;        // opcional: restringir a ciertas herramientas (e.g. "Sponge","Mop")

    Animator anim;
    float speedParam; // suavizado

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (!rb) rb = GetComponentInParent<Rigidbody>();
        if (!playerBody && rb) playerBody = rb.transform;
        if (!heldItemSlot) heldItemSlot = GetComponentInParent<HeldItemSlot>();
    }

    void Update()
    {
        UpdateLocomotionParams();
        UpdateUpperBodyLayer();
    }

    void UpdateLocomotionParams()
    {
        if (!rb) return;

        // Si usás Unity 6 con linearVelocity, dejá esta línea, si no, reemplazá por rb.velocity
        Vector3 v = rb.linearVelocity;
        v.y = 0f;

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

    void UpdateUpperBodyLayer()
    {
        if (cleaningLayerIndex < 0 || cleaningLayerIndex >= anim.layerCount) return;

        bool hasTool = HasValidToolInHand();
        bool cleaningInput = Input.GetKey(cleanKey);
        bool shouldUseCleaning = hasTool && cleaningInput;

        // booleans para tu Animator Controller (por si los usás en los states)
        anim.SetBool("IsHolding", hasTool);
        anim.SetBool("IsCleaning", shouldUseCleaning);

        // blend del peso de la capa de brazos
        float current = anim.GetLayerWeight(cleaningLayerIndex);
        float target = shouldUseCleaning ? 1f : 0f;
        float next = Mathf.MoveTowards(current, target, Time.deltaTime * layerBlendSpeed);
        anim.SetLayerWeight(cleaningLayerIndex, next);
    }

    bool HasValidToolInHand()
    {
        if (!heldItemSlot || !heldItemSlot.HasTool) return false;

        // Si no restringís tool IDs, cualquier herramienta sirve
        if (validToolIds == null || validToolIds.Length == 0) return true;

        string id = heldItemSlot.CurrentTool.toolId;
        for (int i = 0; i < validToolIds.Length; i++)
            if (validToolIds[i] == id) return true;

        return false;
    }

    // ---- API pública que ya tenías ----
    public void SetGrounded(bool grounded) => anim.SetBool("IsGrounded", grounded);
    public void TriggerJump() => anim.SetTrigger("Jump");
    public void TriggerLand() => anim.SetTrigger("Land");
    public void SetCleaning(bool cleaning) => anim.SetBool("IsCleaning", cleaning);
    public void SetHolding(bool holding) => anim.SetBool("IsHolding", holding);
    public void TriggerInteract() => anim.SetTrigger("Interact");
}
