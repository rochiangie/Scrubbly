using UnityEngine;

public class PlayerClean : MonoBehaviour
{
    [Header("Limpieza")]
    [SerializeField] private float range = 2.5f;       // distancia máxima de limpieza
    [SerializeField] private float cleanRate = 1f;     // "trabajo" por segundo
    [SerializeField] private LayerMask dirtLayer;      // capa de suciedad
    [SerializeField] private Transform rayOrigin;      // punto desde donde mira (por defecto la cámara)

    private void Start()
    {
        if (rayOrigin == null && Camera.main != null)
            rayOrigin = Camera.main.transform;
    }

    private void Update()
    {
        // Click izquierdo para limpiar
        if (Input.GetMouseButton(0))
        {
            TryClean();
        }
    }

    private void TryClean()
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range, dirtLayer))
        {
            DirtSpot dirt = hit.collider.GetComponent<DirtSpot>();
            if (dirt != null)
            {
                dirt.CleanTick(cleanRate * Time.deltaTime);
            }
        }
    }
}
