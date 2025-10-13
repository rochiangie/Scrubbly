using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections; // Necesario para el Coroutine

public class LoreDisplay : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el componente TextMeshPro del panel de Lore.")]
    public TMP_Text loreTextComponent;

    // Estructura serializable para ingresar los datos en el Inspector
    [System.Serializable]
    public class CharacterLore
    {
        [Tooltip("El ID exacto del personaje (ej: '9', '7'). Debe coincidir con el CharacterSelection.")]
        public string characterID;

        [TextArea(3, 10)]
        public string loreText;
    }

    [Header("Datos de la Historia")]
    public CharacterLore[] allCharacterLores;

    private const float LORE_LOAD_DELAY = 0.1f; // Pequeño retraso para asegurar que los Singletons estén listos.

    void Start()
    {
        // Iniciamos el proceso de carga con un pequeño retraso.
        StartCoroutine(LoadLoreWithDelay());
    }

    private IEnumerator LoadLoreWithDelay()
    {
        // Espera de un frame o un tiempo mínimo para asegurar que todos los Singletons (como CharacterSelection) hayan terminado su Awake().
        yield return new WaitForSeconds(LORE_LOAD_DELAY);

        // 1. Verificar referencias
        if (loreTextComponent == null)
        {
            Debug.LogError("[LORE DISPLAY] Error: El componente TMP_Text no está asignado en el Inspector.");
            yield break;
        }

        // 2. Obtener el ID del personaje seleccionado (Usamos el Singleton de selección)
        string selectedID = GetSelectedCharacterID();

        if (string.IsNullOrEmpty(selectedID))
        {
            loreTextComponent.text = "Error al cargar la historia. El ID de personaje no está disponible.";
            Debug.LogError("[LORE DISPLAY] Error: CharacterSelection no tiene un ID persistente guardado.");
            yield break;
        }

        Debug.Log($"[LORE DISPLAY] Buscando historia para ID: {selectedID}");

        // 3. Buscar y mostrar la historia
        string lore = GetLoreForCharacter(selectedID);
        loreTextComponent.text = lore;
    }

    // --- LÓGICA DE OBTENCIÓN DE ID ---

    private string GetSelectedCharacterID()
    {
        // CRITICAL FIX: Cambiamos la referencia de GameDataController (desconocido) a CharacterSelection (Singleton conocido).
        if (CharacterSelection.Instance != null)
        {
            return CharacterSelection.Instance.selectedCharacterID;
        }

        // Fallback: Intentar obtener desde PlayerPrefs si el Singleton aún no está listo.
        // Asumiendo que CharacterSelection utiliza la misma clave "SelectedCharacter"
        return PlayerPrefs.GetString("SelectedCharacter", "");
    }

    // --- LÓGICA DE BÚSQUEDA DE LORE ---

    private string GetLoreForCharacter(string id)
    {
        foreach (var charLore in allCharacterLores)
        {
            // Nota: La comparación de strings DEBE ser exacta. '9' != ' 9' (con espacio).
            if (charLore.characterID == id)
            {
                Debug.Log("[LORE DISPLAY] ✅ Historia encontrada y cargada.");
                return charLore.loreText;
            }
        }

        // Texto de Fallback si no se encuentra el ID
        Debug.LogWarning($"[LORE DISPLAY] ⚠️ Fallback: No se encontró historia para el ID: {id}");
        return $"[ID: {id}] Historia no encontrada. Usa este espacio para reflexionar antes de la limpieza.";
    }
}