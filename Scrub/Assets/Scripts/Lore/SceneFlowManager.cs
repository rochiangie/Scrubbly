using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFlowManager : MonoBehaviour
{
    // ================== SINGLETON Y PERSISTENCIA ==================

    public static SceneFlowManager Instance { get; private set; }

    [Header("Persistencia de Personaje")]
    public string selectedCharacterName = "";

    [Header("Configuración de Escenas")]
    // 🛑 AJUSTA ESTOS NOMBRES con los de tus escenas.
    private const string GameSceneName = "Principal";
    // 🟢 NUEVA CONSTANTE: Agregamos la escena conflictiva a la lista
    private const string CasaChickSceneName = "CasaChick";
    private const string LoreSceneName = "LoreScene";
    private const string SeleccionPersonajeSceneName = "SeleccionPersonaje";
    private const string InitialSceneName = "Menu";

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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ================== GESTIÓN DEL PERSONAJE SELECCIONADO ==================

    public void SetSelectedCharacter(string characterName)
    {
        selectedCharacterName = characterName;
        Debug.Log($"[SceneFlow] Personaje guardado: {characterName}");
        LoadLoreScene();
    }

    // ================== Carga de Escenas (Llamadas Públicas) ==================

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

    public void LoadGameScene()
    {
        SceneManager.LoadScene(GameSceneName);
    }

    // Función de ejemplo para cargar la CasaChick
    public void LoadCasaChickScene()
    {
        SceneManager.LoadScene(CasaChickSceneName);
    }

    // ================== CONTROL DE ESCENAS, CURSOR Y JUGADOR ==================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Limpiamos la referencia para obligar a buscar la nueva instancia del Player
        playerController = null;

        // 🟢 CORRECCIÓN CLAVE: Agregamos CasaChick a las escenas que requieren bloqueo
        bool isPlayableScene = scene.name == GameSceneName || scene.name == CasaChickSceneName;

        if (isPlayableScene) // 🟢 ESCENAS DE JUEGO (Principal y CasaChick)
        {
            // 1. Bloqueo del Cursor: Necesario para que el MouseLookController funcione
            Cursor.lockState = CursorLockMode.Locked; // Bloquea en el centro
            Cursor.visible = false;                   // Oculta el puntero

            // 2. Instanciación y Activación de Controles
            // NOTA: Solo instanciar si es el primer nivel o si el personaje no persiste
            if (scene.name == GameSceneName)
            {
                InstantiateSelectedCharacter();
            }

            StartCoroutine(WaitAndActivateControls()); // Activa el MouseLookController

            // 3. Reactivar el Spot Light
            if (playerSpotLight != null)
            {
                playerSpotLight.SetActive(true);
            }
        }
        else // 🔵 ESCENAS DE MENÚ/UI (LoreScene, SeleccionPersonaje, Menu, etc.)
        {
            // 1. Liberación del Cursor
            Cursor.lockState = CursorLockMode.None; // Libera el cursor para usar la UI
            Cursor.visible = true;                  // Muestra el puntero

            // 2. Desactivar controles
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

        GameObject characterPrefab = Resources.Load<GameObject>(selectedCharacterName);

        if (characterPrefab != null)
        {
            GameObject playerSpawn = GameObject.FindGameObjectWithTag("PlayerSpawn");
            Vector3 spawnPosition = playerSpawn != null ? playerSpawn.transform.position : Vector3.zero;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}