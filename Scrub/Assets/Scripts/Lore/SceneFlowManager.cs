using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Necesario para la Coroutine

public class SceneFlowManager : MonoBehaviour
{
    // ================== SINGLETON Y PERSISTENCIA ==================

    // El patrón Singleton permite que cualquier script acceda a esta instancia con SceneFlowManager.Instance
    public static SceneFlowManager Instance { get; private set; }

    [Header("Configuración de Escenas")]
    // 🛑 AJUSTA ESTOS NOMBRES con los de tus escenas.
    private const string GameSceneName = "Principal";
    private const string LoreSceneName = "LoreScene";
    private const string InitialSceneName = "SeleccionPersonaje"; // Asume el nombre de la primera escena

    [Header("Referencias de Escena de Juego")]
    [Tooltip("Arrastra aquí el Spot Light principal del jugador/escena de juego para apagarlo al salir.")]
    public GameObject playerSpotLight;

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

    // ================== Carga de Escenas (Llamadas Públicas) ==================

    // Función a llamar para ir de la Escena de Juego (o Inicial) a la escena "Lore"
    public void LoadLoreScene()
    {
        // 1. Apagar el foco de luz antes de cargar la nueva escena
        if (playerSpotLight != null)
        {
            playerSpotLight.SetActive(false);
            Debug.Log("[SceneFlow] Spot Light de jugador desactivado para la escena Lore.");
        }

        // 2. Desactivar el control de cámara ANTES de cargar la nueva escena.
        FindAndSetPlayerController(false);

        // 3. Carga la escena Lore.
        SceneManager.LoadScene(LoreSceneName);
    }

    // Función ASIGNADA AL BOTÓN "COMENZAR" en la escena "Lore"
    public void LoadGameScene()
    {
        // El control se reactivará en OnSceneLoaded, después de la instanciación del Player.
        SceneManager.LoadScene(GameSceneName);
    }

    // ================== Control de Escenas y Jugador ==================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Limpiamos la referencia para obligar a buscar la nueva instancia del Player
        playerController = null;

        if (scene.name == GameSceneName)
        {
            // Solo activamos el control en la escena del juego.
            StartCoroutine(WaitAndActivateControls());

            // Reactivar el Spot Light
            if (playerSpotLight != null)
            {
                playerSpotLight.SetActive(true);
            }
        }
        else // Para la escena Inicial, Lore o cualquier otra escena de menú:
        {
            // Forzamos la liberación del cursor y visibilidad para la UI.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // También desactiva el control si el Player fue instanciado en esta escena (fallback)
            FindAndSetPlayerController(false);
        }
    }

    private IEnumerator WaitAndActivateControls()
    {
        // Espera dos frames. Suficiente tiempo para que el LOADER instancie al jugador.
        yield return null;
        yield return null;

        FindAndSetPlayerController(true); // Activa el control
    }

    // Busca el MouseLookController y alterna su estado
    private void FindAndSetPlayerController(bool active)
    {
        // Buscamos la referencia solo si no la tenemos.
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
            // La función SetControlsActive() es la que bloquea/libera el cursor
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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}