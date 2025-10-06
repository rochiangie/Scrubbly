using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;

public class SpotlightSelector : MonoBehaviour
{
    // Hacemos que la lista de candidatos se maneje internamente (sin Inspector)
    private Transform[] candidates;

    [Header("Etiqueta para la búsqueda automática")]
    [Tooltip("Etiqueta que tienen TODOS los personajes seleccionables en la escena.")]
    public string CandidateTag = "Player";

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
    // CRÍTICO: Bloquea el input por un frame al cargar la escena.
    bool isInputBlocked = true;


    void Awake()
    {
        // Dejamos Awake vacío, la inicialización ocurre en OnEnable.
    }

    // ===============================================
    // LÓGICA DE INICIALIZACIÓN Y LIMPIEZA
    // ===============================================

    /// <summary>
    /// Se llama cada vez que el objeto se activa (incluyendo la recarga de escena).
    /// </summary>
    private void OnEnable()
    {
        // 1. CARGA DINÁMICA DE CANDIDATOS (¡Crucial al volver a la escena!)
        LoadCandidatesFromScene();

        // 2. Reiniciar estado y bloquear input
        isTransitioning = false;
        index = 0;
        isInputBlocked = true; // El input se bloquea inmediatamente al cargarse la escena

        // 3. Colocación inicial
        if (candidates != null && candidates.Length > 0)
        {
            SnapTo(index);
        }
        else
        {
            Debug.LogError("[SPOTLIGHT] No se encontró ningún personaje con la etiqueta: " + CandidateTag + ". Verifica que los personajes estén en la escena.");
        }
    }

    void LoadCandidatesFromScene()
    {
        GameObject[] candidateObjects = GameObject.FindGameObjectsWithTag(CandidateTag);

        if (candidateObjects.Length > 0)
        {
            // Ordenamos por nombre para asegurar un orden consistente
            candidates = candidateObjects.OrderBy(go => go.name).Select(go => go.transform).ToArray();
            Debug.Log($"[SPOTLIGHT] Candidatos encontrados y cargados: {candidates.Length}");
        }
        else
        {
            candidates = null;
        }
    }

    /// <summary>
    /// Se llama justo antes de que el objeto sea desactivado o destruido.
    /// </summary>
    private void OnDisable()
    {
        // 1. Detener todas las corrutinas activas.
        StopAllCoroutines();

        // 2. Limpiar la referencia (CLAVE para evitar MissingReferenceException).
        candidates = null;
    }

    // ===============================================
    // LÓGICA DE INPUT Y TRANSICIÓN
    // ===============================================

    void Update()
    {
        // CRÍTICO: Bloquea el input en el primer frame.
        if (isInputBlocked)
        {
            isInputBlocked = false; // Desbloquea para el siguiente frame
            return;
        }

        // Comprobación de seguridad
        if (candidates == null || candidates.Length == 0)
        {
            return;
        }

        if (isTransitioning)
        {
            return;
        }

        // Lógica de FOCO (mover)
        if (Input.GetKeyDown(prev1) || Input.GetKeyDown(prev2))
        {
            Focus(-1);
        }
        else if (Input.GetKeyDown(next1) || Input.GetKeyDown(next2))
        {
            Focus(+1);
        }

        // Lógica de CONFIRMAR
        else if (Input.GetKeyDown(confirmKey) ||
                 Input.GetKeyDown(confirmAlt) ||
                 Input.GetKeyDown(KeyCode.KeypadEnter))
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

        // 1. Comprobación estricta del candidato
        if (index < 0 || index >= candidates.Length)
        {
            Debug.LogError("[SELECTION] Índice de candidato fuera de rango: " + index);
            return;
        }

        Transform selectedCandidate = candidates[index];
        // CRÍTICO: Comprueba si la referencia está rota.
        if (selectedCandidate == null)
        {
            Debug.LogError("[SELECTION] El Transform del candidato seleccionado ya fue destruido (NULL Reference).");
            return;
        }

        // 2. Comprobación del controlador
        if (GameDataController.Instance == null)
        {
            Debug.LogError("[SELECTION] GameDataController NO encontrado. Cargando escena...");
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        // 3. Guardar y cargar
        string characterID = selectedCandidate.name;
        Debug.Log($"[SELECTION] Guardando ID Final (Nombre): {characterID}");
        GameDataController.Instance.SetSelectedCharacter(characterID);

        SceneManager.LoadScene(nextSceneName);
    }

    // -------- transición (una sola vez por cambio) --------
    System.Collections.IEnumerator AnimateTo(int i)
    {
        isTransitioning = true;

        if (candidates == null || candidates.Length == 0) { isTransitioning = false; yield break; }

        if (i < 0 || i >= candidates.Length || candidates[i] == null) { isTransitioning = false; yield break; }

        Transform t = candidates[i];

        if (t == null) { isTransitioning = false; yield break; }


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
        if (t == null) return;

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
        if (r == null) return null;
        if (r.name.ToLower().Contains(part.ToLower())) return r;
        for (int i = 0; i < r.childCount; i++)
        {
            var f = FindDeepContains(r.GetChild(i), part);
            if (f) return f;
        }
        return null;
    }
}