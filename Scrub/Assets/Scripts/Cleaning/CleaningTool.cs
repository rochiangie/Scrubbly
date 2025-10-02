// CleaningTool.cs (tu script, con chequeo de herramienta en mano)
using UnityEngine;

public class CleaningTool : MonoBehaviour
{
    [Header("Herramienta")]
    [SerializeField] float range = 2.2f;
    [SerializeField] float cleanRate = 1f;      // “trabajo” base por segundo
    [SerializeField] LayerMask dirtLayer;
    [SerializeField] Transform rayOrigin;       // usually Camera
    [SerializeField] HeldItemSlot heldItemSlot; // referencia al slot de la mano

    void Awake()
    {
        if (rayOrigin == null && Camera.main != null)
            rayOrigin = Camera.main.transform;
    }

    void Update()
    {
        if (!Input.GetMouseButton(0)) return;
        if (rayOrigin == null || heldItemSlot == null) return;

        var tool = heldItemSlot.CurrentTool;
        if (tool == null)
        {
            // No hay herramienta equipada -> no limpia
            return;
        }

        Debug.DrawRay(rayOrigin.position, rayOrigin.forward * range, Color.cyan);

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, dirtLayer, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.TryGetComponent(out DirtSpot dirt))
            {
                if (!dirt.CanBeCleanedBy(tool.toolId))
                {
                    // Herramienta incorrecta para este spot
                    // Debug.Log($"Se requiere {required} y tenés {tool.toolId}");
                    return;
                }

                // Aplica la potencia de la herramienta
                float work = cleanRate * tool.toolPower * Time.deltaTime;
                dirt.CleanTick(work);
            }
        }
    }
}
