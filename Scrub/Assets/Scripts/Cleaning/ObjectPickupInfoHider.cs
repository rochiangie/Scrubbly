using UnityEngine;

public class ObjectPickupInfoHider : MonoBehaviour
{
    public GameObject infoPanel;   // El panel que muestra la info del objeto
    public bool isPickedUp = false; // El player lo levantó

    // Llamá esta función desde tu script de interacción cuando el jugador lo levante.
    public void OnPickedUp()
    {
        isPickedUp = true;

        if (infoPanel != null)
        {
            infoPanel.SetActive(false); // Oculta el panel
        }

        // Si querés, también podés desactivar el script:
        // this.enabled = false;
    }
}
