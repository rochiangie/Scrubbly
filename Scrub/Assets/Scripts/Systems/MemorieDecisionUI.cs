using UnityEngine;
using TMPro; // Necesario para TextMeshPro
using System;
using System.Collections; // Necesario para las corrutinas

public class MemorieDecisionUI : MonoBehaviour
{
    // Singleton para que MemorieObject pueda acceder a él fácilmente.
    public static MemorieDecisionUI Instance { get; private set; }

    [Header("Referencias de la UI")]
    public GameObject decisionPanel; // El objeto Panel principal a activar/desactivar

    [Tooltip("El componente TextMeshProUGUI que muestra la información del objeto.")]
    public TMP_Text infoText;       // Muestra el valor y la instrucción.

    private Action<bool> onDecisionTaken; // Callback para notificar al MemorieObject la decisión tomada
    private Coroutine inputCoroutine;     // Referencia para gestionar la escucha de teclado

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Asegurar que el panel esté inicialmente oculto.
        if (decisionPanel != null)
        {
            decisionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Muestra el panel, bloquea la interacción del jugador y comienza a escuchar las teclas 'Y' o 'N'.
    /// </summary>
    /// <param name="objectName">Nombre del objeto levantado.</param>
    /// <param name="sentimentalValue">Valor sentimental del objeto.</param>
    /// <param name="callback">El método DecideAndNotify del MemorieObject que se llamará al tomar la decisión.</param>
    public void ShowDecisionPanel(string objectName, int sentimentalValue, Action<bool> callback)
    {
        onDecisionTaken = callback;

        // 1. Mostrar texto informativo e instrucción de teclado
        infoText.text = $"Objeto: <color=#FFD700>{objectName}</color>\n" +
                        $"Valor Sentimental: {sentimentalValue}\n\n" +
                        "¿Deseas guardarlo o tirarlo?\n" +
                        "<color=green>[Y]</color> para GUARDAR (Acumular) | " +
                        "<color=red>[N]</color> para DESTRUIR (Desprenderse)";

        // 2. Mostrar el panel
        decisionPanel.SetActive(true);

        // 3. 🟢 ACTIVAR el estado de Decisión Global (Bloquea el movimiento/ataque)
        // Usamos el Manager para bloquear el juego sin Time.timeScale.
        if (SentimentalScoreManager.Instance != null)
        {
            SentimentalScoreManager.SetDecisionActive(true);
        }

        // 4. Comenzar a escuchar la entrada del teclado
        inputCoroutine = StartCoroutine(WaitForDecisionInput());
    }

    private IEnumerator WaitForDecisionInput()
    {
        // Continuar el ciclo de la corrutina mientras el panel esté visible
        while (decisionPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Y)) // Guardar (Acumular Nostalgia)
            {
                TakeDecision(true);
                yield break; // Salir de la corrutina
            }
            else if (Input.GetKeyDown(KeyCode.N)) // Destruir/Tirar (Desprendimiento)
            {
                TakeDecision(false);
                yield break; // Salir de la corrutina
            }
            yield return null; // Esperar al siguiente frame
        }
    }

    // Método llamado al tomar la decisión (por el teclado)
    private void TakeDecision(bool isKept)
    {
        // 1. Detener la escucha del teclado
        if (inputCoroutine != null)
        {
            StopCoroutine(inputCoroutine);
        }

        // 2. Ocultar el panel
        decisionPanel.SetActive(false);

        // 3. 🔴 DESACTIVAR el estado de Decisión Global (Reanuda el movimiento/ataque)
        if (SentimentalScoreManager.Instance != null)
        {
            SentimentalScoreManager.SetDecisionActive(false);
        }

        // 4. Ejecutar la acción de vuelta en el MemorieObject
        onDecisionTaken?.Invoke(isKept);

        onDecisionTaken = null; // Limpiar el callback
    }
}