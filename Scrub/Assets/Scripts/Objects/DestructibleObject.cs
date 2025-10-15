using UnityEngine;

// Asegúrate de que el objeto que use este script tenga un Collider con isTrigger = true
// e implementa la interfaz IAttackable para ser compatible con PlayerInteraction.cs
public class DestructibleObject : MonoBehaviour, IAttackable
{
    // --- Configuración del Objeto ---
    // Quitamos la variable 'puntosDeValorSentimental' porque estos objetos
    // solo contribuyen a la limpieza (DirtCleaned), no al score sentimental.
    // [Header("Configuración de Destrucción")]
    // public int puntosDeValorSentimental = 5; 

    [Header("Efectos")]
    [Tooltip("El Prefab del sistema de partículas a instanciar al destruir.")]
    public GameObject prefabParticulas;
    [Tooltip("El clip de audio a reproducir al destruir.")]
    public AudioClip sonidoDeDestruccion;

    private AudioSource audioSource;
    private bool isDestroyed = false;

    void Start()
    {
        // Se asegura de tener un AudioSource en el objeto para reproducir el sonido
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false; // Importante para que no suene al inicio
    }

    // Método requerido por IAttackable, llamado por PlayerInteraction cuando se ataca con 'F' estando cerca.
    public void ReceiveAttack()
    {
        if (isDestroyed) return; // Evita doble destrucción

        isDestroyed = true;

        // 1. Sonido y Partículas
        if (sonidoDeDestruccion != null)
        {
            // Reproduce el sonido una sola vez
            audioSource.PlayOneShot(sonidoDeDestruccion);
        }

        if (prefabParticulas != null)
        {
            // Instancia las partículas en la posición del objeto
            Instantiate(prefabParticulas, transform.position, Quaternion.identity);
        }

        // 2. Notificación de Limpieza para el TaskManager
        // Esto incrementa el contador de limpieza general.
        GameEvents.DirtCleaned();

        // 3. Destrucción del Objeto (con un pequeño retraso para que el sonido termine)
        Destroy(gameObject, 0.5f);
    }
}