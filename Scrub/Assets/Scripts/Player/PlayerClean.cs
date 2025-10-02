using UnityEngine;

public class PlayerClean : MonoBehaviour
{
    [Header("Limpieza")]
    [SerializeField] private float range = 2.5f;
    [SerializeField] private float cleanRate = 1f;
    [SerializeField] private LayerMask dirtLayer;
    [SerializeField] private Transform rayOrigin; // por defecto la cámara

    [Header("Refs")]
    [SerializeField] private PlayerAnimationController animCtrl;

    private bool cleaningActive;

    private void Start()
    {
        if (!rayOrigin && Camera.main) rayOrigin = Camera.main.transform;
        if (!animCtrl) animCtrl = GetComponentInParent<PlayerAnimationController>() ?? GetComponent<PlayerAnimationController>();
    }

    private void Update()
    {
        bool wasCleaning = cleaningActive;
        cleaningActive = false;

        if (Input.GetMouseButton(0))
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, range, dirtLayer))
            {
                if (hit.collider.TryGetComponent(out DirtSpot dirt))
                {
                    cleaningActive = true;
                    dirt.CleanTick(cleanRate * Time.deltaTime);
                }
            }
        }

        if (cleaningActive != wasCleaning)
            animCtrl?.SetCleaning(cleaningActive);
    }
}
