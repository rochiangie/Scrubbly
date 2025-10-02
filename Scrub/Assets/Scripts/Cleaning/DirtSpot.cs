using UnityEngine;

public class DirtSpot : MonoBehaviour
{
    [SerializeField] private string requiredToolId = ""; // vacío = cualquiera
    [SerializeField] private float dirtAmount = 3f;

    public string RequiredToolId => requiredToolId;

    public bool CanBeCleanedBy(string toolId)
        => string.IsNullOrEmpty(requiredToolId) || requiredToolId == toolId;

    public void CleanTick(float amount)
    {
        dirtAmount -= amount;
        if (dirtAmount <= 0f)
        {
            Debug.Log($"{name} limpio!");
            Destroy(gameObject);
        }
    }
}
