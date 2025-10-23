using UnityEngine;

public class HeldItemSlot : MonoBehaviour
{
    // --- REFERENCIAS PÚBLICAS (YA NO SE USAN PARA EQUIPAMIENTO RÁPIDO) ---
    // 🚨 Puedes eliminar tool1Prefab y tool2Prefab si no los necesitas para nada más.
    [Header("Tool Prefabs para Equipamiento Rápido")]
    public GameObject tool1Prefab; // Dejamos por si los quieres usar para el spawn
    public GameObject tool2Prefab; // Dejamos por si los quieres usar para el spawn

    // 🚨 YA NO NECESITAMOS ESTA VARIABLE AQUÍ, SE PASA POR PARÁMETRO
    // public Transform handSocket; 

    // --- DECLARACIONES PRIVADAS CRÍTICAS ---
    private GameObject currentToolObject;
    private ToolDescriptor currentToolDescriptor;
    private Transform currentHandSocket; // Mantiene la referencia al socket activo

    // --- PROPIEDADES PÚBLICAS (Para que PlayerInteraction acceda) ---
    public ToolDescriptor CurrentTool => currentToolDescriptor;
    public bool HasTool => currentToolObject != null;

    // ... (Start() permanece vacío o con tu lógica de inicialización) ...

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

    // 🚨 ELIMINAMOS COMPLETAMENTE EquipQuickTool() 🚨

    // =========================================================================
    // DESTRUCCIÓN (Corregida)
    // =========================================================================

    // EN HeldItemSlot.cs

    public void DestroyCurrentTool()
    {
        if (currentToolObject != null)
        {
            // Asegurarse de que el objeto destruido no sea el componente HeldItemSlot
            Destroy(currentToolObject);
        }

        // Limpiar siempre las referencias
        currentToolObject = null;
        currentToolDescriptor = null;
        currentHandSocket = null;
    }
}