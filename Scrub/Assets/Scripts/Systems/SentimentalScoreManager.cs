using UnityEngine;
using System.Collections.Generic;
using System;

public class SentimentalScoreManager : MonoBehaviour
{
    public static SentimentalScoreManager Instance { get; private set; }

    public static bool IsDecisionActive { get; private set; } = false;

    [Header("Puntuación Sentimental")]
    public int emotionalBalanceScore = 0;
    public int accumulationScore = 0;

    // Umbrales privados, configurados por TaskManager
    private int minBalanceThreshold = 0;
    private int maxAccumulationThreshold = 0;

    // Propiedades públicas para que UIPauseController.cs pueda acceder a los límites
    public int minBalanceForGoodEnding => minBalanceThreshold;
    public int maxAccumulationForGoodEnding => maxAccumulationThreshold;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Suscripciones:
        GameEvents.OnMemorieDecided += HandleMemorieDecision;
        GameEvents.OnAllDone += CheckFinalScore;

        IsDecisionActive = false;

        minBalanceThreshold = 0;
        maxAccumulationThreshold = 0;
    }

    void OnDisable()
    {
        GameEvents.OnMemorieDecided -= HandleMemorieDecision;
        GameEvents.OnAllDone -= CheckFinalScore;
    }

    // 🛑 NUEVO MÉTODO: Recibe los valores calculados por TaskManager
    public void SetWinThresholds(int totalValue, float balancePct, float accumulationPct)
    {
        // Cálculo del Mínimo Balance requerido (ej. 80% del valor total)
        minBalanceThreshold = Mathf.CeilToInt(totalValue * balancePct);

        // Cálculo del Límite Máximo de Acumulación (ej. 50% del valor total)
        maxAccumulationThreshold = Mathf.FloorToInt(totalValue * accumulationPct);

        Debug.Log($"[ScoreManager] Umbrales Fijados. Mínimo Balance: {minBalanceThreshold} | Máximo Acumulación: {maxAccumulationThreshold}");

        // Forzar actualización inicial de la UI de Score (para que muestre los límites en el menú de pausa)
        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);
    }

    public static void SetDecisionActive(bool isActive)
    {
        IsDecisionActive = isActive;
    }

    private void HandleMemorieDecision(bool isKept, int sentimentalValue)
    {
        if (isKept)
        {
            // Acumulación: Suma valor (riesgo de ser acumulador)
            accumulationScore += Mathf.Abs(sentimentalValue);
        }
        else
        {
            // Balance: La pérdida de valor disminuye el balance emocional (hace más difícil ganar).
            emotionalBalanceScore -= sentimentalValue;
        }

        // Notificar a la UI (UIPauseController)
        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);
    }

    // Método llamado por GameEvents.OnAllDone (disparado por TaskManager al terminar la limpieza)
    public void CheckFinalScore()
    {
        // 🛑 CLAVE: El chequeo usa los umbrales dinámicos.
        bool won = false;
        string finalMessage = "";

        if (minBalanceThreshold == 0 && maxAccumulationThreshold == 0)
        {
            finalMessage = "Error: Umbrales no inicializados. La lógica del TaskManager falló al configurar los límites.";
            won = false;
        }

        // 1. Condición de Pérdida: ACUMULADOR 
        else if (accumulationScore > maxAccumulationThreshold)
        {
            finalMessage = $"📦 ¡FIN! PERDISTE: Acumulador. Acumulación ({accumulationScore}) > Límite ({maxAccumulationThreshold}).";
            won = false;
        }
        // 2. Condición de Victoria: BALANCE ÓPTIMO
        else if (emotionalBalanceScore >= minBalanceThreshold)
        {
            finalMessage = $"🎉 ¡FIN! GANASTE: Balance Óptimo. Balance ({emotionalBalanceScore}) >= Mínimo ({minBalanceThreshold}).";
            won = true;
        }
        // 3. Condición de Pérdida: DESEQUILIBRIO
        else
        {
            finalMessage = $"😢 ¡FIN! PERDISTE: Desequilibrio. Balance ({emotionalBalanceScore}) < Mínimo ({minBalanceThreshold}).";
            won = false;
        }

        Debug.Log(finalMessage);

        // Disparar el evento que mostrará el panel de victoria/derrota
        GameEvents.GameResult(won);
    }
}