using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;

public class TaskManager : MonoBehaviour
{
    // === SINGLETON Y ESTADO GLOBAL ===
    public static TaskManager Instance { get; private set; }
    public static bool IsDecisionActive { get; private set; } = false;

    // === 1. PROGRESO DE LIMPIEZA DUAL ===
    [Header("1. Progreso de Limpieza Dual")]
    public int totalDirtSpots = 0;
    public int cleanedDirtSpots = 0;
    public int totalTrashItems = 0;
    public int cleanedTrashItems = 0;

    // === 2. CONTROL DE TIEMPO ===
    [Header("2. Control de Tiempo")]
    [Tooltip("Duración máxima del nivel en segundos.")]
    public float maxLevelTime = 300f; // 5 minutos por defecto
    public float currentTime;
    public bool timeIsUp = false;

    // === 3. GESTIÓN DE PUNTUACIÓN Y UMBRALES ===
    [Header("3. Puntuación Sentimental")]
    public int emotionalBalanceScore = 0;
    public int accumulationScore = 0;

    [Header("3.1 Análisis de Valor de Memorias")]
    public int totalPositiveMemoriesValue = 0;
    public int totalNegativeMemoriesValue = 0;

    [Header("4. Configuración de Umbrales")]
    public float balanceThresholdPercentage = 0.8f;
    public float accumulationThresholdPercentage = 0.5f;

    public int minBalanceForGoodEnding { get; private set; }
    public int maxAccumulationForGoodEnding { get; private set; }
    private int totalSentimentalValue = 0;

    // === 5. GESTIÓN DE HERRAMIENTAS Y ZONAS (Para ApplyCleanHit) ===
    [Header("5. Gestión de Herramientas")]
    public ToolDescriptor CurrentTool { get; private set; }
    public float damageMultiplier = 1f;
    public bool requireCorrectTool = true;
    public List<DirtSpot> nearbyDirt { get; private set; } = new List<DirtSpot>();

