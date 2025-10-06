using UnityEngine;
using UnityEngine.SceneManagement;

public class GameDataController : MonoBehaviour
{
    // Singleton para acceso global
    public static GameDataController Instance;

    // Esta variable guarda el ID del personaje seleccionado. 
    // Debe coincidir con un ID en SelectedCharacterLoader.AvailableCharacters
    public string SelectedCharacterID = "1"; // Valor por defecto

    public TimedUIPanel notificationPanel;


    private void Awake()
    {
        // === Implementaci�n del Singleton Persistente ===
        if (Instance == null)
        {
            Instance = this;
            // �CR�TICO! Mantiene el GameObject vivo al cambiar de escena.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Si ya existe otra instancia, nos destruimos
            Destroy(gameObject);
        }
    }
    void Start()
    {
        // Llama a la funci�n del panel despu�s de un peque�o retraso si es necesario
        notificationPanel.ShowAndHide();
    }
    /// <summary>
    /// Llamado desde la escena de selecci�n para guardar el personaje elegido.
    /// </summary>
    public void SetSelectedCharacter(string characterID)
    {
        SelectedCharacterID = characterID;
        Debug.Log($"[PERSISTENCE] Personaje seleccionado y guardado: {characterID}");
    }
}