using UnityEngine;

// Asegúrate de que este script esté en objetos con el Tag "Memorie"
// y tengan un componente Carryable (o herede de él).
public class MemorieObject : MonoBehaviour
{
    [Header("Valor Sentimental")]
    [Tooltip("Puntuación que afecta el balance emocional al tomar una decisión.")]
    public int sentimentalValue = 20; // Positivo si es importante, Negativo si es trivial

    // Este es el método que tu Carryable o PlayerInteraction debe llamar
    public void StartDecisionProcess()
    {
        // En una UI real, aquí mostrarías un panel con botones "Guardar" y "Tirar"

        Debug.Log("=====================================================================");
        Debug.Log($"[DECISIÓN] Objeto: {gameObject.name}. Valor: {sentimentalValue} puntos.");
        Debug.Log("Presiona 'Y' para GUARDAR (Acumular Nostalgia) o 'N' para DESTRUIR/TIRAR (Desprenderse).");
        Debug.Log("=====================================================================");

        // Desactivamos temporalmente el PlayerMovement o esperamos un frame
        // En un juego real, la interfaz detendría el juego (Time.timeScale = 0).

        StartCoroutine(WaitForDecisionInput());
    }

    private System.Collections.IEnumerator WaitForDecisionInput()
    {
        // Esperamos un frame para evitar que el Input de recoger se cuele
        yield return null;

        bool decisionMade = false;

        while (!decisionMade)
        {
            if (Input.GetKeyDown(KeyCode.Y)) // Guardar (Acumular Nostalgia)
            {
                Decide(true);
                decisionMade = true;
            }
            else if (Input.GetKeyDown(KeyCode.N)) // Destruir/Tirar (Desprendimiento)
            {
                Decide(false);
                decisionMade = true;
            }
            yield return null;
        }
    }

    private void Decide(bool isKept)
    {
        GameEvents.MemorieDecided(isKept, sentimentalValue);

        if (isKept)
        {
            Debug.Log($"[DECISIÓN] ¡Guardaste {gameObject.name}! Aumenta la acumulación.");
            // Si se guarda, el objeto puede ser destruido del mundo,
            // pero su valor sentimental se registra en el GameManager.
            // Para simular que "lo pusiste en una caja", lo destruimos del mundo:
            Destroy(gameObject);
        }
        else // Destruido/Tirado
        {
            Debug.Log($"[DECISIÓN] Tiraste {gameObject.name}. Afecta tu balance emocional.");
            // Aquí podrías añadir partículas de destrucción si decides "tirarlo a la basura"
            // (Similar al DestructibleObject anterior)
            Destroy(gameObject);
        }
    }
}