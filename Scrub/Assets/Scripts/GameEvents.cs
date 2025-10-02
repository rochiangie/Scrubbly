using System;
using UnityEngine;

public static class GameEvents
{
    public static event Action OnAnyDirtCleaned;
    public static event Action<int, int> OnProgressChanged; // cleaned, total
    public static event Action OnAllTasksCompleted;

    public static void DirtCleaned() => OnAnyDirtCleaned?.Invoke();
    public static void Progress(int cleaned, int total) => OnProgressChanged?.Invoke(cleaned, total);
    public static void AllDone() => OnAllTasksCompleted?.Invoke();
}
