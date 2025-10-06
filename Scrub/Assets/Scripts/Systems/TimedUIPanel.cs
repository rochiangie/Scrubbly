using UnityEngine;
using System.Collections;

public class TimedUIPanel : MonoBehaviour
{
    [Header("Configuración de Tiempo")]
    [Tooltip("Tiempo que el panel permanecerá visible antes de ocultarse.")]
    [SerializeField] private float displayDuration = 3.0f;

    [Header("Referencias")]
    [Tooltip("El panel o GameObject a mostrar/ocultar.")]
    [SerializeField] private GameObject targetPanel;

    private void Awake()
    {
        // Si no se asigna un panel, asumimos que el script está en el propio panel.
        if (targetPanel == null)
        {
            targetPanel = this.gameObject;
        }
    }

    /// <summary>
    /// Inicia el proceso de mostrar el panel y luego ocultarlo después de un tiempo.
    /// </summary>
    public void ShowAndHide()
    {
        // Detiene cualquier corrutina de ocultación previa para evitar conflictos.
        StopAllCoroutines();

        // Se asegura de que el panel se muestre ANTES de empezar el temporizador.
        targetPanel.SetActive(true);

        // Inicia el ciclo de espera y ocultación.
        StartCoroutine(HideAfterTime(displayDuration));
    }

    /// <summary>
    /// Corrutina que espera la duración y luego oculta el panel.
    /// </summary>
    private IEnumerator HideAfterTime(float duration)
    {
        // Espera el tiempo especificado.
        yield return new WaitForSeconds(duration);

        // Oculta el panel.
        targetPanel.SetActive(false);
    }

    // Opcional: Para probar rápidamente en el editor.
    // private void Start()
    // {
    //     // Esto lo puedes descomentar para probar al inicio de la escena.
    //     // ShowAndHide();
    // }
}