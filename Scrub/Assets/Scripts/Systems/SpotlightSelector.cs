using UnityEngine;
using UnityEngine.SceneManagement;

public class SpotlightSelector : MonoBehaviour
{
    [Header("Candidatos en escena (root de cada personaje)")]
    [SerializeField] Transform[] candidates;

    [Header("Prefabs jugables (mismo orden que 'candidates')")]
    [SerializeField] GameObject[] playerPrefabs;

    [Header("Spot (la cámara es HIJA de este)")]
    // lightOffset es RELATIVO al personaje:
    // x = lateral (derecha +), y = altura, z = distancia
    [SerializeField] Vector3 lightOffset = new Vector3(0f, 2.5f, 3.0f);
    [SerializeField] bool viewFromFront = true; // true = mirar la cara; false = detrás (over-the-shoulder)

    [Header("Transición (sólo al cambiar)")]
    [SerializeField] float moveDuration = 0.35f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Cámara hija (opcional, por si querés fijar su pose local)")]
    [Tooltip("Arrastrá la Main Camera o la VCam (debe ser HIJA del Spot). Si lo dejás vacío, no se toca.")]
    [SerializeField] Transform cameraChild;
    [SerializeField] Vector3 cameraLocalPosition = Vector3.zero;
    [SerializeField] Vector3 cameraLocalEuler = Vector3.zero;

    [Header("A qué mira el Spot")]
    [SerializeField] string[] anchorNames = { "CameraAnchor", "head", "mixamorig:Head" };
    [Tooltip("Si true, mira al ROOT (no a la cabeza) para evitar balanceos de idle.")]
    [SerializeField] bool lookAtRootWhenIdle = true;

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
    bool isTransitioning = false;

    void Awake()
    {
        SnapTo(index); // coloca en el primero, sin animar
    }

    void Update()
    {
        if (Input.GetKeyDown(prev1) || Input.GetKeyDown(prev2)) Focus(-1);
        if (Input.GetKeyDown(next1) || Input.GetKeyDown(next2)) Focus(+1);
        if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(confirmAlt)) Confirm();
        // No movemos nada aquí: sólo en el cambio.
    }

    // -------- navegación --------
    void Focus(int dir)
    {
        if (candidates == null || candidates.Length == 0 || isTransitioning) return;

        int n = candidates.Length;
        int newIndex = wrapAround ? (index + dir + n) % n : Mathf.Clamp(index + dir, 0, n - 1);
        if (newIndex == index) return;

        index = newIndex;
        StopAllCoroutines();
        StartCoroutine(AnimateTo(index));
    }

    // -------- confirmar --------
    void Confirm()
    {
        if (candidates == null || candidates.Length == 0) return;

        if (usePrefabs && playerPrefabs != null && index < playerPrefabs.Length && playerPrefabs[index] != null)
            CharacterSelection.Instance.SetSelected(index, playerPrefabs[index]);
        else
            CharacterSelection.Instance.SetSelectedFromExisting(index, candidates[index].gameObject);

        SceneManager.LoadScene(nextSceneName);
    }

    // -------- transición (una sola vez por cambio) --------
    System.Collections.IEnumerator AnimateTo(int i)
    {
        isTransitioning = true;

        if (candidates == null || candidates.Length == 0) { isTransitioning = false; yield break; }

        Transform t = candidates[i];
        Transform anchor = GetAnchor(t);

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 endPos = ComputeSpotPosition(t);
        Vector3 lookPoint = (lookAtRootWhenIdle || anchor == null) ? t.position : anchor.position;
        Quaternion endRot = Quaternion.LookRotation((lookPoint - endPos).normalized, Vector3.up);

        float T = Mathf.Max(0f, moveDuration);
        if (T <= 0.0001f) { SnapTo(i); isTransitioning = false; yield break; }

        float t01 = 0f;
        while (t01 < 1f)
        {
            t01 += Time.deltaTime / T;
            float k = ease != null ? ease.Evaluate(Mathf.Clamp01(t01)) : Mathf.Clamp01(t01);

            transform.position = Vector3.LerpUnclamped(startPos, endPos, k);
            transform.rotation = Quaternion.SlerpUnclamped(startRot, endRot, k);
            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;

        ApplyCameraLocalPose(); // por si querés fijar la pose local de la cámara hija
        isTransitioning = false;
    }

    // -------- colocación instantánea (inicio) --------
    void SnapTo(int i)
    {
        if (candidates == null || candidates.Length == 0) return;

        Transform t = candidates[i];
        Transform anchor = GetAnchor(t);

        Vector3 p = ComputeSpotPosition(t);
        Vector3 lookPoint = (lookAtRootWhenIdle || anchor == null) ? t.position : anchor.position;

        transform.position = p;
        transform.rotation = Quaternion.LookRotation((lookPoint - p).normalized, Vector3.up);

        ApplyCameraLocalPose();
    }

    // -------- cálculo del POS del Spot relativo al personaje --------
    Vector3 ComputeSpotPosition(Transform target)
    {
        // interpretamos lightOffset como offsets relativos al personaje
        float side = lightOffset.x;  // derecha +
        float height = lightOffset.y;  // arriba +
        float dist = lightOffset.z;  // distancia

        // frente = +forward si quiero ver la cara; si no, -forward (detrás)
        Vector3 frontDir = viewFromFront ? target.forward : -target.forward;

        return target.position
             + frontDir.normalized * dist
             + Vector3.up * height
             + target.right * side;
    }


    // -------- helpers --------
    void ApplyCameraLocalPose()
    {
        if (cameraChild)
        {
            cameraChild.localPosition = cameraLocalPosition;
            cameraChild.localRotation = Quaternion.Euler(cameraLocalEuler);
        }
    }

    Transform GetAnchor(Transform root)
    {
        if (lookAtRootWhenIdle || root == null) return null;
        foreach (var n in anchorNames)
        {
            var a = FindDeepContains(root, n);
            if (a) return a;
        }
        return null;
    }

    Transform FindDeepContains(Transform r, string part)
    {
        if (r.name.ToLower().Contains(part.ToLower())) return r;
        for (int i = 0; i < r.childCount; i++)
        {
            var f = FindDeepContains(r.GetChild(i), part);
            if (f) return f;
        }
        return null;
    }
}
