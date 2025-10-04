using UnityEngine;
using UnityEngine.SceneManagement;

public class SpotlightSelector : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private Light spotLight;                 // Tu Spot Light
    [SerializeField] private Transform[] candidates;          // Personajes en la escena (sus Transforms)
    [SerializeField] private GameObject[] playerPrefabs;      // (Opcional) Prefabs equivalentes para la escena Principal (mismo orden que candidates)

    [Header("Spot Movement")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, 0f);
    [SerializeField] private float moveLerp = 10f;
    [SerializeField] private float lookLerp = 15f;

    [Header("Input")]
    [SerializeField] private KeyCode prevKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode nextKey = KeyCode.RightArrow;
    [SerializeField] private KeyCode selectKey = KeyCode.Return;  // Enter
    [SerializeField] private KeyCode selectAltKey = KeyCode.E;    // E

    [Header("Flow")]
    [SerializeField] private string nextSceneName = "Principal";

    private int currentIndex = 0;

    void Awake()
    {
        if (spotLight == null) spotLight = GetComponent<Light>();
        if (candidates == null || candidates.Length == 0)
        {
            Debug.LogError("[SpotlightSelector] Asigná los candidatos (Transforms) en el Inspector.");
        }
        else
        {
            ClampIndex();
            SnapToCurrent();
        }
    }

    void Update()
    {
        if (candidates == null || candidates.Length == 0) return;

        // Navegación
        if (Input.GetKeyDown(prevKey)) { currentIndex--; ClampIndex(); }
        if (Input.GetKeyDown(nextKey)) { currentIndex++; ClampIndex(); }

        // Mover el foco suave al target
        var target = candidates[currentIndex];
        var targetPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, moveLerp * Time.deltaTime);

        // Mirar al personaje (suave)
        var dir = (target.position - transform.position);
        if (dir.sqrMagnitude > 0.0001f)
        {
            var desiredRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, lookLerp * Time.deltaTime);
        }

        // Selección
        if (Input.GetKeyDown(selectKey) || Input.GetKeyDown(selectAltKey))
        {
            ConfirmSelection();
        }
    }

    private void ClampIndex()
    {
        if (candidates.Length == 0) { currentIndex = 0; return; }
        if (currentIndex < 0) currentIndex = candidates.Length - 1;
        if (currentIndex >= candidates.Length) currentIndex = 0;
    }

    private void SnapToCurrent()
    {
        var t = candidates[currentIndex];
        transform.position = t.position + offset;
        transform.rotation = Quaternion.LookRotation(t.position - transform.position, Vector3.up);
    }

    private void ConfirmSelection()
    {
        // Guardar selección para la próxima escena
        var prefab = (playerPrefabs != null && currentIndex < playerPrefabs.Length) ? playerPrefabs[currentIndex] : null;

        if (CharacterSelection.Instance == null)
        {
            // Crear auto si no existe
            var go = new GameObject("CharacterSelection");
            go.AddComponent<CharacterSelection>();
        }

        if (prefab != null)
        {
            CharacterSelection.Instance.SetSelected(currentIndex, prefab);
        }
        else
        {
            // Si no asignaste prefab, persistimos el objeto actual
            CharacterSelection.Instance.SetSelectedFromExisting(currentIndex, candidates[currentIndex].gameObject);
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
