using UnityEngine;

[DefaultExecutionOrder(-100)] // que se ejecute bien temprano
public class SelectedCharacterLoader : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint; // asigná tu PlayerSpawn en el Inspector

    void Start()
    {
        if (spawnPoint == null) spawnPoint = transform; // fallback

        if (CharacterSelection.Instance == null)
        {
            Debug.LogWarning("[SelectedCharacterLoader] No hay CharacterSelection en memoria.");
            return;
        }

        var obj = CharacterSelection.Instance.GetOrSpawn(spawnPoint.position, spawnPoint.rotation);
        if (obj == null) return;

        // Asegurar capa/render layer por si venías de la escena anterior
        obj.gameObject.SetActive(true);
    }
}
