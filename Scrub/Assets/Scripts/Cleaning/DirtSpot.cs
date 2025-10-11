using UnityEngine;

public class DirtSpot : MonoBehaviour
{
    // ===============================================
    //           VARIABLES PÚBLICAS
    // ===============================================

    [Header("Efecto de Destrucción")]
    [Tooltip("Prefab del sistema de partículas que se instanciará al destruirse.")]
    public GameObject destructionEffectPrefab;

    // ----------------------------------------------------------------------------------------------------------------------

    [Header("Efecto Visual de Limpieza")]
    [Tooltip("La opacidad mínima que tendrá el material cuando la suciedad esté casi limpia.")]
    [Range(0f, 1f)]
    public float minOpacity = 0.1f; // Para que no desaparezca completamente antes de tiempo

    private Renderer dirtRenderer; // Componente Renderer para acceder al Material
    private Material dirtMaterial; // El material que vamos a modificar

    // ===============================================
    //          CONFIGURACIÓN DE SALUD Y REQUISITOS
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
    //          MÉTODOS DE UNITY
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

            // IMPORTANTE: Configurar el material para soportar transparencia (Modo Blend)
            SetMaterialToFadeMode(dirtMaterial);

            // Establecer la opacidad inicial (completamente visible)
            UpdateVisualAppearance();
        }
    }

    void Start()
    {
        // 🛑 LÍNEA ACTIVADA: Al inicio, registra este objeto en el manager.
        if (DirtManager.Instance != null)
        {
            DirtManager.Instance.RegisterDirtItem();
        }
    }

    // ----------------------------------------------------------------------------------------------------------------------

    // ===============================================
    //          LÓGICA DE LIMPIEZA
    // ===============================================

    public bool CanBeCleanedBy(string toolId)
    {
        if (string.IsNullOrEmpty(requiredToolId))
        {
            return true;
        }
        return requiredToolId == toolId;
    }

    /// <summary>
    /// Se llama desde el script de interacción (PlayerController) al golpear/usar herramienta.
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

    // ... (El resto de funciones UpdateVisualAppearance y SetMaterialToFadeMode permanecen iguales) ...

    /// <summary>
    /// Actualiza la apariencia visual del dirt spot (transparencia).
    /// </summary>
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

    // Función de utilidad para configurar el material a un modo de Blend (Fade)
    private void SetMaterialToFadeMode(Material material)
    {
        // Estos ajustes son para el Shader Standard de Unity, puede variar para URP/HDRP
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
    //          DESTRUCCIÓN (FINAL Y ROBUSTA)
    // ===============================================

    private void HandleDestruction()
    {
        if (isDestroyed) return;
        isDestroyed = true; // Marca como destruido

        // 🛑 LÍNEA ACTIVADA: NOTIFICAR AL MANAGER 🛑
        if (DirtManager.Instance != null)
        {
            DirtManager.Instance.CleanDirtItem(); // Llama a la función de conteo del manager
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
                main.loop = false;
                main.stopAction = ParticleSystemStopAction.Destroy;
                main.startSizeMultiplier = 0.05f;

                ps.Play();

                // Encuentra la duración más larga entre todos los sistemas
                if (ps.main.duration > maxDuration)
                {
                    maxDuration = ps.main.duration;
                }
            }

            // DESTRUIR EL OBJETO PADRE (effectInstance) CON UN RETRASO
            float destroyDelay = maxDuration + 0.5f;
            Destroy(effectInstance, destroyDelay);
        }

        // 3. DESTRUIR EL OBJETO ACTUAL (LA SUCIEDAD)
        Destroy(gameObject);
    }
}