using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleSpriteChanger : MonoBehaviour
{
    [Header("Sprites ON (Música Sonando)")]
    public Sprite onNormal;
    public Sprite onHighlighted;
    public Sprite onPressed;
    public Sprite onSelected;

    [Header("Sprites OFF (Muteado)")]
    public Sprite offNormal;
    public Sprite offHighlighted;
    public Sprite offPressed;
    public Sprite offSelected;

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
        toggle.onValueChanged.AddListener(UpdateSpriteState);
        UpdateSpriteState(toggle.isOn);
    }

    public void UpdateSpriteState(bool isOn)
    {
        if (targetImage == null || toggle == null) return;

        // 1. Cambiar el Sprite Base (Normal)
        targetImage.sprite = isOn ? onNormal : offNormal;

        // 2. Configurar el SpriteState para las transiciones (Highlighted, Pressed, Selected)
        SpriteState newState = new SpriteState();
        
        if (isOn)
        {
            newState.highlightedSprite = onHighlighted;
            newState.pressedSprite = onPressed;
            newState.selectedSprite = onSelected;
        }
        else
        {
            newState.highlightedSprite = offHighlighted;
            newState.pressedSprite = offPressed;
            newState.selectedSprite = offSelected;
        }

        toggle.spriteState = newState;
        
        // Forzar actualización visual si el botón ya está en un estado (ej: Selected)
        // Esto es un truco para que Unity refresque el estado visual inmediatamente
        toggle.targetGraphic.SetAllDirty();
    }
}
