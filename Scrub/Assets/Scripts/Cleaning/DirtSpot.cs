using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class DirtSpot : MonoBehaviour
{
    // ===============================================
    //               VARIABLES PÚBLICAS Y PRIVADAS
    // ===============================================

    /// <summary>Bandera para indicar si este punto de suciedad ya ha sido limpiado.</summary>
    public bool IsCleaned { get; private set; } = false;

    [Header("Efecto de Destrucción")]
    [Tooltip("Prefab del sistema de partículas que se instanciará al destruirse.")]
    public GameObject destructionEffectPrefab;

    [Header("Efecto Visual de Limpieza")]
    [Tooltip("La opacidad mínima que tendrá el material cuando la suciedad esté casi limpia.")]
    [Range(0f, 1f)]
    public float minOpacity = 0.1f;

    private Renderer dirtRenderer;
    private Material dirtMaterial;

    [Header("Salud y Requisitos")]
    [Tooltip("La vida máxima que tiene la suciedad.")]
    [SerializeField]
    private float maxHealth = 10f;

    [Tooltip("El ID de la herramienta requerida para limpiar esta suciedad.")]
    [SerializeField]
    private string requiredToolId = "Sponge";

    private float currentHealth;
    private bool isHandlingDestruction = false;

    // ✅ NUEVO: Para evitar notificaciones duplicadas
    private bool alreadyNotified = false;

    // ===============================================
    //               MÉTODOS DE UNITY
    // ===============================================

    void Awake()
    {
        currentHealth = maxHealth;
        isHandlingDestruction = false;
        alreadyNotified = false;

        // Inicialización de la transparencia
        dirtRenderer = GetComponent<Renderer>();
        if (dirtRenderer != null)
        {
            dirtMaterial = dirtRenderer.material;
            SetMaterialToFadeMode(dirtMaterial);
            UpdateVisualAppearance();
        }
    }

    void Start()
    {
        // ❌ ELIMINAMOS LÓGICA INCONSISTENTE DE AGREGARSE A LA LISTA DEL TASKMANAGER
        // DEBEMOS ASUMIR QUE TASKMANAGER YA LO HIZO CORRECTAMENTE.
    }

    // ===============================================
    //               LÓGICA DE LIMPIEZA
    // ===============================================

    public bool CanBeCleanedBy(string toolId)
    {
        if (IsCleaned || isHandlingDestruction) return false;

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
        // Si ya estamos manejando la destrucción, ignorar golpes.
        if (isHandlingDestruction || IsCleaned) return;

        // 1. Aplica el daño
        currentHealth -= damage;
        Debug.Log($"[Dirt HIT] {gameObject.name} recibió {damage:F2} de daño. Vida restante: {currentHealth:F2}");

        // 2. Actualiza la apariencia visual inmediatamente
        UpdateVisualAppearance();

        // 3. Comprueba si debe destruirse
        if (currentHealth <= 0)
        {
            HandleDestruction();
        }
    }

    // ===============================================
    //               DESTRUCCIÓN Y FINALIZACIÓN
    // ===============================================

    private void HandleDestruction()
    {
        if (isHandlingDestruction || alreadyNotified) return;
        isHandlingDestruction = true;
        alreadyNotified = true; // ✅ EVITAR NOTIFICACIONES DUPLICADAS
        IsCleaned = true;

        // 🛑 1. NOTIFICAR AL TASKMANAGER (USANDO gameObject.name)
        // TaskManager buscará el objeto por su nombre.
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.NotifySpotCleaned(gameObject.name);
            Debug.Log($"✅ DirtSpot {gameObject.name} notificado al TaskManager");
        }
        else
        {
            Debug.LogError($"❌ TaskManager.Instance es NULL al intentar notificar {gameObject.name}");
        }

        // 2. LLAMADA CRÍTICA A SFX
        if (AudioManager.Instance != null)
        {
            // AudioManager.Instance.PlayCleanSFX(); 
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

        // 5. DESTRUIR EL OBJETO ACTUAL después de un pequeño retraso
        Destroy(gameObject, 0.1f);
    }

    // Coroutine para gestionar la destrucción del efecto de partículas
    private IEnumerator DestroyWithParticles(GameObject effectPrefab)
    {
        GameObject effectInstance = Instantiate(effectPrefab, transform.position, Quaternion.identity);

        float maxDuration = 0f;
        ParticleSystem[] allParticleSystems = effectInstance.GetComponentsInChildren<ParticleSystem>(true);

        if (allParticleSystems.Length == 0)
        {
            Destroy(effectInstance, 2.0f);
            yield break;
        }

        foreach (ParticleSystem ps in allParticleSystems)
        {
            var main = ps.main;
            main.loop = false;
            float duration = main.startDelay.constant + main.duration;
            if (duration > maxDuration)
            {
                maxDuration = duration;
            }
            ps.Play();
        }

        float destroyDelay = maxDuration + 0.1f;
        yield return new WaitForSeconds(destroyDelay);

        if (effectInstance != null)
        {
            Destroy(effectInstance);
        }
    }

    // ===============================================
    //               APARIENCIA VISUAL
    // ===============================================

    private void UpdateVisualAppearance()
    {
        if (dirtMaterial == null) return;

        // El ratio va de 0 (limpio) a 1 (sucio)
        float healthRatio = Mathf.Clamp01(currentHealth / maxHealth);

        // El factor de opacidad va de minOpacity (cuando está casi limpio) a 1f (sucio)
        float currentOpacity = Mathf.Lerp(minOpacity, 1f, healthRatio);

        Color color = dirtMaterial.color;
        color.a = currentOpacity;
        dirtMaterial.color = color;
    }

    private void SetMaterialToFadeMode(Material material)
    {
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    // ✅ NUEVO: Para debug
    void OnMouseDown()
    {
        Debug.Log($"🧹 DirtSpot: {gameObject.name}, Salud: {currentHealth}/{maxHealth}, Limpiado: {IsCleaned}");
    }

    public string GetRequiredToolId()
    {
        return requiredToolId;
    }
}