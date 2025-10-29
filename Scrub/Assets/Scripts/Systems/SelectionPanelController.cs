using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectionPanelController : MonoBehaviour
{
    // ===================================
    // Referencias de UI (Arrastrar en el Inspector)
    // ===================================
    [Header("Referencias de Texto")]
    [Tooltip("Texto fijo, p.ej., el nombre del juego o un título.")]
    public TextMeshProUGUI fixedText;

    [Tooltip("Texto variable, p.ej., el Lore del personaje (se actualiza con SpotlightSelector).")]
    public TextMeshProUGUI changingText;

    [Header("Referencia de Botón")]
    [Tooltip("El componente Button. Se usará para llamar a la función de selección.")]
    public Button selectionButton;

    // ===================================
    // Referencias de Flujo de Juego
    // ===================================
    [Header("Flujo de Juego")]
    [Tooltip("Referencia al SpotlightSelector para activar la acción de Confirmar.")]
    public SpotlightSelector spotlightSelector;

    // ===================================
    // Lógica del Panel
    // ===================================

    void Start()
    {
        // 1. Verificaciones de referencias
        if (fixedText == null || changingText == null || selectionButton == null || spotlightSelector == null)
        {
            Debug.LogError("🚨 SelectionPanelController: Faltan referencias de UI o del SpotlightSelector en el Inspector.");
        }

        // 2. Configuración del Texto Fijo (ejemplo)
        // Puedes cambiar este texto en el Inspector o aquí si lo deseas.
        if (fixedText != null)
        {
            fixedText.text = "Selección de Personaje";
        }

        // 3. Asignar la función de confirmación al botón
        // Esto asegura que al hacer clic en el botón se ejecute la misma lógica que al presionar Enter.
        if (selectionButton != null)
        {
            // Limpiamos cualquier listener anterior y añadimos el nuestro.
            selectionButton.onClick.RemoveAllListeners();
            selectionButton.onClick.AddListener(OnSelectionButtonClicked);
        }
    }

    /// <summary>
    /// Función llamada cuando se hace clic en el botón.
    /// Ejecuta la misma lógica de Confirmar que al presionar ENTER.
    /// </summary>
    public void OnSelectionButtonClicked()
    {
        if (spotlightSelector != null)
        {
            // Llama a la función que confirma la selección y carga la escena.
            spotlightSelector.Confirm();
        }
        else
        {
            Debug.LogError("SpotlightSelector no está referenciado para la confirmación.");
        }
    }
}