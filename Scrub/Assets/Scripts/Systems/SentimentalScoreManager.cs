using UnityEngine;
using System.Collections.Generic;

// Este manager maneja la lógica de victoria/derrota basada en las decisiones emocionales,
// trabajando en conjunto con el TaskManager (que solo se encarga de la limpieza).
public class SentimentalScoreManager : MonoBehaviour
{
    // Singleton pattern
    public static SentimentalScoreManager Instance;

    [Header("Puntuación Sentimental")]
    [Tooltip("El balance emocional: Afectado al TIRAR un objeto. El valor final debe estar en rango.")]
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

    // Maneja la actualización de puntuación basada en la decisión del jugador
    private void HandleMemorieDecision(bool isKept, int sentimentalValue)
    {
        // Lógica de Puntuación:
        if (isKept)
        {
            // Decisión: GUARDAR (Acumulación/Nostalgia)
            // Se suma el valor sentimental del objeto al score de acumulación.
            accumulationScore += Mathf.Abs(sentimentalValue);
        }
        else // isTossed (Tirar/Destruir)
        {
            // Decisión: DESTRUIR/TIRAR (Balance Emocional)
            // Esto afecta el balance emocional, usando el valor NEGATIVO del sentimentalValue.
            // Si sentimentalValue es POSITIVO (Importante), emotionalBalanceScore BAJA.
            // Si sentimentalValue es NEGATIVO (Trivial), emotionalBalanceScore SUBE.
            emotionalBalanceScore -= sentimentalValue;
        }

        // Notificar a la UI (si existe) los nuevos puntajes
        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);

        Debug.Log($"[SCORE] Balance Emocional: {emotionalBalanceScore} | Acumulación: {accumulationScore}");
    }

    // Método llamado por GameEvents.OnAllDone (disparado por TaskManager al finalizar la limpieza)
    public void CheckFinalScore()
    {
        // 1. Condición de Pérdida: ACUMULADOR (Demasiada nostalgia)
        if (accumulationScore > maxAccumulationForGoodEnding)
        {
            Debug.Log($"📦 ¡FIN! PERDISTE. Demasiada acumulación ({accumulationScore} puntos).");
            // Aquí puedes llamar a una función global de Game Over/Fin
            // GameManager.Instance.EndGame(false);
        }
        // 2. Condición de Victoria: BALANCE ÓPTIMO
        else if (emotionalBalanceScore >= minBalanceForGoodEnding)
        {
            Debug.Log($"🎉 ¡FIN! GANASTE. Balance emocional óptimo ({emotionalBalanceScore}).");
            // GameManager.Instance.EndGame(true);
        }
        // 3. Condición de Pérdida: DESEQUILIBRIO (Destrucción de cosas importantes)
        else
        {
            Debug.Log($"😢 ¡FIN! PERDISTE. Desequilibrio emocional por malas decisiones ({emotionalBalanceScore}).");
            // GameManager.Instance.EndGame(false);
        }
    }
}