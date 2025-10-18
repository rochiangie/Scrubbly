using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;

public class TaskManager : MonoBehaviour
{
    // === SINGLETON Y ESTADO GLOBAL ===
    public static TaskManager Instance { get; private set; }
    public static bool IsDecisionActive { get; private set; } = false;

    // === 1. PROGRESO DE LIMPIEZA ===
    [Header("1. Progreso de Limpieza")]
    public int cleanedCount = 0;
    public int totalDirt = 0;

    // === 2. GESTIÓN DE HERRAMIENTAS Y LIMPIEZA ===
    [Header("2. Gestión de Herramientas y Daño")]
    public ToolDescriptor CurrentTool { get; private set; }
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private bool requireCorrectTool = true;
    public List<DirtSpot> nearbyDirt { get; private set; } = new List<DirtSpot>();

    // === 3. PUNTUACIÓN SENTIMENTAL ===
    [Header("3. Puntuación Sentimental")]
    public int emotionalBalanceScore = 0;
    public int accumulationScore = 0;

    [Header("3.1 Análisis de Valor de Memorias")]
    public int totalPositiveMemoriesValue = 0;
    public int totalNegativeMemoriesValue = 0;

    [Header("4. Configuración de Umbrales (Ganar/Perder)")]
    public float balanceThresholdPercentage = 0.8f;
    public float accumulationThresholdPercentage = 0.5f;

    public int minBalanceForGoodEnding { get; private set; }
    public int maxAccumulationForGoodEnding { get; private set; }

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

        GameEvents.OnMemorieDecided += HandleMemorieDecision;
        GameEvents.OnAllDone += CheckFinalScore;

        IsDecisionActive = false;
        CurrentTool = null;

