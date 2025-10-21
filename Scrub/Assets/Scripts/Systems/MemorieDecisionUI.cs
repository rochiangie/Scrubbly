using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MemorieDecisionUI : MonoBehaviour
{
    // ====================================================================
    // 1. SINGLETON PATTERN
    // ====================================================================
    public static MemorieDecisionUI Instance { get; private set; }

    [Header("Referencias de UI")]
    public GameObject decisionPanel; // Asigna el GameObject del panel completo
    public TMP_Text itemNameText;
    public TMP_Text sentimentalValueText;

    // Opcional: Deshabilita estos botones en el Inspector o elimínalos si solo usas S/N
    // public Button keepButton;        
    // public Button discardButton;     

    // Almacena el método que se ejecutará una vez que el jugador tome una decisión.
    private Action<bool> onDecisionMade;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // El panel debe empezar oculto.
        decisionPanel.SetActive(false);

        // IMPORTANTE: Si mantienes los objetos Button en la escena, 
        // ¡asegúrate de que NO tengan listeners conectados para evitar duplicados!
    }

    // ====================================================================
    // 2. ESCUCHA DE TECLADO (Y/N)
    // ====================================================================
    private void Update()
    {
        // Solo verificamos la entrada si el panel está visible.
        if (decisionPanel.activeSelf)
        {
            // Tecla 'S' (Sí / Keep)
            if (Input.GetKeyDown(KeyCode.Y))
            {
                OnDecisionInput(true);
            }

            // Tecla 'N' (No / Discard)
            else if (Input.GetKeyDown(KeyCode.N))
            {
                OnDecisionInput(false);
            }
        }
    }

    // ====================================================================
    // 3. FUNCIÓN DE INICIO
    // ====================================================================

    /// <summary>
    /// Muestra el panel de decisión y configura el callback.
    /// </summary>
    /// <param name="itemName">El nombre del objeto con el que se está interactuando.</param>
    /// <param name="value">El valor sentimental del objeto.</param>
    /// <param name="callback">El método a ejecutar cuando se toma la decisión (DecideAndNotify).</param>
    public void ShowDecisionPanel(string itemName, int value, Action<bool> callback)
    {
        onDecisionMade = callback;

        itemNameText.text = $"Objeto: {itemName}";
        sentimentalValueText.text = $"Valor Sentimental: {value}";

        // Mostrar el panel, pausar, y notificar al manager del estado
        decisionPanel.SetActive(true);
        TaskManager.SetDecisionActive(true);
        Time.timeScale = 0f; // Pausa el juego

        // 🛑 Importante: Mantenemos el cursor bloqueado y oculto.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // NOTA: Aquí también debes deshabilitar el script de PlayerLook/Movimiento si no lo haces con Time.timeScale.
    }

    // ====================================================================
    // 4. FUNCIÓN DE DECISIÓN: Llamada por la entrada de teclado.
    // ====================================================================

    /// <summary>
    /// Se llama cuando el jugador presiona 'S' o 'N'.
    /// </summary>
    /// <param name="isKept">True si se guarda (S), False si se tira/descarta (N).</param>
    private void OnDecisionInput(bool isKept)
    {
        // Ejecutar el callback (DecideAndNotify en MemorieObject.cs)
        if (onDecisionMade != null)
        {
            onDecisionMade.Invoke(isKept);
        }

        // Ocultar la UI y reanudar el juego
        decisionPanel.SetActive(false);
        TaskManager.SetDecisionActive(false);
        Time.timeScale = 1f; // Reanuda el juego

        // Bloquear el cursor de nuevo (si la cámara no se movió, esto es redundante, pero seguro)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // NOTA: Aquí también debes habilitar el script de PlayerLook/Movimiento.

        // Limpiar el callback 
        onDecisionMade = null;
    }
}