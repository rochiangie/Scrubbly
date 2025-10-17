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
        // Optimizamos la sintaxis del Singleton.
        if (Instance == null)
        {
            Instance = this;
            // No destruimos, simplemente aseguramos que el estado inicie limpio.
            IsDecisionActive = false;
        }
        else
        {
            // Si ya existe una instancia, destruye esta para mantener el Singleton.
            Destroy(gameObject);
        }
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
        // Asegúrate de desuscribir los eventos
        GameEvents.OnMemorieDecided -= HandleMemorieDecision;
        GameEvents.OnAllDone -= CheckFinalScore;
    }

    // Método estático para que la UI de decisión pueda activar/desactivar el estado
    // Esto debería ser llamado por MemorieDecisionUI.cs al mostrar/ocultar el panel.
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
            // Se usa Abs() porque acumular es bueno/malo, pero siempre es un valor positivo de acumulación.
            accumulationScore += Mathf.Abs(sentimentalValue);
            Debug.Log($"[SCORE] GUARDADO. Acumulación: +{Mathf.Abs(sentimentalValue)}");
        }
        else // isTossed (Tirar/Destruir)
        {
            // Decisión: DESTRUIR/TIRAR (Balance Emocional)
            // La decisión de tirar un objeto impacta negativamente el balance emocional,
            // ya que son "recuerdos" que deben ser balanceados.
            emotionalBalanceScore -= sentimentalValue;
            Debug.Log($"[SCORE] TIRADO. Balance Emocional: -{sentimentalValue}");
        }

        // Notificar a la UI (si existe) los nuevos puntajes
        // Asumiendo que GameEvents.SentimentalScore toma los dos puntajes.
        GameEvents.SentimentalScore(emotionalBalanceScore, accumulationScore);

        Debug.Log($"[SCORE] Balance Emocional: {emotionalBalanceScore} | Acumulación: {accumulationScore}");
    }

    // Método llamado por GameEvents.OnAllDone (disparado por TaskManager al finalizar la limpieza)
    public void CheckFinalScore()
    {
        bool won = false;
        string finalMessage = "";

        // 1. Condición de Pérdida: ACUMULADOR (Demasiada nostalgia)
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
        // 3. Condición de Pérdida: DESEQUILIBRIO (Destrucción de cosas importantes)
        else
        {
            finalMessage = $"😢 ¡FIN! PERDISTE: Desequilibrio. Balance emocional bajo ({emotionalBalanceScore}).";
            won = false;
        }

        Debug.Log(finalMessage);

        // Llamar al evento final para que la UI final se muestre
        GameEvents.GameResult(won);
    }
}