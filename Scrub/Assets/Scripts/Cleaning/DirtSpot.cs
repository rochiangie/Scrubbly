using UnityEngine;

public class DirtSpot : MonoBehaviour
{
    [Header("Limpieza")]
    [SerializeField] float requiredWork = 3f;   // segundos de “frote”
    [SerializeField] ParticleSystem cleanFX;    // opcional
    [SerializeField] Renderer targetRenderer;   // opcional: desvanecer

    float workDone = 0f;
    bool cleaned = false;

    public bool IsClean => cleaned;

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
            gameObject.SetActive(false); // o Destroy(gameObject);
        }
    }

    void UpdateVisuals()
    {
        if (!targetRenderer) return;
        float t = Mathf.Clamp01(workDone / requiredWork);
        if (targetRenderer.material.HasProperty("_Color"))
        {
            var c = targetRenderer.material.color;
            c.a = 1f - t;
            targetRenderer.material.color = c;
        }
    }
}
