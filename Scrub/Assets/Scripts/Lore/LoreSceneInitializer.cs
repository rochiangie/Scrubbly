using UnityEngine;

public class LoreSceneInitializer : MonoBehaviour
{
    void Start()
    {
        // 🛑 CRÍTICO: Liberar el cursor y hacerlo visible inmediatamente al cargar la escena
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Opcional: Asegurarse de que el tiempo no esté pausado si la escena Lore es un menú estático
        Time.timeScale = 1f;

        Debug.Log("[LoreSceneInitializer] Cursor liberado y visible para interacción con UI.");
    }
}