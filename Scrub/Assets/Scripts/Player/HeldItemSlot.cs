using UnityEngine;

public class HeldItemSlot : MonoBehaviour
{
    // --- REFERENCIAS PÚBLICAS (YA NO SE USAN PARA EQUIPAMIENTO RÁPIDO) ---
    [Header("Tool Prefabs para Equipamiento Rápido")]
    public GameObject tool1Prefab; 
    public GameObject tool2Prefab; 

    // --- DECLARACIONES PRIVADAS CRÍTICAS ---
    private GameObject currentToolObject;
    private ToolDescriptor currentToolDescriptor;
    private Transform currentHandSocket; // Mantiene la referencia al socket activo

    // --- PROPIEDADES PÚBLICAS (Para que PlayerInteraction acceda) ---
    public ToolDescriptor CurrentTool => currentToolDescriptor;
    public bool HasTool => currentToolObject != null;

    // =========================================================================
    // EQUIPAMIENTO: Recibe el prefab a instanciar Y el punto donde instanciar.
    // =========================================================================

    /// <summary>
    /// Recibe el prefab a instanciar Y el punto de la mano (handSocket).
    /// </summary>
    public void EquipToolPrefab(GameObject toolPrefabToInstantiate, Transform targetHandSocket)
    {
        DestroyCurrentTool();

        currentHandSocket = targetHandSocket;

        currentToolObject = Instantiate(toolPrefabToInstantiate, currentHandSocket);
        currentToolObject.transform.localPosition = Vector3.zero;
        currentToolObject.transform.localRotation = Quaternion.identity;

        currentToolDescriptor = currentToolObject.GetComponent<ToolDescriptor>() ?? currentToolObject.GetComponentInParent<ToolDescriptor>();

        if (currentToolDescriptor == null)
        {
            Debug.LogError($"HeldItemSlot: El objeto instanciado ({toolPrefabToInstantiate.name}) NO tiene ToolDescriptor. El sistema de interacción fallará.");
        }
    }

    // ✅ NUEVO: Equipar una herramienta que YA existe en la escena (recogida del suelo)
    public void EquipExistingTool(GameObject toolObject, Transform targetHandSocket)
    {
        DestroyCurrentTool(); // Destruir lo anterior si había algo

        currentHandSocket = targetHandSocket;
        currentToolObject = toolObject;

        // Parentar al socket
        currentToolObject.transform.SetParent(currentHandSocket);
        currentToolObject.transform.localPosition = Vector3.zero;
        currentToolObject.transform.localRotation = Quaternion.identity;

        // Desactivar físicas si las tiene
        Rigidbody rb = currentToolObject.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        Collider col = currentToolObject.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        currentToolDescriptor = currentToolObject.GetComponent<ToolDescriptor>() ?? currentToolObject.GetComponentInParent<ToolDescriptor>();
    }

    public void DestroyCurrentTool()
    {
        if (currentToolObject != null)
        {
            // 🚨 Destrucción de la Tool en la escena
            Destroy(currentToolObject);
        }

        // 🚨 Limpiamos TODAS las referencias
        currentToolObject = null;
        currentToolDescriptor = null;
        
        Debug.Log("HeldItemSlot: Herramienta destruida y referencias limpiadas.");
    }
}