// TaskManager.cs
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;

public class TaskManager : MonoBehaviour
{
    // === SINGLETON Y ESTADO GLOBAL ===
    public static TaskManager Instance { get; private set; }
    public static bool IsDecisionActive { get; private set; } = false; // Bloquea el juego durante las decisiones

    // === 1. PROGRESO DE LIMPIEZA DUAL ===
    [Header("1. Progreso de Limpieza Dual")]
    public int totalDirtSpots = 0;
    public int cleanedDirtSpots = 0;
    public int totalTrashItems = 0;
    public int cleanedTrashItems = 0;

    // === 2. CONTROL DE TIEMPO ===
    [Header("2. Control de Tiempo")]
    [Tooltip("Duración máxima del nivel en segundos.")]
    public float maxLevelTime = 600f; // 🚨 AJUSTADO A 600 SEGUNDOS (10 MINUTOS) 🚨
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
        InitializeRemainingItemsList(); // 🚨 Inicializamos la lista de nombres 🚨
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
    // LÓGICA DE INICIALIZACIÓN Y LISTA DE OBJETOS
    // =========================================================================

    private void InitializeRemainingItemsList()
    {
        remainingItemNames.Clear();

        // Buscar todos los DirtSpots y TrashObjects y guardar sus nombres.
        var dirtSpots = FindObjectsOfType<DirtSpot>(true);
        var trashItems = FindObjectsOfType<TrashObject>(true);

        // Asumiendo que el nombre es la forma más fácil de identificar el objeto.
        remainingItemNames.AddRange(dirtSpots.Select(d => d.name));
        remainingItemNames.AddRange(trashItems.Select(t => t.name));

        // Actualizar totales si la lista inicial de objetos es diferente a la de Awake()
        totalDirtSpots = dirtSpots.Length;
        totalTrashItems = trashItems.Length;

        // Si ya estamos por debajo del umbral al inicio (nivel muy corto), activamos la lista.
        if (remainingItemNames.Count <= itemThresholdToActivateList && remainingItemNames.Count > 0)
        {
            // 🚨 CORRECCIÓN: Usar el nuevo nombre del método de invocación.
            GameEvents.NotifyMissingItems(remainingItemNames);
        }

        CheckCompletion();
    }


    private void InitializeCleaningAnalysis()
    {
        // Las variables totalDirtSpots y totalTrashItems son inicializadas en InitializeRemainingItemsList
        // Aquí solo aseguramos que los contadores estén a cero.
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
    // LÓGICA DE NOTIFICACIÓN DE LIMPIEZA DUAL Y FINALIZACIÓN
    // =========================================================================

    /// <summary> Se llama desde TrashObject.cs </summary>
    public void NotifyTrashCleaned(string itemName)
    {
        cleanedTrashItems++;
        remainingItemNames.Remove(itemName); // 🚨 Quitamos el nombre de la lista
        CheckCompletion();
    }

    /// <summary> Se llama desde DirtSpot.cs </summary>
    public void NotifySpotCleaned(string itemName)
    {
        cleanedDirtSpots++;
        remainingItemNames.Remove(itemName); // 🚨 Quitamos el nombre de la lista
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        int totalCleanableItems = totalDirtSpots + totalTrashItems;
        int cleanedItems = cleanedDirtSpots + cleanedTrashItems;
        int remainingCount = totalCleanableItems - cleanedItems;


        GameEvents.Progress(cleanedItems, totalCleanableItems);

        // 🚨 VERIFICACIÓN DE ACTIVACIÓN DE LA LISTA 🚨
        if (remainingCount <= itemThresholdToActivateList && remainingCount > 0)
        {
            // 🚨 CORRECCIÓN: Usar el nuevo método NotifyMissingItems 🚨
            GameEvents.NotifyMissingItems(remainingItemNames);
        }


        if (cleanedItems >= totalCleanableItems && totalCleanableItems > 0)
        {
            GameEvents.AllDone();
        }
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