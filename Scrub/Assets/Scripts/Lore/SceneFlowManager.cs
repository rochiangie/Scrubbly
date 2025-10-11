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
    // Corregido el nombre de la escena:
    private const string SeleccionPersonajeSceneName = "SeleccionPersonaje";
    private const string InitialSceneName = "Menu"; // Asume el nombre de la primera escena

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

    // ================== GESTIÓN DEL PERSONAJE SELECCIONADO ==================

    // 🛑 Función a llamar desde el botón/UI de selección de personaje en la escena "SeleccionPersonaje"
    public void SetSelectedCharacter(string characterName)
    {
        selectedCharacterName = characterName;
        Debug.Log($"[SceneFlow] Personaje guardado: {characterName}");

        // Una vez seleccionado, procede a la escena de Lore
        LoadLoreScene();
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
            // 🛑 LÓGICA DE INSTANCIACIÓN DEL PERSONAJE:
            // Asegúrate de que este Manager o un script dedicado en la escena principal
            // llame a esta función para cargar el personaje antes de activar los controles.
            InstantiateSelectedCharacter();

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

    // 🛑 NUEVA FUNCIÓN: Instancia el personaje seleccionado.
    private void InstantiateSelectedCharacter()
    {
        if (string.IsNullOrEmpty(selectedCharacterName))
        {
            Debug.LogError("[SceneFlow] No hay personaje seleccionado. Cargando personaje por defecto.");
            // Opcional: Cargar un personaje por defecto aquí.
            return;
        }

        // 🚨 CRÍTICO: El prefab del personaje DEBE estar en una carpeta 'Resources'
        // con el nombre EXACTO guardado en 'selectedCharacterName'.
        GameObject characterPrefab = Resources.Load<GameObject>(selectedCharacterName);

        if (characterPrefab != null)
        {
            // Se instancia el personaje en el punto de inicio de la escena (si hay uno)
            GameObject playerSpawn = GameObject.FindGameObjectWithTag("PlayerSpawn");
            Vector3 spawnPosition = playerSpawn != null ? playerSpawn.transform.position : Vector3.zero;

            // Instanciar el personaje
            GameObject playerInstance = Instantiate(characterPrefab, spawnPosition, Quaternion.identity);
            playerInstance.tag = "Player"; // Asegura que la etiqueta sea "Player" para que FindAndSetPlayerController lo encuentre.

            Debug.Log($"[SceneFlow] Personaje '{selectedCharacterName}' instanciado correctamente.");
        }
        else
        {
            Debug.LogError($"[SceneFlow] ¡ERROR! No se encontró el Prefab '{selectedCharacterName}' en la carpeta Resources.");
        }
    }

    private IEnumerator WaitAndActivateControls()
    {
        // Espera dos frames. Suficiente tiempo para que el personaje se instancie.
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
                // **NOTA:** Aquí asumo que tu MouseLookController está adjunto 
                // al objeto padre del personaje instanciado o a la cámara del jugador.
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