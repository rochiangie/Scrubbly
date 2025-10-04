using UnityEngine;

public class CharacterSelection : MonoBehaviour
{
    public static CharacterSelection Instance { get; private set; }

    public int SelectedIndex { get; private set; } = -1;
    public GameObject SelectedPrefab { get; private set; }
    public GameObject PersistedInstance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetSelected(int index, GameObject prefab)
    {
        SelectedIndex = index;
        SelectedPrefab = prefab;
        PersistedInstance = null;
    }

    public void SetSelectedFromExisting(int index, GameObject existing)
    {
        SelectedIndex = index;
        SelectedPrefab = null;
        PersistedInstance = existing;
        DontDestroyOnLoad(existing);
    }

    /// <summary>
    /// Devuelve una instancia lista en la escena actual (instancia prefab o reubica la persistida).
    /// </summary>
    public GameObject GetOrSpawn(Vector3 position, Quaternion rotation)
    {
        if (SelectedPrefab != null)
        {
            return Instantiate(SelectedPrefab, position, rotation);
        }

        if (PersistedInstance != null)
        {
            PersistedInstance.transform.SetPositionAndRotation(position, rotation);
            return PersistedInstance;
        }

        Debug.LogWarning("[CharacterSelection] No hay Prefab ni instancia persistida.");
        return null;
    }
}
