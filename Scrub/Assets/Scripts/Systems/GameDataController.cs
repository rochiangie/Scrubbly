using UnityEngine;
using UnityEngine.SceneManagement;

public class GameDataController : MonoBehaviour
{
    // Singleton para acceso global
    public static GameDataController Instance;

    // Esta variable guarda el ID del personaje seleccionado.
    private string selectedCharacterID = "1"; // Valor por defecto

    // Referencia al panel de notificación (debe asignarse en el Inspector)
    public TimedUIPanel notificationPanel;


    private void Awake()
    {
        // === Implementación del Singleton Persistente ===
        if (Instance == null)
        {
            Instance = this;
            // Mantiene el GameObject vivo al cambiar de escena.
            DontDestroyOnLoad(gameObject); // ✅ CRÍTICO para persistencia
        }
        else
        {
            // Si ya existe otra instancia, nos destruimos
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Llama a la función del panel después de un pequeño retraso si es necesario
        if (notificationPanel != null)
        {
            notificationPanel.ShowAndHide();
        }
    }

    /// <summary>
    /// 🛑 FUNCIÓN RESTAURADA: Llamado desde SpotlightSelector.cs para guardar el personaje elegido.
    /// </summary>
    public void SetSelectedCharacter(string characterID)
    {
        selectedCharacterID = characterID;
        Debug.Log($"[PERSISTENCE] Personaje seleccionado y guardado: {selectedCharacterID}");
    }

    /// <summary>
    /// Llamado desde scripts como LoreDisplay y SelectedCharacterLoader para recuperar el ID guardado.
    /// </summary>
    public string GetCharacterID()
    {
        return selectedCharacterID;
    }
}