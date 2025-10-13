using UnityEngine;
using System.Collections; // Necesario para Coroutines

public class DirtSpot : MonoBehaviour
{
    // ===============================================
    //               VARIABLES PÚBLICAS Y PRIVADAS
    // ===============================================

    // 🔴 NUEVA PROPIEDAD: Requerida por TaskManager para contar la suciedad al inicio.
    /// <summary>Bandera para indicar si este punto de suciedad ya ha sido limpiado.</summary>
    public bool IsCleaned { get; private set; } = false; // Empieza como sucio (false)

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
    // La bandera isDestroyed ahora se usará como sinónimo de IsCleaned
    private bool isHandlingDestruction = false;

    // ===============================================
    //               MÉTODOS DE UNITY
    // ===============================================

    void Awake()
    {
        currentHealth = maxHealth;
        isHandlingDestruction = false; // Asegurar estado inicial

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
        // NOTA: Se eliminó la dependencia de DirtManager.Instance.RegisterDirtItem();
        // El TaskManager ya encuentra todos los DirtSpots con FindObjectsOfType<DirtSpot>().
    }

    // ===============================================
    //               LÓGICA DE LIMPIEZA
    // ===============================================

    public bool CanBeCleanedBy(string toolId)
    {
        // Si ya está limpio o en proceso de destrucción, no se puede limpiar más.
        if (IsCleaned || isHandlingDestruction) return false;

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
        if (isHandlingDestruction) return;

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
    //               DESTRUCCIÓN Y FINALIZACIÓN
    // ===============================================

    private void HandleDestruction()
    {
        if (isHandlingDestruction) return;
        isHandlingDestruction = true;
        IsCleaned = true; // 🔴 MARCAR COMO LIMPIO PARA EL TaskManager

        // 1. NOTIFICAR AL MANAGER DEL PROGRESO
        // 🔴 CRITICAL FIX: Usar el evento global que escucha el TaskManager
        //GameEvents.OnAnyDirtCleaned?.Invoke();
        GameEvents.DirtCleaned();


        // 2. LLAMADA CRÍTICA A SFX: Disparar el sonido de limpieza antes de destruirse
        // (Aunque PlayCleanSFX ya se llama en CleaningController, este podría ser un sonido final de "desaparición").
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCleanSFX();
        }

        // 3. INSTANCIAR PARTÍCULAS
        if (destructionEffectPrefab != null)
        {
            StartCoroutine(DestroyWithParticles(destructionEffectPrefab));
        }

        // 4. DESACTIVAR EL RENDERER Y COLISIONADOR ANTES DE DESTRUIRSE
        if (dirtRenderer != null) dirtRenderer.enabled = false;
        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        // 5. DESTRUIR EL OBJETO ACTUAL (LA SUCIEDAD) después de un pequeño retraso
        Destroy(gameObject, 0.1f);
    }

    // Coroutine para gestionar la destrucción del efecto de partículas
    private IEnumerator DestroyWithParticles(GameObject effectPrefab)
    {
        GameObject effectInstance = Instantiate(effectPrefab, transform.position, Quaternion.identity);

        // Buscar el ParticleSystem más largo para determinar el tiempo de vida
        float maxDuration = 0f;
        ParticleSystem[] allParticleSystems = effectInstance.GetComponentsInChildren<ParticleSystem>(true);

        if (allParticleSystems.Length == 0)
        {
            // Si no hay PS, simplemente destruir
            Destroy(effectInstance, 2.0f);
            yield break;
        }

        foreach (ParticleSystem ps in allParticleSystems)
        {
            var main = ps.main;

            // Si el prefab tiene loop=true accidentalmente, lo corregimos
            main.loop = false;

            // Usamos StartDelay + Duration para el cálculo
            float duration = main.startDelay.constant + main.duration;
            if (duration > maxDuration)
            {
                maxDuration = duration;
            }

            ps.Play();
        }

        // Esperar la duración máxima de las partículas + un pequeño margen
        float destroyDelay = maxDuration + 0.1f;
        yield return new WaitForSeconds(destroyDelay);

        // Asegurarse de que el objeto de partículas se destruya si aún existe
        if (effectInstance != null)
        {
            Destroy(effectInstance);
        }
    }

    // ===============================================
    //               APARIENCIA VISUAL
    // ===============================================

    private void UpdateVisualAppearance()
    {
        if (dirtMaterial == null) return;

        float healthRatio = currentHealth / maxHealth;
        float currentOpacity = Mathf.Lerp(minOpacity, 1f, healthRatio);

        Color color = dirtMaterial.color;
        color.a = currentOpacity;
        dirtMaterial.color = color;
    }

    private void SetMaterialToFadeMode(Material material)
    {
        // Esto es una configuración de shader estándar para transparencia tipo "Fade"
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}