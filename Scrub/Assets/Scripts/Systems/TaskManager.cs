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
    // ⚠️ REQUIERE QUE LA CLASE ToolDescriptor EXISTA.
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

    // 🛑 CORRECCIÓN CLAVE: Sincronizar UI al inicio.
    void Start()
    {
        InitializeCleaningSystem();
        InitializeSentimentalAnalysis();
        currentTime = maxLevelTime;

        Debug.Log($"🎯 TaskManager inicializado: {totalDirtSpots} manchas, {totalTrashItems} basuras");

        // 🚀 Lógica de sincronización inicial
        var uiManager = FindObjectOfType<CleaningUIManager>();
        int totalItems = totalDirtSpots + totalTrashItems;
        int cleanedItems = cleanedDirtSpots + cleanedTrashItems;

        if (uiManager != null)
        {
            // Llama directamente al método de la UI (más robusto para la inicialización)
            uiManager.ForceUpdate(cleanedItems, totalItems);
        }
        else
        {
            // Si la UI no existe todavía, usa el evento (como fallback)
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
        // === LÓGICA DE TIEMPO ===
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

        // 🛑 SHORTCUTS (Mantenidos para Debug)
        if (Input.GetKeyDown(KeyCode.L) && !gameEnded)
        {
            Debug.Log("DEBUG: Forzando la finalización de las tareas de limpieza.");
            ForceCompleteCleaningTasks();
        }
        if (Input.GetKeyDown(KeyCode.I) && !gameEnded)
        {
            Debug.Log("DEBUG: Forzando el puntaje ideal de victoria.");
            ForceSetIdealScore();
        }
        if (Input.GetKeyDown(KeyCode.P)) DebugCleaningCount();
        if (Input.GetKeyDown(KeyCode.O)) DebugMissingObjects();
        if (Input.GetKeyDown(KeyCode.R) && !gameEnded) ForceResync();
        if (Input.GetKeyDown(KeyCode.Y)) DebugGameResult();
    }

    // =========================================================================
    // 🚀 MÉTODOS DE FIN DE JUEGO Y TRANSICIÓN DE ESCENA 🚀
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
            Debug.LogError($"❌ ERROR al cargar la escena '{sceneToLoad}'. Asegúrate de que la escena esté en File -> Build Settings. Error: {ex.Message}");
        }
    }

    // =========================================================================
    // ✅ MÉTODO CheckFinalScore
    // =========================================================================

    public void CheckFinalScore()
    {
        if (isCheckingFinalScore || gameEnded)
        {
            Debug.LogWarning("⚠️ CheckFinalScore ya está en ejecución o el juego terminó. Ignorando llamada.");
            return;
        }

        isCheckingFinalScore = true;

        try
        {
            if (minBalanceForGoodEnding == 0 && maxAccumulationForGoodEnding == 0)
            {
                InitializeSentimentalAnalysis();
            }

            DebugGameResult();

            bool won = false;

            if (accumulationScore > maxAccumulationForGoodEnding)
            {
                won = false;
            }
            else if (emotionalBalanceScore >= minBalanceForGoodEnding)
            {
                won = true;
            }
            else
            {
                won = false;
            }

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
    // 🚀 MÉTODOS DE SOPORTE PARA UI
    // =========================================================================

    /// <summary>
    /// Fuerza el TaskManager a enviar el progreso actual a la UI (usando el evento).
    /// </summary>
    [ContextMenu("Force Initial Progress Update")]
    public void ForceInitialProgressUpdate()
    {
        if (!gameEnded)
        {
            CheckCompletion();
        }
    }

    // =========================================================================
    // ✅ RESTANTE DEL CÓDIGO (Núcleo de Limpieza)
    // =========================================================================

    private void InitializeCleaningSystem()
    {
        Debug.Log("=== 🔄 INICIALIZANDO SISTEMA DE LIMPIEZA ===");

        // Limpiar todo
        remainingItemNames.Clear();
        allCleanableObjects.Clear();
        objectRegistry.Clear();
        cleanedDirtSpots = 0;
        cleanedTrashItems = 0;

        // Buscar todos los objetos limpiables
        var allDirtSpots = FindObjectsOfType<DirtSpot>(true);
        var allTrashObjects = FindObjectsOfType<TrashObject>(true);

        Debug.Log($"📊 Encontrados: {allDirtSpots.Length} DirtSpots, {allTrashObjects.Length} TrashObjects");

        // ✅ REGISTRAR DIRT SPOTS
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
            else if (dirt != null && dirt.IsCleaned)
            {
                cleanedDirtSpots++;
            }
        }

        // ✅ REGISTRAR TRASH OBJECTS
        foreach (var trash in allTrashObjects)
        {
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
            }
        }

        // Establecer totales CORRECTOS
        totalDirtSpots = allDirtSpots.Length;
        totalTrashItems = allTrashObjects.Length;

        // Verificar consistencia
        ValidateCounters();

        // Activar lista si es necesario
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
            if (id.StartsWith(objectName) || id.Contains(objectName))
            {
                return id;
            }
        }
        return null;
    }

    public void NotifyTrashCleaned(string itemName)
    {
        Debug.Log($"🗑️ [NotifyTrashCleaned] llamado para: {itemName}");

        if (gameEnded) return;

        string objectId = FindObjectIdByName(itemName);
        if (string.IsNullOrEmpty(objectId)) objectId = objectRegistry.Keys.FirstOrDefault(key => key.Contains(itemName));

        if (!string.IsNullOrEmpty(objectId) && remainingItemNames.Contains(objectId))
        {
            cleanedTrashItems++;
            remainingItemNames.Remove(objectId);
            objectRegistry.Remove(objectId);

            CheckCompletion();
        }
        else
        {
            Debug.LogWarning($"⚠️ [NotifyTrashCleaned] No se encontró o ya estaba limpio: {itemName}");
        }
    }

    public void NotifySpotCleaned(string itemName)
    {
        Debug.Log($"🧼 [NotifySpotCleaned] llamado para: {itemName}");

        if (gameEnded) return;

        string objectId = FindObjectIdByName(itemName);
        if (string.IsNullOrEmpty(objectId)) objectId = objectRegistry.Keys.FirstOrDefault(key => key.Contains(itemName));

        if (!string.IsNullOrEmpty(objectId) && remainingItemNames.Contains(objectId))
        {
            cleanedDirtSpots++;
            Debug.Log($"✅ Contador DirtSpots incrementado a: {cleanedDirtSpots}");

            remainingItemNames.Remove(objectId);
            objectRegistry.Remove(objectId);

            CheckCompletion();
        }
        else
        {
            Debug.LogWarning($"⚠️ [NotifySpotCleaned] No se encontró o ya estaba limpio: {itemName}");
        }
    }

    private void CheckCompletion()
    {
        if (gameEnded) return;

        int totalCleanableItems = totalDirtSpots + totalTrashItems;
        int cleanedItems = cleanedDirtSpots + cleanedTrashItems;
        int remainingItems = totalCleanableItems - cleanedItems;

        ValidateCounters();

        Debug.Log($"📊 [PROGRESS CHECK] Limpiado: {cleanedItems} / Total: {totalCleanableItems}. Restantes: {remainingItems}");

        // 🛑 Invocamos el evento de progreso TOTAL usando el método correcto (GameEvents.Progress)
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
        if (!gameEnded)
        {
            CheckFinalScore();
        }
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
        Debug.Log($"Dirt Spots: {cleanedDirtSpots}/{totalDirtSpots} (En escena: {currentDirt.Length})");
        Debug.Log($"Trash Items: {cleanedTrashItems}/{totalTrashItems} (En escena: {currentTrash.Length})");
        Debug.Log($"Items en remainingItemNames: {remainingItemNames.Count}");
        Debug.Log($"Objetos en registry: {objectRegistry.Count}");

        int expectedRemaining = (totalDirtSpots + totalTrashItems) - (cleanedDirtSpots + cleanedTrashItems);
        Debug.Log($"Esperados en lista: {expectedRemaining} vs Actuales: {remainingItemNames.Count}");
    }

    [ContextMenu("Debug Missing Objects")]
    public void DebugMissingObjects()
    {
        var allDirtSpots = FindObjectsOfType<DirtSpot>(true);
        var allTrashObjects = FindObjectsOfType<TrashObject>(true);

        Debug.Log($"=== 🔍 DEBUG DETALLADO ===");
        Debug.Log($"DirtSpots en escena: {allDirtSpots.Length} (limpios: {allDirtSpots.Count(d => d.IsCleaned)})");
        Debug.Log($"TrashObjects en escena: {allTrashObjects.Length} (limpios: {allTrashObjects.Count(t => t.IsCleaned)})");

        Debug.Log($"=== ❌ DIRTSPOTS POR LIMPIAR ===");
        foreach (var dirt in allDirtSpots)
        {
            if (dirt != null && !dirt.IsCleaned)
            {
                string id = GenerateUniqueId(dirt.gameObject);
                bool inList = remainingItemNames.Contains(id);
                Debug.Log($"{(inList ? "✅" : "❌")} Dirt: {dirt.name} -> {id} (En lista: {inList})");
            }
        }

        Debug.Log($"=== ❌ TRASHOBJECTS POR LIMPIAR ===");
        foreach (var trash in allTrashObjects)
        {
            if (trash != null && !trash.IsCleaned)
            {
                string id = GenerateUniqueId(trash.gameObject);
                bool inList = remainingItemNames.Contains(id);
                Debug.Log($"{(inList ? "✅" : "❌")} Trash: {trash.name} -> {id} (En lista: {inList})");
            }
        }
    }

    [ContextMenu("Debug Game Result")]
    public void DebugGameResult()
    {
        Debug.Log($"=== 🎮 DEBUG RESULTADO FINAL ===");
        Debug.Log($"Estado del juego: {(gameEnded ? "TERMINADO" : "EN CURSO")}");
        Debug.Log($"Limpieza: {cleanedDirtSpots + cleanedTrashItems}/{totalDirtSpots + totalTrashItems}");
        Debug.Log($"Balance Emocional: {emotionalBalanceScore} / Mínimo requerido: {minBalanceForGoodEnding}");
        Debug.Log($"Acumulación: {accumulationScore} / Límite máximo: {maxAccumulationForGoodEnding}");
    }

    [ContextMenu("Forzar Resincronización")]
    public void ForceResync()
    {
        if (gameEnded) return;

        Debug.Log("=== 🔄 FORZANDO RESINCRONIZACIÓN COMPLETA ===");

        remainingItemNames.Clear();
        objectRegistry.Clear();
        allCleanableObjects.Clear();

        var allDirtSpots = FindObjectsOfType<DirtSpot>(true);
        var allTrashObjects = FindObjectsOfType<TrashObject>(true);

        totalDirtSpots = allDirtSpots.Length;
        totalTrashItems = allTrashObjects.Length;
        cleanedDirtSpots = allDirtSpots.Count(d => d.IsCleaned);
        cleanedTrashItems = allTrashObjects.Count(t => t.IsCleaned);

        foreach (var dirt in allDirtSpots)
        {
            if (!dirt.IsCleaned)
            {
                string id = GenerateUniqueId(dirt.gameObject);
                remainingItemNames.Add(id);
                objectRegistry[id] = dirt.gameObject;
                allCleanableObjects.Add(dirt.gameObject);
            }
        }

        foreach (var trash in allTrashObjects)
        {
            if (!trash.IsCleaned)
            {
                string id = GenerateUniqueId(trash.gameObject);
                remainingItemNames.Add(id);
                objectRegistry[id] = trash.gameObject;
                allCleanableObjects.Add(trash.gameObject);
            }
        }

        GameEvents.Progress(cleanedDirtSpots + cleanedTrashItems, totalDirtSpots + totalTrashItems);
    }

    [ContextMenu("Reset Game State")]
    public void ResetGameState()
    {
        gameEnded = false;
        isCheckingFinalScore = false;
        Debug.Log("🔄 Estado del juego reseteado. Listo para nueva partida.");
    }
}