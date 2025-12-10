using UnityEngine;
using System.Collections.Generic;

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
    private Collider[] trashColliders; // Array para guardar todos los colliders.

    void Awake()
    {
        trashRenderer = GetComponent<Renderer>();
        trashColliders = GetComponents<Collider>();
        alreadyNotified = false;

        if (string.IsNullOrEmpty(trashName))
            trashName = gameObject.name;
    }

    void Start()
    {
        // === MODIFICACIÓN CLAVE: Llamar a la función de debug aquí ===
        DebugTaggedObjects();
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

        // 1. Notificar al TaskManager ANTES de destruir
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.NotifyTrashCleaned(gameObject);
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

        // Desactivar todos los colliders
        if (trashColliders != null)
        {
            foreach (Collider col in trashColliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }

        // 5. Destruir
        Destroy(gameObject, destroyDelay);
    }

    // === FUNCIÓN DE DEBUGGING ===
    public void DebugTaggedObjects()
    {
        // --- 1. Buscar objetos con el tag "Vidrio" ---
        GameObject[] vidrioObjects = GameObject.FindGameObjectsWithTag("Vidrio");
        //Debug.Log($"--- 🔍 OBJETOS CON TAG: VIDRIO ({vidrioObjects.Length}) ---");
        foreach (GameObject go in vidrioObjects)
        {
            //Debug.Log($"[VIDRIO] Encontrado: {go.name} en posición: {go.transform.position}");
        }

        // --- 2. Buscar objetos con el tag "Peligroso" ---
        GameObject[] peligrosoObjects = GameObject.FindGameObjectsWithTag("Peligroso");
        //Debug.Log($"--- 🔍 OBJETOS CON TAG: PELIGROSO ({peligrosoObjects.Length}) ---");
        foreach (GameObject go in peligrosoObjects)
        {
            //Debug.Log($"[PELIGROSO] Encontrado: {go.name} en posición: {go.transform.position}");
        }

        if (vidrioObjects.Length == 0 && peligrosoObjects.Length == 0)
        {
            //Debug.LogWarning("⚠️ No se encontraron objetos con los tags 'Vidrio' o 'Peligroso'. Asegúrate de que los tags están definidos correctamente en Unity.");
        }
    }
    // ============================

    void OnMouseDown()
    {
        //Debug.Log($"🗑️ TrashObject: {trashName}, Limpiado: {IsCleaned}");
    }
}
