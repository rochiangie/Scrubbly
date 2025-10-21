using UnityEngine;

// Asumo que tienes un script estático llamado GameEvents
// que contiene el evento OnMemorieDecided.
// Si no lo tienes, el siguiente paso será crearlo.
// Esto asume que el objeto tiene el Tag "Memorie"
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
            // Oculta el objeto temporalmente mientras se toma la decisión
            gameObject.SetActive(false);

            // Pasamos la información del objeto y un método de vuelta (callback)
            MemorieDecisionUI.Instance.ShowDecisionPanel(
                gameObject.name,
                sentimentalValue,
                DecideAndNotify // Este es el método que se llamará al pulsar el botón
            );

            // 🛑 Importante: Indicamos al sistema que la UI de decisión está activa.
            TaskManager.SetDecisionActive(true);
        }
        else
        {
            // NO DESTRUIR AQUÍ. Solo loguear el error y dejar que el objeto se quede.
            Debug.LogError("¡MemorieDecisionUI no encontrado! No se puede iniciar la decisión. " +
                           "Verifica que el script esté en la escena y configurado como Singleton.");
        }
    }

    // Este método es el CALLBACK, llamado por MemorieDecisionUI.cs cuando se pulsa un botón
    private void DecideAndNotify(bool isKept)
    {
        // 1. Notificar al sistema de Puntuación (SentimentalScoreManager) a través de eventos
        // 💡 ESTE EVENTO REEMPLAZA LA LLAMADA A SentimentalScoreManager.Instance.UpdateScore()
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

        // 3. Importante: Indicamos que la UI de decisión se ha cerrado.
        TaskManager.SetDecisionActive(false);
    }
}