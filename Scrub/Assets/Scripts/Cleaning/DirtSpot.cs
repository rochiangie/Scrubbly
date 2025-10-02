// DirtSpot.cs (en cada objeto sucio)
using UnityEngine;

public class DirtSpot : MonoBehaviour
{
    [SerializeField] string requiredToolId = "Sponge"; // debe coincidir con la herramienta
    [SerializeField] float dirtAmount = 3f;

    public bool CanBeCleanedBy(string toolId) => toolId == requiredToolId;

    public void CleanTick(float amount)
    {
        dirtAmount -= amount;
        if (dirtAmount <= 0f)
        {
            Debug.Log($"{name} limpio por completo");
            Destroy(gameObject);
        }
    }
}
