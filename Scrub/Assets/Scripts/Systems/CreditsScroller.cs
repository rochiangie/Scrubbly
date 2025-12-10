using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsScroller : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Velocidad a la que sube el texto.")]
    [SerializeField] private float scrollSpeed = 50f;
    
    [Tooltip("Nombre de la escena del Menú Principal para volver al terminar.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Tooltip("Multiplicador de velocidad si el jugador mantiene presionado un botón.")]
    [SerializeField] private float fastForwardMultiplier = 5f;

    [Header("Referencias")]
    [Tooltip("El RectTransform que contiene todo el texto de los créditos.")]
    [SerializeField] private RectTransform creditsContent;

    [Tooltip("Punto Y (altura) donde se considera que los créditos terminaron. Ajusta esto según el largo de tu texto.")]
    [SerializeField] private float endYPosition = 2000f;

    private float currentSpeed;

    void Start()
    {
        // Si no se asignó manualmente, intentamos obtener el RectTransform del objeto actual
        if (creditsContent == null)
        {
            creditsContent = GetComponent<RectTransform>();
        }
        currentSpeed = scrollSpeed;
    }

    void Update()
    {
        // 1. Control de Velocidad (Mantener click o espacio para acelerar)
        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space))
        {
            currentSpeed = scrollSpeed * fastForwardMultiplier;
        }
        else
        {
            currentSpeed = scrollSpeed;
        }

        // 2. Mover el texto hacia arriba
        if (creditsContent != null)
        {
            creditsContent.anchoredPosition += Vector2.up * currentSpeed * Time.deltaTime;

            // 3. Verificar si terminó
            if (creditsContent.anchoredPosition.y >= endYPosition)
            {
                ReturnToMenu();
            }
        }

        // 4. Salir manualmente con Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenu();
        }
    }

    private void ReturnToMenu()
    {
        Debug.Log("Créditos terminados. Volviendo al menú...");
        
        // 🔓 DESBLOQUEAR CURSOR (Importante al volver de gameplay)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Verifica que la escena exista en Build Settings antes de cargar
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
