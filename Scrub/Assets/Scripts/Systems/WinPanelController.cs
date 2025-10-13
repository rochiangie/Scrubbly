using UnityEngine;
using System.Collections;

// Este script gestiona el comportamiento del panel de victoria.
public class WinPanelController : MonoBehaviour
{
    [Header("Referencias de Reinicio")]
    [Tooltip("Arrastra aquí el script GameResetManager de la escena. ¡CRUCIAL!")]
    public GameResetManager resetManager;

    [Header("Temporizador")]
    [Tooltip("Tiempo en segundos que el panel estará visible antes de reiniciar.")]
    public float autoRestartDelay = 5f;

    private void OnEnable()
    {
        // 1. Verificación de Referencia
        if (resetManager == null)
        {
            // Intenta encontrarlo si no está asignado (asumiendo que es persistente o único)
            resetManager = FindObjectOfType<GameResetManager>();
        }

        if (resetManager == null)
        {
            Debug.LogError("[WIN PANEL] ❌ GameResetManager es NULO. El juego no se reiniciará automáticamente. Asigna la referencia.");
            return;
        }

        // 2. Iniciar la Coroutine
        Debug.Log($"[WIN PANEL] Panel activo. Reinicio automático en {autoRestartDelay} segundos...");
        StartCoroutine(AutoRestartCoroutine());
    }

    private IEnumerator AutoRestartCoroutine()
    {
        // Espera el tiempo configurado.
        yield return new WaitForSeconds(autoRestartDelay);

        Debug.Log("[WIN PANEL] Tiempo cumplido. Llamando a la función de reinicio.");

        // 3. Llamar a la función de reinicio
        resetManager.ReturnToMenuAndResetGame();
    }

    private void OnDisable()
    {
        // Detiene la coroutine si el panel se desactiva antes de tiempo (seguridad)
        StopAllCoroutines();
    }
}