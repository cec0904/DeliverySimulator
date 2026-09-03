using UnityEngine;

// Uses collider geometry rather than world AABBs, so a fallen bike and a prefab
// asset yield the same upright placement bounds without instantiating a probe.
public static class MotorbikePlacementBounds
{
    public static bool TryGetLocalBounds(GameObject bike, out Bounds result)
    {
        result = default;
        if (bike == null) return false;
        bool initialized = false;
        foreach (Collider collider in bike.GetComponentsInChildren<Collider>(true))
        {
            if (!collider.enabled || collider.isTrigger) continue;
            Bounds local;
            if (collider is MeshCollider mesh && mesh.sharedMesh != null)
                local = mesh.sharedMesh.bounds;
            else if (collider is BoxCollider box)
                local = new Bounds(box.center, box.size);
            else if (collider is SphereCollider sphere)
                local = new Bounds(sphere.center, Vector3.one * sphere.radius * 2f);
            else if (collider is CapsuleCollider capsule)
            {
                Vector3 size = Vector3.one * capsule.radius * 2f;
                size[capsule.direction] = Mathf.Max(capsule.height, size[capsule.direction]);
                local = new Bounds(capsule.center, size);
            }
            else if (collider is WheelCollider wheel)
            {
                local = new Bounds(wheel.center - Vector3.up * wheel.suspensionDistance * 0.5f,
                    Vector3.one * wheel.radius * 2f + Vector3.up * wheel.suspensionDistance);
            }
            else continue;

            Matrix4x4 toRoot = bike.transform.worldToLocalMatrix * collider.transform.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 sign = new Vector3((corner & 1) == 0 ? -1 : 1,
                    (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                Vector3 point = toRoot.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, sign));
                if (!initialized)
                {
                    result = new Bounds(point, Vector3.zero);
                    initialized = true;
                }
                else result.Encapsulate(point);
            }
        }
        return initialized && result.size.sqrMagnitude > 0.001f;
    }
}
