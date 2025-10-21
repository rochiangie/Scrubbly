// TaskManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class TaskManager : MonoBehaviour
{
    // ====================================================================
    // 1. SINGLETON PATTERN
    // ====================================================================
    public static TaskManager Instance { get; private set; }

    public static bool IsDecisionActive { get; private set; } = false;

    // ====================================================================
    // 2. CONTROL DE TIEMPO
    // ====================================================================
    [Header("Control de Tiempo")]
    public float maxLevelTime = 300f; // 5 minutos
    [HideInInspector] public float currentTime;
    [HideInInspector] public bool timeIsUp = false;

    // ====================================================================
    // 3. ESTADÍSTICAS DE LIMPIEZA
    // ====================================================================
    [Header("Estadísticas de Limpieza")]
    public int totalDirtSpots;

    [HideInInspector] public int totalTrashItems;
    [HideInInspector] public int cleanedDirtSpots = 0;
    [HideInInspector] public int cleanedTrashItems = 0;

    // Lista de nombres pendientes (usada para la UI y el Check de Basura)
    public List<string> remainingItemNames { get; private set; } = new List<string>();

    // ====================================================================
    // 4. ESTADÍSTICAS SENTIMENTALES
    // ====================================================================
    [Header("Estadísticas Sentimentales")]
    public int emotionalBalanceScore = 0;
    public int accumulationScore = 0;

    public int minBalanceForGoodEnding { get; private set; }
    public int maxAccumulationForGoodEnding { get; private set; }

    [Header("4. Configuración de Umbrales")]
    public float balanceThresholdPercentage = 0.8f;
    public float accumulationThresholdPercentage = 0.5f;
    private int totalSentimentalValue = 0;
    public int totalPositiveMemoriesValue = 0;
    public int totalNegativeMemoriesValue = 0;

    // === 5. GESTIÓN DE HERRAMIENTAS Y ZONAS (Para ApplyCleanHit) ===
    [Header("5. Gestión de Herramientas")]
    public ToolDescriptor CurrentTool { get; private set; }
    public float damageMultiplier = 1f;
    public bool requireCorrectTool = true;
    public List<DirtSpot> nearbyDirt { get; private set; } = new List<DirtSpot>();

    // === 6. GESTIÓN DE OBJETOS FALTANTES ===
    [Header("6. Objetos Faltantes")]
    public int itemThresholdToActivateList = 10;

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
        UpdateProgressUI();
        InitializeSentimentalAnalysis();
        currentTime = maxLevelTime;

        // 🚨 LÓGICA DE CONTEO E INICIALIZACIÓN DE LISTA 🚨
        InitializeRemainingItemsList();

        GameObject[] trashObjects = GameObject.FindGameObjectsWithTag("Basura");
        totalTrashItems = trashObjects.Length;

    }

    void OnDestroy()
    {
        GameEvents.OnMemorieDecided -= HandleMemorieDecision;
        GameEvents.OnAllDone -= CheckFinalScore;
        Time.timeScale = 1f;
    }

    void Update()
    {
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

        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("DEBUG: Forzando la finalización de las tareas de limpieza.");
            ForceCompleteCleaningTasks();
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("DEBUG: Forzando el puntaje ideal de victoria.");
            ForceSetIdealScore();
        }
    }

    // =========================================================================
    // LÓGICA DE INICIALIZACIÓN Y PROGRESO
    // =========================================================================

    private void InitializeRemainingItemsList()
    {
        remainingItemNames.Clear();

        var dirtSpots = FindObjectsOfType<DirtSpot>(true);
        var trashObjects = GameObject.FindGameObjectsWithTag("Basura");

        // Usamos el nombre base (sin "(Clone)") para la lista, simplificando la remoción
        remainingItemNames.AddRange(dirtSpots.Select(d => d.name.Replace("(Clone)", "").Trim()));
        remainingItemNames.AddRange(trashObjects.Select(t => t.name.Replace("(Clone)", "").Trim()));

        // Actualizar TOTALES
        totalDirtSpots = dirtSpots.Length;

        if (remainingItemNames.Count <= itemThresholdToActivateList && remainingItemNames.Count > 0)
        {
            GameEvents.NotifyMissingItems(remainingItemNames);
        }

        CheckCompletion();
    }

    private void InitializeCleaningAnalysis()
    {
        cleanedDirtSpots = 0;
        cleanedTrashItems = 0;
        UpdateProgressUI();
    }

    private void InitializeSentimentalAnalysis()
    {
        MemorieObject[] memories = FindObjectsOfType<MemorieObject>();
        totalSentimentalValue = 0;
        totalPositiveMemoriesValue = 0;
        totalNegativeMemoriesValue = 0;

        foreach (var memory in memories)
        {
            // Asumo que 'sentimentalValue' es accesible o expuesto en MemorieObject
            // Usaremos 0 si es inaccesible para evitar un error de compilación
            // En tu código real, debes asegurarte de que esta variable sea pública.
            int value = 0;

            // Si la clase MemorieObject tiene la propiedad 'sentimentalValue', descomenta esto:
            // try { value = memory.sentimentalValue; } catch { Debug.LogWarning("MemorieObject.sentimentalValue no encontrado."); }

            totalSentimentalValue += Mathf.Abs(value);

            if (value >= 0) totalPositiveMemoriesValue += value;
            else totalNegativeMemoriesValue += Mathf.Abs(value);
        }

        minBalanceForGoodEnding = Mathf.CeilToInt(totalPositiveMemoriesValue * balanceThresholdPercentage);
        maxAccumulationForGoodEnding = Mathf.FloorToInt(totalSentimentalValue * accumulationThresholdPercentage);

        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);
    }

    /// <summary>
    /// Notifica a la UI el progreso actual de la limpieza. (Mueve el Slider).
    /// </summary>
    private void UpdateProgressUI()
    {
        int total = totalDirtSpots + totalTrashItems;
        int cleaned = cleanedDirtSpots + cleanedTrashItems;

        GameEvents.Progress(cleaned, total);
    }


    // =========================================================================
    // MANEJADORES DE NOTIFICACIÓN DE LIMPIEZA
    // =========================================================================

    /// <summary>
    /// Llamado por un objeto de Suciedad/Mancha limpiado (DirtSpot).
    /// </summary>
    public void NotifySpotCleaned()
    {
        cleanedDirtSpots++;
        UpdateProgressUI();
        CheckCompletion();
    }

    /// <summary>
    /// Llamado por el objeto de Basura que se recoge/destruye.
    /// </summary>
    public void NotifyTrashCleaned(string itemName)
    {
        // Estripamos el sufijo (Clone) del nombre para la búsqueda
        string itemBaseName = itemName.Replace("(Clone)", "").Trim();

        bool wasRemoved = remainingItemNames.Remove(itemBaseName);

        if (wasRemoved)
        {
            cleanedTrashItems++;
            UpdateProgressUI();

            Debug.Log($"[CONTADOR OK] Ítem '{itemBaseName}' eliminado de la lista. Total Basura Limpiada: {cleanedTrashItems}.");

            CheckCompletion();
        }
        else
        {
            Debug.LogError($"[ERROR CRÍTICO] FALLA AL CONTAR BASURA: No se encontró la base '{itemBaseName}' en la lista de pendientes.");

            string sample = string.Join(", ", remainingItemNames.Take(5));
            Debug.LogWarning($"Ejemplo de ítems restantes (a verificar): {sample}");
        }
    }

    // 🚨 CAMBIO DE PRIVATE A PUBLIC 🚨
    /// <summary>
    /// Devuelve el número total de ítems (Basura + Suciedad) que quedan por limpiar.
    /// </summary>
    public int GetRemainingCleanableItemsCount()
    {
        int total = totalDirtSpots + totalTrashItems;
        int cleaned = cleanedDirtSpots + cleanedTrashItems;

        return total - cleaned;
    }

    private void CheckCompletion()
    {
        int totalCleanableItems = totalDirtSpots + totalTrashItems;
        int cleanedItems = cleanedDirtSpots + cleanedTrashItems;
        int remainingCount = totalCleanableItems - cleanedItems;


        GameEvents.Progress(cleanedItems, totalCleanableItems);

        if (remainingCount <= itemThresholdToActivateList && remainingCount > 0)
        {
            GameEvents.NotifyMissingItems(remainingItemNames);
        }

        if (cleanedItems >= totalCleanableItems && totalCleanableItems > 0)
        {
            GameEvents.AllDone();
        }
    }

    // =========================================================================
    // LÓGICA DE FIN DE JUEGO Y DEBUG SHORTCUTS
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
            return;
        }

        float damage = damageMultiplier * CurrentTool.ToolPower;

        if (requireCorrectTool && !closestDirt.CanBeCleanedBy(CurrentTool.ToolId))
        {
            Debug.LogWarning($"[Clean Hit] Herramienta incorrecta para {closestDirt.name}.");
            return;
        }

        closestDirt.CleanHit(damage);
    }
}