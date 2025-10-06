using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    // DICCIONARIO para almacenar todos los componentes que necesitan inicialización/limpieza
    private Dictionary<string, ISceneInitializable> sceneHandlers = new Dictionary<string, ISceneInitializable>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Suscribirse a los eventos de escena de Unity
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDestroy()
    {
        // Cancelar suscripciones al destruir para evitar errores
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    /// <summary>
    /// Registra un componente para que sea notificado cuando la escena se cargue.
    /// </summary>
    public void RegisterHandler(ISceneInitializable handler)
    {
        if (handler == null || handler.HandlerID == null) return;

        if (!sceneHandlers.ContainsKey(handler.HandlerID))
        {
            sceneHandlers.Add(handler.HandlerID, handler);
        }
    }

    /// <summary>
    /// Notifica a los scripts de la escena que la escena fue cargada.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SCENE MANAGER] Escena cargada: {scene.name}. Iniciando handlers...");

        // Ejecutar la inicialización en todos los handlers registrados
        foreach (var handler in sceneHandlers.Values)
        {
            handler.InitializeScene(scene.name);
        }
    }

    /// <summary>
    /// Notifica a los scripts de la escena que la escena va a ser descargada/destruida.
    /// Esto es vital para la limpieza de referencias.
    /// </summary>
    private void OnSceneUnloaded(Scene scene)
    {
        Debug.Log($"[SCENE MANAGER] Escena descargada: {scene.name}. Limpiando handlers...");

        // Ejecutar la limpieza antes de que los objetos sean destruidos por Unity
        foreach (var handler in sceneHandlers.Values)
        {
            handler.CleanupScene(scene.name);
        }

        // Limpiar la lista de handlers (importante para scripts que se destruyen)
        sceneHandlers.Clear();
    }
}

// ----------------------------------------------------
// INTERFAZ PARA SCRIPTS INICIALIZABLES
// ----------------------------------------------------

// Los scripts que necesitan ser inicializados/limpiados deben implementar esta interfaz.
public interface ISceneInitializable
{
    // ID único para el script (ej: "SpotlightSelector")
    string HandlerID { get; }

    // Método llamado al cargar la escena
    void InitializeScene(string sceneName);

    // Método llamado al descargar la escena (para limpieza)
    void CleanupScene(string sceneName);
}