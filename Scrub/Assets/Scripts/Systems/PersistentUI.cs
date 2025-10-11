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
        // === 1. Implementaci�n del Singleton Persistente ===
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
            Debug.LogError("[PERSISTENT UI] No se encontr� el componente Canvas en el objeto persistente.");
        }
    }

    // === 2. Suscripci�n y Desuscripci�n a Eventos de Escena ===
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
            // 1. Buscamos la c�mara que est� renderizando la escena.
            Camera renderingCamera = FindRenderingCamera();

            if (renderingCamera != null)
            {
                // 2. Asignamos la nueva c�mara al Canvas persistente.
                canvasComponent.worldCamera = renderingCamera;

                // Opcional pero recomendado: si usas un Canvas Scaler, aseg�rate de que use el Camera Plane.
                // canvasComponent.planeDistance = renderingCamera.nearClipPlane + 0.1f; 

                Debug.Log($"[PERSISTENT UI] Canvas reajustado a la c�mara: {renderingCamera.name} en escena: {scene.name}");
            }
            else
            {
                Debug.LogWarning($"[PERSISTENT UI] �ADVERTENCIA! No se encontr� una c�mara para reajustar el Canvas en la escena: {scene.name}.");
            }
        }
    }

    /// <summary>
    /// Busca la c�mara activa que tiene el componente CinemachineBrain o la MainCamera.
    /// </summary>
    private Camera FindRenderingCamera()
    {
        // 1. Intentar encontrar la c�mara principal (est�ndar de Unity).
        Camera mainCam = Camera.main;

        if (mainCam != null)
        {
            // 2. Si la encontramos, verificamos si tiene un Cinemachine Brain (la c�mara que queremos).
            if (mainCam.GetComponent<CinemachineBrain>() != null)
            {
                return mainCam;
            }
            // Si la MainCamera est� activa y no tiene Brain (escena de men�, por ejemplo), la usamos.
            return mainCam;
        }

        // 3. Si Camera.main fall�, buscamos cualquier c�mara activa con el Brain.
        CinemachineBrain brain = FindObjectOfType<CinemachineBrain>();
        if (brain != null)
        {
            return brain.GetComponent<Camera>();
        }

        // 4. �ltimo recurso: buscar todas las c�maras.
        Camera[] allCameras = Camera.allCameras;
        foreach (Camera cam in allCameras)
        {
            if (cam.enabled && cam.tag == "MainCamera")
            {
                return cam;
            }
        }

        return null; // No se encontr� una c�mara v�lida.
    }
}