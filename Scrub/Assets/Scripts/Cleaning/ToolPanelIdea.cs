// ToolPanelIdea.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ToolPanelIdea : MonoBehaviour
{
    [Header("1. Referencias de Paneles")]
    [Tooltip("El GameObject del panel de Tools que NO pausa el juego (ENTER/TAB).")]
    public GameObject toolMenuPanel;

    [Tooltip("El GameObject del panel de PAUSA que SÍ pausa el juego (ESC).")]
    public GameObject pauseMenuPanel;

    [Header("Dependencias")]
    public MouseLookController mouseLookComponent;

    private bool isPaused = false; // Controla Time.timeScale
    private bool isToolMenuOpen = false; // Controla el panel de tools

    void Awake()
    {
        if (toolMenuPanel != null) toolMenuPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (mouseLookComponent == null)
        {
            Debug.LogError("ToolPanelIdea: El componente MouseLookController NO está asignado en el Inspector.", this);
        }
    }

    // =========================================================================
    // 1. FUNCIÓN PAUSA PRINCIPAL (Llamada por ESC)
    // =========================================================================

    public void TogglePause()
    {
        // Si el panel de tools está abierto, lo cerramos antes de pausar (o viceversa)
        if (isToolMenuOpen) ToggleToolsPanel();

        isPaused = !isPaused;

        if (isPaused)
        {
            // --- ABRIR MENÚ PAUSA Y CONGELAR TIEMPO ---
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);

            Time.timeScale = 0f;
            HandleCursorAndCamera(true);
        }
        else
        {
            // --- CERRAR MENÚ PAUSA Y REANUDAR TIEMPO ---
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

            Time.timeScale = 1f;
            HandleCursorAndCamera(false);
        }
    }

    // =========================================================================
    // 2. FUNCIÓN TOOLS PANEL (Llamada por ENTER/TAB)
    // =========================================================================

    public void ToggleToolsPanel()
    {
        // Si el juego está en pausa, no se puede abrir el panel de tools.
        if (Time.timeScale == 0f) return;

        isToolMenuOpen = !isToolMenuOpen;

        if (toolMenuPanel != null)
        {
            toolMenuPanel.SetActive(isToolMenuOpen);
        }

        // Bloqueo de cámara y cursor, pero Time.timeScale sigue siendo 1.
        HandleCursorAndCamera(isToolMenuOpen);
    }

    // =========================================================================
    // LÓGICA DE GESTIÓN DE ESTADO (Función Unificada)
    // =========================================================================

    private void HandleCursorAndCamera(bool activateMenu)
    {
        // 1. BLOQUEO DIRECTO DE CÁMARA
        if (mouseLookComponent != null)
        {
            // Desactiva la rotación si el menú está activo (activateMenu=true)
            mouseLookComponent.enabled = !activateMenu;
        }

        // 2. LIBERAR/BLOQUEAR CURSOR
        if (activateMenu)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ResumeGameButton()
    {
        // Asumimos que este botón está en el panel de PAUSA
        if (isPaused)
        {
            TogglePause();
        }
    }
}