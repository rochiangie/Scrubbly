using UnityEngine;

public class TaskManager : MonoBehaviour
{
    int totalDirt;
    int cleaned;

    void Start()
    {
        totalDirt = FindObjectsOfType<DirtSpot>(true).Length;
        cleaned = 0;
        GameEvents.Progress(cleaned, totalDirt);
        GameEvents.OnAnyDirtCleaned += HandleCleaned;
    }

    void OnDestroy()
    {
        GameEvents.OnAnyDirtCleaned -= HandleCleaned;
    }

    void HandleCleaned()
    {
        cleaned++;
        GameEvents.Progress(cleaned, totalDirt);
        if (cleaned >= totalDirt)
            GameEvents.AllDone();
    }
}
