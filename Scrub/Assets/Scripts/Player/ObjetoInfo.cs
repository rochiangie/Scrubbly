using UnityEngine;

public class ObjetoInfo : MonoBehaviour
{
    [Header("Panel de información")]
    public GameObject infoPanel;   // ← Lo arrastrás desde el inspector

    // Llamado desde el Player cuando lo levanta
    public void OcultarPanel()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }
}
