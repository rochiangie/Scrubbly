// ToolHandler.cs
using UnityEngine;

public class ToolHandler : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("El GameObject del panel de selección de herramientas (Canvas/Panel contenedor).")]
    public GameObject selectionPanelUI;

    [Header("Interoperabilidad UI")]
    [Tooltip("El Manager que controla otros sliders o paneles que deben ocultarse.")]
    public MonoBehaviour slidersManager;
    [Tooltip("El script que controla el movimiento de la cámara/mouse.")]
    public MonoBehaviour mouseLook;

    private bool isToolsPanelOpen = false; // Estado actual del panel

    void Awake()
    {
        // Asegura que el panel esté oculto al iniciar el juego.
        if (selectionPanelUI != null)
        {
            selectionPanelUI.SetActive(false);
        }
        // ❌ NO SE INICIALIZA NINGÚN OTRO SCRIPT AQUÍ ❌
    }

    void Start()
    {
        // NO HACER NADA AQUÍ.
    }

    // -------------------------------------------------------------------------
    // ➡️ TogglePause() [Función principal llamada por PlayerInteraction al presionar Enter]
    // -------------------------------------------------------------------------
    public void TogglePause()
    {
        // Opcional: Si TaskManager es Singleton, puedes bloquear la pausa durante decisiones.
        // if (TaskManager.Instance != null && TaskManager.IsDecisionActive) return;

        if (selectionPanelUI != null)
        {
            isToolsPanelOpen = !isToolsPanelOpen;

            if (isToolsPanelOpen)
            {
                // ABRIR MENÚ
                selectionPanelUI.SetActive(true);
                slidersManager?.gameObject.SetActive(false);

                // Bloquea el movimiento de la cámara (mouseLook.SendMessage usa el método SetControlsActive)
                if (mouseLook != null)
                    mouseLook.SendMessage("SetControlsActive", false, SendMessageOptions.DontRequireReceiver);

                // Libera el cursor para que pueda hacer clic en los botones.
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // CERRAR MENÚ
                selectionPanelUI.SetActive(false);

                // Reactiva el movimiento de la cámara
                if (mouseLook != null)
                    mouseLook.SendMessage("SetControlsActive", true, SendMessageOptions.DontRequireReceiver);

                // Bloquea el cursor para el control de la cámara en el juego.
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Debug.Log($"[TOOL HANDLER] Panel de Herramientas {(isToolsPanelOpen ? "ABIERTO" : "CERRADO")}.");
        }
    }
}