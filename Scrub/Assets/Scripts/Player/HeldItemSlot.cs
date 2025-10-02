using UnityEngine;

public class HeldItemSlot : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;
    public ToolDescriptor CurrentTool { get; private set; }

    public bool HasTool => CurrentTool != null;

    public void Equip(ToolDescriptor tool)
    {
        CurrentTool = tool;

        // Desactivar f�sicas mientras est� en la mano
        if (tool.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        SetAllCollidersTrigger(tool.gameObject, true);

        // Reparent
        tool.transform.SetParent(holdPoint);
        tool.transform.localPosition = Vector3.zero;
        tool.transform.localRotation = Quaternion.identity;
        tool.transform.localScale = Vector3.one;
    }

    public ToolDescriptor Unequip()
    {
        var tool = CurrentTool;
        if (tool == null) return null;

        // Reactivar f�sicas
        if (tool.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
        }
        SetAllCollidersTrigger(tool.gameObject, false);

        tool.transform.SetParent(null);
        CurrentTool = null;
        return tool;
    }

    private void SetAllCollidersTrigger(GameObject go, bool isTrigger)
    {
        var colliders = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) c.isTrigger = isTrigger;
    }
}
