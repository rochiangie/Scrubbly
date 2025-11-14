// PlayerAnimationController.cs - CÓDIGO MODIFICADO
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Rigidbody rb;             // del cuerpo (Capsule)
    [SerializeField] Transform playerBody;     // para convertir vel. a local
    [SerializeField] HeldItemSlot heldItemSlot; // slot de herramienta en mano

    [Header("Locomotion")]
    [SerializeField] float speedSmooth = 8f;

    [Header("Upper Body (Cleaning Layer)")]
    // Nota: La capa de limpieza ahora solo se activa cuando se tiene la herramienta, no por input.
    [SerializeField] int cleaningLayerIndex = 1;    // índice de la capa con Avatar Mask (brazos)
    [SerializeField] float layerBlendSpeed = 10f;   // qué tan rápido sube/baja el peso
    [SerializeField] string[] validToolIds;         // opcional: restringir a ciertas herramientas

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
        // Lógica de Upper Body (limpieza) movida al final para control manual
        UpdateUpperBodyLayerWeight();

        // ¡IMPORTANTE! Si usas Input.GetKey en otra parte para limpiar, 
        // y quieres que esa tecla active la animación, debes llamarla aquí.
        // Si la activación va por un botón de UI o interacción (ej. TriggerInteract), 
        // no necesitas ninguna de las dos líneas de Input.

        // --- Ejemplo de cómo activar un trigger con una tecla (si aún lo necesitas) ---
        // if (Input.GetKeyDown(KeyCode.R) && HasValidToolInHand())
        // {
        //     TriggerInteract(); 
        // }
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

    /// <summary>
    /// CONTROL DE PESO DE CAPA SUPERIOR: Ahora solo se encarga de subir/bajar el peso 
    /// de la capa si tiene una herramienta VÁLIDA en mano, pero NO activa la animación de "Limpieza"
    /// (esa animación debe ser controlada por un Trigger o un SetBool temporal).
    /// </summary>
    void UpdateUpperBodyLayerWeight()
    {
        if (cleaningLayerIndex < 0 || cleaningLayerIndex >= anim.layerCount) return;

        bool hasTool = HasValidToolInHand();

        // 1. Control del parámetro de si está sosteniendo (no dependiente del input)
        anim.SetBool("IsHolding", hasTool);

        // 2. Control del peso de la capa: El peso sube solo si tiene la herramienta
        // (Esto asume que quieres que la capa de brazos esté activa para poder hacer la animación de limpieza,
        // incluso si el jugador está en el estado Idle/Walking).
        float current = anim.GetLayerWeight(cleaningLayerIndex);
        float target = hasTool ? 1f : 0f; // Sube peso si tiene herramienta, baja si no tiene.
        float next = Mathf.MoveTowards(current, target, Time.deltaTime * layerBlendSpeed);
        anim.SetLayerWeight(cleaningLayerIndex, next);

        // 3. ELIMINAMOS el control constante de "IsCleaning" por tecla, 
        // permitiendo que el Animator Controller maneje la animación de limpieza por Trigger o SetBool temporal.
        // Las líneas boolean cleaningInput y bool shouldUseCleaning han sido eliminadas.
    }

    bool HasValidToolInHand()
    {
        if (!heldItemSlot || !heldItemSlot.HasTool) return false;

        // Si no restringís tool IDs, cualquier herramienta sirve
        if (validToolIds == null || validToolIds.Length == 0) return true;

        // Verifica si el ID de la herramienta actual es válido
        // Asumiendo que heldItemSlot.CurrentTool tiene un campo .toolId
        string id = heldItemSlot.CurrentTool.toolId;
        for (int i = 0; i < validToolIds.Length; i++)
            if (validToolIds[i] == id) return true;

        return false;
    }

    // ---- API pública que ya tenías ----
    public void SetGrounded(bool grounded) => anim.SetBool("IsGrounded", grounded);
    public void TriggerJump() => anim.SetTrigger("Jump");
    public void TriggerLand() => anim.SetTrigger("Land");

    // Estas funciones ya estaban en el código original y son las que debes llamar
    // desde el script que maneja el click/interacción:
    public void SetCleaning(bool cleaning) => anim.SetBool("IsCleaning", cleaning);
    public void SetHolding(bool holding) => anim.SetBool("IsHolding", holding);
    public void TriggerInteract() => anim.SetTrigger("Interact");
}