using UnityEngine;

public class UIPanelActivator : MonoBehaviour
{
    // Asigna el GameObject del panel aquí
    [SerializeField] private GameObject trashPanel;

    // Puedes hacer un método público para activarlo
    public void ShowPanel()
    {
        if (trashPanel != null)
        {
            trashPanel.SetActive(true);
        }
    }

    // Y otro para desactivarlo
    public void HidePanel()
    {
        if (trashPanel != null)
        {
            trashPanel.SetActive(false);
        }
    }
}