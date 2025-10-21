using UnityEngine;
using System.Collections;

public class TrashObject : MonoBehaviour
{
    [Header("Configuración de Basura")]
    public string trashName;

    [Header("Efectos de Destrucción")]
    // Usaremos estas variables para los efectos.
    public AudioClip destroySound;          // El clip de audio para el sonido de destrucción
    public GameObject destructionParticlesPrefab; // La Prefab del sistema de partículas
    public float destroyDelay = 0.1f;

    public bool IsCleaned { get; private set; } = false;

    private bool alreadyNotified = false;
    private Renderer trashRenderer;
    private Collider trashCollider;

    void Awake()
    {
        trashRenderer = GetComponent<Renderer>();
        trashCollider = GetComponent<Collider>();
        alreadyNotified = false;

        if (string.IsNullOrEmpty(trashName))
            trashName = gameObject.name;
    }

    void Start()
    {
        // ✅ Verificar que estamos en la lista del TaskManager
        if (TaskManager.Instance != null && !TaskManager.Instance.remainingItemNames.Contains(trashName))
        {
            Debug.LogWarning($"⚠️ TrashObject {trashName} no está en la lista del TaskManager. Agregando...");
            TaskManager.Instance.remainingItemNames.Add(trashName);
        }
    }

    /// <summary>
    /// Llamado cuando el jugador interactúa con esta basura
    /// </summary>
    public void EliminateTrash()
    {
        Debug.Log($"🆘 EliminateTrash() llamado en {trashName}");
        CleanTrash(); // Llama al método principal de limpieza
    }

    // ✅ MÉTODO PRINCIPAL DE LIMPIEZA
    public void CleanTrash()
    {
        if (IsCleaned)
        {
            Debug.LogWarning($"⚠️ {trashName} ya estaba limpiado");
            return;
        }

        IsCleaned = true;

        // 1. Notificar al TaskManager
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.NotifyTrashCleaned(trashName);
            Debug.Log($"✅ Notificado TaskManager: {trashName}");
        }

        // --- INICIO DE EFECTOS (SONIDO Y PARTÍCULAS) ---

        // 2. Reproducir Sonido
        if (destroySound != null)
        {
            // Reproduce el sonido una sola vez en la posición del objeto
            AudioSource.PlayClipAtPoint(destroySound, transform.position);
        }

        // 3. Instanciar Partículas
        if (destructionParticlesPrefab != null)
        {
            // Crea las partículas en la posición de la basura
            Instantiate(destructionParticlesPrefab, transform.position, Quaternion.identity);
        }

        // --- FIN DE EFECTOS ---

        // 4. Desactivar Componentes de Interacción
        // Esto previene interacciones adicionales mientras esperamos la destrucción
        if (trashRenderer != null) trashRenderer.enabled = false;
        if (trashCollider != null) trashCollider.enabled = false;

        // 5. Destruir
        // Usamos destroyDelay para permitir que los efectos se inicien antes de que el objeto desaparezca
        Destroy(gameObject, destroyDelay);
    }

    // ✅ Para debug
    void OnMouseDown()
    {
        Debug.Log($"🗑️ TrashObject: {trashName}, Limpiado: {IsCleaned}");
    }
}