using UnityEngine;
using System.Collections.Generic;

public class PlayerTrashInteraction : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    [Tooltip("Distancia máxima para detectar basura")]
    public float interactionRange = 3f;

    [Header("Input")]
    [Tooltip("Tecla para eliminar basura")]
    public KeyCode trashInteractionKey = KeyCode.F;

    [Header("Layer Mask")]
    [Tooltip("Layers en los que buscar basura")]
    public LayerMask trashLayerMask = -1; // -1 = todos los layers

    private TrashObject currentTrash;
    private List<TrashObject> nearbyTrash = new List<TrashObject>();

    void Update()
    {
        FindNearestTrash();

        if (Input.GetKeyDown(trashInteractionKey))
        {
            TryInteractWithTrash();
        }
    }

    void FindNearestTrash()
    {
        // Limpiar lista de basura cercana
        nearbyTrash.Clear();

        // Buscar todos los objetos TrashObject en el rango
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRange, trashLayerMask);

        foreach (var hitCollider in hitColliders)
        {
            TrashObject trash = hitCollider.GetComponent<TrashObject>();
            if (trash != null && !trash.IsCleaned)
            {
                nearbyTrash.Add(trash);
            }
        }

        // Encontrar la basura más cercana
        TrashObject closestTrash = null;
        float minDistanceSqr = interactionRange * interactionRange;

        foreach (var trash in nearbyTrash)
        {
            if (trash != null && !trash.IsCleaned)
            {
                float distanceSqr = (trash.transform.position - transform.position).sqrMagnitude;
                if (distanceSqr < minDistanceSqr)
                {
                    closestTrash = trash;
                    minDistanceSqr = distanceSqr;
                }
            }
        }

        currentTrash = closestTrash;
    }

    void TryInteractWithTrash()
    {
        if (currentTrash != null && !currentTrash.IsCleaned)
        {
            // ✅ CORRECCIÓN: Usar el método correcto
            currentTrash.CleanTrash(); // O el método que tenga tu TrashObject
            Debug.Log($"🗑️ Basura eliminada: {currentTrash.name}");
        }
        else
        {
            Debug.Log("No se encontró basura cercana para eliminar");
        }
    }

    // Visualizar el rango de interacción en el Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        if (currentTrash != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTrash.transform.position);
        }
    }

    // Métodos para trigger (opcional)
    void OnTriggerEnter(Collider other)
    {
        TrashObject trash = other.GetComponent<TrashObject>();
        if (trash != null && !trash.IsCleaned)
        {
            if (!nearbyTrash.Contains(trash))
                nearbyTrash.Add(trash);
        }
    }

    void OnTriggerExit(Collider other)
    {
        TrashObject trash = other.GetComponent<TrashObject>();
        if (trash != null && nearbyTrash.Contains(trash))
        {
            nearbyTrash.Remove(trash);
            if (currentTrash == trash)
                currentTrash = null;
        }
    }
}