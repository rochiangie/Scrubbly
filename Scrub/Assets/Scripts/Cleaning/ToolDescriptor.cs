using UnityEngine;

public class ToolDescriptor : MonoBehaviour
{
    [Header("Identificación y Potencia")]
    // 📢 Valor por defecto ajustado para coincidir con tu sistema
    [SerializeField] public string toolId = "Escoba";
    [SerializeField] public float toolPower = 1f;       // multiplicador del daño (damageMultiplier)

    [Header("Durabilidad")]
    [Tooltip("Cantidad de veces que se puede usar antes de destruirse")]
    [SerializeField] private int maxUses = 10;
    private int remainingUses;

    // Propiedades públicas para solo lectura
    public string ToolId => toolId;
    public float ToolPower => toolPower;
    public int RemainingUses => remainingUses;


    private void Awake()
    {
        // Inicializa la durabilidad al cargar el objeto
        remainingUses = maxUses;
    }

    /// <summary>
    /// Intenta usar la herramienta una vez. Consume un uso y destruye el objeto si llega a cero.
    /// Es llamado por CleaningController para verificar durabilidad.
    /// </summary>
    /// <returns>True si la herramienta se usó exitosamente y sigue activa. False si se gastó o ya estaba gastada.</returns>
    public bool TryUse()
    {
        if (remainingUses <= 0)
        {
            return false;
        }

        remainingUses--;
        Debug.Log($"[TOOL] {toolId} usado. Usos restantes: {remainingUses}/{maxUses}");

        if (remainingUses <= 0)
        {
            // La herramienta llegó a su fin.
            Debug.Log($"[TOOL] {toolId} se ha gastado. Destruyendo objeto.");

            // 🛑 CRÍTICO: Destruir el GameObject (la herramienta desaparece)
            Destroy(gameObject);

            // Retornamos falso porque la herramienta ya no existe para el golpe actual
            return false;
        }

        // Se usó, se consumió un uso, y todavía queda vida útil.
        return true;
    }
}