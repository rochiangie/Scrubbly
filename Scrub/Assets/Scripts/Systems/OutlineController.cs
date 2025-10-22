using UnityEngine;
using System.Collections.Generic;

public class OutlineController : MonoBehaviour
{
    [Header("Configuración")]
    public List<string> acceptedTags = new List<string> { "Dirt", "Memorie", "Basura" };
    public float maxDistance = 5f;
    public LayerMask targetMask;
    public Camera playerCamera;

    [Header("Debug")]
    public bool showDebugRays = true;

    private Outline currentOutline;
    private Outline lastOutline;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
            Debug.LogError("No se encontró la cámara");
    }

    void Update()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        bool hitValidObject = Physics.Raycast(ray, out hit, maxDistance, targetMask) &&
                             IsTagAccepted(hit.collider.tag);

        if (showDebugRays)
        {
            Color rayColor = hitValidObject ? Color.green : Color.red;
            Debug.DrawRay(ray.origin, ray.direction * maxDistance, rayColor);
        }

        if (hitValidObject)
        {
            currentOutline = hit.collider.GetComponent<Outline>();
            if (currentOutline == null)
                currentOutline = hit.collider.GetComponentInParent<Outline>();
            if (currentOutline == null)
                currentOutline = hit.collider.GetComponentInChildren<Outline>();
        }
        else
        {
            currentOutline = null;
        }

        // Activar/desactivar outline
        if (currentOutline != lastOutline)
        {
            if (lastOutline != null)
                lastOutline.enabled = false;

            if (currentOutline != null)
            {
                currentOutline.enabled = true;
                Debug.Log($"Outline activado en: {currentOutline.gameObject.name}");
            }

            lastOutline = currentOutline;
        }
    }

    bool IsTagAccepted(string tag)
    {
        return acceptedTags.Contains(tag);
    }

    void OnDisable()
    {
        if (lastOutline != null)
        {
            lastOutline.enabled = false;
            lastOutline = null;
        }
    }
}