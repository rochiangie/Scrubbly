using UnityEngine;
using TMPro; // Asegúrate de usar TextMeshPro para la UI

public class StatusMessageDisplay : MonoBehaviour
{
    public static StatusMessageDisplay Instance;

    [Header("Referencias de UI")]
    // 📢 Asigna tu componente de Texto aquí (ej: TextMeshProUGUI)
    public TextMeshProUGUI messageText;
    // 📢 Asigna el GameObject del panel que contiene el texto (para mostrar/ocultar)
    public GameObject messagePanel;

    [Header("Configuración")]
    [Tooltip("Duración que el mensaje estará visible en pantalla (en segundos).")]
    public float displayDuration = 5f;

    private float hideTime = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Opcional: Si quieres que el mensaje persista entre escenas, añade DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Aseguramos que el panel esté oculto al inicio
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Si el panel está activo y el tiempo de ocultar ha pasado, lo desactivamos
        if (messagePanel.activeSelf && Time.time >= hideTime)
        {
            messagePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Muestra un mensaje en pantalla durante la duración configurada.
    /// </summary>
    /// <param name="message">El texto a mostrar.</param>
    public void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
        if (messagePanel != null)
        {
            messagePanel.SetActive(true);
        }
        // Calcular el tiempo en el que se debe ocultar
        hideTime = Time.time + displayDuration;
    }
}