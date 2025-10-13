using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Necesario para la Coroutine

public class SceneFlowManager : MonoBehaviour
{
    // ================== SINGLETON Y PERSISTENCIA ==================

    // El patrón Singleton permite que cualquier script acceda a esta instancia con SceneFlowManager.Instance
    public static SceneFlowManager Instance { get; private set; }

    [Header("Persistencia de Personaje")]
    // Almacena el nombre (o ID) del personaje seleccionado. 
    // ¡Asegúrate de que este nombre coincida con el Prefab!
    public string selectedCharacterName = "";

    [Header("Configuración de Escenas")]
    // 🛑 AJUSTA ESTOS NOMBRES con los de tus escenas.
    private const string GameSceneName = "Principal";
    private const string LoreSceneName = "LoreScene";
    private const string SeleccionPersonajeSceneName = "SeleccionPersonaje"; // Nombre corregido
    private const string InitialSceneName = "Menu"; // Asume el nombre de la primera escena

    [Header("Referencias de Escena de Juego")]
    [Tooltip("Arrastra aquí el Spot Light principal del jugador/escena de juego para apagarlo al salir.")]
    public GameObject playerSpotLight;

    // Asumimos que esta clase tiene el método 'SetControlsActive(bool active)'
    private MouseLookController playerController;

    private void Awake()
    {
        // Implementación del Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // CRÍTICO: Mantiene este objeto vivo entre escenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ================== GESTIÓN DEL PERSONAJE SELECCIONADO ==================

    /// <summary>
    /// Función a llamar desde el botón/UI de selección de personaje en la escena "SeleccionPersonaje".
    /// </summary>
    public void SetSelectedCharacter(string characterName)
    {
        selectedCharacterName = characterName;
        Debug.Log($"[SceneFlow] Personaje guardado: {characterName}");
        LoadLoreScene();
    }

    // ================== Carga de Escenas (Llamadas Públicas) ==================

    /// <summary>
    /// Carga la escena de Lore, deshabilitando el Spot Light y el control del ratón antes.
    /// </summary>
    public void LoadLoreScene()
    {
        // 1. Desactivar el control de cámara ANTES de cargar la nueva escena.
        FindAndSetPlayerController(false);

        // 2. Apagar el foco de luz
        if (playerSpotLight != null)
        {
            playerSpotLight.SetActive(false);
            Debug.Log("[SceneFlow] Spot Light de jugador desactivado para la escena Lore.");
        }

        // 3. Carga la escena Lore.
        SceneManager.LoadScene(LoreSceneName);
    }

    /// <summary>
    /// Función ASIGNADA AL BOTÓN "COMENZAR" en la escena "Lore".
    /// </summary>
    public void LoadGameScene()
    {
        SceneManager.LoadScene(GameSceneName);
    }

    // ================== CONTROL DE ESCENAS, CURSOR Y JUGADOR ==================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Limpiamos la referencia para obligar a buscar la nueva instancia del Player
        playerController = null;

        if (scene.name == GameSceneName) // 🟢 ESCENA DE JUEGO (Principal)
        {
            // 1. Bloqueo del Cursor
            Cursor.lockState = CursorLockMode.Locked; // Bloquea en el centro
            Cursor.visible = false;                   // Oculta el puntero

            // 2. Instanciación y Activación de Controles
            InstantiateSelectedCharacter();
            StartCoroutine(WaitAndActivateControls()); // Activa el MouseLookController

            // 3. Reactivar el Spot Light
            if (playerSpotLight != null)
            {
                playerSpotLight.SetActive(true);
            }
        }
        else // 🔵 ESCENAS DE MENÚ/UI (LoreScene, SeleccionPersonaje, Menu)
        {
            // 1. Liberación del Cursor
            Cursor.lockState = CursorLockMode.None; // Libera el cursor para usar la UI
            Cursor.visible = true;                  // Muestra el puntero

            // 2. Desactivar controles (por si el jugador persiste)
            FindAndSetPlayerController(false);
        }
    }

    /// <summary>
    /// Instancia el personaje seleccionado en el Prefab Loader.
    /// </summary>
    private void InstantiateSelectedCharacter()
    {
        if (string.IsNullOrEmpty(selectedCharacterName))
        {
            Debug.LogError("[SceneFlow] No hay personaje seleccionado. Cargando personaje por defecto.");
            return;
        }

        // 🚨 CRÍTICO: El prefab del personaje DEBE estar en una carpeta 'Resources'
        GameObject characterPrefab = Resources.Load<GameObject>(selectedCharacterName);

        if (characterPrefab != null)
        {
            // Busca el punto de inicio.
            GameObject playerSpawn = GameObject.FindGameObjectWithTag("PlayerSpawn");
            Vector3 spawnPosition = playerSpawn != null ? playerSpawn.transform.position : Vector3.zero;

            // Instanciar el personaje
            GameObject playerInstance = Instantiate(characterPrefab, spawnPosition, Quaternion.identity);
            playerInstance.tag = "Player"; // Asegura la etiqueta

            Debug.Log($"[SceneFlow] Personaje '{selectedCharacterName}' instanciado correctamente.");
        }
        else
        {
            Debug.LogError($"[SceneFlow] ¡ERROR! No se encontró el Prefab '{selectedCharacterName}' en la carpeta Resources.");
        }
    }

    private IEnumerator WaitAndActivateControls()
    {
        // Espera dos frames para asegurar que el Player se haya instanciado y el Awake/Start haya terminado.
        yield return null;
        yield return null;

        FindAndSetPlayerController(true); // Activa el control
    }

    /// <summary>
    /// Busca el MouseLookController y alterna su estado de actividad.
    /// </summary>
    private void FindAndSetPlayerController(bool active)
    {
        if (playerController == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<MouseLookController>();
            }
        }

        if (playerController != null)
        {
            // Asumimos que SetControlsActive() habilita/deshabilita el script o su funcionalidad.
            playerController.SetControlsActive(active);
            Debug.Log($"[SceneFlow] Control de cámara {(active ? "ACTIVADO" : "DESACTIVADO")}.");
        }
        else if (active)
        {
            Debug.LogWarning("[SceneFlow] Intentó activar el control, pero MouseLookController no fue encontrado. Verifique la etiqueta 'Player'.");
        }
    }

    // ================== Suscripción de Eventos ==================

    private void OnEnable()
    {
        // Suscribirse al evento de carga de escenas.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Desuscribirse para evitar fugas de memoria y errores.
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}