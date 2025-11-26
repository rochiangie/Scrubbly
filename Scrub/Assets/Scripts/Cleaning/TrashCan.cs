using UnityEngine;

public class TrashCan : MonoBehaviour, IInteractable
{
    [Header("Trash Type - Tag Based")]
    [Tooltip("Tag del tipo de basura que acepta este basurero (Vidrio, Plastico, Peligroso, Carton)")]
    public string acceptedTrashTag = "Plastico";

    [Header("Visual Settings")]
    [Tooltip("Color del basurero y su etiqueta")]
    public Color binColor = Color.yellow;

    [Tooltip("Nombre descriptivo del tipo de basura")]
    public string displayName = "PLÁSTICO";

    [Header("Animation Settings")]
    public Animator animator;
    public string openParamName = "IsOpened";
    
    [Tooltip("Si está activado, usa un parámetro separado para cerrar. Si no, usa solo IsOpened")]
    public bool useSeparateCloseParam = false;
    public string closeParamName = "IsClosed";
    
    [Header("Auto-Close Settings")]
    [Tooltip("Tiempo en segundos antes de cerrar automáticamente")]
    public float autoCloseDelay = 3f;
    [Tooltip("Si está activado, el basurero se cierra automáticamente")]
    public bool autoClose = true;

    [Header("Label Settings")]
    public Vector3 labelOffset = new Vector3(0, 2, 0);
    public float labelDistance = 5f;

    private bool isOpen = false;
    private string labelText;
    private float closeTimer = 0f;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();

        // Validar que el Animator tenga un Controller asignado
        if (animator != null && animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[TRASHCAN] ⚠️ {displayName}: El Animator no tiene un AnimatorController asignado. " +
                           "Por favor asigna un Animator Controller en el Inspector.", gameObject);
        }

        // Configurar texto del label
        labelText = $"{displayName}\n({acceptedTrashTag})";

        Debug.Log($"[TRASHCAN] 🗑️ Basurero configurado: {displayName} | Acepta tag: '{acceptedTrashTag}'");
    }

    private void Update()
    {
        // Cierre automático
        if (isOpen && autoClose && closeTimer > 0f)
        {
            closeTimer -= Time.deltaTime;
            if (closeTimer <= 0f)
            {
                Close();
            }
        }
    }

    // Implementación de la interfaz IInteractable
    public void Interact()
    {
        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        if (!isOpen)
        {
            isOpen = true;
            closeTimer = autoCloseDelay;
            
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool(openParamName, true);
                
                if (useSeparateCloseParam)
                {
                    animator.SetBool(closeParamName, false);
                }
            }
            
            Debug.Log($"[TRASHCAN] 🗑️ {displayName} abierto");
        }
    }

    public void Close()
    {
        if (isOpen)
        {
            isOpen = false;
            closeTimer = 0f;
            
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool(openParamName, false);
                
                if (useSeparateCloseParam)
                {
                    animator.SetBool(closeParamName, true);
                }
            }
            
            Debug.Log($"[TRASHCAN] 🗑️ {displayName} cerrado");
        }
    }

    void OnGUI()
    {
        // Mostrar cartel solo si el jugador está cerca
        if (Camera.main == null) return;

        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        if (distance > labelDistance) return;

        Vector3 labelWorldPos = transform.position + labelOffset;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(labelWorldPos);

        if (screenPos.z > 0)
        {
            // Fondo del cartel
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(new Rect(screenPos.x - 60, Screen.height - screenPos.y - 30, 120, 60), "");

            // Texto del cartel
            GUI.color = binColor;
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;

            GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y - 30, 120, 60), labelText, style);
        }
    }
}
