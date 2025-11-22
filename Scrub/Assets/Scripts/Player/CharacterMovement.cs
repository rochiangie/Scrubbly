using UnityEngine;
using UnityEngine.InputSystem; // ¡Necesitas esta librería!

public class CharacterMovement : MonoBehaviour
{
    // --- Configuración Pública ---
    public float moveSpeed = 5.0f;
    public float lookSensitivity = 0.1f; // Sensibilidad del Ratón (Pointer/Delta)
    public float gamepadLookRate = 100.0f; // Sensibilidad del Gamepad (Stick)

    // --- Componentes ---
    private CharacterController _controller;
    private Transform _cameraTransform;

    // --- Estado de Entrada ---
    // Usaremos los Vector2 provistos por el Input System para la acción "Move" y "Look"
    private Vector2 _moveInput;
    private Vector2 _lookInput;

    // --- Estado de Rotación Interna ---
    private float _cameraPitch = 0.0f;

    // Se llama una vez al inicio para configurar
    void Start()
    {
        // 1. Obtener Componentes
        _controller = GetComponent<CharacterController>();
        // Asegúrate de que la cámara principal tiene el tag "MainCamera"
        _cameraTransform = Camera.main.transform;

        // 2. Configuración de la Vista
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Se llama en cada frame para aplicar movimiento y vista
    void Update()
    {
        // 1. Aplicar Movimiento
        ApplyMovement();

        // 2. Aplicar Vista
        ApplyLookRotation();
    }

    // --- Funciones de Callback del Input System ---

    // Esta función se llama cuando la acción "Move" (WASD o Left Stick) se activa
    public void OnMove(InputAction.CallbackContext context)
    {
        // Lee el valor del control (Vector2) que está activo
        _moveInput = context.ReadValue<Vector2>();

        // El .normalized del script anterior ya no es necesario aquí, 
        // ya que el valor de la entrada ya está normalizado a 1.
    }

    // Esta función se llama cuando la acción "Look" (Right Stick o Delta/Pointer) se activa
    public void OnLook(InputAction.CallbackContext context)
    {
        _lookInput = context.ReadValue<Vector2>();

        // NOTA: No aplicamos ninguna multiplicación de sensibilidad aquí, 
        // ya que la lógica de aplicación en ApplyLookRotation es más segura.
    }

    // --- Lógica de Aplicación de Movimiento (IDÉNTICA) ---
    private void ApplyMovement()
    {
        // Calcular la dirección del movimiento en el espacio del mundo
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        // Mapear la entrada 2D a la dirección 3D
        Vector3 desiredMove = (forward * _moveInput.y + right * _moveInput.x);

        // Mover el controlador de personaje
        _controller.Move(desiredMove * moveSpeed * Time.deltaTime);
    }

    // --- Lógica de Aplicación de Vista (MODIFICADA para doble sensibilidad) ---
    private void ApplyLookRotation()
    {
        // La clave es saber si la entrada es un delta (Ratón/Pointer) o un Stick (Gamepad)

        Vector2 finalLookInput;

        // Comprobamos si la entrada es del tipo "Delta" (como el ratón).
        // Si el valor del input es muy pequeño, asumimos que es un delta.
        // O mejor, podemos usar una propiedad del Input System si el MonoBehaviour está conectado
        // directamente al PlayerInput component.

        // Dado que el Input System ya resuelve la prioridad, solo necesitamos aplicar la sensibilidad
        // diferente al tipo de control. Si la magnitud es > 1.0 (típico del ratón), usamos una sensibilidad;
        // si la magnitud es <= 1.0 (típico de un stick), usamos la otra.

        // 1. Decidir la Sensibilidad
        if (_lookInput.magnitude > 1.5f) // El ratón/Pointer da un valor de delta grande.
        {
            // Usar sensibilidad de ratón/Pointer
            finalLookInput = _lookInput * lookSensitivity;
        }
        else
        {
            // Usar sensibilidad de Gamepad/Stick
            // Multiplicamos por Time.deltaTime para hacer la rotación suave y frame-rate independiente.
            finalLookInput = _lookInput * gamepadLookRate * Time.deltaTime;
        }

        // 2. Rotación del Cuerpo (Horizontal: afecta el movimiento)
        transform.Rotate(Vector3.up * finalLookInput.x);

        // 3. Rotación de la Cámara (Vertical: afecta solo la vista)
        _cameraPitch -= finalLookInput.y;

        // Clamping (limitar) el ángulo vertical
        _cameraPitch = Mathf.Clamp(_cameraPitch, -90f, 90f);

        _cameraTransform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
    }
}