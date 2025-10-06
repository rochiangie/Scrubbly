using UnityEngine;

public class DirtSpot : MonoBehaviour
{
    // [Header("Configuración de Limpieza")] // Opcional
    [SerializeField] private string requiredToolId = "";
    [SerializeField] public float dirtHealth = 3.0f;

    // Bandera para evitar doble conteo si se golpea varias veces después de morir
    private bool isCleaned = false;

    public string RequiredToolId => requiredToolId;

    // ===============================================
    // REGISTRO EN EL MANAGER
    // ===============================================

    void Start()
    {
        // 🛑 CRÍTICO: Registrarse en el DirtManager al inicio
        if (DirtManager.Instance != null)
        {
            DirtManager.Instance.RegisterDirtItem();
        }
        else
        {
            Debug.LogError("[DIRTSPOT] DirtManager no encontrado. El conteo de progreso no funcionará para este objeto.");
        }
    }

    // ===============================================
    // LÓGICA DE HERRAMIENTAS
    // ===============================================

    public bool CanBeCleanedBy(string toolId)
        => string.IsNullOrEmpty(requiredToolId) || requiredToolId == toolId;

    // ===============================================
    // LÓGICA DE DAÑO Y DESTRUCCIÓN
    // ===============================================

    public void CleanHit(float damage)
    {
        if (isCleaned) return; // Ya está limpio, ignorar golpes adicionales

        dirtHealth -= damage;
        // Debug.Log($"[DIRT STATUS] {name} recibió {damage:F2} de daño. Vida restante: {dirtHealth:F2}");

        if (dirtHealth <= 0f)
        {
            // 1. Notificar al gestor ANTES de destruirse
            if (DirtManager.Instance != null)
            {
                // 🛑 CRÍTICO: Informar al Manager que se ha limpiado un objeto
                DirtManager.Instance.CleanDirtItem();
            }

            // 2. Marcar como limpio y evitar más notificaciones
            isCleaned = true;

            // 3. Destruir el objeto
            // Debug.Log($"[DIRT] {name} limpio! Destruyendo objeto.");
            Destroy(gameObject);
        }
    }
}