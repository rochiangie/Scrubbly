// ToolPanelIdea.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ToolPanelIdea : MonoBehaviour
{
    [Header("1. Control de Pausa")]
    [Tooltip("El GameObject del panel de Tools que debe activarse.")]
    public GameObject toolMenuPanel;

    // 🚨 CAMBIO A REFERENCIA PÚBLICA MANUAL 🚨
    [Header("Dependencias")]
    [Tooltip("Arrastra el componente MouseLookController (el que mueve la cámara) aquí desde la Jerarquía.")]
    public MouseLookController mouseLookComponent;

    private bool isPaused = false;

    void Awake()
    {
        // ❌ ELIMINAMOS FindObjectOfType para forzar la asignación manual y evitar fallos de Awake. ❌

        if (toolMenuPanel != null)
        {
            toolMenuPanel.SetActive(false);
        }
    }

    void Start()
    {
        // Estado inicial: Cursor bloqueado y oculto (modo de juego)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;

        if (mouseLookComponent == null)
        {
            Debug.LogError("ToolPanelIdea: El componente MouseLookController NO está asignado en el Inspector. La cámara no se bloqueará.", this);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            TogglePause();
        }
    }

    // =========================================================================
    // LÓGICA DE APERTURA Y BLOQUEO
    // =========================================================================

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            // --- ABRIR MENÚ ---
            if (toolMenuPanel != null)
            {
                toolMenuPanel.SetActive(true);
            }

            // 1. BLOQUEO DIRECTO DE CÁMARA
            if (mouseLookComponent != null)
            {
                mouseLookComponent.enabled = false; // Desactiva el componente que maneja la rotación
            }

            // 2. LIBERAR CURSOR
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // --- CERRAR MENÚ ---
            if (toolMenuPanel != null)
            {
                toolMenuPanel.SetActive(false);
            }

            // 1. DESBLOQUEO DIRECTO DE CÁMARA
            if (mouseLookComponent != null)
            {
                mouseLookComponent.enabled = true; // Reactiva la rotación de la cámara
            }

            // 2. BLOQUEAR CURSOR
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}