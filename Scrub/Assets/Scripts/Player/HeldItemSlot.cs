using UnityEngine;

public class HeldItemSlot : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;
    public ToolDescriptor CurrentTool { get; private set; }

    public bool HasTool => CurrentTool != null;

    public void Equip(ToolDescriptor tool)
    {
        CurrentTool = tool;

        if (tool.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        SetAllCollidersTrigger(tool.gameObject, true);

        var t = tool.transform;

        // 👉 Mantener escala mundial al re-parentar
        t.SetParent(holdPoint, true); // worldPositionStays = true (conserva escala/rot/pos en mundo)

        // Alinear a la mano sin tocar la escala
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        // ❌ No tocar la escala:
        // t.localScale = Vector3.one;  // <--- QUITAR ESTA LÍNEA
    }

    public ToolDescriptor Unequip()
    {
        var tool = CurrentTool;
        if (tool == null) return null;

        if (tool.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = false;

        SetAllCollidersTrigger(tool.gameObject, false);

        // Mantener escala al soltar
        tool.transform.SetParent(null, true); // true = conserva escala mundial
        CurrentTool = null;
        return tool;
    }


    private void SetAllCollidersTrigger(GameObject go, bool isTrigger)
    {
        var colliders = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) c.isTrigger = isTrigger;
    }
}
