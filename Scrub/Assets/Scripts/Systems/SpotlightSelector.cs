using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine; // CM3 (Unity 6)

public class SpotlightSelector : MonoBehaviour
{
    [Header("Candidatos en escena (root de cada personaje)")]
    [SerializeField] Transform[] candidates;

    [Header("Prefabs jugables (mismo orden que 'candidates')")]
    [SerializeField] GameObject[] playerPrefabs;

    [Header("Luz")]
    [SerializeField] Light spotLight;
    [SerializeField] Vector3 lightOffset = new Vector3(0f, 3.5f, 0f);
    [SerializeField] float lightMoveLerp = 10f;
    [SerializeField] float lightLookLerp = 15f;

    [Header("Cámara (si dejás vcam vacío, usa Camera.main)")]
    [SerializeField] CinemachineCamera vcam;     // opcional (CM3)
    [SerializeField] Camera fallbackCamera;      // se autoasigna a Camera.main
    [SerializeField] bool useTargetForward = true;
    [SerializeField] float camDistance = 3.5f;
    [SerializeField] float camHeight = 1.7f;
    [SerializeField] float camSide = 0f;
    [SerializeField] float camMoveLerp = 6f;
    [SerializeField] float camLookLerp = 10f;
    [SerializeField] string[] cameraAnchorNames = { "CameraAnchor", "Head", "mixamorig:Head" };

    [Header("Input")]
    [SerializeField] KeyCode prev1 = KeyCode.LeftArrow;
    [SerializeField] KeyCode prev2 = KeyCode.A;
    [SerializeField] KeyCode next1 = KeyCode.RightArrow;
    [SerializeField] KeyCode next2 = KeyCode.D;
    [SerializeField] KeyCode confirmKey = KeyCode.Return;
    [SerializeField] KeyCode confirmAlt = KeyCode.Space;

    [Header("Flujo")]
    [SerializeField] string nextSceneName = "Principal";
    [SerializeField] bool wrapAround = true;
    [SerializeField] bool usePrefabs = true;

    int index = 0;

    void Awake()
    {
        if (fallbackCamera == null) fallbackCamera = Camera.main;
        if (spotLight == null) spotLight = FindObjectOfType<Light>();
        if (candidates == null || candidates.Length == 0)
            Debug.LogWarning("[SpotlightSelector] Sin candidatos.");

        SnapAllNow();
    }

    void Update()
    {
        if (Input.GetKeyDown(prev1) || Input.GetKeyDown(prev2)) Focus(-1);
        if (Input.GetKeyDown(next1) || Input.GetKeyDown(next2)) Focus(+1);
        if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(confirmAlt)) Confirm();

        TickLight();
        TickCamera();
    }

    // -------- navegación --------
    void Focus(int dir)
    {
        if (candidates == null || candidates.Length == 0) return;
        int n = candidates.Length;
        if (wrapAround) index = (index + dir + n) % n;
        else index = Mathf.Clamp(index + dir, 0, n - 1);
    }

    // -------- confirmar --------
    void Confirm()
    {
        if (candidates == null || candidates.Length == 0) return;

        if (usePrefabs && playerPrefabs != null &&
            index < playerPrefabs.Length && playerPrefabs[index] != null)
        {
            CharacterSelection.Instance.SetSelected(index, playerPrefabs[index]);
        }
        else
        {
            CharacterSelection.Instance.SetSelectedFromExisting(index, candidates[index].gameObject);
        }

        SceneManager.LoadScene(nextSceneName);
    }

    // -------- luz --------
    void TickLight()
    {
        if (!spotLight || candidates == null || candidates.Length == 0) return;
        var t = candidates[index];
        var targetPos = t.position + lightOffset;
        spotLight.transform.position = Smooth(spotLight.transform.position, targetPos, lightMoveLerp);

        var toLook = t.position - spotLight.transform.position;
        if (toLook.sqrMagnitude > 0.0001f)
        {
            var look = Quaternion.LookRotation(toLook.normalized, Vector3.up);
            spotLight.transform.rotation = Smooth(spotLight.transform.rotation, look, lightLookLerp);
        }
    }

    // -------- cámara --------
    void TickCamera()
    {
        if (candidates == null || candidates.Length == 0) return;
        var t = candidates[index];
        var anchor = FindAnchor(t) ?? t;

        // A) Cinemachine 3
        if (vcam != null)
        {
            vcam.Target.TrackingTarget = anchor;
            vcam.Target.LookAtTarget = anchor;
            return;
        }

        // B) Cámara normal
        if (fallbackCamera == null) return;
        var c = fallbackCamera.transform;

        Vector3 forward = useTargetForward ? t.forward : Vector3.forward;
        Vector3 desiredPos = t.position - forward.normalized * camDistance + Vector3.up * camHeight + t.right * camSide;

        c.position = Smooth(c.position, desiredPos, camMoveLerp);

        var dir = (anchor.position - c.position);
        if (dir.sqrMagnitude > 0.0001f)
        {
            var look = Quaternion.LookRotation(dir.normalized, Vector3.up);
            c.rotation = Smooth(c.rotation, look, camLookLerp);
        }
    }

    // -------- helpers --------
    void SnapAllNow()
    {
        if (candidates == null || candidates.Length == 0) return;

        var t = candidates[index];
        var anchor = FindAnchor(t) ?? t;

        if (spotLight)
        {
            spotLight.transform.position = t.position + lightOffset;
            spotLight.transform.LookAt(t);
        }

        if (vcam != null)
        {
            vcam.Target.TrackingTarget = anchor;
            vcam.Target.LookAtTarget = anchor;
        }
        else if (fallbackCamera != null)
        {
            Vector3 forward = useTargetForward ? t.forward : Vector3.forward;
            Vector3 p = t.position - forward.normalized * camDistance + Vector3.up * camHeight + t.right * camSide;
            fallbackCamera.transform.position = p;
            fallbackCamera.transform.LookAt(anchor);
        }
    }

    Transform FindAnchor(Transform root)
    {
        foreach (var n in cameraAnchorNames)
        {
            var a = FindDeep(root, n);
            if (a) return a;
        }
        return null;
    }

    Transform FindDeep(Transform r, string name)
    {
        if (r.name == name) return r;
        for (int i = 0; i < r.childCount; i++)
        {
            var f = FindDeep(r.GetChild(i), name);
            if (f) return f;
        }
        return null;
    }

    static Vector3 Smooth(Vector3 from, Vector3 to, float s) { float k = 1f - Mathf.Exp(-Mathf.Max(0f, s) * Time.deltaTime); return Vector3.LerpUnclamped(from, to, k); }
    static Quaternion Smooth(Quaternion f, Quaternion t, float s) { float k = 1f - Mathf.Exp(-Mathf.Max(0f, s) * Time.deltaTime); return Quaternion.SlerpUnclamped(f, t, k); }
}
