using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using System.Collections.Generic;

public class TaskManager : MonoBehaviour
{
    // === SINGLETON Y ESTADO GLOBAL ===
    public static TaskManager Instance { get; private set; }
    public static bool IsDecisionActive { get; private set; } = false;

    // === CONTROL DE EJECUCIÓN MÚLTIPLE ===
    private bool isCheckingFinalScore = false;
    private bool gameEnded = false;

    // 🚀 AÑADIDO: ESCENAS DE FIN DE JUEGO
    [Header("8. Escenas de Fin de Juego")]
    [Tooltip("Nombre de la escena de 'Final Bueno' (Victoria)")]
    public string goodEndingSceneName = "GoodEndingScene";
    [Tooltip("Nombre de la escena de 'Final Malo' (Derrota)")]
    public string badEndingSceneName = "BadEndingScene";

    // === 1. PROGRESO DE LIMPIEZA DUAL ===
    [Header("1. Progreso de Limpieza Dual")]
    public int totalDirtSpots = 0;
    public int cleanedDirtSpots = 0;
    public int totalTrashItems = 0;
    public int cleanedTrashItems = 0;

    [Header("1.1 Detalle por Tipo")]
    public int totalGlass = 0;
    public int cleanedGlass = 0;
    public int totalPaper = 0; // Papel/Carton
    public int cleanedPaper = 0;
    public int totalPlastic = 0;
    public int cleanedPlastic = 0;
    public int totalHazardous = 0;
    public int cleanedHazardous = 0;

    // === 2. CONTROL DE TIEMPO ===
    [Header("2. Control de Tiempo")]
    [Tooltip("Duración máxima del nivel en segundos.")]
    public float maxLevelTime = 600f;
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

    // === 6. GESTIÓN DE OBJETOS FALTANTES ===
    [Header("6. Objetos Faltantes")]
    [Tooltip("Número de items restantes para activar la lista de la UI.")]
    public int itemThresholdToActivateList = 10;
    public List<string> remainingItemNames { get; private set; } = new List<string>();

    // === 5. GESTIÓN DE HERRAMIENTAS Y ZONAS ===
    [Header("5. Gestión de Herramientas")]
    public ToolDescriptor CurrentTool { get; private set; }
    public float damageMultiplier = 1f;
    public bool requireCorrectTool = true;
    public List<DirtSpot> nearbyDirt { get; private set; } = new List<DirtSpot>();

    // === 7. DEBUG Y SEGUIMIENTO ===
    [Header("7. Debug y Seguimiento")]
    public List<GameObject> allCleanableObjects = new List<GameObject>();
    public Dictionary<string, GameObject> objectRegistry = new Dictionary<string, GameObject>();

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
        GameEvents.OnAllDone += HandleAllDone;

