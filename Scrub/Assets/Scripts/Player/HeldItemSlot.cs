using UnityEngine;

// [Código de HeldItemSlot.cs (Modificado)]

public class HeldItemSlot : MonoBehaviour
{
    // --- REFERENCIAS PARA EQUIPAMIENTO RÁPIDO (Asignar en Inspector) ---

    [Header("Tool Prefabs para Equipamiento Rápido")]
    // 🚨 PASO CRÍTICO: Asigna los prefabs de las 2 herramientas aquí.
    // Usamos ToolDescriptor para asegurar que el prefab tenga el componente.
    public ToolDescriptor tool1DescriptorPrefab; // Ejemplo: CleaningTool Prefab
    public ToolDescriptor tool2DescriptorPrefab; // Ejemplo: CleanTool Prefab

    [Header("Socket")]
    // Transform que actúa como la 'mano' del jugador
    public Transform handSocket;

    private GameObject currentToolObject;
    private ToolDescriptor currentToolDescriptor;

    // --- PROPIEDADES PÚBLICAS ---

    public ToolDescriptor CurrentTool => currentToolDescriptor;
    public bool HasTool => currentToolObject != null;

    void Start()
    {
        // Se inicializa handSocket si es necesario (lógica de tu proyecto)
        if (handSocket == null)
        {
            handSocket = transform.Find("HoldPoint");
            if (handSocket == null)
            {
                handSocket = new GameObject("HeldItemSocket").transform;
                handSocket.SetParent(transform);
                handSocket.localPosition = new Vector3(0, 1.5f, 0.5f);
            }
        }
    }

    /// <summary>
    /// Método para recoger una herramienta del mundo (llamado desde PlayerInteraction.TryPickupOrDrop).
    /// </summary>
    public void EquipToolPrefab(GameObject toolPrefabToInstantiate)
    {
        DestroyCurrentTool();

        // Instanciar en handSocket.
        currentToolObject = Instantiate(toolPrefabToInstantiate, handSocket);
        currentToolObject.transform.localPosition = Vector3.zero;
        currentToolObject.transform.localRotation = Quaternion.identity;

        // Obtener el descriptor.
        currentToolDescriptor = currentToolObject.GetComponent<ToolDescriptor>() ?? currentToolObject.GetComponentInParent<ToolDescriptor>();

        if (currentToolDescriptor == null)
        {
            Debug.LogError($"HeldItemSlot: El objeto instanciado ({toolPrefabToInstantiate.name}) no tiene ToolDescriptor.");
        }
    }

    /// <summary>
    /// 🆕 MÉTODO CORREGIDO: Equipa la herramienta asociada al índice (1 o 2).
    /// </summary>
    public void EquipQuickTool(int index)
    {
        ToolDescriptor targetDescriptor = null;

        if (index == 1)
        {
            targetDescriptor = tool1DescriptorPrefab;
        }
        else if (index == 2)
        {
            targetDescriptor = tool2DescriptorPrefab;
        }

        if (targetDescriptor == null)
        {
            Debug.LogWarning($"HeldItemSlot: El Prefab para el índice {index} está sin asignar o el índice es inválido.");
            return;
        }

        // Si ya tengo esa herramienta equipada, la desequipo (función de toggle)
        if (currentToolDescriptor != null && currentToolDescriptor.ToolId == targetDescriptor.ToolId)
        {
            DestroyCurrentTool();
            return;
        }

        // Equipamos la herramienta instanciando su GameObject (el Prefab).
        EquipToolPrefab(targetDescriptor.gameObject);
        Debug.Log($"HeldItemSlot: Herramienta {index} equipada: {targetDescriptor.name}");
    }


    /// <summary>
    /// Método para destruir la herramienta actual (Soltar o Cambiar).
    /// </summary>
    public void DestroyCurrentTool()
    {
        if (currentToolObject != null)
        {
            // Nota: Usar DestroyImmediate si estás en modo edición, pero para runtime 'Destroy' es correcto.
            Destroy(currentToolObject);
            currentToolObject = null;
            currentToolDescriptor = null;
        }
    }
}