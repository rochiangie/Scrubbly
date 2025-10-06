using UnityEngine;

public class CharacterSelection : MonoBehaviour
{
    // Hacemos que la instancia sea privada para seguir el patrón Singleton
    public static CharacterSelection Instance { get; private set; }

    // El ID (Nombre) del personaje seleccionado
    public string SelectedCharacterID { get; private set; } = string.Empty;

    // Eliminamos SelectedIndex, SelectedPrefab y PersistedInstance

    void Awake()
    {
        // Implementación estándar del Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 🛑 CRÍTICO: Mantenemos la persistencia del controlador, NO del personaje.
        DontDestroyOnLoad(gameObject);
    }

    // Eliminamos SetSelected(), SetSelectedFromExisting(), DestroyPersistedIfAny()
    // y GetOrSpawn() ya que esa lógica ahora debe estar en el SelectedCharacterLoader.

    /// <summary>
    /// Guarda el ID (nombre) del personaje seleccionado.
    /// Esta es la función que debe llamar SpotlightSelector.
    /// </summary>
    public void SetSelectedID(string characterID)
    {
        SelectedCharacterID = characterID;
        Debug.Log($"[SelectionController] ID Guardado: {characterID}");
    }

    /// <summary>
    /// Devuelve el ID guardado. Usado por SelectedCharacterLoader.
    /// </summary>
    public string GetSelectedID()
    {
        return SelectedCharacterID;
    }
}