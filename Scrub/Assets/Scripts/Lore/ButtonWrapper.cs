using UnityEngine;

// Puedes poner este script directamente en el GameObject del bot�n "Comenzar"
public class ButtonWrapper : MonoBehaviour
{
    // Esta funci�n ser� llamada por el evento OnClick() del bot�n.
    public void StartGameFromLore()
    {
        // 1. Accede al SceneFlowManager a trav�s del Singleton (Instance)
        if (SceneFlowManager.Instance != null)
        {
            // 2. Llama a la funci�n LoadGameScene del Manager persistente
            SceneFlowManager.Instance.LoadGameScene();
        }
        else
        {
            Debug.LogError("�ERROR CR�TICO! SceneFlowManager no fue encontrado. Aseg�rate de que existe en la escena inicial y usa DontDestroyOnLoad.");
        }
    }
}