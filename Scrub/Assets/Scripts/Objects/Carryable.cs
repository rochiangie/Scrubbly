// Carryable.cs

using UnityEngine;
using System.Collections.Generic; // << ESTO ES CRÍTICO

public class Carryable : MonoBehaviour
{
    private Rigidbody rb;
    private Collider[] carryableColliders;
    // Guardamos las referencias de los colliders del jugador para Drop.
    private List<Collider> storedPlayerColliders = new List<Collider>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        carryableColliders = GetComponents<Collider>();
    }

    // 🛑 MÉTODO PickUp: Acepta un array de Colliders del jugador.
    public void PickUp(Transform parent, Collider[] playerColliders)
    {
        if (rb == null || carryableColliders.Length == 0 || playerColliders.Length == 0)
        {
            Debug.LogError("Carryable: Faltan componentes o el PlayerCollider es nulo.");
            return;
        }

        // 1. Guardar el array de Colliders del Player
        storedPlayerColliders.Clear();
        storedPlayerColliders.AddRange(playerColliders);

        // 2. IGNORAR CADA COLLIDER DEL CUBO CON CADA COLLIDER DEL PLAYER
        foreach (Collider cubeCol in carryableColliders)
        {
            foreach (Collider playerCol in playerColliders)
            {
                // Ignorar la colisión
                Physics.IgnoreCollision(cubeCol, playerCol, true);
            }
        }

        // 3. Desactivar la física y la gravedad (se mueve con el jugador)
        rb.useGravity = false;
        rb.isKinematic = true;

        // 4. Establecer la posición de agarre.
        transform.SetParent(parent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        if (rb == null || carryableColliders.Length == 0) return;

        // 1. REACTIVAR TODAS LAS COLISIONES IGNORADAS
        if (storedPlayerColliders.Count > 0)
        {
            foreach (Collider cubeCol in carryableColliders)
            {
                foreach (Collider playerCol in storedPlayerColliders)
                {
                    Physics.IgnoreCollision(cubeCol, playerCol, false);
                }
            }
            storedPlayerColliders.Clear();
        }

        // 2. Devolver el parent al mundo
        transform.SetParent(null);

        // 3. Reactivar la física
        rb.isKinematic = false;
        rb.useGravity = true;

        // 4. Limpiamos la velocidad
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}