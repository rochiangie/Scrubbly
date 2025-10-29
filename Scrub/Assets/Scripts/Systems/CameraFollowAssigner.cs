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

        // 🛑 LÓGICA CLAVE: Buscar el Transform del objeto hijo en toda la jerarquía
        Transform headTarget = FindDeepChild(player.transform, HeadObjectName);

        if (headTarget == null)
        {
            Debug.LogError($"[CAMERA] No se encontró el objeto hijo '{HeadObjectName}' en la jerarquía del Player. Usando el cuerpo principal como fallback.");
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

    // 🔍 FUNCIÓN AGREGADA: Busca un hijo por nombre en la jerarquía profunda
    private Transform FindDeepChild(Transform aParent, string aName)
    {
        // 1. Verificar si el padre actual es el objetivo
        if (aParent.name == aName) return aParent;

        // 2. Iterar sobre todos los hijos
        foreach (Transform child in aParent)
        {
            // 3. Llamada recursiva a la función
            Transform result = FindDeepChild(child, aName);

            // 4. Si se encuentra el resultado, lo devuelve
            if (result != null)
                return result;
        }
        // 5. Si no se encuentra en esta rama, devuelve null
        return null;
    }
}