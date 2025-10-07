using UnityEngine;

public class BoundaryTeleporter : MonoBehaviour
{
    [Header("Punto de Retorno")]
    [Tooltip("La Transform a donde el jugador será teletransportado de vuelta.")]
    public Transform insideTeleportTarget;

    [Header("Tags de Jugador")]
    [Tooltip("El Tag que usa el objeto raíz del jugador (debe ser 'Player').")]
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        // 1. Verificar si el objeto que entró es el jugador
        if (other.CompareTag(playerTag))
        {
            // 2. Intentar encontrar el objeto raíz del jugador
            // Esto es crucial si el collider está en un hijo (como el CharacterController)
            Transform playerRoot = other.transform.root;

            // 3. Verificar si el jugador tiene la etiqueta correcta
            if (playerRoot.CompareTag(playerTag))
            {
                TeleportPlayer(playerRoot.gameObject);
            }
        }
    }

    private void TeleportPlayer(GameObject player)
    {
        if (insideTeleportTarget != null)
        {
            // Opcional: Desactivar temporalmente el CharacterController (si existe) 
            // para evitar problemas con la teletransportación.
            CharacterController cc = player.GetComponent<CharacterController>();
            bool wasEnabled = false;

            if (cc != null)
            {
                wasEnabled = cc.enabled;
                cc.enabled = false;
            }

            // Realizar la teletransportación al punto de retorno
            player.transform.position = insideTeleportTarget.position;
            player.transform.rotation = insideTeleportTarget.rotation;

            // Reactivar el CharacterController
            if (cc != null)
            {
                cc.enabled = wasEnabled;
            }

            Debug.Log($"[Teleporter] Jugador teletransportado de vuelta a {insideTeleportTarget.name}.");
        }
        else
        {
            Debug.LogError("[Teleporter] ¡ERROR! El 'Inside Teleport Target' no está asignado.");
        }
    }
}