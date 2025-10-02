using UnityEngine;

public class DirtSpot : MonoBehaviour
{
    [Header("Limpieza")]
    [SerializeField] float requiredWork = 3f;   // segundos de “frote” total
    [SerializeField] ParticleSystem cleanFX;    // opcional
    [SerializeField] Renderer targetRenderer;   // opcional: para desvanecer

    float workDone = 0f;
    bool cleaned = false;

    public bool IsClean => cleaned;

    // Llamado por CleaningTool
    public void CleanTick(float deltaWork)
    {
        if (cleaned) return;
        workDone += deltaWork;
        UpdateVisuals();

        if (workDone >= requiredWork)
        {
            cleaned = true;
            if (cleanFX) cleanFX.Play();
            GameEvents.DirtCleaned();
            // podés destruir o desactivar:
            gameObject.SetActive(false);
        }
    }

    void UpdateVisuals()
    {
        if (!targetRenderer) return;
        float t = Mathf.Clamp01(workDone / requiredWork);
        // ejemplo simple: bajar alpha si el material lo permite
        if (targetRenderer.material.HasProperty("_Color"))
        {
            var c = targetRenderer.material.color;
            c.a = 1f - t;
            targetRenderer.material.color = c;
        }
    }
}
