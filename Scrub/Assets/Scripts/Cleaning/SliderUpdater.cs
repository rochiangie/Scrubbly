// SliderUpdater.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderUpdater : MonoBehaviour
{
    private Slider progressSlider;

    [Header("Referencias (Opcional)")]
    public TextMeshProUGUI progressText;

    void Awake()
    {
        progressSlider = GetComponent<Slider>();

        if (progressSlider == null)
        {
            Debug.LogError("[SliderUpdater] Error: No se encontró el componente Slider en este GameObject.");
        }

        progressSlider.minValue = 0f;

        // 🚨 REMOVIDO: Se elimina el hardcode de progressSlider.maxValue = 1f;
        // El valor máximo será establecido por el TaskManager.

        progressSlider.value = 0f; // Inicializa en 0.
    }

    void OnEnable()
    {
        GameEvents.OnProgressUpdate += UpdateSlider;
        Debug.Log("[SliderUpdater] Suscrito al evento de GameEvents.OnProgressUpdate.");
    }

    void OnDisable()
    {
        GameEvents.OnProgressUpdate -= UpdateSlider;
    }

    /// <summary>
    /// Recibe los valores de limpieza del TaskManager a través de GameEvents y usa valores absolutos.
    /// </summary>
    private void UpdateSlider(int cleaned, int total)
    {
        if (progressSlider == null || total <= 0) return;

        // 🚨 CORRECCIÓN CLAVE: Usar los valores absolutos del TaskManager 🚨

        // 1. Establece el valor máximo al Total General (111)
        progressSlider.maxValue = total;

        // 2. Establece el valor actual al total limpiado (82)
        progressSlider.value = cleaned;

        // Opcional: El cálculo de progreso de 0.0 a 1.0 ya no es necesario aquí.
        // float progress = (float)cleaned / total; 

        if (progressText != null)
        {
            // Muestra los números absolutos: 82 / 111
            progressText.text = $"Limpieza: {cleaned} / {total}";
        }
    }
}