using System;
using UnityEngine;
using System.Collections.Generic;

// Clase estática que actúa como un bus de eventos global para desacoplar sistemas.
public static class GameEvents
{
    // ===================================
    // 1. EVENTOS DE LIMPIEZA
    // ===================================

    // Disparado cada vez que un DirtSpot es limpiado.
    public static event Action OnAnyDirtCleaned;

    // Disparado al actualizar el progreso de limpieza general (Limpiado / Total).
    public static event Action<int, int> OnProgressUpdate;

    // ===================================
    // 2. EVENTOS DE SENTIMENTALISMO Y UI
    // ===================================

    // Disparado por MemorieObject al decidir GUARDAR (S) o DESTRUIR (N).
    public static event Action<bool, int> OnMemorieDecided; // (isKept, SentimentalValue)

    // Disparado por SentimentalScoreManager al cambiar los puntajes. Escuchado por UIPauseController.
    public static event Action<int, int> OnSentimentalScoreUpdate; // (BalanceScore, AccumulationScore)

    // Disparado por el SentimentalScoreManager al final del juego.
    public static event Action<bool> OnGameResult; // (True = Ganó, False = Perdió)

    // Disparado para notificar el fin de la limpieza general (usado por SentimentalScoreManager).
    public static event Action OnAllDone;

    // Disparado para alternar la visibilidad de algún panel de score.
    public static event Action OnToggleScorePanel;

    // 🚨 DECLARACIÓN DEL EVENTO: Este es el nombre que deben usar los suscriptores. 🚨
    public static event Action<List<string>> ActivateMissingItemList;


    // ===================================
    // MÉTODOS PÚBLICOS DE INVOCACIÓN
    // ===================================

    // Invocado por el CleaningTool o DirtSpot.
    public static void DirtCleaned()
    {
        OnAnyDirtCleaned?.Invoke();
    }

    // Invocado por el TaskManager/CleaningController.
    public static void Progress(int cleaned, int total)
    {
        OnProgressUpdate?.Invoke(cleaned, total);
    }

    // Invocado por el TaskManager.
    public static void AllDone()
    {
        OnAllDone?.Invoke();
    }

    // Invocado por el MemorieObject (Callback del panel S/N).
    public static void MemorieDecided(bool isKept, int sentimentalValue)
    {
        OnMemorieDecided?.Invoke(isKept, sentimentalValue);
    }

    // Invocado por el SentimentalScoreManager.
    public static void SentimentalScore(int currentBalance, int accumulationScore)
    {
        OnSentimentalScoreUpdate?.Invoke(currentBalance, accumulationScore);
    }

    // Invocado por el TaskManager (al final del juego).
    public static void GameResult(bool won)
    {
        OnGameResult?.Invoke(won);
    }

    // Invocado por el jugador/botón para mostrar/ocultar el panel.
    public static void ToggleScorePanel()
    {
        OnToggleScorePanel?.Invoke();
    }

    // 🚨 MÉTODO CORREGIDO: Renombrado a NotifyMissingItems para evitar conflicto (CS0102) 🚨
    public static void NotifyMissingItems(List<string> items)
    {
        ActivateMissingItemList?.Invoke(items);
    }
}