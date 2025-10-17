using UnityEngine;
using System.Collections.Generic;

public class SentimentalScoreManager : MonoBehaviour
{
    // Singleton pattern
    public static SentimentalScoreManager Instance { get; private set; }

    // Indica si el panel de decisión (S/N) está activo.
    public static bool IsDecisionActive { get; private set; } = false;

    [Header("Puntuación Sentimental")]
    public int emotionalBalanceScore = 0;
    public int accumulationScore = 0;

    [Header("Umbrales de Final")]
    public int minBalanceForGoodEnding = 50;
    public int maxAccumulationForGoodEnding = 150;

    void Awake()
    {
        // 1. Singleton y Persistencia
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 🛑 CLAVE: Asegura que el manager no se destruya al cargar otras escenas.
        DontDestroyOnLoad(gameObject);

        // 2. 🛑 CLAVE: Movemos la suscripción de OnEnable() a Awake(). 
        // Esto asegura que el Manager esté escuchando desde el primer frame.
        GameEvents.OnMemorieDecided += HandleMemorieDecision;
        GameEvents.OnAllDone += CheckFinalScore;

        IsDecisionActive = false;
    }

    // Ya no es necesario el OnEnable, pero lo dejamos vacío para referencia.
    // Los eventos ya están suscritos en Awake().
    void OnEnable() { }

    // 🛑 ES CRÍTICO mantener el OnDisable para evitar errores al salir del juego.
    void OnDisable()
    {
        GameEvents.OnMemorieDecided -= HandleMemorieDecision;
        GameEvents.OnAllDone -= CheckFinalScore;
    }

    // Método estático para que la UI de decisión pueda activar/desactivar el estado
    public static void SetDecisionActive(bool isActive)
    {
        IsDecisionActive = isActive;
        Debug.Log($"[STATE] IsDecisionActive: {IsDecisionActive}");
    }

    // Maneja la actualización de puntuación basada en la decisión del jugador
    private void HandleMemorieDecision(bool isKept, int sentimentalValue)
    {
        // Lógica de Puntuación:
        if (isKept)
        {
            // Decisión: GUARDAR
            accumulationScore += Mathf.Abs(sentimentalValue);
            Debug.Log($"[SCORE] GUARDADO. Acumulación: +{Mathf.Abs(sentimentalValue)}");
        }
        else // isTossed (Tirar/Destruir)
        {
            // Decisión: DESTRUIR/TIRAR
            emotionalBalanceScore -= sentimentalValue;
            Debug.Log($"[SCORE] TIRADO. Balance Emocional: -{sentimentalValue}");
        }

        // Notificar a la UI (UIPauseController) a través del bus de eventos
        // 🛑 Esta llamada es la que actualiza los Sliders.
        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);

        Debug.Log($"[SCORE] Balance Emocional: {emotionalBalanceScore} | Acumulación: {accumulationScore}");
    }

    // Método llamado por GameEvents.OnAllDone
    public void CheckFinalScore()
    {
        bool won = false;
        string finalMessage = "";

        // 1. Condición de Pérdida: ACUMULADOR
        if (accumulationScore > maxAccumulationForGoodEnding)
        {
            finalMessage = $"📦 ¡FIN! PERDISTE: Acumulador. Demasiada acumulación ({accumulationScore} puntos).";
            won = false;
        }
        // 2. Condición de Victoria: BALANCE ÓPTIMO
        else if (emotionalBalanceScore >= minBalanceForGoodEnding)
        {
            finalMessage = $"🎉 ¡FIN! GANASTE: Balance Óptimo. Balance emocional de {emotionalBalanceScore}.";
            won = true;
        }
        // 3. Condición de Pérdida: DESEQUILIBRIO
        else
        {
            finalMessage = $"😢 ¡FIN! PERDISTE: Desequilibrio. Balance emocional bajo ({emotionalBalanceScore}).";
            won = false;
        }

        Debug.Log(finalMessage);

        GameEvents.GameResult(won);
    }
}