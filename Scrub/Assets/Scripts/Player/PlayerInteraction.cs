using UnityEngine;
using System;


public class PlayerInteraction : MonoBehaviour
{
    [Header("Interacción")]
    public float interactRange = 2.5f;
    [Tooltip("La máscara se usa solo para referencia visual.")]
    public LayerMask toolsLayer;
    public Transform rayOrigin;
    public Transform holdPoint;

    [Header("Refs")]
    public PlayerAnimationController animCtrl;

    private Carryable carried;
    private Transform playerRoot;

    // VARIABLE DE ESTADO para interacción por TRIGGER (Puertas)
    private IInteractable currentInteractable = null;

    void Awake()
    {
        // 🛑 CRÍTICO: Asegurarse de que rayOrigin es la cámara.
        if (!rayOrigin && Camera.main) rayOrigin = Camera.main.transform;
        if (!animCtrl) animCtrl = GetComponentInChildren<PlayerAnimationController>() ?? GetComponent<PlayerAnimationController>();

        playerRoot = transform;

        // Inicializa toolsLayer para que sea visible en el Inspector, aunque la lógica final no la usa.
        int toolsLayerInt = LayerMask.NameToLayer("Tools");
        if (toolsLayerInt != -1)
        {
            toolsLayer = 1 << toolsLayerInt;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            TryInteract();
    }

    // FUNCIONES PÚBLICAS para ser llamadas por el TRIGGER de la puerta
    public void SetCurrentInteractable(IInteractable interactable)
    {
        currentInteractable = interactable;
        Debug.Log("Trigger detectado: Interacción IInteractable (Puerta) posible.");
    }

    public void ClearCurrentInteractable()
    {
        currentInteractable = null;
        Debug.Log("Trigger abandonado: Interacción IInteractable finalizada.");
    }

    void TryInteract()
    {
        // 🛑 Lógica 1: Soltar objeto
        if (carried)
        {
            carried.Drop();
            carried = null;
            animCtrl?.SetHolding(false);
            animCtrl?.TriggerInteract();
            return;
        }

        // 🛑 Lógica 2: Interacción por TRIGGER (Puerta)
        if (currentInteractable != null)
        {
            Debug.Log("[Trigger Interact] Ejecutando interacción IInteractable.");
            currentInteractable.Interact();
            animCtrl?.TriggerInteract();
            return;
        }

        // 🛑 Lógica 3: Interacción por RAYCASTALL (Carryable - Cubo)
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        // Usamos RaycastAll con máscara ~0 (Golpea TODO)
        RaycastHit[] hits = Physics.RaycastAll(ray, interactRange, ~0);

        // Ordenamos los hits por distancia
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            // Debug para ver todo lo que se golpea
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.cyan, 0.5f);

            // Ignoramos el propio personaje
            if (hit.collider.transform.root == playerRoot)
            {
                continue;
            }

            // 🛑 BUSCAR Carryable en el objeto golpeado
            Carryable c = hit.collider.GetComponent<Carryable>();
            // Si no está en el mismo, buscamos en el padre.
            if (c == null) c = hit.collider.GetComponentInParent<Carryable>();

            if (c != null)
            {
                // 🛑 ¡ÉXITO! Cubo encontrado.
                Debug.Log($"[Carryable Raycast] ¡Éxito! Recogiendo: {hit.collider.name}.");

                // Asegurar HoldPoint (Se mantiene la lógica para crearlo si es null)
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
        }

        Debug.Log("[Interacción Fallida] No hay Trigger activo ni Carryable enfrente.");
    }
}