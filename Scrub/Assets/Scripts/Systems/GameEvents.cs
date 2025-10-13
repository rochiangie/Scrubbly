using System;
using UnityEngine;

// Clase estática para gestionar eventos globales del juego.
public static class GameEvents
{
    // 1. EVENTO DE LIMPIEZA (El que TaskManager y DirtSpot usan)
    // Se dispara cuando CUALQUIER DirtSpot es limpiado/destruido.
    public static event Action OnAnyDirtCleaned;

    // 2. EVENTO DE PROGRESO (Necesita dos parámetros: Limpiado actual y Total)
    public static event Action<int, int> OnProgressUpdate;

    // 3. EVENTO DE FINALIZACIÓN (Disparado cuando totalDirt == cleaned)
    public static event Action OnAllDone;

    // ===================================
    // MÉTODOS PÚBLICOS DE INVOCACIÓN (Llamar a estos métodos dispara los eventos)
    // ===================================

    public static void DirtCleaned()
    {
        // El signo de interrogación '?' previene errores si no hay suscriptores.
        OnAnyDirtCleaned?.Invoke();
    }

    public static void Progress(int cleaned, int total)
    {
        OnProgressUpdate?.Invoke(cleaned, total);
    }

    public static void AllDone()
    {
        OnAllDone?.Invoke();
    }
}