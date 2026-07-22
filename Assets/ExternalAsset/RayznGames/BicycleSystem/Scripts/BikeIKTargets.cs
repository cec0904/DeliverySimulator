using UnityEngine;

namespace rayzngames
{    
    public class BikeIKTargets : MonoBehaviour
    {
        [Header("BikePositions")]
        [SerializeField] Transform handleLeft;
        [SerializeField] Transform handleRight;
        [SerializeField] Transform pedalLeft;
        [SerializeField] Transform pedalRight;

        [Header("IK Rig Targets")]
        [SerializeField] Transform leftHandTarget;
        [SerializeField] Vector3 leftHandRotation = new Vector3(0, 0, 0);
        [SerializeField] Transform rightHandTarget;
        [SerializeField] Vector3 rightHandRotation = new Vector3(0, 0, 0);
        [SerializeField] Transform leftFootTarget;
        [SerializeField] Vector3 leftFootRotation = new Vector3(0, 0, 0);
        [SerializeField] Transform rightFootTarget;
        [SerializeField] Vector3 rightFootRotation = new Vector3(0, 0, 0);

        void OnEnable()
        {
            ApplyTargets();
        }

        void Update()
        {
            ApplyTargets();
        }

        public void ApplyTargets()
        {
            ApplyTarget(handleLeft, leftHandTarget, leftHandRotation);
            ApplyTarget(handleRight, rightHandTarget, rightHandRotation);
            ApplyTarget(pedalLeft, leftFootTarget, leftFootRotation);
            ApplyTarget(pedalRight, rightFootTarget, rightFootRotation);
        }

        public void ApplyScooterGripRotationIfUnset()
        {
            // The custom scooter markers currently contain identity rotations.
            // These are the grip-axis corrections used by the package scooter rig.
            if (leftHandRotation.sqrMagnitude < 0.0001f)
            {
                leftHandRotation = new Vector3(0f, 90f, 90f);
            }

            if (rightHandRotation.sqrMagnitude < 0.0001f)
            {
                rightHandRotation = new Vector3(0f, -90f, -90f);
            }
        }

        static void ApplyTarget(Transform source, Transform target, Vector3 rotationOffset)
        {
            if (source == null || target == null)
            {
                return;
            }

            Quaternion targetRotation = source.rotation * Quaternion.Euler(rotationOffset);
            target.SetPositionAndRotation(source.position, targetRotation);
        }
    }
}
