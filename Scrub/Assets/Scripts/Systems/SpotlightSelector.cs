using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;

public class SpotlightSelector : MonoBehaviour
{
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
    [SerializeField] int mouseClickButton = 0;
    [SerializeField] LayerMask raycastLayer;

    [Header("Flujo")]
    [SerializeField] string nextSceneName = "Lore";
    [SerializeField] bool wrapAround = true;
    [SerializeField] bool usePrefabs = true;

    int index = 0;
    bool isTransitioning = false;
    bool isInputBlocked = true;

    void Awake()
    {
        if (raycastLayer.value == 0)
        {
            raycastLayer = ~0;
        }
    }

    private void OnEnable()
    {
        LoadCandidatesFromScene();

        isTransitioning = false;
        index = 0;
        isInputBlocked = true;

        if (candidates != null && candidates.Length > 0)
        {
            // Usar la llave correcta para PlayerPrefs, aunque la gestiona CharacterSelection
            int lastIndex = PlayerPrefs.GetInt("LastSelectedIndex", 0);
            index = Mathf.Clamp(lastIndex, 0, candidates.Length - 1);
            SnapTo(index);
        }
        else
        {
            Debug.LogError("[SPOTLIGHT] No se encontró ningún personaje con la etiqueta: " + CandidateTag);
        }
    }

    void LoadCandidatesFromScene()
    {
        GameObject[] candidateObjects = GameObject.FindGameObjectsWithTag(CandidateTag);

        if (candidateObjects.Length > 0)
        {
            candidates = candidateObjects.OrderBy(go => go.name).Select(go => go.transform).ToArray();
            Debug.Log($"[SPOTLIGHT] Candidatos encontrados y cargados: {candidates.Length}");
        }
        else
        {
            candidates = null;
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        candidates = null;
    }

    void Update()
    {
        if (isInputBlocked)
        {
            isInputBlocked = false;
            return;
        }

        if (candidates == null || candidates.Length == 0 || isTransitioning)
        {
            return;
        }

        if (Input.GetKeyDown(prev1) || Input.GetKeyDown(prev2))
        {
            Focus(-1);
        }
        else if (Input.GetKeyDown(next1) || Input.GetKeyDown(next2))
        {
            Focus(+1);
        }
        else if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(confirmAlt) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Confirm();
        }
        else if (Input.GetMouseButtonDown(mouseClickButton))
        {
            HandleMouseClick();
        }
    }

    void HandleMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, raycastLayer))
        {
            // Buscar el Transform raíz del candidato
            Transform clickedRoot = hit.transform;
            while (clickedRoot != null && !clickedRoot.CompareTag(CandidateTag))
            {
                clickedRoot = clickedRoot.parent;
            }

            if (clickedRoot != null)
            {
                int clickedIndex = -1;
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (candidates[i] == clickedRoot)
                    {
                        clickedIndex = i;
                        break;
                    }
                }

                if (clickedIndex != -1)
                {
                    if (clickedIndex == index)
                    {
                        Confirm();
                    }
                    else
                    {
                        StopAllCoroutines();
                        index = clickedIndex;
                        StartCoroutine(AnimateTo(index));
                    }
                }
            }
        }
    }

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

    void Confirm()
    {
        if (candidates == null || candidates.Length == 0) return;

        if (index < 0 || index >= candidates.Length || candidates[index] == null)
        {
            Debug.LogError("[SELECTION] Índice de candidato fuera de rango: " + index);
            return;
        }

        Transform selectedCandidate = candidates[index];

        PlayerPrefs.SetInt("LastSelectedIndex", index);

        // 🔴 VERIFICAR CharacterSelection (Debe existir previamente)
        if (CharacterSelection.Instance == null)
        {
            // Intentar encontrar CharacterSelection en la escena (puede haber sido creado por otro script)
            var cs = FindObjectOfType<CharacterSelection>();
            if (cs == null)
            {
                Debug.LogError("[SELECTION] ❌ CharacterSelection NO encontrado. Cargando escena sin guardar ID.");
                SceneManager.LoadScene(nextSceneName);
                return;
            }
        }

        // Obtener el ID del personaje
        CharacterIDTag idTag = selectedCandidate.GetComponent<CharacterIDTag>();
        string characterID = (idTag != null) ? idTag.characterID : selectedCandidate.name;

        Debug.Log($"[SELECTION] 🔥 Confirmando personaje: {characterID}");

        // 🔴 GUARDAR PERSONAJE
        CharacterSelection.Instance.SetSelectedID(characterID);

        // 🔴 VERIFICAR AudioManager ANTES de cambiar escena
        if (AudioManager.Instance != null)
        {
            Debug.Log("[SELECTION] AudioManager encontrado, ejecutando debug...");
            AudioManager.Instance.DebugAudioStatus();
        }
        else
        {
            Debug.LogError("[SELECTION] ❌ AudioManager NO encontrado");
        }

        // 🔴 DESTRUIR SpotlightSelector
        Debug.Log("[SELECTION] 🗑️ Destruyendo SpotlightSelector...");
        Destroy(gameObject);

        // 🔴 CAMBIAR A ESCENA LORE
        Debug.Log($"[SELECTION] 🎯 Cargando escena: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    System.Collections.IEnumerator AnimateTo(int i)
    {
        isTransitioning = true;

        if (candidates == null || candidates.Length == 0 || i < 0 || i >= candidates.Length || candidates[i] == null)
        {
            isTransitioning = false;
            yield break;
        }

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