using UnityEngine;

public class DirtSpot : MonoBehaviour
{
    // ===============================================
    //               VARIABLES PÚBLICAS Y PRIVADAS
    // ===============================================

    [Header("Efecto de Destrucción")]
    [Tooltip("Prefab del sistema de partículas que se instanciará al destruirse.")]
    public GameObject destructionEffectPrefab;

    [Header("Efecto Visual de Limpieza")]
    [Tooltip("La opacidad mínima que tendrá el material cuando la suciedad esté casi limpia.")]
    [Range(0f, 1f)]
    public float minOpacity = 0.1f;

    private Renderer dirtRenderer; // Componente Renderer para acceder al Material
    private Material dirtMaterial; // El material que vamos a modificar

    [Header("Salud y Requisitos")]
    [Tooltip("La vida máxima que tiene la suciedad.")]
    [SerializeField]
    private float maxHealth = 10f;

    [Tooltip("El ID de la herramienta requerida para limpiar esta suciedad.")]
    [SerializeField]
    private string requiredToolId = "Sponge";

    private float currentHealth;
    private bool isDestroyed = false; // Bandera para evitar doble conteo/notificación

    // ===============================================
    //              MÉTODOS DE UNITY
    // ===============================================

    void Awake()
    {
        currentHealth = maxHealth;

        // Inicialización de la transparencia
        dirtRenderer = GetComponent<Renderer>();
        if (dirtRenderer != null)
        {
            // Crea una instancia del material para que solo este objeto se vea afectado
            dirtMaterial = dirtRenderer.material;

            // Configurar el material para soportar transparencia
            SetMaterialToFadeMode(dirtMaterial);

            // Establecer la opacidad inicial (completamente visible)
            UpdateVisualAppearance();
        }
    }

    void Start()
    {
        // Al inicio, registra este objeto en el manager (asumiendo que DirtManager existe).
        // Esto es necesario para el conteo de progreso del juego.
        if (DirtManager.Instance != null)
        {
            // Asumo que tu DirtManager tiene un método RegisterDirtItem()
            DirtManager.Instance.RegisterDirtItem();
        }
    }

    // ===============================================
    //              LÓGICA DE LIMPIEZA
    // ===============================================

    public bool CanBeCleanedBy(string toolId)
    {
        // Si no se requiere una herramienta específica, cualquier herramienta funciona.
        if (string.IsNullOrEmpty(requiredToolId))
        {
            return true;
        }
        return requiredToolId == toolId;
    }

    /// <summary>
    /// Se llama desde el script de interacción (CleaningController) al golpear.
    /// </summary>
    public void CleanHit(float damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth); // Asegurar que la salud no sea negativa

        // Llama a la función para desvanecer el objeto
        UpdateVisualAppearance();

        if (currentHealth <= 0)
        {
            HandleDestruction();
        }
    }

    // ===============================================
    //              APARIENCIA VISUAL
    // ===============================================

    private void UpdateVisualAppearance()
    {
        if (dirtMaterial == null) return;

        // Calcular el porcentaje de salud restante (0 a 1)
        float healthRatio = currentHealth / maxHealth;

        // Mapear el ratio de salud a un valor de opacidad
        float currentOpacity = Mathf.Lerp(minOpacity, 1f, healthRatio);

        // Crear un nuevo color con la opacidad calculada
        Color color = dirtMaterial.color;
        color.a = currentOpacity;
        dirtMaterial.color = color;
    }

    private void SetMaterialToFadeMode(Material material)
    {
        // Configuración para permitir transparencia (Shader Standard/Legacy)
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }


    // ===============================================
    //            DESTRUCCIÓN Y SFX
    // ===============================================

    private void HandleDestruction()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // 🔥 1. LLAMADA CRÍTICA A SFX: Disparar el sonido de limpieza antes de destruirse
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCleanSFX();
        }

        // 2. NOTIFICAR AL MANAGER DEL PROGRESO
        if (DirtManager.Instance != null)
        {
            // Llama a la función de conteo del manager
            // (Asumimos que tu DirtManager tiene un método CleanDirtItem())
            DirtManager.Instance.CleanDirtItem();
        }

        // 3. INSTANCIAR PARTÍCULAS
        if (destructionEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);

            // Forzar la escala (si es necesario)
            effectInstance.transform.localScale = Vector3.one;

            // Calcular la duración máxima de las partículas
            float maxDuration = 0f;
            ParticleSystem[] allParticleSystems = effectInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in allParticleSystems)
            {
                var main = ps.main;
                main.loop = false;
                main.stopAction = ParticleSystemStopAction.Destroy;

                ps.Play();

                if (ps.main.duration > maxDuration)
                {
                    maxDuration = ps.main.duration;
                }
            }

            // Destruir el objeto padre de las partículas después de que terminen.
            float destroyDelay = maxDuration + 0.5f;
            Destroy(effectInstance, destroyDelay);
        }

        // 4. DESTRUIR EL OBJETO ACTUAL (LA SUCIEDAD)
        Destroy(gameObject);
    }
}