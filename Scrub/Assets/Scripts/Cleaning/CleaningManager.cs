using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using System.Collections;

public class CleaningManager : MonoBehaviour
{
    // === EVENTOS ESTATÍCOS ===
    // Evento para notificar a la UI (conteo de basura)
    public static event Action<int, int> OnTrashCountUpdated;
    // Evento para notificar a la UI (tiempo)
    public static event Action<float> OnTimeUpdated;

    // === CONFIGURACIÓN DE MISIÓN ===
    [Header("Configuración de Misión")]
    [Tooltip("Tiempo límite en segundos para completar la limpieza.")]
    [SerializeField] private float timeLimit = 120f; // 2 minutos por defecto
    [SerializeField] private string creditsSceneName = "Credits"; // Nombre de la escena de créditos

    // === PANELES DE FIN DE JUEGO (VICTORIA/DERROTA) ===
    [Header("Paneles de Fin de Juego")]
    [Tooltip("Panel de UI que se activa al ganar.")]
    [SerializeField] private GameObject victoryPanel;
    [Tooltip("Panel de UI que se activa al perder.")]
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private float endScreenDisplayTime = 5f; // Tiempo antes de ir a créditos

    // === ESTADO DEL JUEGO ===
    private List<GameObject> remainingTrash = new List<GameObject>();
    private int totalTrashCount = 0;
    private float timeRemaining;
    private bool isGameOver = false;

    private const string TRASH_TAG = "Trash";
    private const int TOTAL_TRASH_GOAL = 0;

    void Awake()
    {
        // Puedes implementar el patrón Singleton aquí si es necesario (e.g., if (Instance == null) Instance = this;)
    }

    void Start()
    {
        // 1. Inicializar el estado de la misión
        GameObject[] trashObjects = GameObject.FindGameObjectsWithTag(TRASH_TAG);
        remainingTrash.AddRange(trashObjects);
        totalTrashCount = remainingTrash.Count;

        timeRemaining = timeLimit;

        // 2. Estado inicial de la UI (para TrashUIManager)
        SendCurrentState();

        // 3. Ocultar los paneles de fin de juego al inicio
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);

        if (totalTrashCount == 0) Debug.LogWarning("No se encontraron objetos con el Tag 'Trash'.");
        else Debug.Log($"Tarea de limpieza iniciada. Total a limpiar: {totalTrashCount}");
    }

    void Update()
    {
        if (isGameOver) return;

        // 1. Actualizar el temporizador
        timeRemaining -= Time.deltaTime;

        // 2. Disparar el evento de tiempo para la UI (TrashUIManager)
        OnTimeUpdated?.Invoke(timeRemaining);

        // 3. Lógica de Derrota por tiempo
        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            EndGame(false); // Derrota
        }
    }

    // Método para enviar el estado actual (usado por la UI al suscribirse)
    public void SendCurrentState()
    {
        int currentCleanedCount = totalTrashCount - remainingTrash.Count;
        OnTrashCountUpdated?.Invoke(currentCleanedCount, totalTrashCount);

        // También enviamos el estado del tiempo para el inicializador de la UI
        OnTimeUpdated?.Invoke(timeRemaining);
    }

    public void TrashDeposited(GameObject trashObject)
    {
        // Esto se llama desde el Carryable.TryDepositAndDestroy()
        if (isGameOver || trashObject == null) return;

        // Usa Find() para ser seguro, aunque FirstOrDefault es más eficiente si la lista es grande
        GameObject itemToClean = remainingTrash.FirstOrDefault(item => item == trashObject);

        if (itemToClean == null)
        {
            // Opcional: Esto ayuda a detectar si un objeto es depositado dos veces.
            Debug.LogWarning($"[Manager] Objeto {trashObject.name} ya fue contado o no está en la lista de pendientes.");
            return;
        }

        // 1. Quita el objeto de la lista de pendientes.
        remainingTrash.Remove(itemToClean);

        // 2. Envía el nuevo conteo
        SendCurrentState();

        // 3. DESTRUCCIÓN
        Destroy(itemToClean);

        // 4. Lógica de Victoria
        if (remainingTrash.Count == TOTAL_TRASH_GOAL)
        {
            EndGame(true); // Victoria
        }
    }

    /// <summary>
    /// Termina el juego, muestra el panel correspondiente y programa la transición de escena.
    /// </summary>
    private void EndGame(bool success)
    {
        isGameOver = true;

        // 1. Mostrar el panel y configurar la UI (sin pausar el juego aún)
        GameObject activePanel = success ? victoryPanel : defeatPanel;
        GameObject inactivePanel = success ? defeatPanel : victoryPanel;
        if (inactivePanel != null) inactivePanel.SetActive(false);

        if (activePanel != null)
        {
            activePanel.SetActive(true);
            Debug.Log($"FIN DEL JUEGO: {(success ? "VICTORIA" : "DERROTA")}");

            // 2. INICIAMOS LA CORRUTINA PARA PAUSAR Y CONTAR TIEMPO REAL
            StartCoroutine(EndGameSequence());
        }
        else
        {
            Debug.LogError($"El panel de {(success ? "VICTORIA" : "DERROTA")} no está asignado.");
            LoadCreditsScene();
        }
    }

    // Coroutine para manejar el tiempo de visualización de la pantalla final sin escala.
    private IEnumerator EndGameSequence()
    {
        // 1. Pausa el juego para el jugador
        Time.timeScale = 0f;

        // 2. Espera el tiempo de visualización de la pantalla final (usando tiempo real)
        float timer = 0f;
        while (timer < endScreenDisplayTime)
        {
            timer += Time.unscaledDeltaTime; // Cuenta el tiempo real
            yield return null;
        }

        // 3. Despausa y carga la escena
        LoadCreditsScene();
    }

    private void LoadCreditsScene()
    {
        Time.timeScale = 1f; // Despausa el juego antes de cargar la escena
        SceneManager.LoadScene(creditsSceneName);
    }
}