using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cinemachine; // �A�adimos el 'using' que faltaba para la referencia!

public class PersistentUI : MonoBehaviour
{
    public static PersistentUI Instance;
    private Canvas canvasComponent;

    private void Awake()
    {
        // Implementaci�n del patr�n Singleton para la persistencia
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
            Debug.LogError("[PERSISTENT UI] No se encontr� el componente Canvas en el objeto persistente.");
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

        // Solo reajustamos si el Canvas est� configurado para renderizarse con una c�mara
        if (canvasComponent.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Camera renderingCamera = FindRenderingCamera();

            if (renderingCamera != null)
            {
                // Asignamos la c�mara (la MainCamera con CinemachineBrain)
                canvasComponent.worldCamera = renderingCamera;
                Debug.Log($"[PERSISTENT UI] Canvas reajustado a la c�mara: {renderingCamera.name} en escena: {scene.name}");
            }
            else
            {
                Debug.LogWarning($"[PERSISTENT UI] �ADVERTENCIA! No se encontr� la MainCamera (con Tag: MainCamera) para reajustar el Canvas en la escena: {scene.name}.");
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
            // 2. Si tiene el Cinemachine Brain, es la c�mara que queremos.
            if (mainCam.GetComponent<CinemachineBrain>() != null)
            {
                return mainCam;
            }
            // Si no tiene Brain, pero es la MainCamera (ej. Men�), la usamos.
            return mainCam;
        }

        // 3. Fallback: Buscar cualquier CinemachineBrain activo.
        CinemachineBrain brain = FindObjectOfType<CinemachineBrain>();
        if (brain != null)
        {
            return brain.GetComponent<Camera>();
        }

        return null; // No se encontr� una c�mara v�lida.
    }
}