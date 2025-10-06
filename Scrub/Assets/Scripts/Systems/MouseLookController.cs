using UnityEngine;

public class MouseLookController : MonoBehaviour
{
    [Header("Sensibilidad")]
    public float mouseSensitivity = 200f;

    [Header("Límites de Rotación Vertical")]
    public float upLimit = 85f;
    public float downLimit = -85f;

    [Header("Referencias")]
    [Tooltip("El objeto que recibirá la rotación vertical (se asigna automáticamente buscando el Head o por SetHeadTarget).")]
    public Transform headLookTarget;

    private float rotationX = 0f;
    private const string HeadObjectName = "Head";

    private bool hasLoggedError = false;

    void Start()
    {
        // 🛑 SOLO LO ESENCIAL EN START
        Cursor.lockState = CursorLockMode.Locked;
    }

    // 🛑 FUNCIÓN PÚBLICA REQUERIDA POR HeadLookRegistrar.cs
    // Permite que la "cabeza" se asigne a sí misma después de la instanciación.
    public void SetHeadTarget(Transform head)
    {
        if (head != null && headLookTarget == null)
        {
            headLookTarget = head;
            Debug.Log($"[MouseLook] ¡ASIGNACIÓN ÉXITO! Head Target asignado por SetHeadTarget a: {head.name}");

            // Inicialización de la rotación vertical
            rotationX = headLookTarget.localEulerAngles.x;
            if (rotationX > 180f) rotationX -= 360f;

            // Reinicia el flag de error
            hasLoggedError = false;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        // 1. Asignación Dinámica (Si la asignación por SetHeadTarget falla, intenta la búsqueda)
        if (headLookTarget == null)
        {
            TryAssignHeadTarget();
            if (headLookTarget == null) return;
        }

        // 2. Cálculo del Input y Rotación
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;


        // ROTACIÓN HORIZONTAL (Lados)
        transform.Rotate(Vector3.up * mouseX);


        // ROTACIÓN VERTICAL (Arriba/Abajo)
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, downLimit, upLimit);

        headLookTarget.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    // Función auxiliar de búsqueda como respaldo si SetHeadTarget falla
    private void TryAssignHeadTarget()
    {
        if (headLookTarget != null) return;

        // Búsqueda directa en hijos (rápida pero no profunda)
        Transform foundHead = transform.Find(HeadObjectName);

        if (foundHead != null)
        {
            SetHeadTarget(foundHead); // Usamos la propia función SetHeadTarget para inicializar
            return;
        }

        // Solo logeamos el error si no se ha logeado antes
        if (headLookTarget == null && hasLoggedError == false)
        {
            Debug.LogError($"[MouseLook] ¡Advertencia! No se encontró el objeto llamado '{HeadObjectName}'. El personaje aún no está listo.");
            hasLoggedError = true;
        }
    }
}