using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private string mouseXInputName = "Mouse X";
    [SerializeField] private string mouseYInputName = "Mouse Y";
    [SerializeField] private float mouseSensitivity = 150f;

    [SerializeField] private Transform playerBody; // Asignar en Inspector
    private float xAxisClamp = 0f;
    private bool cursorIsLocked = true;

    private void Awake()
    {
        ApplyCursorState(); // Arranca bloqueado
    }

    private void Update()
    {
        HandleCursorToggle();  // ⬅️ ahora se chequea todas las frames
        CameraRotation();
    }

    private void HandleCursorToggle()
    {
        // Esc para liberar, clic izq para volver a bloquear
        if (Input.GetKeyDown(KeyCode.Escape))
            cursorIsLocked = false;
        else if (Input.GetMouseButtonDown(0))
            cursorIsLocked = true;

        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        Cursor.lockState = cursorIsLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !cursorIsLocked;
    }

    private void CameraRotation()
    {
        float mouseX = Input.GetAxis(mouseXInputName) * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis(mouseYInputName) * mouseSensitivity * Time.deltaTime;

        // Si te queda invertido el Y, cambiá "+=" por "-="
        xAxisClamp += mouseY;

        if (xAxisClamp > 90f)
        {
            xAxisClamp = 90f;
            mouseY = 0f;
            ClampXAxisRotationToValue(270f);
        }
        else if (xAxisClamp < -90f)
        {
            xAxisClamp = -90f;
            mouseY = 0f;
            ClampXAxisRotationToValue(90f);
        }

        transform.Rotate(Vector3.left * mouseY);

        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
    }

    private void ClampXAxisRotationToValue(float value)
    {
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.x = value;
        transform.eulerAngles = eulerRotation;
    }
}
