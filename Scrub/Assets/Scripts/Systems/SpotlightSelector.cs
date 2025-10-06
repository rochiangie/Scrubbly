using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;

public class SpotlightSelector : MonoBehaviour
{
    [Header("Candidatos en escena (root de cada personaje)")]
    [SerializeField] Transform[] candidates;

    [Header("Prefabs jugables (mismo orden que 'candidates')")]
    [SerializeField] GameObject[] playerPrefabs;

    [Header("Spot (la cámara es HIJA de este)")]
    [SerializeField] Vector3 lightOffset = new Vector3(0f, 2.5f, 3.0f);
    [SerializeField] bool viewFromFront = true;

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
        SnapTo(index);
    }

    void Update()
    {
        // 1. Si estamos en transición de Spotlight, ignoramos todo el input para evitar bugs.
        if (isTransitioning)
        {
            return;
        }

        // 2. Lógica de FOCO (mover)
        // El 'Focus' actualiza el 'index' y comienza la corrutina AnimateTo, lo que establece isTransitioning = true.
        if (Input.GetKeyDown(prev1) || Input.GetKeyDown(prev2))
        {
            Focus(-1);
        }
        else if (Input.GetKeyDown(next1) || Input.GetKeyDown(next2))
        {
            Focus(+1);
        }

        // 3. Lógica de CONFIRMAR
        // Esto solo se ejecuta si NO estamos ya en transición (ver el 'return' de arriba).
        else if (Input.GetKeyDown(confirmKey) ||
                 Input.GetKeyDown(confirmAlt) ||
                 Input.GetKeyDown(KeyCode.KeypadEnter)) // Añadimos el Enter numérico
        {
            Confirm();
        }
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

    // -------- confirmar (Lógica de guardado) --------
    void Confirm()
    {
        if (candidates == null || candidates.Length == 0) return;
        if (GameDataController.Instance == null)
        {
            Debug.LogError("[SELECTION] GameDataController NO encontrado. Cargando escena...");
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        // 🛑 CAMBIO CLAVE: Usamos el nombre del Transform del candidato actual (ej: "1", "9", "Male")
        string characterID = candidates[index].name;

        // Puedes usar el nombre del Prefab en su lugar si lo prefieres, pero el nombre del Transform es más directo
        /*
        if (usePrefabs && playerPrefabs != null && index < playerPrefabs.Length && playerPrefabs[index] != null)
        {
            characterID = playerPrefabs[index].name;
        }
        else
        {
            characterID = candidates[index].name;
        }
        */

        // 1. Guardar el ID (el nombre) en el controlador persistente
        Debug.Log($"[SELECTION] Guardando ID Final (Nombre): {characterID}");
        GameDataController.Instance.SetSelectedCharacter(characterID);

        // 2. Cargar la siguiente escena
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

        ApplyCameraLocalPose();
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
        float side = lightOffset.x;
        float height = lightOffset.y;
        float dist = lightOffset.z;

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