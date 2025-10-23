using UnityEngine;

// IMPORTANTE: Asumiendo que 'CleaningTool' y 'CleanTool' tienen un método 'Use()'.
// Si no lo tienen o necesitas una llamada más específica (ej. 'StartCleaning', 'StartWiping'),
// debes ajustar el método 'UseActiveTool'.

public class PlayerToolHandler : MonoBehaviour
{
    // Define los tipos de herramientas que el jugador puede tener.
    public enum ToolType { None, Cleaning, Clean }

    [Header("Tool Prefabs")]
    // Asigna tus prefabs de CleaningTool y CleanTool en el Inspector.
    public CleaningTool cleaningToolPrefab; // Scripts/Cleaning/CleaningTool.cs 
    public CleanTool cleanToolPrefab;       // Scripts/Cleaning/CleanTool.cs 

    [Header("Socket")]
    // El punto de la mano/cuerpo donde se instanciará la herramienta.
    public Transform heldItemSocket;

    // El componente de la herramienta actualmente equipada.
    private MonoBehaviour currentTool;
    private ToolType activeToolType = ToolType.None;


    /// <summary>
    /// Intenta equipar la herramienta especificada. Si ya está equipada, la desequipa.
    /// Esto funciona como un 'toggle' (activar/desactivar).
    /// </summary>
    /// <param name="toolToEquip">El tipo de herramienta a equipar.</param>
    public void EquipTool(ToolType toolToEquip)
    {
        // 1. Si la herramienta ya está activa, la desequipamos (toggle off)
        if (activeToolType == toolToEquip)
        {
            UnequipTool();
            return;
        }

        // 2. Desequipar cualquier herramienta anterior
        if (currentTool != null)
        {
            UnequipTool();
        }

        // 3. Seleccionar el prefab y equipar la nueva herramienta
        GameObject prefabToInstantiate = null;

        switch (toolToEquip)
        {
            case ToolType.Cleaning:
                if (cleaningToolPrefab != null)
                    prefabToInstantiate = cleaningToolPrefab.gameObject;
                break;
            case ToolType.Clean:
                if (cleanToolPrefab != null)
                    prefabToInstantiate = cleanToolPrefab.gameObject;
                break;
            default:
                Debug.LogError("Intentando equipar un ToolType no soportado: " + toolToEquip);
                return;
        }

        if (prefabToInstantiate != null && heldItemSocket != null)
        {
            // Instanciar el objeto como hijo del socket (la mano)
            GameObject newToolObject = Instantiate(prefabToInstantiate, heldItemSocket);

            // Asegurar posición y rotación relativas (para que se alinee con el socket)
            newToolObject.transform.localPosition = Vector3.zero;
            newToolObject.transform.localRotation = Quaternion.identity;

            // Almacenar la referencia del componente principal
            if (toolToEquip == ToolType.Cleaning)
            {
                currentTool = newToolObject.GetComponent<CleaningTool>();
            }
            else if (toolToEquip == ToolType.Clean)
            {
                currentTool = newToolObject.GetComponent<CleanTool>();
            }

            activeToolType = toolToEquip;
            Debug.Log("Tool Equipped: " + toolToEquip);
        }
        else
        {
            // Si falta el prefab o el socket
            Debug.LogWarning("Cannot equip tool. Prefab or Held Item Socket is missing.");
        }
    }

    /// <summary>
    /// Elimina la herramienta actualmente equipada.
    /// </summary>
    public void UnequipTool()
    {
        if (currentTool != null)
        {
            Destroy(currentTool.gameObject);
            currentTool = null;
            activeToolType = ToolType.None;
            Debug.Log("Tool Unequipped.");
        }
    }

    /// <summary>
    /// Llama a la función 'Use()' en la herramienta activa.
    /// </summary>
    public void UseActiveTool()
    {
        if (currentTool == null)
        {
            Debug.Log("No tool equipped to use.");
            return;
        }

        // Llamar al método 'Use()' a través de la interfaz genérica (si existe) 
        // o mediante un casting explícito si el script lo permite.

        // Usaremos la reflexión simple por ahora, asumiendo que tienen el método:
        currentTool.SendMessage("Use", SendMessageOptions.DontRequireReceiver);
    }
}