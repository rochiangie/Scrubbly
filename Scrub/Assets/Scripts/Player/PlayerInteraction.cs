using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interacción")]
    public float interactRange = 2.5f;
    public LayerMask interactLayer = ~0;
    public Transform rayOrigin;  // normalmente la cámara
    public Transform holdPoint;  // un empty en el pecho

    [Header("Refs")]
    public PlayerAnimationController animCtrl;

    private Carryable carried;

    void Awake()
    {
        if (!rayOrigin && Camera.main) rayOrigin = Camera.main.transform;
        if (!animCtrl) animCtrl = GetComponentInChildren<PlayerAnimationController>() ?? GetComponent<PlayerAnimationController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            TryInteract();
    }

    void TryInteract()
    {
        // Si ya llevo algo, suelto
        if (carried)
        {
            carried.Drop();
            carried = null;
            animCtrl?.SetHolding(false);
            animCtrl?.TriggerInteract();
            return;
        }

        // Buscar algo delante
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            // 1) Objetos cargables
            if (hit.collider.TryGetComponent(out Carryable c))
            {
                if (!holdPoint)
                {
                    var hp = new GameObject("HoldPoint").transform;
                    hp.SetParent(transform);
                    hp.localPosition = new Vector3(0, 1.2f, 0.6f);
                    holdPoint = hp;
                }

                c.PickUp(holdPoint);
                carried = c;
                animCtrl?.SetHolding(true);
                animCtrl?.TriggerInteract();
                return;
            }

            // 2) Cualquier cosa que implemente IInteractable (interfaz)
            var interactable = hit.collider.GetComponent(typeof(IInteractable)) as IInteractable;
            if (interactable != null)
            {
                interactable.Interact();
                animCtrl?.TriggerInteract();
            }
        }
    }
}

public interface IInteractable { void Interact(); }
