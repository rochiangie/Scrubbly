using UnityEngine;

[RequireComponent(typeof(HeldItemSlot))]
public class PickupAndCleanController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera rayCamera;          // se auto-asigna a Camera.main si está null
    [SerializeField] private HeldItemSlot heldItemSlot; // se auto-asigna del mismo GameObject

    [Header("Pickup")]
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private float pickupRange = 2.2f;
    [SerializeField] private LayerMask grabbableLayer;  // capa de herramientas (cubo, mop, etc.)

    [Header("Cleaning")]
    [SerializeField] private KeyCode cleanKey = KeyCode.R;
    [SerializeField] private float cleanRange = 2.5f;
    [SerializeField] private float baseCleanRate = 1f;  // trabajo base por segundo
    [SerializeField] private LayerMask dirtLayer;       // capa de objetos sucios

    private void Awake()
    {
        if (heldItemSlot == null)
        {
            heldItemSlot = GetComponent<HeldItemSlot>();
            if (heldItemSlot == null)
                Debug.LogError("[PickupAndCleanController] No encontré HeldItemSlot en el Player.");
        }

        if (rayCamera == null)
        {
            if (Camera.main != null) rayCamera = Camera.main;
            else Debug.LogError("[PickupAndCleanController] No hay Camera asignada ni Camera.main en escena.");
        }
    }

    private void Update()
    {
        // Si falta algo crítico, no ejecutes lógica para evitar NullRef
        if (heldItemSlot == null || rayCamera == null) return;

        if (Input.GetKeyDown(pickupKey))
        {
            if (heldItemSlot.HasTool)
            {
                var dropped = heldItemSlot.Unequip();
                if (dropped != null && dropped.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.AddForce(transform.forward * 1.5f, ForceMode.VelocityChange);
                }
            }
            else
            {
                TryPickupTool();
            }
        }

        if (Input.GetKey(cleanKey))
        {
            TryCleanTick();
        }
    }

    private void TryPickupTool()
    {
        if (rayCamera == null) { if (Camera.main != null) rayCamera = Camera.main; else { Debug.LogError("[Pickup DEBUG] No hay Camera."); return; } }

        // 1) Raycast con salto de self + máscara Tools
        Vector3 origin = rayCamera.transform.position + rayCamera.transform.forward * 0.15f; // evita pegarte a vos
        Vector3 dir = rayCamera.transform.forward;
        Debug.DrawRay(origin, dir * pickupRange, Color.red);

        // Raycast sin máscara para ver el primer obstáculo real
        if (Physics.Raycast(origin, dir, out var firstHit, pickupRange, ~0, QueryTriggerInteraction.Collide))
        {
            // saltar cualquier collider del Player
            if (!firstHit.collider.transform.IsChildOf(transform))
            {
                // si el primer hit ya es la herramienta y no hay nada delante, equipamos
                var toolOnFirst = firstHit.collider.GetComponentInParent<ToolDescriptor>();
                if (toolOnFirst != null && ((1 << firstHit.collider.gameObject.layer) & grabbableLayer.value) != 0)
                {
                    Debug.Log($"[Pickup] EQUIP (raycast): {toolOnFirst.name}");
                    heldItemSlot.Equip(toolOnFirst);
                    return;
                }
            }
        }

        // 2) RaycastAll por si hay varios hits alineados (saltamos self y buscamos el PRIMER Tools visible)
        var all = Physics.RaycastAll(origin, dir, pickupRange, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(all, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in all)
        {
            if (h.collider.transform.IsChildOf(transform)) continue; // saltear Player
            if (((1 << h.collider.gameObject.layer) & grabbableLayer.value) == 0) continue; // solo Tools

            var tool = h.collider.GetComponentInParent<ToolDescriptor>();
            if (tool != null)
            {
                Debug.Log($"[Pickup] EQUIP (raycastAll): {tool.name}");
                heldItemSlot.Equip(tool);
                return;
            }
        }

        // 3) Fallback: OverlapSphere cerca del centro de mira (solo Tools) → EQUIPA
        Vector3 probe = origin + dir * 1.0f;
        var around = Physics.OverlapSphere(probe, 0.9f, grabbableLayer, QueryTriggerInteraction.Collide);
        foreach (var c in around)
        {
            var tool = c.GetComponentInParent<ToolDescriptor>();
            if (tool != null && !c.transform.IsChildOf(transform))
            {
                Debug.Log($"[Pickup] EQUIP (overlap): {tool.name}");
                heldItemSlot.Equip(tool);
                return;
            }
        }

        Debug.Log("[Pickup] No encontré herramientas para equipar.");
    }



    private void TryCleanTick()
    {
        if (!heldItemSlot.HasTool)
        {
            Debug.Log("[Clean DEBUG] No hay herramienta equipada.");
            return;
        }

        var tool = heldItemSlot.CurrentTool;
        Ray ray = new Ray(rayCamera.transform.position, rayCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * cleanRange, Color.cyan);

        if (Physics.Raycast(ray, out var hit, cleanRange, dirtLayer, QueryTriggerInteraction.Collide))
        {
            Debug.Log($"[Clean DEBUG] Raycast golpeó: {hit.collider.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");

            if (hit.collider.TryGetComponent<DirtSpot>(out var dirt))
            {
                Debug.Log($"[Clean DEBUG] El objeto {hit.collider.name} tiene DirtSpot, aplicando limpieza con {tool.toolId}");
                float work = baseCleanRate * (tool != null ? tool.toolPower : 1f) * Time.deltaTime;
                dirt.CleanTick(work);
            }
            else
            {
                Debug.Log("[Clean DEBUG] El objeto golpeado no tiene DirtSpot.");
            }
        }
        else
        {
            Debug.Log("[Clean DEBUG] Raycast no golpeó nada en la capa Dirt.");
        }
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-wire en editor para evitar olvidos
        if (heldItemSlot == null) heldItemSlot = GetComponent<HeldItemSlot>();
        if (rayCamera == null && Camera.main != null) rayCamera = Camera.main;
    }
#endif
}
