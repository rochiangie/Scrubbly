using UnityEngine;

public class DirtSpot : MonoBehaviour
{
    // ===============================================
    //               VARIABLES PÚBLICAS
    // ===============================================

    [Header("Efecto de Destrucción")]
    [Tooltip("Prefab del sistema de partículas que se instanciará al destruirse.")]
    public GameObject destructionEffectPrefab;

    // ===============================================
    //               CONFIGURACIÓN DE SALUD Y REQUISITOS
    // ===============================================

    [Header("Salud y Requisitos")]
    [Tooltip("La vida máxima que tiene la suciedad.")]
    [SerializeField]
    private float maxHealth = 10f; // VALOR BAJO PARA PRUEBAS (10f)

    [Tooltip("El ID de la herramienta requerida para limpiar esta suciedad (ej. 'Sponge', 'Scrubber').")]
    [SerializeField]
    private string requiredToolId = "Sponge";

    private float currentHealth;

    // ===============================================
    //               MÉTODOS DE UNITY
    // ===============================================

    void Awake()
    {
        // Inicializa la vida del objeto
        currentHealth = maxHealth;
    }

    // ===============================================
    //               LÓGICA DE LIMPIEZA
    // ===============================================

    /// <summary>
    /// Devuelve TRUE si la herramienta con el 'toolId' proporcionado puede limpiar este objeto.
    /// </summary>
    public bool CanBeCleanedBy(string toolId)
    {
        if (string.IsNullOrEmpty(requiredToolId))
        {
            return true;
        }

        return requiredToolId == toolId;
    }

    /// <summary>
    /// Aplica el daño de limpieza al objeto de suciedad.
    /// (Llamado desde CleaningController.cs)
    /// </summary>
    public void CleanHit(float damage)
    {
        // 1. Aplica el daño
        currentHealth -= damage;
        // DLog eliminado aquí.

        // 2. Comprueba si debe destruirse
        if (currentHealth <= 0)
        {
            HandleDestruction();
        }
    }

    // ===============================================
    //             DESTRUCCIÓN (CON PARTÍCULAS)
    // ===============================================

    /// <summary>
    /// Maneja la destrucción del objeto, incluyendo la instanciación de partículas.
    /// </summary>
    private void HandleDestruction()
    {
        // 1. INSTANCIAR EL EFECTO DE PARTÍCULAS
        if (destructionEffectPrefab != null)
        {
            // Instancia el efecto en la posición del objeto antes de destruirlo.
            // El Prefab de partículas debe tener Stop Action = Destroy para auto-eliminarse.
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);

            // DLog eliminado aquí.
        }

        // 2. DESTRUIR EL OBJETO ACTUAL (LA SUCIEDAD)
        // DLog eliminado aquí.
        Destroy(gameObject);
    }
}