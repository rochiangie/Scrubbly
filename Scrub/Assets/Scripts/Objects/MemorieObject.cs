using UnityEngine;

// Asegúrate de que este script esté en objetos con el Tag "Memorie"
public class MemorieObject : MonoBehaviour
{
    [Header("Valor Sentimental")]
    public int sentimentalValue = 20;

    // Método llamado por PlayerInteraction cuando se recoge con 'E' (o tu tecla de PickUp)
    public void StartDecisionProcess()
    {
        // 1. Delegar la interfaz al Manager de UI
        if (MemorieDecisionUI.Instance != null)
        {
            // Pasamos la información del objeto y un método de vuelta (callback)
            MemorieDecisionUI.Instance.ShowDecisionPanel(
                gameObject.name,
                sentimentalValue,
                DecideAndNotify // Este es el método que se llamará al pulsar el botón
            );
        }
        else
        {
            Debug.LogError("¡MemorieDecisionUI no encontrado! No se puede iniciar la decisión.");
            // Si no se encuentra, destruimos el objeto para evitar que el juego se rompa.
            Destroy(gameObject);
        }
    }

    // Este método es el CALLBACK, llamado por MemorieDecisionUI.cs cuando se pulsa un botón
    private void DecideAndNotify(bool isKept)
    {
        // 1. Notificar al SentimentalScoreManager
        GameEvents.MemorieDecided(isKept, sentimentalValue);

        if (isKept)
        {
            Debug.Log($"[DECISIÓN] ¡Guardaste {gameObject.name}! Suma a la acumulación.");
        }
        else // Tirar/Destruir
        {
            Debug.Log($"[DECISIÓN] Tiraste {gameObject.name}. Afecta el balance emocional.");
        }

        // 2. Eliminar el objeto del juego
        Destroy(gameObject);
    }
}