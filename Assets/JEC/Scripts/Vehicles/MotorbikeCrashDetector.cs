using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class MotorbikeCrashDetector : MonoBehaviour
{
    [SerializeField, Min(0f)] private float minimumImpactSpeed = 4f;

    private MotorbikeMount motorbikeMount;

    private void Awake()
    {
        motorbikeMount = GetComponent<MotorbikeMount>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null) TryProcessImpact(collision.collider, collision.relativeVelocity.magnitude);
    }

    private bool TryProcessImpact(Collider otherCollider, float impactSpeed)
    {
        if (RespawnManager.IsTransitionActive ||
            impactSpeed < minimumImpactSpeed)
        {
            return false;
        }

        if (motorbikeMount == null)
        {
            motorbikeMount = GetComponent<MotorbikeMount>();
        }

        if (motorbikeMount == null || !motorbikeMount.IsMounted)
        {
            return false;
        }

        if (otherCollider == null)
        {
            return false;
        }

        RespawnReason? reason = null;

        if (otherCollider.GetComponentInParent<CitizenAI>() != null)
        {
            reason = RespawnReason.CitizenCrash;
        }
        else if (otherCollider.GetComponentInParent<TrafficCarAI>() != null ||
                 otherCollider.GetComponentInParent<PoliceCarAI>() != null)
        {
            reason = RespawnReason.VehicleCrash;
        }

        if (reason.HasValue)
        {
            return RespawnManager.TryRequestRespawn(
                reason.Value,
                motorbikeMount.CurrentRider
            );
        }
        return false;
    }
}
