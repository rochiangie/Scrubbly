using UnityEngine;
using System.Collections.Generic;

// Este manager maneja la lógica de victoria/derrota basada en las decisiones emocionales,
// e incluye la propiedad estática para controlar el flujo de la interacción (pausa sin Time.timeScale).
public class SentimentalScoreManager : MonoBehaviour
{
    // Singleton pattern
    public static SentimentalScoreManager Instance { get; private set; }

    // 📢 NUEVA PROPIEDAD ESTÁTICA PARA EL CONTROL DE ESTADO
    // Indica si el panel de decisión (Y/N) está activo.
    public static bool IsDecisionActive { get; private set; } = false;

    [Header("Puntuación Sentimental")]
    [Tooltip("El balance emocional: Afectado al TIRAR un objeto. Debe estar en el rango de victoria.")]
    public int emotionalBalanceScore = 0;
    [Tooltip("El total de puntos acumulados por GUARDAR objetos. Demasiado alto = Acumulador.")]
    public int accumulationScore = 0;

    [Header("Umbrales de Final")]
    [Tooltip("Mínimo de EmotionalBalanceScore para un Final Acertado.")]
    public int minBalanceForGoodEnding = 50;
    [Tooltip("Máximo de AccumulationScore para evitar el Final Acumulador.")]
    public int maxAccumulationForGoodEnding = 150;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Reiniciamos el estado por si acaso (útil al cargar escenas)
        IsDecisionActive = false;
    }

    void OnEnable()
    {
        // Suscripción al evento que se dispara cuando el jugador decide GUARDAR (Y) o TIRAR (N)
        GameEvents.OnMemorieDecided += HandleMemorieDecision;

        // Suscripción al evento que dispara el TaskManager cuando TERMINA la limpieza
        GameEvents.OnAllDone += CheckFinalScore;
    }

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
            // Decisión: GUARDAR (Acumulación/Nostalgia)
            accumulationScore += Mathf.Abs(sentimentalValue);
        }
        else // isTossed (Tirar/Destruir)
        {
            // Decisión: DESTRUIR/TIRAR (Balance Emocional)
            emotionalBalanceScore -= sentimentalValue;
        }

        // Notificar a la UI (si existe) los nuevos puntajes
        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);

        Debug.Log($"[SCORE] Balance Emocional: {emotionalBalanceScore} | Acumulación: {accumulationScore}");
    }

    // Método llamado por GameEvents.OnAllDone (disparado por TaskManager al finalizar la limpieza)
    public void CheckFinalScore()
    {
        bool won = false;

        // 1. Condición de Pérdida: ACUMULADOR (Demasiada nostalgia)
        if (accumulationScore > maxAccumulationForGoodEnding)
        {
            Debug.Log($"📦 ¡FIN! PERDISTE. Demasiada acumulación ({accumulationScore} puntos).");
            won = false;
        }
        // 2. Condición de Victoria: BALANCE ÓPTIMO
        else if (emotionalBalanceScore >= minBalanceForGoodEnding)
        {
            Debug.Log($"🎉 ¡FIN! GANASTE. Balance emocional óptimo ({emotionalBalanceScore}).");
            won = true;
        }
        // 3. Condición de Pérdida: DESEQUILIBRIO (Destrucción de cosas importantes)
        else
        {
            Debug.Log($"😢 ¡FIN! PERDISTE. Desequilibrio emocional por malas decisiones ({emotionalBalanceScore}).");
            won = false;
        }

        // Llamar al evento final para que la UI final se muestre
        GameEvents.AllDone(won);
    }
}