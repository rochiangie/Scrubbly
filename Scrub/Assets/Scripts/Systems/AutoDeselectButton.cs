using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AutoDeselectButton : MonoBehaviour, IPointerClickHandler
{
    // Este script hace que el botón pierda el "foco" inmediatamente después de hacer click.
    // Esto evita que se quede con el sprite de "Selected" o "Highlighted" pegado.

    public void OnPointerClick(PointerEventData eventData)
    {
        // Deseleccionar el objeto actual en el EventSystem
        EventSystem.current.SetSelectedGameObject(null);
    }
}
