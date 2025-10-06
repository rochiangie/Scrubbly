using UnityEngine;
using Unity.Cinemachine;

public class CameraFollowAssigner : MonoBehaviour
{
    [Header("Referencias de Cámara (CM3)")]
    public CinemachineCamera Vcam;

    [Header("Objetivo")]
    public string PlayerTag = "Player";

    // 🛑 NUEVA VARIABLE: Nombre del objeto hijo (e.g., "Head")
    [Tooltip("Nombre exacto del objeto hijo dentro del Player al que la cámara debe apuntar/seguir.")]
    public string HeadObjectName = "Head";

    void Start()
    {
        if (Vcam == null)
        {
            Debug.LogError("[CAMERA] Vcam no está asignada.");
            return;
        }

        var player = GameObject.FindGameObjectWithTag(PlayerTag);
        if (player == null)
        {
            Debug.LogError($"[CAMERA] No se encontró tag '{PlayerTag}'.");
            return;
        }

        // 🛑 LÓGICA CLAVE: Buscar el Transform del objeto hijo ("Head")
        Transform headTarget = player.transform.Find(HeadObjectName);

        if (headTarget == null)
        {
            Debug.LogError($"[CAMERA] No se encontró el objeto hijo '{HeadObjectName}' dentro de '{player.name}'. Usando el cuerpo principal como fallback.");
            headTarget = player.transform; // Fallback: usa el cuerpo principal
        }

        // --- ASIGNACIÓN DE CINEMACHINE (CM3) ---

        // 1. Obtener la estructura de objetivo actual
        var target = Vcam.Target;

        // 2. Asignar el nuevo objetivo (Head o Player)
        target.TrackingTarget = headTarget;
        target.CustomLookAtTarget = false;

        // 3. Asignar la estructura de vuelta a la Vcam
        Vcam.Target = target;

        Debug.Log("[CAMERA] Cinemachine (CM3) asignado a: " + headTarget.name);
    }
}