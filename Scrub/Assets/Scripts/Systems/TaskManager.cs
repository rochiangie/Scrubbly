using UnityEngine;
using System.Linq;

public class TaskManager : MonoBehaviour
{
    // Singleton (simplificado para accesibilidad)
    public static TaskManager Instance { get; private set; }

    [Header("Progreso de Limpieza")]
    public int cleanedCount = 0;
    public int totalDirt = 0;

    [Header("Configuración de Umbrales")]
    [Tooltip("El mínimo de Balance Emocional necesario (como % del Valor Total de Memorias).")]
    public float balanceThresholdPercentage = 0.8f;
    [Tooltip("El máximo de Acumulación permitido (como % del Valor Total de Memorias).")]
    public float accumulationThresholdPercentage = 0.5f;

    // Variable interna para almacenar el valor total de la escena
    private int totalSentimentalValue = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // La inicialización de la limpieza se movió a Start()
    }

    void Start()
    {
        // 🛑 CLAVE: Mover la lógica pesada a Start() para asegurar que todos los objetos existen.
        InitializeCleaningAnalysis();
        InitializeSentimentalAnalysis();
    }

    private void InitializeCleaningAnalysis()
    {
        // 🛑 CORRECCIÓN: Usamos FindObjectsOfType<DirtSpot>() para el conteo total.
        totalDirt = FindObjectsOfType<DirtSpot>().Length;
        cleanedCount = 0;

        Debug.Log($"[TaskManager] Total de Manchas de Suciedad: {totalDirt}.");

        // Notificar a la UI inicial
        GameEvents.Progress(cleanedCount, totalDirt);
    }

    private void InitializeSentimentalAnalysis()
    {
        // Encontrar todos los scripts MemorieObject en la escena
        MemorieObject[] memories = FindObjectsOfType<MemorieObject>();

        totalSentimentalValue = 0;

        // Sumar los valores sentimentales de todos los objetos
        foreach (var memory in memories)
        {
            totalSentimentalValue += memory.sentimentalValue;
        }

        Debug.Log($"[TaskManager] Análisis Sentimental Completo: {memories.Length} Memorias. Valor Total: {totalSentimentalValue}.");

        // 🛑 CLAVE: Configuramos el SentimentalScoreManager con los umbrales calculados.
        if (SentimentalScoreManager.Instance != null)
        {
            SentimentalScoreManager.Instance.SetWinThresholds(
                totalSentimentalValue,
                balanceThresholdPercentage,
                accumulationThresholdPercentage
            );
        }
        else
        {
            Debug.LogError("SentimentalScoreManager.Instance es null. Asegúrate de que se inicialice antes de que el TaskManager llame a Start().");
        }
    }

    // Método que actualiza el progreso de limpieza (debe ser llamado por el objeto DirtSpot al limpiarse)
    public void UpdateCleaningProgress()
    {
        cleanedCount++;

        // Notificar a la UI (UIPauseController)
        GameEvents.Progress(cleanedCount, totalDirt);

        // 🛑 CLAVE: Comprobar la finalización de la limpieza y disparar el chequeo final.
        if (cleanedCount >= totalDirt && totalDirt > 0)
        {
            GameEvents.AllDone();
            Debug.Log("¡TAREAS DE LIMPIEZA COMPLETADAS! Disparando evento AllDone.");
        }
    }
}