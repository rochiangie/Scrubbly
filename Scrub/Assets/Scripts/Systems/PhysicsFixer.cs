using UnityEngine;

public class PhysicsFixer : MonoBehaviour
{
    void Awake()
    {
        FixConcaveColliders();
    }

    private void FixConcaveColliders()
    {
        // Find all Rigidbodies in the scene
        Rigidbody[] allRigidbodies = FindObjectsOfType<Rigidbody>();

        int fixedCount = 0;

        foreach (var rb in allRigidbodies)
        {
            // Skip kinematic rigidbodies as they support concave colliders (triggers mostly)
            // But the error says "dynamic Rigidbody", so we focus on non-kinematic ones.
            // However, checking isKinematic might be tricky if it's toggled later.
            // The error specifically complains about dynamic ones.
            
            MeshCollider meshCollider = rb.GetComponent<MeshCollider>();
            
            if (meshCollider != null && !meshCollider.convex)
            {
                // If the RB is not kinematic, it MUST be convex.
                if (!rb.isKinematic)
                {
                    meshCollider.convex = true;
                    fixedCount++;
                    Debug.Log($"🔧 PhysicsFixer: Fixed Concave MeshCollider on '{rb.gameObject.name}' (Set Convex = True)");
                }
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log($"✅ PhysicsFixer: Fixed {fixedCount} invalid MeshColliders.");
        }
    }
}
