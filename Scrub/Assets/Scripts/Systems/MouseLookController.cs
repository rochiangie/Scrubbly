using UnityEngine;

public class MouseLookController : MonoBehaviour
{
    // === Configuración ===
    [Header("Sensibilidad")]
    public float mouseSensitivity = 200f;

    [Header("Límites de Rotación Vertical")]
    public float upLimit = 85f;
    public float downLimit = -85f;

    [Header("Referencias")]
    [Tooltip("El objeto que recibirá la rotación vertical (Generalmente la cámara).")]
    public Transform headLookTarget;

    [Header("Control de Estado")]
    [Tooltip("La variable privada que almacena si los controles están activos.")]
    [SerializeField] private bool _controlsActive = true;

    // 📢 PROPIEDAD PÚBLICA: Permite que PlayerMovement y otros scripts lean el estado sin errores.
    public bool ControlsActive
    {
        get { return _controlsActive; }
    }

    // Quitamos la variable 'toggleControlKey' para evitar conflictos con PauseManager.

    // === Variables privadas ===
    private float rotationX = 0f;
    private bool hasLoggedError = false;

    // ================== Unity Lifecycle ==================

    void Start()
    {
        // El control inicial es determinado por '_controlsActive'
        SetControlsActive(_controlsActive);
    }

    void Update()
    {
        // 🛑 Lógica de Escape ELIMINADA: PauseManager se encarga de llamar a SetControlsActive.

        // CRÍTICO: Salir si el control está inactivo (menú)
        // Esto bloquea la rotación de la cámara cuando el menú de pausa está activo.
        if (!_controlsActive) return;

        // 1. Asignación Dinámica
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
    /// Activa o desactiva el control de cámara/cabeza del jugador y ajusta el cursor.
    /// Este método es llamado por el UIPauseController.
    /// </summary>
    public void SetControlsActive(bool active)
    {
        _controlsActive = active; // ⬅️ Actualiza la variable interna

        if (active)
        {
            // MODO JUEGO: Reactivar el control y bloquear el cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (headLookTarget == null)
            {
                TryAssignHeadTarget();
            }
        }
        else
        {
            // MODO PAUSA/MENÚ: Desactivar el control y liberar el cursor
            // 📢 Esto permite el clickeo en la UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Función llamada por HeadLookRegistrar.cs para asignar la referencia de la cabeza.
    /// </summary>
    public void SetHeadTarget(Transform head)
    {
        if (head != null && headLookTarget == null)
        {
            headLookTarget = head;
            Debug.Log($"[MouseLook] ¡ASIGNACIÓN ÉXITO! Head Target asignado por SetHeadTarget a: {head.name}");

            rotationX = headLookTarget.localEulerAngles.x;
            if (rotationX > 180f) rotationX -= 360f;

            hasLoggedError = false;
        }
    }

    // ================== Fallback de Asignación ==================

    private const string HeadObjectName = "Head";

    private void TryAssignHeadTarget()
    {
        if (headLookTarget != null) return;

        Transform foundHead = transform.Find(HeadObjectName);

        if (foundHead != null)
        {
            SetHeadTarget(foundHead);
            return;
        }

        if (headLookTarget == null && hasLoggedError == false)
        {
            Debug.LogError($"[MouseLook] ¡Advertencia! No se encontró el objeto llamado '{HeadObjectName}'. Verifique que el HeadLookRegistrar está adjunto a la cabeza.");
            hasLoggedError = true;
        }
    }
}