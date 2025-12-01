using UnityEngine;
using System.Collections.Generic;

public class TrashCan : MonoBehaviour, IInteractable
{
    [Header("Trash Type - Tag Based")]
    [Tooltip("Tags del tipo de basura que acepta este basurero (puede aceptar múltiples variaciones)")]
    public string[] acceptedTrashTags = new string[] { "Plastico", "Residuos", "Vidrio", "Peligrosos", "Papeles" };

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
            Debug.LogWarning($"[TRASHCAN] ⚠️ {displayName}: El Animator no tiene un AnimatorController asignado.", gameObject);
        }

        // 🛡️ AUTO-CONFIGURACIÓN DE SEGURIDAD
        // Si la lista está vacía o solo tiene "Trash", agregamos los tags comunes para evitar frustración
        if (acceptedTrashTags == null || acceptedTrashTags.Length == 0 || (acceptedTrashTags.Length == 1 && acceptedTrashTags[0] == "Trash"))
        {
            Debug.LogWarning($"[TRASHCAN] ⚠️ {displayName} tenía configuración de tags incompleta. Auto-agregando tags comunes.");
            acceptedTrashTags = new string[] { "Vidrio", "Plastico", "Papeles", "Peligrosos", "Bolsas", "Trash", "Residuos" };
        }

        // Configurar texto del label
        string tagsDisplay = string.Join(", ", acceptedTrashTags);
        labelText = $"{displayName}\n({tagsDisplay})";

        Debug.Log($"[TRASHCAN] 🗑️ Basurero configurado: {displayName} | Acepta tags: '{tagsDisplay}'");
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

    // ---------------------------------------------------------
    // 🗑️ LÓGICA DE DETECCIÓN DE BASURA (MEJORADA)
    // ---------------------------------------------------------

    [Header("Absorption Settings")]
    public Transform trashSuckPoint; // Punto hacia donde va la basura (fondo del tacho)
    public float suckDuration = 0.5f; // Duración de la animación de absorción
    public AnimationCurve suckCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Set para evitar procesar el mismo objeto múltiples veces mientras se anima
    private HashSet<int> processedTrashIds = new HashSet<int>();

    private void OnTriggerEnter(Collider other)
    {
        ProcessTrashCollision(other);
    }

    // Usamos OnTriggerStay para detectar cuando el jugador suelta el objeto DENTRO del trigger
    private void OnTriggerStay(Collider other)
    {
        ProcessTrashCollision(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcessTrashCollision(collision.collider);
    }

    private void ProcessTrashCollision(Collider other)
    {
        // 1. Buscar componentes
        TrashObject trash = other.GetComponent<TrashObject>() ?? other.GetComponentInParent<TrashObject>();
        Carryable carryable = other.GetComponent<Carryable>() ?? other.GetComponentInParent<Carryable>();

        if (trash != null && !trash.IsCleaned)
        {
            // Evitar procesar si ya está en la lista de procesados
            if (processedTrashIds.Contains(trash.GetInstanceID())) return;

            // 2. Verificar si el jugador lo está sosteniendo
            if (carryable != null && carryable.IsCarried)
            {
                // Si lo tiene en la mano, SOLO abrimos la tapa como feedback, pero NO lo destruimos
                if (!isOpen)
                {
                    Open();
                    // Mantener abierto mientras el jugador tenga la basura cerca
                    closeTimer = autoCloseDelay;
                }
                return; 
            }

            // 3. Verificar Tag (Soporta tag en el objeto, en el padre o en la raíz)
            bool isCorrectTag = false;
            
            // Debug: Mostrar el tag del objeto
            string objectTag = other.tag;
            string parentTag = other.transform.parent != null ? other.transform.parent.tag : "null";
            string rootTag = other.transform.root.tag;
            
            Debug.Log($"[TRASHCAN DEBUG] Objeto: {other.name} | Tag: '{objectTag}' | Parent Tag: '{parentTag}' | Root Tag: '{rootTag}'");
            Debug.Log($"[TRASHCAN DEBUG] Buscando tags: {string.Join(", ", acceptedTrashTags)}");
            
            foreach (string acceptedTag in acceptedTrashTags)
            {
                if (other.CompareTag(acceptedTag) || 
                    (other.transform.parent != null && other.transform.parent.CompareTag(acceptedTag)) ||
                    other.transform.root.CompareTag(acceptedTag))
                {
                    isCorrectTag = true;
                    Debug.Log($"[TRASHCAN DEBUG] ✅ Match encontrado con tag: '{acceptedTag}'");
                    break;
                }
            }

            if (isCorrectTag)
            {
                // Evitar procesar dos veces el mismo objeto si ya se está absorbiendo
                if (trash.IsCleaned) return;

                Debug.Log($"[TRASHCAN] ✅ Absorbiendo basura: {trash.name}");
                
                // Marcar como procesado inmediatamente
                processedTrashIds.Add(trash.GetInstanceID());

                // 4. Iniciar efecto de absorción
                StartCoroutine(SuckAndDestroy(trash));

                // Feedback visual del tacho
                if (!isOpen) Open();
                closeTimer = autoCloseDelay;
            }
            else
            {
                Debug.LogWarning($"[TRASHCAN] ❌ Objeto rechazado: {other.name} (Tag: '{objectTag}' no coincide con {string.Join(", ", acceptedTrashTags)})");
            }
        }
    }

    private System.Collections.IEnumerator SuckAndDestroy(TrashObject trash)
    {
        // Doble check de seguridad
        if (trash == null || trash.IsCleaned) yield break;

        // Desactivar físicas para controlarlo manualmente
        Rigidbody rb = trash.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
        }
        
        // Desactivar colisiones para que no estorbe
        Collider col = trash.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Vector3 startPos = trash.transform.position;
        Quaternion startRot = trash.transform.rotation;
        Vector3 startScale = trash.transform.localScale;

        // Si no hay punto definido, usar el centro del tacho un poco más abajo
        Vector3 targetPos = trashSuckPoint != null ? trashSuckPoint.position : transform.position - Vector3.up * 0.5f;
        
        float timer = 0f;

        while (timer < suckDuration)
        {
            // 🛡️ PROTECCIÓN: Si el objeto fue destruido externamente, detener la corrutina
            if (trash == null) yield break;

            timer += Time.deltaTime;
            float t = timer / suckDuration;
            float curveT = suckCurve.Evaluate(t);

            // Interpolar posición hacia el fondo del tacho
            trash.transform.position = Vector3.Lerp(startPos, targetPos, curveT);
            
            // Rotar un poco aleatoriamente o alinear
            trash.transform.rotation = Quaternion.Lerp(startRot, Quaternion.identity, curveT);

            // Reducir escala a 0
            trash.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, curveT);

            yield return null;
        }

        // Finalmente destruir y sumar puntos (Solo si sigue existiendo y no ha sido limpiado por otro proceso)
        if (trash != null && !trash.IsCleaned)
        {
            trash.EliminateTrash();
        }
        
        // Limpiar ID del set (aunque el objeto se destruya, es buena práctica)
        if (trash != null) processedTrashIds.Remove(trash.GetInstanceID());
    }
}
