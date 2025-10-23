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

    // === Control de Estado ===
    [Header("Control de Estado")]
    [Tooltip("La variable privada que almacena si los controles de juego (movimiento) están activos.")]
    [SerializeField] private bool _controlsActive = true;

    [Tooltip("La variable que bloquea la cámara por UI (e.g., panel de Tools).")]
    [SerializeField] private bool _isCameraLocked = false; // 🚨 NUEVA BANDERA 🚨

    // 📢 PROPIEDAD PÚBLICA: Permite que PlayerMovement y otros scripts lean el estado sin errores.
    public bool ControlsActive
    {
        get { return _controlsActive; }
    }

    // 📢 NUEVA PROPIEDAD PÚBLICA para el estado de bloqueo de la cámara.
    public bool IsCameraLocked
    {
        get { return _isCameraLocked; }
    }

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
        // 🛑 LÓGICA DE BLOQUEO CRÍTICO 🛑
        // Salir si el control está inactivo (menú de pausa general)
        // O si la cámara está bloqueada (panel de Tools)
        if (!_controlsActive || _isCameraLocked) return; // 🚨 COMPROBACIÓN AÑADIDA 🚨

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
    /// Activa o desactiva el control de la cámara/cabeza (Usado por UIPauseController para PAUSA/DECISIÓN).
    /// También gestiona el cursor, asumiendo un estado de pausa total.
    /// </summary>
    public void SetControlsActive(bool active)
    {
        _controlsActive = active; // ⬅️ Actualiza la variable interna

        if (active)
        {
            // MODO JUEGO (General): Reactivar el control y bloquear el cursor
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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// 🚨 NUEVA FUNCIÓN 🚨
    /// Bloquea o desbloquea la rotación de la cámara, usado específicamente para Menús Flotantes (Tools)
    /// donde el juego sigue corriendo pero la cámara debe estar fija.
    /// </summary>
    public void SetLockState(bool isLocked)
    {
        _isCameraLocked = isLocked;

        // El MouseLookController también puede encargarse de la visibilidad del cursor para este caso
        if (isLocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Solo si el control de juego general está ACTIVO, volvemos a bloquear el cursor.
            if (_controlsActive)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
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