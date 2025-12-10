using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleSpriteChanger : MonoBehaviour
{
    [Header("Sprites")]
    [Tooltip("Sprite cuando el Toggle está ACTIVADO (ON/Música sonando)")]
    public Sprite onSprite;

    [Tooltip("Sprite cuando el Toggle está DESACTIVADO (OFF/Muteado)")]
    public Sprite offSprite;

    [Header("Referencia Visual")]
    [Tooltip("La imagen que cambiará. Si se deja vacío, usará la imagen de este mismo objeto.")]
    public Image targetImage;

    private Toggle toggle;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        if (targetImage == null) targetImage = GetComponent<Image>();
    }

    void Start()
    {
        // 1. Suscribirse al evento
        toggle.onValueChanged.AddListener(UpdateSpriteState);

        // 2. Actualizar visualmente al inicio
        UpdateSpriteState(toggle.isOn);
    }

    // Usamos LateUpdate para "ganarle" al sistema de UI de Unity si intenta cambiar el sprite
    void LateUpdate()
    {
        UpdateVisuals();
    }

    public void UpdateSpriteState(bool isOn)
    {
        // Solo guardamos el estado, el cambio visual real ocurre en LateUpdate
        // para asegurar que persista.
    }

    private void UpdateVisuals()
    {
        if (targetImage == null) return;

        Sprite desiredSprite = toggle.isOn ? onSprite : offSprite;

        // Solo asignamos si es diferente para no sobrecargar
        if (targetImage.sprite != desiredSprite && desiredSprite != null)
        {
            targetImage.sprite = desiredSprite;
        }
    }
}
