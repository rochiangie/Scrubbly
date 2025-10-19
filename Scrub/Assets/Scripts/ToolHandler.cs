// ToolHandler.cs (Limpio y Finalizado para la Pausa)
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Mantenemos por si es necesario en otras partes

public class ToolHandler : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("El GameObject del panel de selección de herramientas (Canvas/Panel contenedor).")]
    public GameObject selectionPanelUI; // Mantenemos esta variable si TogglePause la usa.

    [Header("Interoperabilidad UI")]
    public MonoBehaviour slidersManager;
    public MonoBehaviour mouseLook;

    // ❌ ELIMINAMOS ESTAS VARIABLES OBSOLETAS ❌
    // private ToolSelectionPanel toolSelectionPanelScript;
    // private GameObject currentToolObject; 

    private bool isToolsPanelOpen = false; // Usaremos esta para el estado del panel

    void Awake()
    {
        // NO HACER GetComponent<ToolSelectionPanel>() aquí.

        if (selectionPanelUI != null)
        {
            selectionPanelUI.SetActive(false);
        }
    }

    void Start()
    {
        // NO HACER .Initialize() aquí.
    }

    // Nota: Si este script NO maneja las herramientas, la función Update() puede ir vacía.
    // Si este script es el ToolPanelIdea.cs (que contiene TogglePause), Update() detecta Enter.

    // -------------------------------------------------------------------------
    // ➡️ TogglePause() [Usamos la versión más reciente con Mouse Locker y sin TimeScale] 
    // -------------------------------------------------------------------------
    public void TogglePause()
    {
        // Si tienes TaskManager, lo pones aquí: if (TaskManager.IsDecisionActive) return; 

        if (selectionPanelUI != null)
        {
            isToolsPanelOpen = !isToolsPanelOpen;

            if (isToolsPanelOpen)
            {
                // ABRIR
                selectionPanelUI.SetActive(true);
                slidersManager?.gameObject.SetActive(false);

                if (mouseLook != null)
                    mouseLook.SendMessage("SetControlsActive", false, SendMessageOptions.DontRequireReceiver);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // CERRAR
                selectionPanelUI.SetActive(false);

                if (mouseLook != null)
                    mouseLook.SendMessage("SetControlsActive", true, SendMessageOptions.DontRequireReceiver);

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Debug.Log($"[TOOL HANDLER] Panel de Herramientas {(isToolsPanelOpen ? "ABIERTO" : "CERRADO")}.");
        }
    }
}