    // =========================================================================
    // AWAKE, START & UPDATE
    // =========================================================================

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
        currentTime = maxLevelTime;
    }

    void OnDestroy()
    {
        GameEvents.OnMemorieDecided -= HandleMemorieDecision;
        GameEvents.OnAllDone -= CheckFinalScore;
        Time.timeScale = 1f;
    }

    void Update()
    {
        // === LÓGICA DE TIEMPO ===
        if (!timeIsUp && currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                timeIsUp = true;
                GameEvents.GameResult(false);
                Debug.Log("¡TIEMPO AGOTADO! Derrota instantánea.");
                return;
            }
        }

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
    // LÓGICA DE NOTIFICACIÓN DE LIMPIEZA DUAL
    // =========================================================================

    /// <summary> Se llama desde TrashObject.cs </summary>
    public void NotifyTrashCleaned()
    {
        cleanedTrashItems++;
        CheckCompletion();
    }

    /// <summary> Se llama desde DirtSpot.cs </summary>
    public void NotifySpotCleaned()
    {
        cleanedDirtSpots++;
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        int total = totalDirtSpots + totalTrashItems;
        int cleaned = cleanedDirtSpots + cleanedTrashItems;

        GameEvents.Progress(cleaned, total);

        if (cleaned >= total && total > 0)
        {
            GameEvents.AllDone();
        }
    }

    // =========================================================================
    // LÓGICA DE INICIALIZACIÓN
    // =========================================================================

    private void InitializeCleaningAnalysis()
    {
        // Contar ambos tipos de objetos para los totales
        totalDirtSpots = FindObjectsOfType<DirtSpot>().Length;
        totalTrashItems = FindObjectsOfType<TrashObject>().Length;

        cleanedDirtSpots = 0;
        cleanedTrashItems = 0;

        GameEvents.Progress(0, totalDirtSpots + totalTrashItems);
    }

    private void InitializeSentimentalAnalysis()
    {
        // Asumiendo que existe MemorieObject.cs y sentimentalValue
        MemorieObject[] memories = FindObjectsOfType<MemorieObject>();
        totalSentimentalValue = 0;
        totalPositiveMemoriesValue = 0;
        totalNegativeMemoriesValue = 0;

        foreach (var memory in memories)
        {
            // Asumiendo que 'sentimentalValue' es público o property en MemorieObject
            // (Esta es la variable que tu MemorieObject debe exponer)
            // NOTA: Es común tener que usar una propiedad de solo lectura aquí si 'sentimentalValue' es privado.
            int value = memory.sentimentalValue;
            totalSentimentalValue += Mathf.Abs(value);

            if (value >= 0) totalPositiveMemoriesValue += value;
            else totalNegativeMemoriesValue += Mathf.Abs(value);
        }

        // Cálculo de umbrales
        minBalanceForGoodEnding = Mathf.CeilToInt(totalPositiveMemoriesValue * balanceThresholdPercentage);
        maxAccumulationForGoodEnding = Mathf.FloorToInt(totalSentimentalValue * accumulationThresholdPercentage);

        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);
    }

    // =========================================================================
    // LÓGICA DE DEBUG (SHORTCUTS)
    // =========================================================================

    private void ForceCompleteCleaningTasks()
    {
        ForceSetIdealScore();

        cleanedDirtSpots = totalDirtSpots;
        cleanedTrashItems = totalTrashItems;

        int total = totalDirtSpots + totalTrashItems;
        GameEvents.Progress(total, total);

        if (total > 0)
        {
            Debug.Log("DEBUG: Forzando FINAL DEL JUEGO DIRECTO para evitar doble disparo de evento.");
            CheckFinalScore();
        }
    }

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

        emotionalBalanceScore = minBalanceForGoodEnding + 50;
        accumulationScore = 10;

        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);

        Debug.Log($"DEBUG: Puntuación fijada a VICTORY: Balance={emotionalBalanceScore} (Mín: {minBalanceForGoodEnding}) | Acumulación={accumulationScore} (Lím: {maxAccumulationForGoodEnding})");
    }

    // =========================================================================
    // 📢 LÓGICA DE LIMPIEZA Y DAÑO A DIRTSPOTS
    // =========================================================================

    /// <summary>
    /// Aplica daño a la suciedad más cercana. Llamado desde CleaningController.cs.
    /// </summary>
    public void ApplyCleanHit(Vector3 playerPosition)
    {
        if (CurrentTool == null) return;

        // Limpiar referencias nulas (objetos ya destruidos)
        nearbyDirt.RemoveAll(dirt => dirt == null);
        if (nearbyDirt.Count == 0) return;

        // Encontrar el DirtSpot más cercano
        DirtSpot closestDirt = nearbyDirt
            .OrderBy(dirt => Vector3.Distance(playerPosition, dirt.transform.position))
            .FirstOrDefault();

        if (closestDirt == null) return;

        // 1. Consumir durabilidad de la herramienta
        bool successfullyUsed = CurrentTool.TryUse();
        if (!successfullyUsed)
        {
            // Si la herramienta se rompe, el CleaningController debe borrar la referencia.
            return;
        }

        float damage = damageMultiplier * CurrentTool.ToolPower;

        // 2. Comprobar si la herramienta es correcta (si se requiere)
        if (requireCorrectTool && !closestDirt.CanBeCleanedBy(CurrentTool.ToolId))
        {
            Debug.LogWarning($"[Clean Hit] Herramienta incorrecta para {closestDirt.name}.");
            return;
        }

        // 3. Aplicar el daño
        closestDirt.CleanHit(damage);
    }

    // =========================================================================
    // OTROS MÉTODOS DE JUEGO (Necesarios para la compilación completa)
    // =========================================================================

    public void RegisterTool(ToolDescriptor tool) { CurrentTool = tool; }

    public void CheckFinalScore()
    {
        bool won = false;
        string finalMessage = "";

        if (minBalanceForGoodEnding == 0 && maxAccumulationForGoodEnding == 0)
        {
            finalMessage = "Error: Umbrales no inicializados.";
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

        GameEvents.GameResult(won);
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

    public static void SetDecisionActive(bool isActive)
    {
        IsDecisionActive = isActive;
    }
}