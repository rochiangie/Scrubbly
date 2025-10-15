using System;
using UnityEngine;

public static class GameEvents
{
    // EVENTOS DE LIMPIEZA (Se mantienen)
    public static event Action OnAnyDirtCleaned;
    public static event Action<int, int> OnProgressUpdate; // Limpiado / Total Suciedad

    // NUEVO: EVENTOS DE SENTIMENTALISMO (Gestión de Objetos Memorie)
    // Dispara al decidir GUARDAR o DESTRUIR un objeto Memorie.
    // Parámetros: bool isKept (true=Guardado, false=Destruido/Tirado)
    public static event Action<bool, int> OnMemorieDecided; // isKept, SentimentalValue
    public static event Action<int, int> OnSentimentalScoreUpdate; // Score Actual, Score de Acumulación


    // EVENTO DE FINALIZACIÓN
    public static event Action OnAllDone;

    // ===================================
    // MÉTODOS PÚBLICOS DE INVOCACIÓN
    // ===================================

    // Limpieza (Mantiene el código original)
    public static void DirtCleaned()
    {
        OnAnyDirtCleaned?.Invoke();
    }

    public static void Progress(int cleaned, int total)
    {
        OnProgressUpdate?.Invoke(cleaned, total);
    }

    // Finalización (Mantiene el código original)
    public static void AllDone() // ¡SIN PARÁMETROS!
    {
        OnAllDone?.Invoke();
    }

    // NUEVO: Invocación de Decisión de Memoria
    public static void MemorieDecided(bool isKept, int sentimentalValue)
    {
        OnMemorieDecided?.Invoke(isKept, sentimentalValue);
    }

    // NUEVO: Invocación de Actualización de Score
    public static void SentimentalScore(int currentScore, int accumulationScore)
    {
        OnSentimentalScoreUpdate?.Invoke(currentScore, accumulationScore);
    }
}