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

    // Lista de suciedad cercana (se llena desde PlayerInteraction.cs)
    public List<DirtSpot> nearbyDirt { get; private set; } = new List<DirtSpot>();

    // === 3. PUNTUACIÓN SENTIMENTAL ===
    [Header("3. Puntuación Sentimental")]
    public int emotionalBalanceScore = 0;
    public int accumulationScore = 0;

    [Header("4. Configuración de Umbrales (Ganar/Perder)")]
    public float balanceThresholdPercentage = 0.8f;
    public float accumulationThresholdPercentage = 0.5f;

    // Umbrales calculados, para que la UI de pausa los lea
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

        // Inicialización de conteo de suciedad
        InitializeCleaningAnalysis();
    }

    void Start()
    {
        // Inicialización de score sentimental
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
        // 🛑 CONTEO CORRECTO
        totalDirt = FindObjectsOfType<DirtSpot>().Length;
        cleanedCount = 0;
        GameEvents.Progress(cleanedCount, totalDirt);
    }

    private void InitializeSentimentalAnalysis()
    {
        // ... (Tu lógica de análisis sentimental) ...
        MemorieObject[] memories = FindObjectsOfType<MemorieObject>();
        totalSentimentalValue = memories.Sum(m => m.sentimentalValue);
        minBalanceForGoodEnding = Mathf.CeilToInt(totalSentimentalValue * balanceThresholdPercentage);
        maxAccumulationForGoodEnding = Mathf.FloorToInt(totalSentimentalValue * accumulationThresholdPercentage);

        Debug.Log($"[TaskManager] Umbrales Fijados. Mínimo Balance: {minBalanceForGoodEnding} | Máximo Acumulación: {maxAccumulationForGoodEnding}");
        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);
    }

    // =========================================================================
    // 🛑 MÉTODOS DE HERRAMIENTAS Y LIMPIEZA 🛑
    // =========================================================================

    public void RegisterTool(ToolDescriptor tool) { CurrentTool = tool; }
    public void DropTool(ToolDescriptor tool, Vector3 dropDirection, float dropForce)
    {
        if (tool == null) return;
        Carryable carryable = tool.GetComponent<Carryable>();
        if (carryable != null) { carryable.Drop(); }
        CurrentTool = null;
    }

    /// <summary>
    /// APLICACIÓN DE GOLPE: Simula la acción del antiguo CleaningController.
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

        if (!successfullyUsed) { CurrentTool = null; return; }

        float damage = damageMultiplier * CurrentTool.toolPower;

        if (requireCorrectTool && !closestDirt.CanBeCleanedBy(CurrentTool.toolId))
        {
            Debug.LogWarning($"[Clean FAIL: Tool Mismatch] Herramienta incorrecta.");
            return;
        }

        // 🛑 LÍNEA CLAVE: Aplicar daño al objeto de suciedad.
        closestDirt.CleanHit(damage);
    }

    // =========================================================================
    // LÓGICA DE CONTADORES Y EVENTOS
    // =========================================================================

    /// <summary>
    /// 🛑 LÍNEA CLAVE: Hace que la barra se mueva. Llamada por DirtSpot.cs.
    /// </summary>
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
        if (isKept) accumulationScore += Mathf.Abs(sentimentalValue);
        else emotionalBalanceScore -= sentimentalValue;

        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);
    }

    // =========================================================================
    // LÓGICA DE FINAL DEL JUEGO
    // =========================================================================

    public void CheckFinalScore()
    {
        // ... (Tu lógica de victoria/derrota) ...
    }

    public static void SetDecisionActive(bool isActive)
    {
        IsDecisionActive = isActive;
    }
}