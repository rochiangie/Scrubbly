using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

public class HeldItemSlot : MonoBehaviour
{
    // 🚨 1. SINGLETON: Punto de acceso estático para la UI.
    public static HeldItemSlot Instance { get; private set; }

    private Transform holdPoint;

    // Estado interno
    public ToolDescriptor CurrentTool { get; private set; }
    public bool HasTool => CurrentTool != null;

    [Header("Notificación de UI")]
    public TMP_Text notificationText;
    private float notificationTimer = 0f;
    private const float NotificationDuration = 2.0f;

    // Dependencias
    private UIPauseController uiController;


    void Awake()
    {
        // 🚨 CRÍTICO: Configuración del Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Asignamos el holdPoint al propio transform del WeaponSlot.
        holdPoint = this.transform;
        uiController = FindObjectOfType<UIPauseController>();
    }

    void Update()
    {
        // Temporizador de notificación
        if (notificationTimer > 0)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0 && notificationText != null)
            {
                notificationText.text = "";
            }
        }
    }

    // =========================================================================
    // 🚀 MÉTODOS DE EQUIPAMIENTO (PUNTOS DE ACCESO) 🚀
    // =========================================================================

    /// <summary>
    /// Método estático llamado por los botones de la UI (Para objetos persistentes).
    /// </summary>
    // Fragmento CRÍTICO de HeldItemSlot.cs

    public static void StaticEquipToolPrefab(GameObject toolPrefab)
    {
        // 🚨 DEBUG CRÍTICO: Si ves este log, el botón funciona.
        Debug.Log($"[BOTÓN CLICKED] Intentando equipar: {toolPrefab.name}.");

        if (Instance == null)
        {
            Debug.LogError("HeldItemSlot: Intento de equipar herramienta, pero HeldItemSlot.Instance es nulo. Verifique la persistencia del Player.");
            return;
        }

        // Ejecutar el método de instancia para el trabajo real.
        Instance.EquipToolPrefab(toolPrefab);
    }

    /// <summary>
    /// 🚨 CRÍTICO: Método de INSTANCIA llamado por PlayerInteraction.cs 🚨
    /// Este método es el que resuelve el error EquipToolPrefab.
    /// </summary>
    public void EquipToolPrefab(GameObject toolPrefab)
    {
        DestroyCurrentTool();

        if (toolPrefab == null || holdPoint == null)
        {
            Debug.LogError("No se puede equipar: Prefab o punto de agarre es nulo.");
            return;
        }

        // 1. Instanciar directamente como hijo del holdPoint (Resuelve el error de Prefab Asset)
        GameObject newToolInstance = Instantiate(toolPrefab, holdPoint);

        // 2. Alinear al origen del hueso
        newToolInstance.transform.localPosition = Vector3.zero;
        newToolInstance.transform.localRotation = Quaternion.identity;

        ToolDescriptor toolDescriptor = newToolInstance.GetComponent<ToolDescriptor>() ?? newToolInstance.GetComponentInParent<ToolDescriptor>();

        if (toolDescriptor == null)
        {
            Debug.LogError($"'{toolPrefab.name}' no tiene ToolDescriptor. Destruyendo instancia.");
            Destroy(newToolInstance);
            return;
        }

        // 3. Usar el método de equipamiento (solo para physics/colliders).
        Equip(toolDescriptor);

        // 4. Notificación y UI
        ShowNotification($"Herramienta equipada: {toolDescriptor.ToolId}");
        uiController?.ToggleToolsPanel();
    }

    /// <summary>
    /// Destruye la instancia de la herramienta actualmente equipada.
    /// </summary>
    public void DestroyCurrentTool()
    {
        var toolToDestroy = Unequip();

        if (toolToDestroy != null)
        {
            Destroy(toolToDestroy.gameObject);
            ShowNotification("Herramienta destruida.");
        }
    }

    private void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationTimer = NotificationDuration;
        }
    }


    // =========================================================================
    // MÉTODOS DE GESTIÓN DE TRANSFORM/PHYSICS
    // =========================================================================

    public void Equip(ToolDescriptor tool)
    {
        CurrentTool = tool;

        if (tool.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        SetAllCollidersTrigger(tool.gameObject, true);
    }

    public ToolDescriptor Unequip()
    {
        var tool = CurrentTool;
        if (tool == null) return null;

        if (tool.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = false;

        SetAllCollidersTrigger(tool.gameObject, false);

        // Quitar al padre (para soltar)
        tool.transform.SetParent(null, true);
        CurrentTool = null;
        return tool;
    }

    private void SetAllCollidersTrigger(GameObject go, bool isTrigger)
    {
        var colliders = go.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) c.isTrigger = isTrigger;
    }
}