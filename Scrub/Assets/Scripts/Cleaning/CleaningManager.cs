using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;

public class CleaningManager : MonoBehaviour
{
    public static event Action<int, int> OnTrashCountUpdated;

    // 📢 NUEVO: Referencia al GameObject principal del Panel de UI (asignar en Inspector)
    [Header("UI Activation")]
    [SerializeField] private GameObject trashUIPanel;

    private List<GameObject> remainingTrash = new List<GameObject>();
    private int totalTrashCount = 0;

    private const string TRASH_TAG = "Trash";

    void Start()
    {
        // 📢 ACTIVACIÓN DE LA UI: Asegura que el panel esté activo ANTES de contar la basura
        if (trashUIPanel != null)
        {
            trashUIPanel.SetActive(true);
            Debug.Log("[Manager] Panel de UI activado.");
        }

        // 1. Encuentra el número total de objetos con el Tag "Trash" en la escena.
        GameObject[] trashObjects = GameObject.FindGameObjectsWithTag(TRASH_TAG);
        remainingTrash.AddRange(trashObjects);
        totalTrashCount = remainingTrash.Count;

        // 2. Envía el conteo inicial a la UI (OnTrashCountUpdated)
        SendCurrentState();

        if (totalTrashCount == 0) Debug.LogWarning("No se encontraron objetos con el Tag 'Trash'.");
        else Debug.Log($"Tarea de limpieza iniciada. Total a limpiar: {totalTrashCount}");
    }

    // 📢 NUEVO: Método para enviar el estado actual (usado por la UI al suscribirse)
    public void SendCurrentState()
    {
        int currentCleanedCount = totalTrashCount - remainingTrash.Count;
        OnTrashCountUpdated?.Invoke(currentCleanedCount, totalTrashCount);
    }

    /// <summary>
    /// Llamado por el TrashCan cada vez que un objeto 'Trash' es depositado.
    /// </summary>
    public void TrashDeposited(GameObject trashObject)
    {
        if (trashObject == null) return;

        GameObject itemToClean = remainingTrash.FirstOrDefault(item => item == trashObject);

        if (itemToClean == null)
        {
            Debug.LogWarning($"[Manager] Objeto {trashObject?.name} ya fue contado o no está en la lista de pendientes. Ignorando doble conteo.");
            return;
        }

        // 1. Quita el objeto de la lista de pendientes.
        remainingTrash.Remove(itemToClean);

        // 2. Envía el nuevo conteo
        SendCurrentState();

        // 3. DESTRUCCIÓN
        Destroy(itemToClean);

        // 4. Verifica si se ha completado la tarea.
        if (remainingTrash.Count == 0)
        {
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        Debug.Log("¡NIVEL COMPLETADO! Toda la basura ha sido limpiada.");
    }
}