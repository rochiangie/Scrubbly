using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DebugWinPanel : MonoBehaviour
{
    // 📢 NUEVA VARIABLE: La tecla que abrirá/cerrará el panel
    [Header("Control de Debug")]
    public KeyCode toggleKey = KeyCode.J;

    void Start()
    {
        // Asegúrate de que el panel esté oculto al inicio
        gameObject.SetActive(false);
    }

    void Update()
    {
        // 📢 NUEVA LÓGICA: Detectar la pulsación de la tecla J (o la que esté asignada)
        if (Input.GetKeyDown(toggleKey))
        {
            // Alterna el estado activo actual del GameObject al que está adjunto el script (el panel)
            gameObject.SetActive(!gameObject.activeSelf);
            Debug.Log($"[DEBUG] Panel de Trampas alternado con la tecla {toggleKey}. Estado: {gameObject.activeSelf}");
        }
    }

    // Método para simular la limpieza de TODAS las tareas.
    public void CompleteAllTasks()
    {
        // El código de simulación de tareas permanece igual
        TaskManager taskManager = FindObjectOfType<TaskManager>();
        if (taskManager == null)
        {
            Debug.LogError("No se encontró el TaskManager.");
            return;
        }

        Debug.Log("DEBUG: Forzando fin de fase de Limpieza (GameEvents.AllDone).");
        GameEvents.AllDone();

        // Opcional: Desactivar este panel de debug al ejecutar la acción
        gameObject.SetActive(false);
    }

    // Método para simular un Score de Victoria (Equilibrio)
    public void SetGoodScore()
    {
        TaskManager manager = TaskManager.Instance;
        if (manager == null)
        {
            Debug.LogError("No se encontró el SentimentalScoreManager.");
            return;
        }

        // Establecer un puntaje conocido que garantiza la victoria
        int goodBalance = manager.minBalanceForGoodEnding + 25; // 75/50
        int safeAccumulation = manager.maxAccumulationForGoodEnding - 50; // 100/150

        manager.emotionalBalanceScore = goodBalance;
        manager.accumulationScore = safeAccumulation;

        Debug.Log($"DEBUG: Puntuación fijada a VICTORY: Balance={goodBalance}, Acumulación={safeAccumulation}");
        GameEvents.SentimentalScore(goodBalance, safeAccumulation);
    }

    // Método para simular un Score de Derrota (Acumulador o Desequilibrio)
    public void SetBadScore()
    {
        TaskManager manager = TaskManager.Instance;
        if (manager == null)
        {
            Debug.LogError("No se encontró el SentimentalScoreManager.");
            return;
        }

        // Simular derrota por ACUMULADOR (Acumulación alta)
        int lowBalance = 60;
        int highAccumulation = manager.maxAccumulationForGoodEnding + 50; // 200/150

        manager.emotionalBalanceScore = lowBalance;
        manager.accumulationScore = highAccumulation;

        Debug.Log($"DEBUG: Puntuación fijada a DEFEAT (Acumulador): Balance={lowBalance}, Acumulación={highAccumulation}");
        GameEvents.SentimentalScore(lowBalance, highAccumulation);
    }
}