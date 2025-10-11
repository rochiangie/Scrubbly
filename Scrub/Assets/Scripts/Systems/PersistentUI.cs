using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cinemachine;                 // Necesario si usas CinemachineBrain

public class PersistentUI : MonoBehaviour
{
    public static PersistentUI Instance;
    private Canvas canvasComponent;

    private void Awake()
    {
        // === 1. Implementación del Singleton Persistente ===
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Obtener la referencia al componente Canvas
        canvasComponent = GetComponent<Canvas>();
        if (canvasComponent == null)
        {
            Debug.LogError("[PERSISTENT UI] No se encontró el componente Canvas en el objeto persistente.");
        }
    }

    // === 2. Suscripción y Desuscripción a Eventos de Escena ===
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Se ejecuta cada vez que se carga una nueva escena.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (canvasComponent == null) return;

        // --- Paso A: Ajuste para el modo Screen Space - Camera ---
        if (canvasComponent.renderMode == RenderMode.ScreenSpaceCamera)
        {
            // 1. Buscamos la cámara que está renderizando la escena.
            Camera renderingCamera = FindRenderingCamera();

            if (renderingCamera != null)
            {
                // 2. Asignamos la nueva cámara al Canvas persistente.
                canvasComponent.worldCamera = renderingCamera;

                // Opcional pero recomendado: si usas un Canvas Scaler, asegúrate de que use el Camera Plane.
                // canvasComponent.planeDistance = renderingCamera.nearClipPlane + 0.1f; 

                Debug.Log($"[PERSISTENT UI] Canvas reajustado a la cámara: {renderingCamera.name} en escena: {scene.name}");
            }
            else
            {
                Debug.LogWarning($"[PERSISTENT UI] ¡ADVERTENCIA! No se encontró una cámara para reajustar el Canvas en la escena: {scene.name}.");
            }
        }
    }

    /// <summary>
    /// Busca la cámara activa que tiene el componente CinemachineBrain o la MainCamera.
    /// </summary>
    private Camera FindRenderingCamera()
    {
        // 1. Intentar encontrar la cámara principal (estándar de Unity).
        Camera mainCam = Camera.main;

        if (mainCam != null)
        {
            // 2. Si la encontramos, verificamos si tiene un Cinemachine Brain (la cámara que queremos).
            if (mainCam.GetComponent<CinemachineBrain>() != null)
            {
                return mainCam;
            }
            // Si la MainCamera está activa y no tiene Brain (escena de menú, por ejemplo), la usamos.
            return mainCam;
        }

        // 3. Si Camera.main falló, buscamos cualquier cámara activa con el Brain.
        CinemachineBrain brain = FindObjectOfType<CinemachineBrain>();
        if (brain != null)
        {
            return brain.GetComponent<Camera>();
        }

        // 4. Último recurso: buscar todas las cámaras.
        Camera[] allCameras = Camera.allCameras;
        foreach (Camera cam in allCameras)
        {
            if (cam.enabled && cam.tag == "MainCamera")
            {
                return cam;
            }
        }

        return null; // No se encontró una cámara válida.
    }
}