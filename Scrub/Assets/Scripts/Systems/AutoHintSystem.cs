using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AutoHintSystem : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Tiempo en segundos antes de mostrar las pistas (300s = 5 min).")]
    public float timeToShowHints = 300f;

    [Header("Colores por Tipo")]
    public Color glassColor = new Color(0f, 1f, 1f); // Cyan
    public Color plasticColor = new Color(1f, 1f, 0f); // Amarillo
    public Color paperColor = new Color(0.6f, 0.4f, 0.2f); // Marrón
    public Color hazardousColor = new Color(1f, 0f, 0f); // Rojo
    public Color organicColor = new Color(0.4f, 1f, 0.4f); // Verde
    public Color bagsColor = new Color(0.8f, 0.8f, 0.8f); // Gris
    public Color dirtSpotColor = new Color(0.5f, 0.25f, 0f); // Marrón oscuro
    public Color memorieColor = new Color(1f, 0f, 1f); // Magenta
    public Color defaultHintColor = new Color(1f, 0.5f, 0f); // Naranja (Fallback)

    [Tooltip("Ancho del outline de pista.")]
    public float hintWidth = 5f;

    private bool hintsActivated = false;

    void Start()
    {
        StartCoroutine(HintTimerRoutine());
    }

    private IEnumerator HintTimerRoutine()
    {
        Debug.Log($"[AutoHint] Temporizador iniciado. Pistas en {timeToShowHints} segundos.");
        yield return new WaitForSeconds(timeToShowHints);
        ActivateHints();
    }

    [ContextMenu("Activar Pistas Ahora")]
    public void ActivateHints()
    {
        if (hintsActivated) return;
        hintsActivated = true;

        Debug.Log("[AutoHint] 💡 ¡Activando pistas visuales con colores específicos!");

        Outline[] allOutlines = FindObjectsByType<Outline>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        int count = 0;
        foreach (Outline outline in allOutlines)
        {
            if (outline.CompareTag("Player")) continue;

            // Determinar color según el Tag
            Color targetColor = defaultHintColor;
            string tag = outline.gameObject.tag;

            if (tag == "Vidrio") targetColor = glassColor;
            else if (tag == "Plastico") targetColor = plasticColor;
            else if (tag == "Papeles") targetColor = paperColor;
            else if (tag == "Peligrosos") targetColor = hazardousColor;
            else if (tag == "Organico") targetColor = organicColor;
            else if (tag == "Bolsas") targetColor = bagsColor;
            else if (tag == "Memorie") targetColor = memorieColor;
            else if (outline.GetComponent<DirtSpot>() != null) targetColor = dirtSpotColor;

            // Activar y configurar
            outline.OutlineColor = targetColor;
            outline.OutlineWidth = hintWidth;
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.enabled = true;
            
            count++;
        }

        Debug.Log($"[AutoHint] Se activaron {count} outlines de ayuda.");
    }
}
