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
    [Tooltip("El objeto que recibirá la rotación vertical (Asignado por HeadLookRegistrar).")]
    public Transform headLookTarget;

    [Header("Control de Estado")]
    [Tooltip("Si es False, el mouse es liberado para interactuar con la UI.")]
    [SerializeField] private bool controlsActive = true;

    // 📢 NUEVA VARIABLE: Tecla para alternar el control
    [Tooltip("Tecla para activar/desactivar el control del mouse (ej: Menú de Pausa).")]
    public KeyCode toggleControlKey = KeyCode.Escape;

    // === Variables privadas ===
    private float rotationX = 0f;
    private bool hasLoggedError = false;

    // ================== Unity Lifecycle ==================

    void Start()
    {
        // El control inicial es determinado por 'controlsActive'
        SetControlsActive(controlsActive);
    }

    void Update()
    {
        // 📢 NUEVA LÓGICA: DETECCIÓN DE TECLA ESCAPE (o la tecla asignada)
        if (Input.GetKeyDown(toggleControlKey))
        {
            // Alterna el estado de control (de activo a inactivo, o viceversa)
            SetControlsActive(!controlsActive);
        }

        // 🛑 CRÍTICO: Salir si el control está inactivo (menú)
        // Ya no necesitamos revisar Time.timeScale aquí, pues SetControlsActive maneja el flujo.
        if (!controlsActive) return;

        // 1. Asignación Dinámica (Intenta la búsqueda solo si el SetHeadTarget falló)
        if (headLookTarget == null)
        {
            TryAssignHeadTarget();
            if (headLookTarget == null) return;
        }

        // 2. Cálculo del Input y Rotación
        // Usamos Time.deltaTime para hacer la sensibilidad independiente del framerate
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
    /// Función llamada por HeadLookRegistrar.cs para asignar la referencia de la cabeza.
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
    /// Al llamar a esta función, la tecla ESC automáticamente libera o bloquea el mouse.
    /// </summary>
    public void SetControlsActive(bool active)
    {
        controlsActive = active;

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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Opcional: Aquí puedes disparar un evento si necesitas mostrar un menú de pausa.
            // GameEvents.ShowPauseMenu(); 
        }

        // El script de movimiento (PlayerMovement.cs) y otros deben revisar la variable 'controlsActive'
        // o usar un sistema de estados global si también quieres pausar el movimiento.
    }

    // ================== Fallback de Asignación ==================

    private const string HeadObjectName = "Head";

    // Función auxiliar de búsqueda como respaldo si SetHeadTarget falla
    private void TryAssignHeadTarget()
    {
        if (headLookTarget != null) return;

        // Búsqueda simple por nombre de hijo
        Transform foundHead = transform.Find(HeadObjectName);

        if (foundHead != null)
        {
            SetHeadTarget(foundHead);
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