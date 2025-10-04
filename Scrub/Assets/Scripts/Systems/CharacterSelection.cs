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
        if (PersistedInstance) { Destroy(PersistedInstance); PersistedInstance = null; }
        SelectedPrefab = prefab;
        SelectedIndex = index;
    }

    public void SetSelectedFromExisting(int index, GameObject existing)
    {
        SelectedPrefab = null;
        SelectedIndex = index;
        PersistedInstance = existing;
        DontDestroyOnLoad(existing);
    }

    public void DestroyPersistedIfAny()
    {
        if (PersistedInstance) { Destroy(PersistedInstance); PersistedInstance = null; }
    }

    public GameObject GetOrSpawn(Vector3 pos, Quaternion rot)
    {
        if (SelectedPrefab) return Object.Instantiate(SelectedPrefab, pos, rot);
        if (PersistedInstance) { PersistedInstance.transform.SetPositionAndRotation(pos, rot); return PersistedInstance; }
        Debug.LogWarning("[CharacterSelection] No hay personaje seleccionado.");
        return null;
    }
}