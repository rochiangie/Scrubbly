using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Controla la escena de créditos.
/// Muestra los créditos y luego regresa automáticamente al menú principal.
/// </summary>
public class CreditsSceneController : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Tiempo en segundos que se mostrarán los créditos antes de volver al menú")]
    [SerializeField] private float creditsDisplayTime = 15f;

    [Tooltip("Nombre de la escena del menú principal (primera escena)")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Opcional: Transiciones")]
    [Tooltip("Si quieres un fade out antes de cambiar de escena, asigna el CanvasGroup aquí")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    
    [Tooltip("Duración del fade out en segundos")]
    [SerializeField] private float fadeDuration = 1f;

    private void Start()
    {
        // Iniciar la secuencia de créditos
        StartCoroutine(CreditsSequence());
    }

    private IEnumerator CreditsSequence()
    {
        // Mostrar créditos
        Debug.Log("[Credits] Mostrando créditos");
        yield return new WaitForSeconds(creditsDisplayTime);

        // Fade out (opcional)
        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(FadeOut());
        }

        // Regresar al menú principal
        Debug.Log($"[Credits] Regresando a la escena: {mainMenuSceneName}");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        float startAlpha = fadeCanvasGroup.alpha;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsedTime / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Método público para regresar al menú inmediatamente (puede ser llamado por un botón "Skip")
    /// </summary>
    public void ReturnToMainMenuNow()
    {
        StopAllCoroutines();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}

