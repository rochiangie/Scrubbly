using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Cinemachine;

public class PersistentUI : MonoBehaviour
{
    public static PersistentUI Instance;
    private Canvas canvasComponent;

    // Distancia al plano de corte cercano de la cámara. 
    // Un valor pequeño como 0.1f es seguro para que el Canvas se dibuje 'justo' delante.
    private const float CANVAS_PLANE_OFFSET = 0.1f;

    private void Awake()
    {
        // Implementación del patrón Singleton para la persistencia
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

        canvasComponent = GetComponent<Canvas>();
        if (canvasComponent == null)
        {
            Debug.LogError("[PERSISTENT UI] No se encontró el componente Canvas en el objeto persistente.");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (canvasComponent == null) return;

        // Solo reajustamos si el Canvas está configurado para renderizarse con una cámara
        if (canvasComponent.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Camera renderingCamera = FindRenderingCamera();

            if (renderingCamera != null)
            {
                // 1. Asignamos la nueva cámara
                canvasComponent.worldCamera = renderingCamera;

                // 2. AJUSTE CRÍTICO: Movemos el plano de renderizado del Canvas para que esté 
                //    justo delante del plano de corte cercano de la cámara (Near Clip Plane).
                //    Esto asegura que se dibuje correctamente en la nueva vista.
                canvasComponent.planeDistance = renderingCamera.nearClipPlane + CANVAS_PLANE_OFFSET;

                Debug.Log($"[PERSISTENT UI] Canvas reajustado a la cámara: {renderingCamera.name} en escena: {scene.name}. Distancia del plano: {canvasComponent.planeDistance}");
            }
            else
            {
                Debug.LogWarning($"[PERSISTENT UI] ¡ADVERTENCIA! No se encontró la MainCamera (con Tag: MainCamera) para reajustar el Canvas en la escena: {scene.name}.");
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
            // 2. Si tiene el Cinemachine Brain, es la cámara que queremos.
            if (mainCam.GetComponent<CinemachineBrain>() != null)
            {
                return mainCam;
            }
            // Si no tiene Brain, pero es la MainCamera (ej. Menú), la usamos.
            return mainCam;
        }

        // 3. Fallback: Buscar cualquier CinemachineBrain activo.
        CinemachineBrain brain = FindObjectOfType<CinemachineBrain>();
        if (brain != null)
        {
            return brain.GetComponent<Camera>();
        }

        return null; // No se encontró una cámara válida.
    }
}