using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("Mouse")]
    [SerializeField] private string mouseXInputName = "Mouse X";
    [SerializeField] private string mouseYInputName = "Mouse Y";
    [SerializeField] private float mouseSensitivity = 150f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private bool rotateBodyWithMouse = false; // ⬅️ por defecto desactivado

    [Header("Refs")]
    [SerializeField] private Transform playerBody; // arrastrá la Capsule
    private float xAxisClamp = 0f;
    private bool cursorIsLocked = true;

    void Awake()
    {
        ApplyCursorState();
        if (!playerBody) playerBody = transform.root; // fallback
    }

    void Update()
    {
        HandleCursorToggle();
        CameraRotation();
    }

    private void HandleCursorToggle()
    {
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
        float mouseY = Input.GetAxis(mouseYInputName) * mouseSensitivity * Time.deltaTime * (invertY ? 1f : -1f);

        xAxisClamp += mouseY;
        if (xAxisClamp > 90f) { xAxisClamp = 90f; mouseY = 0f; ClampXAxisRotationToValue(270f); }
        if (xAxisClamp < -90f) { xAxisClamp = -90f; mouseY = 0f; ClampXAxisRotationToValue(90f); }

        // Cámara arriba/abajo
        transform.Rotate(Vector3.left * mouseY);

        // Cuerpo izquierda/derecha (opcional)
        if (rotateBodyWithMouse && playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
    }

    private void ClampXAxisRotationToValue(float value)
    {
        Vector3 euler = transform.eulerAngles;
        euler.x = value;
        transform.eulerAngles = euler;
    }
}
