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
    private Dictionary<string, GameObject> objectRegistry = new Dictionary<string, GameObject>();

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
    }

    void Start()
    {
        // INICIALIZAR EN ORDEN CORRECTO
        InitializeCleaningSystem();
        InitializeSentimentalAnalysis();
        currentTime = maxLevelTime;

        Debug.Log($"🎯 TaskManager inicializado: {totalDirtSpots} manchas, {totalTrashItems} basuras");
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

        // 🛑 SHORTCUT 2: Puntaje Ideal (Tecla I)
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("DEBUG: Forzando el puntaje ideal de victoria.");
            ForceSetIdealScore();
        }

        // 🛑 SHORTCUT 3: Debug del conteo (Tecla P)
        if (Input.GetKeyDown(KeyCode.P))
        {
            DebugCleaningCount();
        }

        // 🛑 SHORTCUT 4: Debug de objetos faltantes (Tecla O)
        if (Input.GetKeyDown(KeyCode.O))
        {
            DebugMissingObjects();
        }

        // 🛑 SHORTCUT 5: Resincronización forzada (Tecla R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            ForceResync();
        }
    }

    // =========================================================================
    // ✅ MÉTODO DE INICIALIZACIÓN CORREGIDO
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
                    string displayName = string.IsNullOrEmpty(trash.trashName) ? uniqueId : $"{trash.trashName}_{uniqueId}";
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

        Debug.Log($"✅ Totales: {totalDirtSpots} DirtSpots ({cleanedDirtSpots} limpios), {totalTrashItems} TrashObjects ({cleanedTrashItems} limpios)");
        Debug.Log($"📋 Items en lista: {remainingItemNames.Count}");

        // Verificar consistencia
        ValidateCounters();

        // Actualizar UI inicial
        int totalCleaned = cleanedDirtSpots + cleanedTrashItems;
        int totalItems = totalDirtSpots + totalTrashItems;
        GameEvents.Progress(totalCleaned, totalItems);

        // Activar lista si es necesario
        if (remainingItemNames.Count <= itemThresholdToActivateList && remainingItemNames.Count > 0)
        {
            GameEvents.NotifyMissingItems(remainingItemNames);
        }
    }

    // =========================================================================
    // ✅ MÉTODOS DE IDENTIFICACIÓN ÚNICA
    // =========================================================================

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

    // =========================================================================
    // ✅ MÉTODOS DE NOTIFICACIÓN CORREGIDOS
    // =========================================================================

    public void NotifyTrashCleaned(string itemName)
    {
        string objectId = FindObjectIdByName(itemName);

        if (string.IsNullOrEmpty(objectId))
        {
            // Buscar directamente en el registro
            objectId = objectRegistry.Keys.FirstOrDefault(key => key.Contains(itemName));
        }

        if (!string.IsNullOrEmpty(objectId) && remainingItemNames.Contains(objectId))
        {
            cleanedTrashItems++;
            remainingItemNames.Remove(objectId);
            objectRegistry.Remove(objectId);

            Debug.Log($"🗑️ Trash limpiado: {itemName} -> {objectId} ({cleanedTrashItems}/{totalTrashItems})");
            CheckCompletion();
        }
        else
        {
            Debug.LogWarning($"⚠️ TrashObject {itemName} no encontrado. IDs disponibles: {string.Join(", ", objectRegistry.Keys)}");
            // Forzar resincronización si hay inconsistencia
            ForceResync();
        }
    }

    public void NotifySpotCleaned(string itemName)
    {
        string objectId = FindObjectIdByName(itemName);

        if (string.IsNullOrEmpty(objectId))
        {
            objectId = objectRegistry.Keys.FirstOrDefault(key => key.Contains(itemName));
        }

        if (!string.IsNullOrEmpty(objectId) && remainingItemNames.Contains(objectId))
        {
            cleanedDirtSpots++;
            remainingItemNames.Remove(objectId);
            objectRegistry.Remove(objectId);

            Debug.Log($"🧹 DirtSpot limpiado: {itemName} -> {objectId} ({cleanedDirtSpots}/{totalDirtSpots})");
            CheckCompletion();
        }
        else
        {
            Debug.LogWarning($"⚠️ DirtSpot {itemName} no encontrado. IDs disponibles: {string.Join(", ", objectRegistry.Keys)}");
            ForceResync();
        }
    }

    private void CheckCompletion()
    {
        int totalCleanableItems = totalDirtSpots + totalTrashItems;
        int cleanedItems = cleanedDirtSpots + cleanedTrashItems;

        // Validar consistencia
        ValidateCounters();

        GameEvents.Progress(cleanedItems, totalCleanableItems);

        // Activar lista si es necesario
        if (remainingItemNames.Count <= itemThresholdToActivateList && remainingItemNames.Count > 0)
        {
            GameEvents.NotifyMissingItems(remainingItemNames);
        }

        if (cleanedItems >= totalCleanableItems && totalCleanableItems > 0)
        {
            Debug.Log($"🎉 ¡TODA LA BASURA LIMPIADA! {cleanedItems}/{totalCleanableItems}");
            GameEvents.AllDone();
        }
    }

    // =========================================================================
    // ✅ VALIDACIÓN DE CONTADORES
    // =========================================================================

    private void ValidateCounters()
    {
        var currentDirtSpots = FindObjectsOfType<DirtSpot>(true);
        var currentTrashObjects = FindObjectsOfType<TrashObject>(true);

        int actualDirtSpots = currentDirtSpots.Length;
        int actualTrashObjects = currentTrashObjects.Length;
        int actualCleanedDirt = currentDirtSpots.Count(d => d.IsCleaned);
        int actualCleanedTrash = currentTrashObjects.Count(t => t.IsCleaned);

        bool needsResync = false;

        if (totalDirtSpots != actualDirtSpots)
        {
            Debug.LogWarning($"⚠️ INCONSISTENCIA DirtSpots: {totalDirtSpots} vs {actualDirtSpots}");
            needsResync = true;
        }

        if (totalTrashItems != actualTrashObjects)
        {
            Debug.LogWarning($"⚠️ INCONSISTENCIA TrashObjects: {totalTrashItems} vs {actualTrashObjects}");
            needsResync = true;
        }

        if (cleanedDirtSpots != actualCleanedDirt)
        {
            Debug.LogWarning($"⚠️ INCONSISTENCIA DirtSpots limpios: {cleanedDirtSpots} vs {actualCleanedDirt}");
            needsResync = true;
        }

        if (cleanedTrashItems != actualCleanedTrash)
        {
            Debug.LogWarning($"⚠️ INCONSISTENCIA TrashObjects limpios: {cleanedTrashItems} vs {actualCleanedTrash}");
            needsResync = true;
        }

        if (needsResync)
        {
            Debug.Log("🔄 Se detectaron inconsistencias, forzando resincronización...");
            ForceResync();
        }
    }

    // =========================================================================
    // ✅ MÉTODOS DE DEBUG MEJORADOS
    // =========================================================================

    [ContextMenu("Debug Cleaning Count")]
    public void DebugCleaningCount()
    {
        var currentDirt = FindObjectsOfType<DirtSpot>(true);
        var currentTrash = FindObjectsOfType<TrashObject>(true);

        Debug.Log($"=== 🧹 RESUMEN DE LIMPIEZA ===");
        Debug.Log($"Progreso Total: {cleanedDirtSpots + cleanedTrashItems}/{totalDirtSpots + totalTrashItems}");
        Debug.Log($"Dirt Spots: {cleanedDirtSpots}/{totalDirtSpots} (En escena: {currentDirt.Length})");
        Debug.Log($"Trash Items: {cleanedTrashItems}/{totalTrashItems} (En escena: {currentTrash.Length})");
        Debug.Log($"Items en remainingItemNames: {remainingItemNames.Count}");
        Debug.Log($"Objetos en registry: {objectRegistry.Count}");

        // Verificación rápida de consistencia
        int expectedRemaining = (totalDirtSpots + totalTrashItems) - (cleanedDirtSpots + cleanedTrashItems);
        Debug.Log($"Esperados en lista: {expectedRemaining} vs Actuales: {remainingItemNames.Count}");

        if (expectedRemaining != remainingItemNames.Count)
        {
            Debug.LogError($"❌ INCONSISTENCIA: expectedRemaining ({expectedRemaining}) != remainingItemNames ({remainingItemNames.Count})");
        }
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

        Debug.Log($"=== 📋 remainingItemNames ({remainingItemNames.Count}) ===");
        foreach (string itemId in remainingItemNames)
        {
            Debug.Log($"📌 {itemId}");
        }
    }

    [ContextMenu("Forzar Resincronización")]
    public void ForceResync()
    {
        Debug.Log("=== 🔄 FORZANDO RESINCRONIZACIÓN COMPLETA ===");

        // Limpiar todo
        remainingItemNames.Clear();
        objectRegistry.Clear();
        allCleanableObjects.Clear();

        // Recontar desde cero
        var allDirtSpots = FindObjectsOfType<DirtSpot>(true);
        var allTrashObjects = FindObjectsOfType<TrashObject>(true);

        // Actualizar contadores
        totalDirtSpots = allDirtSpots.Length;
        totalTrashItems = allTrashObjects.Length;
        cleanedDirtSpots = allDirtSpots.Count(d => d.IsCleaned);
        cleanedTrashItems = allTrashObjects.Count(t => t.IsCleaned);

        // Reconstruir registros
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

        Debug.Log($"✅ Resincronizado: {cleanedDirtSpots + cleanedTrashItems}/{totalDirtSpots + totalTrashItems}");
        Debug.Log($"📊 Dirt: {cleanedDirtSpots}/{totalDirtSpots}, Trash: {cleanedTrashItems}/{totalTrashItems}");

        // Actualizar UI
        GameEvents.Progress(cleanedDirtSpots + cleanedTrashItems, totalDirtSpots + totalTrashItems);
    }

    // =========================================================================
    // 🎯 RESTANTE DEL CÓDIGO (sin cambios)
    // =========================================================================

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
        ForceSetIdealScore();
        cleanedDirtSpots = totalDirtSpots;
        cleanedTrashItems = totalTrashItems;
        remainingItemNames.Clear();
        objectRegistry.Clear();

        int total = totalDirtSpots + totalTrashItems;
        GameEvents.Progress(total, total);

        if (total > 0)
        {
            CheckFinalScore();
        }
    }

    private void ForceSetIdealScore()
    {
        if (minBalanceForGoodEnding == 0 || maxAccumulationForGoodEnding == 0)
        {
            InitializeSentimentalAnalysis();
        }

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

        if (requireCorrectTool && !closestDirt.CanBeCleanedBy(CurrentTool.ToolId))
        {
            Debug.LogWarning($"[Clean Hit] Herramienta incorrecta para {closestDirt.name}.");
            return;
        }

        closestDirt.CleanHit(damage);
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
}