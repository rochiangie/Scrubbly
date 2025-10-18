using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;

// GESTOR CENTRAL DE TODO (SCORE, LIMPIEZA, HERRAMIENTAS, ESTADO GLOBAL)
public class TaskManager : MonoBehaviour
{
    // === SINGLETON Y ESTADO GLOBAL ===
    public static TaskManager Instance { get; private set; }

    // Bandera de estado de la UI de decisión
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

    // Lista de suciedad cercana. (Alimentada por PlayerInteraction.cs)
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
        // 1. Configuración Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 2. Suscribirse a los eventos clave
        GameEvents.OnMemorieDecided += HandleMemorieDecision;
        GameEvents.OnAllDone += CheckFinalScore;

        IsDecisionActive = false;
        CurrentTool = null;

        // 3. Inicialización de conteo de suciedad
        InitializeCleaningAnalysis();
    }

    void Start()
    {
        // 4. Inicialización del análisis sentimental
        InitializeSentimentalAnalysis();
    }

    void OnDestroy()
    {
        GameEvents.OnMemorieDecided -= HandleMemorieDecision;
        GameEvents.OnAllDone -= CheckFinalScore;
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

    // Scripts/Systems/TaskManager.cs (Fragmento)

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

            if (value >= 0)
            {
                totalPositiveMemoriesValue += value; // 🛑 Suma de todos los valores POSITIVOS
            }
            else
            {
                totalNegativeMemoriesValue += Mathf.Abs(value);
            }
        }

        // 🛑 CORRECCIÓN CLAVE: El Balance Mínimo ahora se basa en el TOTAL POSITIVO.
        // Esto lo hace mucho más alcanzable.
        minBalanceForGoodEnding = Mathf.CeilToInt(totalPositiveMemoriesValue * balanceThresholdPercentage);

        // El Límite de Acumulación se mantiene basado en el valor TOTAL absoluto
        maxAccumulationForGoodEnding = Mathf.FloorToInt(totalSentimentalValue * accumulationThresholdPercentage);

        Debug.Log($"[TaskManager] Umbrales Fijados. Mínimo Balance: {minBalanceForGoodEnding} | Máximo Acumulación: {maxAccumulationForGoodEnding}");

        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);
    }

    // =========================================================================
    // MÉTODOS DE HERRAMIENTAS Y LIMPIEZA
    // =========================================================================

    public void RegisterTool(ToolDescriptor tool)
    {
        CurrentTool = tool;
        Debug.Log($"[ToolManager] Herramienta {tool.name} registrada.");
    }

    public void DropTool(ToolDescriptor tool, Vector3 dropDirection, float dropForce)
    {
        if (tool == null) return;
        Carryable carryable = tool.GetComponent<Carryable>();
        if (carryable != null)
        {
            carryable.Drop();
        }
        CurrentTool = null;
        Debug.Log($"[ToolManager] Herramienta {tool.name} desequipada y soltada.");
    }

    /// <summary>
    /// 🛑 APLICACIÓN DE GOLPE: Aplica el golpe de limpieza al objeto DirtSpot más cercano.
    /// </summary>
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

        if (!successfullyUsed)
        {
            Debug.LogWarning($"[Clean HIT FAIL] Herramienta '{CurrentTool.toolId}' se gastó.");
            CurrentTool = null;
            return;
        }

        float damage = damageMultiplier * CurrentTool.toolPower;

        if (requireCorrectTool && !closestDirt.CanBeCleanedBy(CurrentTool.toolId))
        {
            Debug.LogWarning($"[Clean FAIL 1: Tool Mismatch] Herramienta incorrecta.");
            return;
        }

        // Aplicar daño real a la suciedad (la mancha de suciedad se destruirá/limpiará si la salud llega a 0)
        closestDirt.CleanHit(damage);

        Debug.Log($"[Clean HIT OK] Aplicando {damage:F2} de daño a {closestDirt.name}.");
    }

    // =========================================================================
    // LÓGICA DE CONTADORES Y EVENTOS
    // =========================================================================

    /// <summary>
    /// 🛑 HACE QUE LA BARRA SE MUEVA. Aumenta el conteo de suciedad limpia. Llamada por DirtSpot.cs.
    /// </summary>
    public void NotifyDirtCleaned()
    {
        cleanedCount++;
        GameEvents.Progress(cleanedCount, totalDirt);

        if (cleanedCount >= totalDirt && totalDirt > 0)
        {
            GameEvents.AllDone();
            Debug.Log("¡TAREAS DE LIMPIEZA COMPLETADAS! Disparando evento AllDone.");
        }
    }

    /// <summary>
    /// Actualiza la puntuación sentimental según la decisión y el valor del objeto.
    /// </summary>
    private void HandleMemorieDecision(bool isKept, int sentimentalValue)
    {
        int absoluteValue = Mathf.Abs(sentimentalValue);

        if (isKept) // El jugador elige GUARDAR la Memoria
        {
            accumulationScore += absoluteValue;
            if (sentimentalValue < 0) emotionalBalanceScore -= absoluteValue;
        }
        else // El jugador elige TIRAR/DESTRUIR la Memoria
        {
            if (sentimentalValue > 0) emotionalBalanceScore -= sentimentalValue;
            else emotionalBalanceScore += absoluteValue;
        }

        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);
        Debug.Log($"[SCORE] Decisión: {(isKept ? "Guardar" : "Tirar")} ({sentimentalValue}). Balance: {emotionalBalanceScore} | Acumulación: {accumulationScore}");
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
            finalMessage = "Error: Umbrales no inicializados. La lógica de análisis de memorias falló.";
            won = false;
        }
        else if (accumulationScore > maxAccumulationForGoodEnding)
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
        GameEvents.GameResult(won);
    }

    public static void SetDecisionActive(bool isActive)
    {
        IsDecisionActive = isActive;
    }
}