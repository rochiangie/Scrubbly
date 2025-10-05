using UnityEngine;
using UnityEngine.SceneManagement; // Necesario si usas SceneManager en el futuro

public class GameDataController : MonoBehaviour
{
    // Hacemos que sea un Singleton estático
    public static GameDataController Instance;

    // Esta variable guarda el ID del personaje seleccionado entre escenas
    public string SelectedCharacterID = "Default_Character";

    private void Awake()
    {
        // === Implementación del Singleton Persistente ===
        if (Instance == null)
        {
            Instance = this;
            // ¡CRÍTICO! Mantiene el GameObject vivo al cambiar de escena.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Si ya existe otra instancia, nos destruimos
            Destroy(gameObject);
        }
        // ====================================
    }

    /// <summary>
    /// Llamado desde la escena de selección para guardar el personaje elegido.
    /// </summary>
    public void SetSelectedCharacter(string characterID)
    {
        SelectedCharacterID = characterID;
        Debug.Log($"[PERSISTENCE] Personaje seleccionado y guardado: {characterID}");
    }
}