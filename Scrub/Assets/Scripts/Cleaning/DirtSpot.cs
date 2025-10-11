using UnityEngine;

public class DirtSpot : MonoBehaviour
{
    // ===============================================
    //               VARIABLES PÚBLICAS
    // ===============================================

    [Header("Efecto de Destrucción")]
    [Tooltip("Prefab del sistema de partículas que se instanciará al destruirse.")]
    public GameObject destructionEffectPrefab;

    // ===============================================
    //               CONFIGURACIÓN DE SALUD Y REQUISITOS
    // ===============================================

    [Header("Salud y Requisitos")]
    [Tooltip("La vida máxima que tiene la suciedad.")]
    [SerializeField]
    private float maxHealth = 10f; // Ajusta este valor en el Inspector.

    [Tooltip("El ID de la herramienta requerida para limpiar esta suciedad.")]
    [SerializeField]
    private string requiredToolId = "Sponge";

    private float currentHealth;
    private bool isDestroyed = false; // Bandera para evitar doble conteo/notificación

    // ===============================================
    //               MÉTODOS DE UNITY
    // ===============================================

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        // Al inicio, registra este objeto en el manager si existe una instancia.
        if (DirtManager.Instance != null)
        {
            DirtManager.Instance.RegisterDirtItem();
        }
    }

    // ===============================================
    //               LÓGICA DE LIMPIEZA
    // ===============================================

    public bool CanBeCleanedBy(string toolId)
    {
        if (string.IsNullOrEmpty(requiredToolId))
        {
            return true;
        }

        return requiredToolId == toolId;
    }

    public void CleanHit(float damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            HandleDestruction();
        }
    }

    // ===============================================
    //             DESTRUCCIÓN (FINAL Y ROBUSTA)
    // ===============================================

    private void HandleDestruction()
    {
        if (isDestroyed) return;
        isDestroyed = true; // Marca como destruido

        // 1. NOTIFICAR AL MANAGER
        if (DirtManager.Instance != null)
        {
            DirtManager.Instance.CleanDirtItem();
        }

        // 2. INSTANCIAR Y CONFIGURAR PARTÍCULAS
        if (destructionEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
            ParticleSystem[] allParticleSystems = effectInstance.GetComponentsInChildren<ParticleSystem>();

            // Forzar la escala del objeto instanciado a 1,1,1 para asegurar el tamaño
            effectInstance.transform.localScale = Vector3.one;

            // CALCULAR LA DURACIÓN MÁXIMA
            float maxDuration = 0f;

            foreach (ParticleSystem ps in allParticleSystems)
            {
                var main = ps.main;

                // Desactivar loopeo
                main.loop = false;

                // Importante: No uses stopAction = Destroy aquí si usas el temporizador global.
                // Sin embargo, si lo incluyes no causa problemas y asegura la no repetición de loop.
                main.stopAction = ParticleSystemStopAction.Destroy;

                // Controlar tamaño (al 5% para que sea pequeño)
                main.startSizeMultiplier = 0.05f;

                ps.Play();

                // Encuentra la duración más larga entre todos los sistemas
                if (ps.main.duration > maxDuration)
                {
                    // La duración del sistema se basa en Duration (si no loopea) o StartLifetime
                    maxDuration = ps.main.duration;
                }
            }

            // DESTRUIR EL OBJETO PADRE (effectInstance) CON UN RETRASO
            // Usamos la duración más larga + un pequeño margen (ej. 0.5s)
            float destroyDelay = maxDuration + 0.5f;
            Destroy(effectInstance, destroyDelay);
        }

        // 3. DESTRUIR EL OBJETO ACTUAL (LA SUCIEDAD)
        Destroy(gameObject);
    }
}