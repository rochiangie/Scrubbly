using UnityEngine;
using UnityEngine.UI;

public class SliderUpdater : MonoBehaviour
{
    private Slider progressSlider;

    void Start() // Cambié de Awake a Start, por si el DirtManager se inicializa después.
    {
        progressSlider = GetComponent<Slider>();

        // 🛑 ESTA ES LA CONEXIÓN CLAVE QUE FALTA POR CÓDIGO 🛑
        if (DirtManager.Instance != null && progressSlider != null)
        {
            // Suscribe la función UpdateSliderValue al evento del DirtManager.
            DirtManager.Instance.OnProgressUpdated.AddListener(UpdateSliderValue);

            Debug.Log("[SliderUpdater] Suscrito al evento de progreso de limpieza.");
        }
        else
        {
            if (DirtManager.Instance == null) Debug.LogError("SliderUpdater no encontró el DirtManager.");
        }

        // Inicializa el Slider (opcional, el manager lo hace, pero es buena práctica)
        progressSlider.minValue = 0f;
        progressSlider.maxValue = 1f;
    }

    // La función que recibe el valor del manager.
    public void UpdateSliderValue(float progress)
    {
        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }
    }

    // Asegúrate de remover el Listener al destruir el objeto
    private void OnDestroy()
    {
        if (DirtManager.Instance != null)
        {
            DirtManager.Instance.OnProgressUpdated.RemoveListener(UpdateSliderValue);
        }
    }
}