        IsDecisionActive = false;
        CurrentTool = null;
        gameEnded = false;
    }

    void Start()
    {
        InitializeCleaningSystem();
        InitializeSentimentalAnalysis();
        currentTime = maxLevelTime;

        Debug.Log($"🎯 TaskManager inicializado: {totalDirtSpots} manchas, {totalTrashItems} basuras");

        var uiManager = FindObjectOfType<CleaningUIManager>();
        int totalItems = totalDirtSpots + totalTrashItems;
        int cleanedItems = cleanedDirtSpots + cleanedTrashItems;

        if (uiManager != null)
        {
            uiManager.ForceUpdate(cleanedItems, totalItems);
        }
        else
        {
            ForceInitialProgressUpdate();
        }
    }

    void OnDestroy()
    {
        GameEvents.OnMemorieDecided -= HandleMemorieDecision;
        GameEvents.OnAllDone -= HandleAllDone;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (!timeIsUp && currentTime > 0 && !gameEnded)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                timeIsUp = true;
                Debug.Log("¡TIEMPO AGOTADO! Transicionando a Final Malo.");
                EndGame(false);
                return;
            }
        }

        // Shortcuts de Debug
        if (Input.GetKeyDown(KeyCode.L) && !gameEnded) ForceCompleteCleaningTasks();
        if (Input.GetKeyDown(KeyCode.I) && !gameEnded) ForceSetIdealScore();
        if (Input.GetKeyDown(KeyCode.P)) DebugCleaningCount();
        if (Input.GetKeyDown(KeyCode.O)) DebugMissingObjects();
        if (Input.GetKeyDown(KeyCode.R) && !gameEnded) ForceResync();
        if (Input.GetKeyDown(KeyCode.Y)) DebugGameResult();
    }

    // =========================================================================
    // 🚀 MÉTODOS DE FIN DE JUEGO
    // =========================================================================

    private void EndGame(bool won)
    {
        if (gameEnded) return;

        gameEnded = true;
        string sceneToLoad = won ? goodEndingSceneName : badEndingSceneName;
        string result = won ? "VICTORIA" : "DERROTA";

        Debug.Log($"🎉 Juego Terminado: {result}. Transicionando a: {sceneToLoad}");
        GameEvents.GameResult(won);
        Time.timeScale = 1f;

        try
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ ERROR al cargar la escena '{sceneToLoad}': {ex.Message}");
        }
    }

    public void CheckFinalScore()
    {
        if (isCheckingFinalScore || gameEnded)
        {
            Debug.LogWarning("⚠️ CheckFinalScore ignorado (ya en ejecución o juego terminado).");
            return;
        }

        isCheckingFinalScore = true;

        try
        {
            // --- LÓGICA DE VICTORIA SIMPLIFICADA ---
            int totalItems = totalDirtSpots + totalTrashItems;
            int cleanedItems = cleanedDirtSpots + cleanedTrashItems;

            // Ganamos si limpiamos todo
            bool won = cleanedItems >= totalItems;

            if (won) Debug.Log($"🏆 VICTORIA: {cleanedItems}/{totalItems} objetos limpiados.");
            else Debug.LogWarning($"⚠️ Faltan objetos: {cleanedItems}/{totalItems}.");

            EndGame(won);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ERROR en CheckFinalScore: {e.Message}");
            EndGame(false);
        }
        finally
        {
            isCheckingFinalScore = false;
        }
    }

    // =========================================================================
    // 🚀 MÉTODOS DE SOPORTE
    // =========================================================================

    [ContextMenu("Force Initial Progress Update")]
    public void ForceInitialProgressUpdate()
    {
        if (!gameEnded) CheckCompletion();
    }

    private void InitializeCleaningSystem()
    {
        Debug.Log("=== 🔄 INICIALIZANDO SISTEMA DE LIMPIEZA ===");

        remainingItemNames.Clear();
        allCleanableObjects.Clear();
        objectRegistry.Clear();
        cleanedDirtSpots = 0;
        cleanedTrashItems = 0;
        
        totalGlass = 0; cleanedGlass = 0;
        totalPaper = 0; cleanedPaper = 0;
        totalPlastic = 0; cleanedPlastic = 0;
        totalHazardous = 0; cleanedHazardous = 0;

        var allDirtSpots = FindObjectsOfType<DirtSpot>(true);
        var allTrashObjects = FindObjectsOfType<TrashObject>(true);

        foreach (var dirt in allDirtSpots)
        {
            if (dirt != null && !dirt.IsCleaned)
            {
                string uniqueId = GenerateUniqueId(dirt.gameObject);
                if (!objectRegistry.ContainsKey(uniqueId))
                {
                    objectRegistry[uniqueId] = dirt.gameObject;
                    remainingItemNames.Add(uniqueId);
                    allCleanableObjects.Add(dirt.gameObject);
                }
            }
            else if (dirt != null && dirt.IsCleaned) cleanedDirtSpots++;
        }

        foreach (var trash in allTrashObjects)
        {
            string tag = trash.tag;
            if (tag == "Vidrio") totalGlass++;
            else if (tag == "Carton" || tag == "Papel") totalPaper++;
            else if (tag == "Plastico") totalPlastic++;
            else if (tag == "Peligroso") totalHazardous++;

            if (trash != null && !trash.IsCleaned)
            {
                string uniqueId = GenerateUniqueId(trash.gameObject);
                if (!objectRegistry.ContainsKey(uniqueId))
                {
                    objectRegistry[uniqueId] = trash.gameObject;
                    remainingItemNames.Add(uniqueId);
                    allCleanableObjects.Add(trash.gameObject);
                }
            }
            else if (trash != null && trash.IsCleaned) 
            {
                cleanedTrashItems++;
                if (tag == "Vidrio") cleanedGlass++;
                else if (tag == "Carton" || tag == "Papel") cleanedPaper++;
                else if (tag == "Plastico") cleanedPlastic++;
                else if (tag == "Peligroso") cleanedHazardous++;
            }
        }

        totalDirtSpots = allDirtSpots.Length;
        totalTrashItems = allTrashObjects.Length;

        ValidateCounters();

        if (remainingItemNames.Count <= itemThresholdToActivateList && remainingItemNames.Count > 0)
        {
            GameEvents.NotifyMissingItems(remainingItemNames);
        }
    }

    private string GenerateUniqueId(GameObject obj)
    {
        Vector3 pos = obj.transform.position;
        return $"{obj.name}_({pos.x:F0},{pos.y:F0},{pos.z:F0})";
    }

    private string FindObjectIdByName(string objectName)
    {
        foreach (var id in objectRegistry.Keys)
        {
            if (id.StartsWith(objectName) || id.Contains(objectName)) return id;
        }
        return null;
    }

    public void NotifyTrashCleaned(string itemName)
    {
        if (gameEnded) return;

        string objectId = FindObjectIdByName(itemName);
        if (string.IsNullOrEmpty(objectId)) objectId = objectRegistry.Keys.FirstOrDefault(key => key.Contains(itemName));

        if (!string.IsNullOrEmpty(objectId) && remainingItemNames.Contains(objectId))
        {
            if (objectRegistry.TryGetValue(objectId, out GameObject obj) && obj != null)
            {
                string tag = obj.tag;
                if (tag == "Vidrio") cleanedGlass++;
                else if (tag == "Carton" || tag == "Papel") cleanedPaper++;
                else if (tag == "Plastico") cleanedPlastic++;
                else if (tag == "Peligroso") cleanedHazardous++;
            }

            cleanedTrashItems++;
            remainingItemNames.Remove(objectId);
            objectRegistry.Remove(objectId);
            CheckCompletion();
        }
    }

    public void NotifySpotCleaned(string itemName)
    {
        if (gameEnded) return;

        string objectId = FindObjectIdByName(itemName);
        if (string.IsNullOrEmpty(objectId)) objectId = objectRegistry.Keys.FirstOrDefault(key => key.Contains(itemName));

        if (!string.IsNullOrEmpty(objectId) && remainingItemNames.Contains(objectId))
        {
            cleanedDirtSpots++;
            remainingItemNames.Remove(objectId);
            objectRegistry.Remove(objectId);
            CheckCompletion();
        }
    }

    private void CheckCompletion()
    {
        if (gameEnded) return;

        int totalCleanableItems = totalDirtSpots + totalTrashItems;
        int cleanedItems = cleanedDirtSpots + cleanedTrashItems;
        
        ValidateCounters();

        GameEvents.Progress(cleanedItems, totalCleanableItems);

        if (remainingItemNames.Count <= itemThresholdToActivateList && remainingItemNames.Count > 0)
        {
            GameEvents.NotifyMissingItems(remainingItemNames);
        }

        if (cleanedItems >= totalCleanableItems && totalCleanableItems > 0 && !gameEnded)
        {
            Debug.Log($"🎉 ¡TODA LA BASURA LIMPIADA! Llamando a AllDone...");
            GameEvents.AllDone();
        }
    }

    private void HandleAllDone()
    {
        if (!gameEnded) CheckFinalScore();
    }

    private void ValidateCounters()
    {
        var currentDirtSpots = FindObjectsOfType<DirtSpot>(true);
        var currentTrashObjects = FindObjectsOfType<TrashObject>(true);

        int actualDirtSpots = currentDirtSpots.Length;
        int actualTrashObjects = currentTrashObjects.Length;
        int actualCleanedDirt = currentDirtSpots.Count(d => d.IsCleaned);
        int actualCleanedTrash = currentTrashObjects.Count(t => t.IsCleaned);

        bool needsResync = false;
        if (totalDirtSpots != actualDirtSpots) needsResync = true;
        if (totalTrashItems != actualTrashObjects) needsResync = true;
        if (cleanedDirtSpots != actualCleanedDirt) needsResync = true;
        if (cleanedTrashItems != actualCleanedTrash) needsResync = true;

        if (needsResync && !gameEnded)
        {
            Debug.Log("🔄 Se detectaron inconsistencias, forzando resincronización...");
            ForceResync();
        }
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

    private void ForceCompleteCleaningTasks()
    {
        if (gameEnded) return;
        ForceSetIdealScore();

        cleanedDirtSpots = totalDirtSpots;
        cleanedTrashItems = totalTrashItems;
        
        cleanedGlass = totalGlass;
        cleanedPaper = totalPaper;
        cleanedPlastic = totalPlastic;
        cleanedHazardous = totalHazardous;

        remainingItemNames.Clear();
        objectRegistry.Clear();

        int total = totalDirtSpots + totalTrashItems;
        GameEvents.Progress(total, total);

        if (total > 0 && !gameEnded) CheckFinalScore();
    }

    private void ForceSetIdealScore()
    {
        if (minBalanceForGoodEnding == 0 || maxAccumulationForGoodEnding == 0) InitializeSentimentalAnalysis();
        emotionalBalanceScore = minBalanceForGoodEnding + 50;
        accumulationScore = 10;
        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);
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
            CurrentTool = null;
            return;
        }

        float damage = damageMultiplier * CurrentTool.ToolPower;
        if (requireCorrectTool && !closestDirt.CanBeCleanedBy(CurrentTool.ToolId)) return;

        closestDirt.CleanHit(damage);
    }

    public void RegisterTool(ToolDescriptor tool) { CurrentTool = tool; }

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

    public int GetRemainingCleanableItemsCount()
    {
        int total = totalDirtSpots + totalTrashItems;
        int cleaned = cleanedDirtSpots + cleanedTrashItems;
        return total - cleaned;
    }

    public static void SetDecisionActive(bool isActive)
    {
        IsDecisionActive = isActive;
    }

    // =========================================================================
    // ✅ MÉTODOS DE DEBUG
    // =========================================================================

    [ContextMenu("Debug Cleaning Count")]
    public void DebugCleaningCount()
    {
        var currentDirt = FindObjectsOfType<DirtSpot>(true);
        var currentTrash = FindObjectsOfType<TrashObject>(true);

        Debug.Log($"=== 🧹 RESUMEN DE LIMPIEZA ===");
        Debug.Log($"Estado juego: {(gameEnded ? "TERMINADO" : "EN CURSO")}");
        Debug.Log($"Progreso Total: {cleanedDirtSpots + cleanedTrashItems}/{totalDirtSpots + totalTrashItems}");
        Debug.Log($"Dirt Spots: {cleanedDirtSpots}/{totalDirtSpots}");
        Debug.Log($"Trash Items: {cleanedTrashItems}/{totalTrashItems}");
    }

    [ContextMenu("Debug Missing Objects")]
    public void DebugMissingObjects()
    {
        Debug.Log($"=== ❌ OBJETOS FALTANTES ===");
        foreach (var name in remainingItemNames)
        {
            Debug.Log($"Falta: {name}");
        }
    }

    [ContextMenu("Debug Game Result")]
    public void DebugGameResult()
    {
        Debug.Log($"=== 🎮 DEBUG RESULTADO FINAL ===");
        Debug.Log($"Estado del juego: {(gameEnded ? "TERMINADO" : "EN CURSO")}");
        Debug.Log($"Limpieza: {cleanedDirtSpots + cleanedTrashItems}/{totalDirtSpots + totalTrashItems}");
    }

    [ContextMenu("Forzar Resincronización")]
    public void ForceResync()
    {
        if (gameEnded) return;
        Debug.Log("=== 🔄 FORZANDO RESINCRONIZACIÓN COMPLETA ===");
        InitializeCleaningSystem();
    }

    [ContextMenu("Reset Game State")]
    public void ResetGameState()
    {
        gameEnded = false;
        isCheckingFinalScore = false;
        Debug.Log("🔄 Estado del juego reseteado.");
    }
}