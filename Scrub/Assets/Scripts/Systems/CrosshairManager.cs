using UnityEngine;
using UnityEngine.UI;

public class CrosshairManager : MonoBehaviour
{
    [Header("Configuración de Mira")]
    [Tooltip("Tamaño del punto central")]
    public float dotSize = 5f;
    [Tooltip("Color del punto central")]
    public Color dotColor = new Color(1f, 1f, 1f, 0.8f); // Blanco semi-transparente
    
    private Image crosshairImage;

    void Start()
    {
        CreateCrosshair();
    }

    void CreateCrosshair()
    {
        // 1. Buscar el Canvas principal
        Canvas canvas = FindObjectOfType<Canvas>();
        
        // Si no hay Canvas, crear uno básico
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UI_Canvas_Auto");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            Debug.Log("[CrosshairManager] Se creó un Canvas automático porque no se encontró uno.");
        }

        // 2. Verificar si ya existe una mira (para no duplicar)
        Transform existingCrosshair = canvas.transform.Find("AutoCrosshair");
        if (existingCrosshair != null)
        {
            crosshairImage = existingCrosshair.GetComponent<Image>();
            return;
        }

        // 3. Crear el objeto de la mira
        GameObject crosshairObj = new GameObject("AutoCrosshair");
        crosshairObj.transform.SetParent(canvas.transform, false);

        // 4. Configurar la imagen
        crosshairImage = crosshairObj.AddComponent<Image>();
        crosshairImage.color = dotColor;
        
        // Usar un sprite de círculo si es posible, si no, será un cuadrado por defecto (que al ser pequeño parece un punto)
        // Unity por defecto tiene un sprite "Knob" o "UISprite", pero no podemos cargarlo fácilmente por código sin Resources.
        // Así que usamos el cuadrado por defecto (null sprite) que funciona bien como punto pixel.

        // 5. Centrar en la pantalla
        RectTransform rect = crosshairImage.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f); // Centro
        rect.anchorMax = new Vector2(0.5f, 0.5f); // Centro
        rect.pivot = new Vector2(0.5f, 0.5f);     // Pivote en el centro
        rect.anchoredPosition = Vector2.zero;     // Posición 0,0
        rect.sizeDelta = new Vector2(dotSize, dotSize); // Tamaño

        // Asegurar que no bloquee los clicks del mouse
        crosshairImage.raycastTarget = false;

        Debug.Log("[CrosshairManager] Mira (punto blanco) creada exitosamente en el centro de la pantalla.");
    }
}
