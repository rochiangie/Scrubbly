// CleaningManager.cs - FINAL Y CORREGIDO

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // 📢 NUEVO

public class CleaningManager : MonoBehaviour
{
    private List<GameObject> remainingTrash = new List<GameObject>(); // 📢 CAMBIO: Lista de objetos VIVOS
    private int totalTrashCount = 0;

    private const string TRASH_TAG = "Trash";

    void Start()
    {
        // 1. Encuentra el número total de objetos y los almacena en la lista.
        GameObject[] trashObjects = GameObject.FindGameObjectsWithTag(TRASH_TAG);
        remainingTrash.AddRange(trashObjects); // Añade todos los objetos a la lista.

        totalTrashCount = remainingTrash.Count;

        if (totalTrashCount == 0)
        {
            Debug.LogWarning("No se encontraron objetos con el Tag 'Trash'.");
        }
        else
        {
            Debug.Log($"Tarea de limpieza iniciada. Total a limpiar: {totalTrashCount}");
        }
    }

    /// <summary>
    /// Llamado por el TrashCan cada vez que un objeto 'Trash' es depositado.
    /// </summary>
    /// <param name="trashObject">El GameObject que fue depositado.</param>
    public void TrashDeposited(GameObject trashObject)
    {
        // 📢 SEGURIDAD CRUCIAL: Solo procedemos si el objeto AÚN NO ha sido contado
        // y todavía está en nuestra lista de objetos vivos.
        if (trashObject == null || !remainingTrash.Contains(trashObject))
        {
            Debug.LogWarning($"[Manager] Objeto ya contado o nulo: {trashObject?.name}. Ignorando doble conteo.");
            return;
        }

        // 1. Quita el objeto de la lista de pendientes.
        remainingTrash.Remove(trashObject);

        int currentCleanedCount = totalTrashCount - remainingTrash.Count;

        Debug.Log($"Basura depositada y contada: {currentCleanedCount} / {totalTrashCount}");

        // 2. DESTRUCCIÓN: Destruye el objeto.
        Destroy(trashObject);

        // 3. Verifica si se ha completado la tarea.
        if (remainingTrash.Count == 0)
        {
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        Debug.Log("¡NIVEL COMPLETADO! Toda la basura ha sido limpiada.");
        // SceneManager.LoadScene("SiguienteNivel");
    }
}