using UnityEngine;
using UnityEngine.Events; // Para los eventos de notificación

public class DirtManager : MonoBehaviour
{
    public static DirtManager Instance;

    // Conteo total de suciedad
    private int totalDirtCount = 0;
    // Suciedad que aún queda por limpiar
    private int remainingDirtCount = 0;

    // Eventos que notifican a la UI cuando el conteo cambia
    // El float es el valor de progreso (0.0 a 1.0)
    public UnityEvent<float> OnProgressUpdated = new UnityEvent<float>();
    // Evento que se dispara cuando todo está limpio
    public UnityEvent OnAllCleaned = new UnityEvent();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // No usamos DontDestroyOnLoad porque este manager es local a la escena de la cabaña.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Se llama al inicio para establecer la cuenta total de suciedad en la escena.
    /// </summary>
    public void RegisterDirtItem()
    {
        totalDirtCount++;
        remainingDirtCount++;
        // Se llama a UpdateProgress en el primer frame para inicializar la barra
        UpdateProgress();
    }

    /// <summary>
    /// Se llama desde CleanableObject.cs cuando se interactúa con el objeto.
    /// </summary>
    // Dentro de DirtManager.cs

    public void CleanDirtItem()
    {
        remainingDirtCount = Mathf.Max(0, remainingDirtCount - 1);
        UpdateProgress();

        if (remainingDirtCount <= 0)
        {
            Debug.Log("[CLEANING] ¡Cabaña completamente limpia!");

            // 🛑 NUEVA LÍNEA CRÍTICA: Detener el juego
            Time.timeScale = 1f;

            OnAllCleaned.Invoke();
        }
    }

    private void UpdateProgress()
    {
        if (totalDirtCount == 0)
        {
            OnProgressUpdated.Invoke(1.0f); // 100% limpio si no hay suciedad registrada
            return;
        }

        // Calcula el progreso (un float entre 0.0 y 1.0)
        float progress = 1.0f - ((float)remainingDirtCount / totalDirtCount);

        Debug.Log($"[CLEANING] Progreso de limpieza: {progress * 100:F0}%");

        // Notifica a la UI
        OnProgressUpdated.Invoke(progress);
    }
}