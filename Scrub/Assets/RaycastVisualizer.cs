using UnityEngine;

public class RaycastVisualizer : MonoBehaviour
{
    [Tooltip("El Prefab que se usará como marcador (el punto de mira).")]
    public GameObject hitMarkerPrefab;

    [Tooltip("Distancia máxima que alcanzará el Raycast.")]
    public float raycastDistance = 100f;

    private GameObject currentMarker;

    void Start()
    {
        // 1. Instanciar el marcador si no existe
        if (hitMarkerPrefab != null)
        {
            currentMarker = Instantiate(hitMarkerPrefab);
            // Asegúrate de que no se destruya al cargar escenas si es necesario
        }
    }

    void Update()
    {
        // El Raycast se lanza desde el centro de la pantalla/cámara
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance))
        {
            // Raycast golpeó algo

            // 1. Mostrar el marcador
            if (currentMarker != null)
            {
                currentMarker.SetActive(true);

                // 2. Mover el marcador a la posición exacta del impacto
                currentMarker.transform.position = hit.point;

                // 3. Orientar el marcador para que mire hacia la cámara (opcional, útil para Quads/UI 3D)
                currentMarker.transform.rotation = Quaternion.LookRotation(hit.normal);
                // Si quieres que el círculo esté perfectamente plano sobre la superficie, 
                // usa hit.normal para la rotación (como se hizo aquí).
            }

            // Opcional: Para el debug del raycast en la vista de escena
            Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.green);
        }
        else
        {
            // El Raycast no golpeó nada
            if (currentMarker != null)
            {
                // Ocultar el marcador cuando no haya impacto
                currentMarker.SetActive(false);
            }

            // Opcional: Para el debug del raycast en la vista de escena (línea roja)
            Debug.DrawRay(transform.position, transform.forward * raycastDistance, Color.red);
        }
    }
}