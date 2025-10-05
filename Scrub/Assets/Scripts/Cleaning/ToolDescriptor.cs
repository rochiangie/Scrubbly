// ToolDescriptor.cs

using UnityEngine;

public class ToolDescriptor : MonoBehaviour
{
    [SerializeField] public string toolId = "Sponge"; // ej: "Sponge", "Mop", "Spray"
    [SerializeField] public float toolPower = 1f;    // multiplicador del daño (damagePerHit)
}