using UnityEngine;

public class CleaningTool : MonoBehaviour
{
    [Header("Identificaci�n")]
    // Este ID debe coincidir con el 'requiredToolId' en DirtSpot
    public string toolID = "CepilloBasico";

    [Header("Durabilidad")]
    [Tooltip("Cantidad de veces que se puede usar antes de destruirse")]
    [SerializeField] private int maxUses = 10;
    private int remainingUses;

    [Header("Efectos")]
    [Tooltip("El da�o que hace cada uso a la suciedad (DirtSpot.dirtHealth)")]
    [SerializeField] private float damagePerUse = 1.0f;

    public float DamagePerUse => damagePerUse;
    public string ToolID => toolID;

    void Awake()
    {
        remainingUses = maxUses;
    }

    // Funci�n CR�TICA: Llamada por el CleaningController despu�s de un golpe exitoso
    public bool UseTool()
    {
        if (remainingUses <= 0)
        {
            return false; // No se puede usar, ya est� gastada
        }

        remainingUses--;
        Debug.Log($"[TOOL] {toolID} usado. Usos restantes: {remainingUses}/{maxUses}");

        if (remainingUses <= 0)
        {
            Debug.Log($"[TOOL] {toolID} se ha gastado. Destruyendo objeto.");
            // Destruye el objeto de la herramienta (desaparece del inventario/mano)
            Destroy(gameObject);
            return false;
        }

        return true; // Se us� correctamente y a�n queda vida
    }
}