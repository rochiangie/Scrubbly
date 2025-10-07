using UnityEngine;

public class MouseLookController : MonoBehaviour
{
    [Header("Sensibilidad")]
    public float mouseSensitivity = 200f;

    [Header("Límites de Rotación Vertical")]
    public float upLimit = 85f;
    public float downLimit = -85f;

    [Header("Referencias")]
    [Tooltip("El objeto que recibirá la rotación vertical (Asignado por HeadLookRegistrar).")]
    public Transform headLookTarget;

    [Header("Control de Estado")]
    [Tooltip("Si es False, el mouse es liberado para interactuar con la UI.")]
    [SerializeField] private bool controlsActive = true; // Control de menú

    private float rotationX = 0f;
    private bool hasLoggedError = false; // Para evitar spam de errores

    // ================== Unity Lifecycle ==================

    void Start()
    {
        // El control inicial es determinado por 'controlsActive'
        SetControlsActive(controlsActive);
    }

    void Update()
    {
        // 🛑 CRÍTICO: Salir si el juego está pausado o si el control está inactivo (menú)
        if (Time.timeScale == 0f || !controlsActive) return;

        // 1. Asignación Dinámica (Intenta la búsqueda solo si el SetHeadTarget falló)
        if (headLookTarget == null)
        {
            TryAssignHeadTarget();
            if (headLookTarget == null) return;
        }

        // 2. Cálculo del Input y Rotación
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;


        // ROTACIÓN HORIZONTAL (Lados): Aplicada al Cuerpo (este transform)
        transform.Rotate(Vector3.up * mouseX);


        // ROTACIÓN VERTICAL (Arriba/Abajo): Aplicada a la Cabeza
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, downLimit, upLimit);

        headLookTarget.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    // ================== Funciones de Comunicación y Control ==================

    /// <summary>
    /// Función llamada por HeadLookRegistrar.cs para asignar la referencia de la cabeza
    /// después de la instanciación.
    /// </summary>
    public void SetHeadTarget(Transform head)
    {
        if (head != null && headLookTarget == null)
        {
            headLookTarget = head;
            Debug.Log($"[MouseLook] ¡ASIGNACIÓN ÉXITO! Head Target asignado por SetHeadTarget a: {head.name}");

            // Inicialización de la rotación vertical
            rotationX = headLookTarget.localEulerAngles.x;
            if (rotationX > 180f) rotationX -= 360f;

            hasLoggedError = false;
        }
    }

    /// <summary>
    /// Activa o desactiva el control de cámara/cabeza del jugador y ajusta el cursor.
    /// Útil para menús (como la escena Lore) o paneles de pausa.
    /// </summary>
    /// <param name="active">True para jugar (bloqueado), False para menú (libre).</param>
    public void SetControlsActive(bool active)
    {
        controlsActive = active;

        if (active)
        {
            // Reactivar el control y bloquear el cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (headLookTarget == null)
            {
                // Si se reactiva y aún no tiene la cabeza, intenta buscarla.
                TryAssignHeadTarget();
            }
        }
        else
        {
            // Desactivar el control y liberar el cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // ================== Fallback de Asignación ==================

    private const string HeadObjectName = "Head"; // El nombre que se busca

    // Función auxiliar de búsqueda como respaldo si SetHeadTarget falla
    private void TryAssignHeadTarget()
    {
        if (headLookTarget != null) return;

        // Búsqueda simple por nombre de hijo (solo si la asignación externa falla)
        Transform foundHead = transform.Find(HeadObjectName);

        if (foundHead != null)
        {
            SetHeadTarget(foundHead); // Usamos la propia función SetHeadTarget para inicializar
            return;
        }

        // Solo logeamos el error si no se ha logeado antes
        if (headLookTarget == null && hasLoggedError == false)
        {
            Debug.LogError($"[MouseLook] ¡Advertencia! No se encontró el objeto llamado '{HeadObjectName}'. Verifique que el HeadLookRegistrar está adjunto a la cabeza.");
            hasLoggedError = true;
        }
    }
}