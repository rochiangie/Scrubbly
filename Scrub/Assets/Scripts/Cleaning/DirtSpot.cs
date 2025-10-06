// DirtSpot.cs

using UnityEngine;

public class DirtSpot : MonoBehaviour
{
    [SerializeField] private string requiredToolId = "";
    [SerializeField] public float dirtHealth = 3.0f; // Hacemos 'public' temporalmente para el log de diagn�stico

    public string RequiredToolId => requiredToolId;

    public bool CanBeCleanedBy(string toolId)
        => string.IsNullOrEmpty(requiredToolId) || requiredToolId == toolId;

    public void CleanHit(float damage)
    {
        dirtHealth -= damage;
        // Log de estado de vida dentro de la suciedad
        //Debug.Log($"[DIRT STATUS] {name} recibi� {damage:F2} de da�o. Vida restante: {dirtHealth:F2}");

        if (dirtHealth <= 0f)
        {
            //Debug.Log($"[DIRT] {name} limpio! Destruyendo objeto.");
            Destroy(gameObject);
        }
    }
}