using UnityEngine;
using TMPro; // Asegúrate de que esta librería exista en tu proyecto.
using System.Linq; // Necesario si quieres usar consultas LINQ, pero no es estrictamente necesario para la lógica actual.

public class LoreDisplay : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el componente TextMeshPro del panel de Lore.")]
    public TMP_Text loreTextComponent;

    // Estructura serializable para ingresar los datos en el Inspector
    [System.Serializable]
    public class CharacterLore
    {
        [Tooltip("El ID exacto del personaje (ej: '9', '7'). Debe coincidir con el GameDataController.")]
        public string characterID;

        [TextArea(3, 10)]
        public string loreText;
    }

    [Header("Datos de la Historia")]
    public CharacterLore[] allCharacterLores;

    void Start()
    {
        // 1. Verificar referencias (Es vital que el TMP_Text NO esté nulo)
        if (loreTextComponent == null)
        {
            Debug.LogError("[LORE DISPLAY] Error: El componente TMP_Text no está asignado.");
            return;
        }

        // 2. Obtener el ID del personaje seleccionado (usa GameDataController, que es persistente)
        string selectedID = GameDataController.Instance.GetCharacterID();

        if (string.IsNullOrEmpty(selectedID))
        {
            loreTextComponent.text = "Error al cargar la historia. El ID de personaje no está disponible.";
            Debug.LogError("[LORE DISPLAY] Error: GameDataController no tiene un ID persistente.");
            return;
        }

        // 3. Buscar y mostrar la historia
        string lore = GetLoreForCharacter(selectedID);
        loreTextComponent.text = lore;
    }

    private string GetLoreForCharacter(string id)
    {
        foreach (var charLore in allCharacterLores)
        {
            if (charLore.characterID == id)
            {
                return charLore.loreText;
            }
        }

        // Texto de Fallback si no se encuentra el ID
        return $"[ID: {id}] Historia no encontrada. Usa este espacio para reflexionar antes de la limpieza.";
    }
}