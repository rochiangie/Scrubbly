using UnityEngine;
using UnityEditor;

public class BoxColliderAutoFitter : Editor
{
    [MenuItem("Tools/Collider/Ajustar BoxCollider a Bounds (Seleccionados)", true)]
    private static bool ValidateAutoFitBoxCollider()
    {
        // Esta función de validación comprueba si la opción del menú debe estar activa.
        // Solo estará activa si hay al menos un objeto seleccionado que tenga un BoxCollider.
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go.GetComponent<BoxCollider>() != null)
            {
                return true;
            }
        }
        return false;
    }

    [MenuItem("Tools/Collider/Ajustar BoxCollider a Bounds (Seleccionados)")]
    private static void AutoFitBoxCollider()
    {
        // Se utiliza la función Undo para permitir deshacer la acción (Ctrl+Z).
        Undo.RecordObjects(Selection.gameObjects, "Auto Ajustar BoxCollider");

        foreach (GameObject go in Selection.gameObjects)
        {
            BoxCollider collider = go.GetComponent<BoxCollider>();

            if (collider == null)
            {
                Debug.LogWarning($"El objeto '{go.name}' no tiene un BoxCollider. Saltando.", go);
                continue;
            }

            // 1. Obtener la envoltura total (Bounds) de todos los Renderers hijos.
            Bounds bounds = new Bounds();
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                Debug.LogWarning($"El objeto '{go.name}' no tiene Renderers (MeshRenderer/SkinnedMeshRenderer) para calcular los límites.", go);
                continue;
            }

            // Inicializar los límites con el primer Renderer.
            bounds = renderers[0].bounds;

            // 2. Expandir los límites para incluir todos los Renderers.
            for (int i = 1; i < renderers.Length; i++)
            {
                // Solo se expanden los límites si el Renderer está activo.
                if (renderers[i].enabled)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            // 3. Transformar los límites del espacio mundial al espacio local del Collider.

            // El centro del BoxCollider debe estar en el espacio local del objeto (transform.localPosition).
            Vector3 localCenter = go.transform.InverseTransformPoint(bounds.center);

            // El tamaño del BoxCollider es el tamaño del Bounds sin transformación de escala.
            // Para obtener el tamaño local, dividimos el tamaño mundial por la escala del objeto.
            Vector3 localSize = bounds.size;
            Transform currentTransform = go.transform;

            // CRÍTICO: El tamaño del collider debe compensar la escala del transform.
            localSize.x /= currentTransform.localScale.x;
            localSize.y /= currentTransform.localScale.y;
            localSize.z /= currentTransform.localScale.z;

            // 4. Aplicar los nuevos valores al BoxCollider.
            collider.center = localCenter;
            collider.size = localSize;

            Debug.Log($"BoxCollider de '{go.name}' ajustado. Centro: {localCenter}, Tamaño: {localSize}", go);
        }
    }
}