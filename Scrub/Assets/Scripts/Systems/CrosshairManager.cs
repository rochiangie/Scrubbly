using UnityEngine;

public class CrosshairManager : MonoBehaviour
{
    [Header("Configuración de Mira")]
    [Tooltip("Tamaño del punto central en pixeles")]
    public float size = 10f;
    [Tooltip("Color del punto central")]
    public Color color = Color.white;

    private Texture2D texture;
    private GUIStyle style;

    void Awake()
    {
        // Crear una textura blanca de 1x1 pixel
        texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();

        style = new GUIStyle();
        style.normal.background = texture;
    }

    void OnGUI()
    {
        // Calcular el centro de la pantalla
        float x = (Screen.width - size) / 2;
        float y = (Screen.height - size) / 2;

        // Dibujar el punto
        GUI.Box(new Rect(x, y, size, size), GUIContent.none, style);
    }
}
