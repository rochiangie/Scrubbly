// ToolDescriptor.cs (en el cubo herramienta)
using UnityEngine;

public class ToolDescriptor : MonoBehaviour
{
    [SerializeField] public string toolId = "Sponge"; // ej: "Sponge", "Mop", "Spray"
    [SerializeField] public float toolPower = 1f;     // multiplicador

    // Por si querés expandir a más cosas (durabilidad, etc.)
}
