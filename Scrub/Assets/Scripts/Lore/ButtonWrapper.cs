using UnityEngine;

// Puedes poner este script directamente en el GameObject del botón "Comenzar"
public class ButtonWrapper : MonoBehaviour
{
    // Esta función será llamada por el evento OnClick() del botón.
    public void StartGameFromLore()
    {
        // 1. Accede al SceneFlowManager a través del Singleton (Instance)
        if (SceneFlowManager.Instance != null)
        {
            // 2. Llama a la función LoadGameScene del Manager persistente
            SceneFlowManager.Instance.LoadGameScene();
        }
        else
        {
            Debug.LogError("¡ERROR CRÍTICO! SceneFlowManager no fue encontrado. Asegúrate de que existe en la escena inicial y usa DontDestroyOnLoad.");
        }
    }
}