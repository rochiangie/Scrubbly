using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
using TMPro;

public class SpotlightSelector : MonoBehaviour
{
    // ===================================
    // NUEVAS REFERENCIAS PARA EL LORE
    // ===================================
    [Header("UI y Lore")]
    [Tooltip("El componente TextMeshProUGUI que mostrará la descripción del personaje (Lore).")]
    public TextMeshProUGUI loreTextComponent;

    // 🟢 NUEVA REFERENCIA
    [Tooltip("El componente TextMeshProUGUI que mostrará la Tarea/Misión del personaje.")]
    public TextMeshProUGUI tasksTextComponent;
    // ----------------------------------

    [Tooltip("Distancia máxima de raycast para detectar el personaje.")]
    public float detectionDistance = 50f;

    private Transform[] candidates;
    private CharacterData currentSelectionData; // Cache del script de datos del personaje actual
    private Light spotLight; // Referencia a la luz del Spot

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
        spotLight = GetComponent<Light>();
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
        currentSelectionData = null;

        if (candidates != null && candidates.Length > 0)
        {
            int lastIndex = PlayerPrefs.GetInt("LastSelectedIndex", 0);
            index = Mathf.Clamp(lastIndex, 0, candidates.Length - 1);
            SnapTo(index);

            CharacterData initialData = candidates[index].GetComponent<CharacterData>();
            if (initialData != null)
            {
                UpdateLoreUI(initialData);
            }
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

        DetectCharacterInFocus();

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

    private void DetectCharacterInFocus()
    {
        if (spotLight == null || loreTextComponent == null) return;

        Vector3 origin = spotLight.transform.position;
        Vector3 direction = spotLight.transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, detectionDistance, raycastLayer))
        {
            if (hit.collider != null)
            {
                CharacterData hitData = hit.collider.GetComponentInParent<CharacterData>();

                if (hitData != null && hitData != currentSelectionData)
                {
                    UpdateLoreUI(hitData);
                }
            }
        }
    }

    // FUNCIÓN: Actualiza el Canvas y el Singleton (CharacterSelection)
    private void UpdateLoreUI(CharacterData newSelection)
    {
        currentSelectionData = newSelection;

        // Comprobación de referencia de texto principal
        if (loreTextComponent == null)
        {
            Debug.LogError("🚨 Lore Text Component no está asignado.");
            return;
        }

        // Comprobación de referencia de texto de tareas
        if (tasksTextComponent == null)
        {
            Debug.LogError("🚨 Tasks Text Component no está asignado.");
            // Permitimos que continúe si el loreTextComponent sí existe.
        }

        if (currentSelectionData != null)
        {
            string characterID = currentSelectionData.prefabName;

            // 1. Mostrar el texto de lore
            loreTextComponent.text = currentSelectionData.loreText;

            // 2. Lógica para el texto de TAREAS
            if (tasksTextComponent != null)
            {
                string taskText = "";
                string separator = "\n\n"; // Doble salto de línea para párrafos

                // Suponemos que el ID del personaje es convertible a int (o usamos el string)
                if (int.TryParse(characterID, out int id) && id == 1) // Elara (Limpieza)
                {
                    taskText = "🎯 **El Santuario de la Perfección** 🧼" + separator +
                        "Tu objetivo es alcanzar la esterilidad total y el orden absoluto." + separator +
                        "**1. Descontaminación Total (Dirt):** Usa tu equipo de limpieza para eliminar cualquier rastro de mugre o gérmenes. Cada mancha visible debe desaparecer." + separator +
                        "**2. Purificación del Exterior (Bolsas):** Recoge y saca todas las bolsas contaminantes del área. Transportar y desechar estos objetos fuera del espacio habitable es tu ritual para mantener el control.";
                }
                else if (int.TryParse(characterID, out int id2) && id2 == 9) // Daniel (Acumulación)
                {
                    taskText = "🎯 **La Liberación de la Memoria** 🗑️" + separator +
                        "Debes liberar tu mente y tu hogar del peso de la acumulación." + separator +
                        "**1. Descontaminación Total (Dirt):** (Mecánica compartida) Elimina activamente la suciedad." + separator +
                        "**2. Deshacerse del Exceso (Acumulables):** Identifica y descarta los objetos acumulables sin valor. Supera el pánico a desechar, ya que cada objeto liberado es un paso hacia la recuperación de tu espacio y tu vida.";
                }
                else
                {
                    taskText = $"🎯 Tarea asignada (ID: {characterID}): Explora el mundo y descubre tu destino.";
                }

                // 🟢 NOTA: Si usas TextMeshPro, puedes usar negritas (<b>) y emojis.
                // He añadido formato para mejorar la presentación.

                tasksTextComponent.text = taskText;
            }

            // 3. Guardar el ID en el Singleton
            if (CharacterSelection.Instance != null)
            {
                CharacterSelection.Instance.SetSelectedID(characterID);
            }
        }
        else // Si newSelection es null
        {
            loreTextComponent.text = "Dirige el foco al personaje para ver su historia.";
            if (tasksTextComponent != null)
            {
                tasksTextComponent.text = "Selecciona un personaje para ver su misión.";
            }
        }
    }

    void HandleMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, raycastLayer))
        {
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
                    CharacterData clickedData = clickedRoot.GetComponent<CharacterData>();
                    if (clickedData != null)
                    {
                        UpdateLoreUI(clickedData);
                    }

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

        if (candidates[index] != null)
        {
            CharacterData newFocusedData = candidates[index].GetComponent<CharacterData>();
            UpdateLoreUI(newFocusedData);
        }

        StopAllCoroutines();
        StartCoroutine(AnimateTo(index));
    }

    public void Confirm()
    {
        if (candidates == null || candidates.Length == 0) return;

        if (index < 0 || index >= candidates.Length || candidates[index] == null)
        {
            Debug.LogError("[SELECTION] Índice de candidato fuera de rango: " + index);
            return;
        }

        Transform selectedCandidate = candidates[index];

        PlayerPrefs.SetInt("LastSelectedIndex", index);

        CharacterData finalData = selectedCandidate.GetComponent<CharacterData>();

        if (finalData != null && CharacterSelection.Instance != null)
        {
            string characterID = finalData.prefabName; // Usamos el ID del CharacterData

            // 1. Guarda el ID del prefab
            CharacterSelection.Instance.SetSelectedID(characterID);
            Debug.Log($"[SELECTION] 🔥 Confirmando personaje y guardando ID: {characterID}");

            // 2. CARGA DE ESCENA
            Debug.Log($"[SELECTION] 🎯 Iniciando rutaje de escena en base al ID: {characterID}");
            CharacterSelection.Instance.GoToGameScene();

            // 3. DESTRUIR
            Debug.Log("[SELECTION] 🗑️ Destruyendo SpotlightSelector...");
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("Error crítico: CharacterData no disponible o CharacterSelection no inicializado. Cancelando carga.");
        }
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