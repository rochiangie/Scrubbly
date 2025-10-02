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
        if (rayCamera == null)
        {
            if (Camera.main != null) rayCamera = Camera.main;
            else { Debug.LogError("[Pickup DEBUG] No hay rayCamera ni Camera.main."); return; }
        }

        Ray ray = new Ray(rayCamera.transform.position, rayCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * pickupRange, Color.red);

        // 1) Raycast SIN filtro de layer para ver qué hay enfrente
        if (Physics.Raycast(ray, out var anyHit, pickupRange, ~0, QueryTriggerInteraction.Collide))
        {
            string layerName = LayerMask.LayerToName(anyHit.collider.gameObject.layer);
            Debug.Log($"[Pickup DEBUG] (SIN máscara) Golpeó: {anyHit.collider.name} | Layer: {layerName}");

            // Info adicional útil
            if (anyHit.collider.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast"))
                Debug.LogWarning("[Pickup DEBUG] El objeto está en 'Ignore Raycast' (no lo vas a poder golpear con Raycast filtrado).");

            // Si ve un ToolDescriptor sin filtrar, mostramos
            if (anyHit.collider.GetComponentInParent<ToolDescriptor>() != null)
            {
                var tool = anyHit.collider.GetComponentInParent<ToolDescriptor>();
                Debug.Log($"[Pickup DEBUG] (SIN máscara) Ese hit tiene ToolDescriptor: {tool.toolId}");
            }
        }
        else
        {
            Debug.Log("[Pickup DEBUG] (SIN máscara) No golpeó NADA enfrente.");
        }

        // 2) Raycast CON máscara de grabbableLayer (lo que usa tu lógica real)
        if (Physics.Raycast(ray, out var hit, pickupRange, grabbableLayer, QueryTriggerInteraction.Collide))
        {
            Debug.Log($"[Pickup DEBUG] (CON máscara) Golpeó: {hit.collider.name} | Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

            // Buscar ToolDescriptor en el objeto golpeado o sus padres
            var tool = hit.collider.GetComponentInParent<ToolDescriptor>();
            if (tool != null)
            {
                Debug.Log($"[Pickup DEBUG] Equipando herramienta: {tool.name} (ID: {tool.toolId})");
                heldItemSlot.Equip(tool);
                return;
            }
            else
            {
                Debug.Log("[Pickup DEBUG] (CON máscara) El hit NO tiene ToolDescriptor ni en padres.");
            }
        }
        else
        {
            Debug.Log("[Pickup DEBUG] (CON máscara) Raycast no golpeó nada en grabbableLayer.");
        }

        // 3) Fallback por proximidad (SIN máscara) -> busca cualquier ToolDescriptor cerca
        var probeCenter = rayCamera.transform.position + rayCamera.transform.forward * 1.0f;
        var around = Physics.OverlapSphere(probeCenter, 0.9f, ~0, QueryTriggerInteraction.Collide);
        foreach (var col in around)
        {
            var tool = col.GetComponentInParent<ToolDescriptor>();
            if (tool != null)
            {
                Debug.Log($"[Pickup DEBUG] (OVERLAP SIN máscara) Encontré ToolDescriptor cerca: {tool.name} | Layer: {LayerMask.LayerToName(col.gameObject.layer)}");
                // Si querés, equipá igual para test:
                // heldItemSlot.Equip(tool); return;
            }
        }
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
