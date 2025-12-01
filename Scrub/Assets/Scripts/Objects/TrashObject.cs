using UnityEngine;

public class TrashObject : MonoBehaviour
{
    [Header("Configuración de Basura")]
    public string trashName;

    [Header("Efectos de Destrucción")]
    public AudioClip destroySound;
    public GameObject destructionParticlesPrefab;
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
        // TaskManager ya escanea todos los objetos en su Start()
    }

    public void EliminateTrash()
    {
        Debug.Log($"🆘 EliminateTrash() llamado en {trashName}");
        CleanTrash();
    }

    public void CleanTrash()
    {
        if (IsCleaned)
        {
            Debug.LogWarning($"⚠️ {trashName} ya estaba limpiado");
            return;
        }

        IsCleaned = true;

        // 1. Notificar al TaskManager ANTES de destruir (capturando el tag primero)
        if (TaskManager.Instance != null)
        {
            // Pasar el tag directamente para que TaskManager pueda actualizar el contador correcto
            TaskManager.Instance.NotifyTrashCleanedWithTag(trashName, gameObject.tag);
            Debug.Log($"✅ Notificado TaskManager: {trashName} (Tag: {gameObject.tag})");
        }

        // 2. Reproducir Sonido
        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, transform.position);
        }

        // 3. Instanciar Partículas
        if (destructionParticlesPrefab != null)
        {
            Instantiate(destructionParticlesPrefab, transform.position, Quaternion.identity);
        }

        // 4. Desactivar Componentes de Interacción
        if (trashRenderer != null) trashRenderer.enabled = false;
        if (trashCollider != null) trashCollider.enabled = false;

        // 5. Destruir
        Destroy(gameObject, destroyDelay);
    }

    void OnMouseDown()
    {
        Debug.Log($"🗑️ TrashObject: {trashName}, Limpiado: {IsCleaned}");
    }
}