        InitializeCleaningAnalysis();
    }

    void Start()
    {
        InitializeSentimentalAnalysis();
    }

    void OnDestroy()
    {
        GameEvents.OnMemorieDecided -= HandleMemorieDecision;
        GameEvents.OnAllDone -= CheckFinalScore;
    }

    void Update()
    {
        // 🛑 SHORTCUT 1: Completar Tareas de Limpieza (Tecla L)
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("DEBUG: Forzando la finalización de las tareas de limpieza.");
            ForceCompleteCleaningTasks();
        }

        // 🛑 SHORTCUT 2: Poner Puntaje Ideal de Victoria (Tecla I)
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("DEBUG: Forzando el puntaje ideal de victoria.");
            ForceSetIdealScore();
        }
    }

    // =========================================================================
    // LÓGICA DE DEBUG (SHORTCUTS)
    // =========================================================================

    /// <summary>
    /// Forzar la finalización de todas las tareas de limpieza.
    /// </summary>
    // TaskManager.cs (Fragmento de la función ForceCompleteCleaningTasks)

    private void ForceCompleteCleaningTasks()
    {
        // 1. Poner score ideal
        ForceSetIdealScore();

        // 2. Completar conteo de limpieza
        cleanedCount = totalDirt;
        GameEvents.Progress(cleanedCount, totalDirt);

        if (totalDirt > 0)
        {
            // 🛑 ESTO PREVIENE EL DOBLE DISPARO
            // Llama al chequeo final DE FORMA DIRECTA, sin usar GameEvents.AllDone()
            Debug.Log("DEBUG: Forzando FINAL DEL JUEGO DIRECTO para evitar doble disparo de evento.");
            CheckFinalScore();
        }
    }

    /// <summary>
    /// Forzar un Emotional Balance alto y una Accumulation baja.
    /// </summary>
    private void ForceSetIdealScore()
    {
        if (minBalanceForGoodEnding == 0 || maxAccumulationForGoodEnding == 0)
        {
            InitializeSentimentalAnalysis();
            if (minBalanceForGoodEnding == 0 || maxAccumulationForGoodEnding == 0)
            {
                Debug.LogError("No se pudo forzar el score: Umbrales no calculados correctamente.");
                return;
            }
        }

        // El balance debe ser cómodamente mayor que el mínimo
        emotionalBalanceScore = minBalanceForGoodEnding + 50;

        // La acumulación debe ser muy baja (ej. 10)
        accumulationScore = 10;

        // Notificar a la UI
        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);

        Debug.Log($"DEBUG: Puntuación fijada a VICTORY: Balance={emotionalBalanceScore} (Mín: {minBalanceForGoodEnding}) | Acumulación={accumulationScore} (Lím: {maxAccumulationForGoodEnding})");
    }

    // =========================================================================
    // LÓGICA DE INICIALIZACIÓN
    // =========================================================================

    private void InitializeCleaningAnalysis()
    {
        totalDirt = FindObjectsOfType<DirtSpot>().Length;
        cleanedCount = 0;
        GameEvents.Progress(cleanedCount, totalDirt);
    }

    private void InitializeSentimentalAnalysis()
    {
        MemorieObject[] memories = FindObjectsOfType<MemorieObject>();
        totalSentimentalValue = 0;
        totalPositiveMemoriesValue = 0;
        totalNegativeMemoriesValue = 0;

        foreach (var memory in memories)
        {
            int value = memory.sentimentalValue;
            totalSentimentalValue += Mathf.Abs(value);

            if (value >= 0) totalPositiveMemoriesValue += value;
            else totalNegativeMemoriesValue += Mathf.Abs(value);
        }

        minBalanceForGoodEnding = Mathf.CeilToInt(totalPositiveMemoriesValue * balanceThresholdPercentage);
        maxAccumulationForGoodEnding = Mathf.FloorToInt(totalSentimentalValue * accumulationThresholdPercentage);

        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);
    }

    // =========================================================================
    // LÓGICA DE JUEGO
    // =========================================================================

    public void RegisterTool(ToolDescriptor tool) { CurrentTool = tool; }
    public void DropTool(ToolDescriptor tool, Vector3 dropDirection, float dropForce)
    {
        if (tool == null) return;
        Carryable carryable = tool.GetComponent<Carryable>();
        if (carryable != null) { carryable.Drop(); }
        CurrentTool = null;
    }

    public void ApplyCleanHit(Vector3 playerPosition)
    {
        if (CurrentTool == null) return;
        nearbyDirt.RemoveAll(dirt => dirt == null);
        if (nearbyDirt.Count == 0) return;

        DirtSpot closestDirt = nearbyDirt
            .OrderBy(dirt => Vector3.Distance(playerPosition, dirt.transform.position))
            .FirstOrDefault();

        if (closestDirt == null) return;

        bool successfullyUsed = CurrentTool.TryUse();
        if (!successfullyUsed) { CurrentTool = null; return; }

        float damage = damageMultiplier * CurrentTool.toolPower;

        if (requireCorrectTool && !closestDirt.CanBeCleanedBy(CurrentTool.toolId)) { return; }

        closestDirt.CleanHit(damage);
    }

    public void NotifyDirtCleaned()
    {
        cleanedCount++;
        GameEvents.Progress(cleanedCount, totalDirt);

        if (cleanedCount >= totalDirt && totalDirt > 0)
        {
            GameEvents.AllDone();
        }
    }

    private void HandleMemorieDecision(bool isKept, int sentimentalValue)
    {
        int absoluteValue = Mathf.Abs(sentimentalValue);

        if (isKept)
        {
            accumulationScore += absoluteValue;
            if (sentimentalValue < 0) emotionalBalanceScore -= absoluteValue;
        }
        else
        {
            if (sentimentalValue > 0) emotionalBalanceScore -= sentimentalValue;
            else emotionalBalanceScore += absoluteValue;
        }

        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);
    }

    // =========================================================================
    // LÓGICA DE FINAL DEL JUEGO
    // =========================================================================

    public void CheckFinalScore()
    {
        bool won = false;
        string finalMessage = "";

        if (minBalanceForGoodEnding == 0 && maxAccumulationForGoodEnding == 0)
        {
            finalMessage = "Error: Umbrales no inicializados. La lógica del TaskManager falló al configurar los límites.";
            Debug.LogError(finalMessage);
            return;
        }

        if (accumulationScore > maxAccumulationForGoodEnding)
        {
            finalMessage = $"📦 ¡FIN! PERDISTE: Acumulador. Acumulación ({accumulationScore}) > Límite ({maxAccumulationForGoodEnding}).";
            won = false;
        }
        else if (emotionalBalanceScore >= minBalanceForGoodEnding)
        {
            finalMessage = $"🎉 ¡FIN! GANASTE: Balance Óptimo. Balance ({emotionalBalanceScore}) >= Mínimo ({minBalanceForGoodEnding}).";
            won = true;
        }
        else
        {
            finalMessage = $"😢 ¡FIN! PERDISTE: Desequilibrio. Balance ({emotionalBalanceScore}) < Mínimo ({minBalanceForGoodEnding}).";
            won = false;
        }

        Debug.Log(finalMessage);

        // Disparar el evento final.
        GameEvents.GameResult(won);
    }

    public static void SetDecisionActive(bool isActive)
    {
        IsDecisionActive = isActive;
    }
}