using UnityEngine;
using System.Collections;

public class AutoClosePanel : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("El panel que se mostrará al inicio. Si se deja vacío, se usará el objeto donde esté este script.")]
    [SerializeField] private GameObject infoPanel;

    [Tooltip("Tiempo en segundos que el panel permanecerá visible.")]
    [SerializeField] private float displayDuration = 10f;

    void Start()
    {
        // Si no se asignó un panel manualmente, asumimos que este script está adjunto al panel mismo
        if (infoPanel == null)
        {
            infoPanel = gameObject;
        }

        // 1. Mostrar el panel al inicio
        infoPanel.SetActive(true);

        // 2. Iniciar la cuenta regresiva para cerrarlo
        StartCoroutine(ClosePanelAfterDelay());
    }

    private IEnumerator ClosePanelAfterDelay()
    {
        // Esperar el tiempo configurado
        yield return new WaitForSeconds(displayDuration);
        
        // 3. Ocultar el panel
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
            Debug.Log("Panel de info cerrado automáticamente.");
        }
    }
}
