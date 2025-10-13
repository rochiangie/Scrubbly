using UnityEngine;
using UnityEngine.UI;
using TMPro; // Asegúrate de tener esta librería para el texto

public class SliderUpdater : MonoBehaviour
{
    private Slider progressSlider;

    [Header("Referencias (Opcional)")]
    [Tooltip("Componente TextMeshPro para mostrar el progreso como 'X/Y'.")]
    public TextMeshProUGUI progressText;

    void Awake()
    {
        progressSlider = GetComponent<Slider>();

        if (progressSlider == null)
        {
            Debug.LogError("[SliderUpdater] Error: No se encontró el componente Slider en este GameObject.");
        }

        progressSlider.minValue = 0f;
        progressSlider.maxValue = 1f;
        progressSlider.value = 0f; // Inicializa en 0.
    }

    void OnEnable()
    {
        // 🛑 CONEXIÓN CLAVE: Suscribirse al evento de GameEvents
        GameEvents.OnProgressUpdate += UpdateSlider;
        Debug.Log("[SliderUpdater] Suscrito al evento de GameEvents.OnProgressUpdate.");
    }

    void OnDisable()
    {
        // 🛑 DESUSCRIPCIÓN CLAVE
        GameEvents.OnProgressUpdate -= UpdateSlider;
    }

    /// <summary>
    /// Recibe los valores de limpieza del TaskManager a través de GameEvents.
    /// </summary>
    private void UpdateSlider(int cleaned, int total)
    {
        if (progressSlider == null || total <= 0) return;

        // Calcular el progreso (0.0 a 1.0)
        float progress = (float)cleaned / total;

        progressSlider.value = progress;

        if (progressText != null)
        {
            progressText.text = $"Limpieza: {cleaned} / {total}";
        }
    }
}