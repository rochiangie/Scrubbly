using UnityEngine;
using UnityEngine.SceneManagement;

public class GameResetManager : MonoBehaviour
{
    // Constantes usadas por otros Singletons
    private const string SELECTED_CHARACTER_KEY = "SelectedCharacter";
    private const string MUSIC_TOGGLE_KEY = "MusicMuted"; // Añadida para resetear el estado de mute

    [Header("Configuración de Reinicio")]
    [Tooltip("El índice de la escena del Menú Principal en las Build Settings (normalmente es 0).")]
    public int mainMenuSceneIndex = 0;

    /// <summary>
    /// Limpia los datos persistentes de la sesión (personaje, settings) y carga la escena del Menú Principal.
    /// Asigna esta función al evento OnClick() del botón de reinicio/menú.
    /// </summary>
    public void ReturnToMenuAndResetGame()
    {
        Debug.Log("[GAME RESET] 🔄 Iniciando el restablecimiento del juego y cargando menú.");

        // ===================================
        // 1. Limpiar Singletons (Datos en Memoria)
        // ===================================

        // Limpiar CharacterSelection: Es crucial para que el jugador pueda elegir uno nuevo.
        if (CharacterSelection.Instance != null)
        {
            CharacterSelection.Instance.selectedCharacterID = string.Empty;
            Debug.Log("[GAME RESET] CharacterSelection ID limpiado.");
        }

        // Limpiar AudioManager: Restablece la configuración si el AudioManager es persistente.
        if (AudioManager.Instance != null)
        {
            // Opcional: Asegurar que el audio empieza con el clip de menú.
            AudioManager.Instance.PlayMenuMusic();
            Debug.Log("[GAME RESET] AudioManager restablecido a música de menú.");
        }

        // ===================================
        // 2. Limpiar PlayerPrefs (Datos en Disco)
        // ===================================

        // Eliminar el ID del personaje seleccionado (para garantizar una nueva selección).
        if (PlayerPrefs.HasKey(SELECTED_CHARACTER_KEY))
        {
            PlayerPrefs.DeleteKey(SELECTED_CHARACTER_KEY);
            Debug.Log("[GAME RESET] PlayerPrefs: ID de personaje limpiado.");
        }

        // Eliminar el estado de mute/toggle (opcional, pero asegura que la música empiece ON).
        if (PlayerPrefs.HasKey(MUSIC_TOGGLE_KEY))
        {
            PlayerPrefs.DeleteKey(MUSIC_TOGGLE_KEY);
            Debug.Log("[GAME RESET] PlayerPrefs: Estado de mute de música limpiado.");
        }

        // Guardar todos los cambios al disco.
        PlayerPrefs.Save();

        // ===================================
        // 3. Cargar la Escena Inicial
        // ===================================

        // Utilizamos el índice público para cargar la primera escena.
        SceneManager.LoadScene(mainMenuSceneIndex);

        Debug.Log($"[GAME RESET] ✅ Juego reiniciado. Escena cargada: Índice {mainMenuSceneIndex}.");
    }
}