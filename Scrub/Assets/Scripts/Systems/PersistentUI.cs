using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cinemachine; // ¡Añadimos el 'using' que faltaba para la referencia!

public class PersistentUI : MonoBehaviour
{
    public static PersistentUI Instance;
    private Canvas canvasComponent;

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
                // Asignamos la cámara (la MainCamera con CinemachineBrain)
                canvasComponent.worldCamera = renderingCamera;
                Debug.Log($"[PERSISTENT UI] Canvas reajustado a la cámara: {renderingCamera.name} en escena: {scene.name}");
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