using UnityEngine;
using System.Collections;

public class TrashObject : MonoBehaviour
{
    [Header("Configuración de Basura")]
    public string trashName;
    public bool IsCleaned { get; private set; } = false;

    [Header("Efectos")]
    public GameObject destructionEffectPrefab;

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
        CleanTrash(); // Llama al método que SÍ funciona
    }

    // ✅ MÉTODO ALTERNATIVO
    public void CleanTrash()
    {
        if (IsCleaned)
        {
            Debug.LogWarning($"⚠️ {trashName} ya estaba limpiado");
            return;
        }

        IsCleaned = true;

        // Notificar al TaskManager
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.NotifyTrashCleaned(trashName);
            Debug.Log($"✅ Notificado TaskManager: {trashName}");
        }

        // Efectos visuales
        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        // Desactivar y destruir
        if (trashRenderer != null) trashRenderer.enabled = false;
        if (trashCollider != null) trashCollider.enabled = false;

        Destroy(gameObject, 0.1f);
    }

    // ✅ Para debug
    void OnMouseDown()
    {
        Debug.Log($"🗑️ TrashObject: {trashName}, Limpiado: {IsCleaned}");
    }
}