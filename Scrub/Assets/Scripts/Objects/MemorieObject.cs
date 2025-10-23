using UnityEngine;
using System; // Necesario para Action

// Asumo que tienes un script estático llamado GameEvents
// que contiene el evento OnMemorieDecided.
// Esto asume que el objeto tiene el Tag "Memorie"
public class MemorieObject : MonoBehaviour
{
    [Header("Valor Sentimental")]
    public int sentimentalValue = 20;

    // Método llamado por PlayerInteraction cuando se recoge con 'Q' o Click
    public void StartDecisionProcess(UIPauseController uiController)
    {
        // 1. Delegar la interfaz al Manager de UI
        if (uiController != null)
        {
            // Oculta el objeto temporalmente mientras se toma la decisión
            gameObject.SetActive(false);

            // Pasamos la información del objeto y el método de vuelta (callback)
            uiController.ShowToolsPanelAtWorldPosition(
                gameObject.name,
                sentimentalValue,
                DecideAndNotify // Este es el método que se llamará al pulsar Y/N
            );
        }
        else
        {
            Debug.LogError("¡UIPauseController no encontrado! No se puede iniciar la decisión. ");
        }
    }

    /// <summary>
    /// Este método es el CALLBACK, llamado por UIPauseController.cs cuando se pulsa Y/N.
    /// </summary>
    /// <param name="isKept">True si se guarda (Y), False si se tira/descarta (N).</param>
    // 🚨 CORRECCIÓN CRÍTICA: Se cambia a PUBLIC para permitir el acceso desde PlayerInteraction.cs 🚨
    public void DecideAndNotify(bool isKept)
    {
        // 1. Notificar al sistema de Puntuación
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

        // 3. Importante: Ya no necesitamos llamar a TaskManager.SetDecisionActive(false) aquí, 
        // ya que el UIPauseController lo hace inmediatamente después de llamar a este callback.
    }
}