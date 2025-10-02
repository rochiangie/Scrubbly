using UnityEngine;

public class CleaningTool : MonoBehaviour
{
    [Header("Herramienta")]
    [SerializeField] float range = 2.2f;
    [SerializeField] float cleanRate = 1f;      // “trabajo” por segundo
    [SerializeField] LayerMask dirtLayer;
    [SerializeField] Transform rayOrigin;       // usually Camera

    void Reset()
    {
        rayOrigin = Camera.main ? Camera.main.transform : transform;
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, range, dirtLayer))
            {
                if (hit.collider.TryGetComponent(out DirtSpot dirt))
                {
                    dirt.CleanTick(cleanRate * Time.deltaTime);
                }
            }
        }
    }
}
