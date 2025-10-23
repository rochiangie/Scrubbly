using UnityEngine;

// ⚠️ Adjuntar este script a un GameObject vacío en la escena de la UI.
public class ToolSceneManager : MonoBehaviour
{
    // Método que los botones de la UI deben llamar.
    // Este método ya no es estático y es fácil de encontrar.
    public void SelectTool(GameObject toolPrefab)
    {
        // Verificar el Singleton (Jugador persistente)
        if (HeldItemSlot.Instance == null)
        {
            Debug.LogError("ToolSceneManager: HeldItemSlot (Jugador) no está activo en el juego. No se puede equipar la herramienta.");
            return;
        }

        // Redirigir la llamada a la instancia viva y correcta del Jugador.
        HeldItemSlot.Instance.EquipToolPrefab(toolPrefab);
    }
}