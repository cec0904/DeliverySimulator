using UnityEngine;

namespace JEC.Vehicles
{
    /// <summary>
    /// Keeps a world-space motorbike indicator facing the gameplay camera and
    /// moves it smoothly up and down around its authored prefab position.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MotorbikeWorldIndicator : MonoBehaviour
    {
        [Header("Visibility")]
        [Tooltip("Only this visual object is hidden while the player is mounted.")]
        [SerializeField] private GameObject visualRoot;

        [Header("Camera Facing")]
        [Tooltip("Leave empty to use the camera tagged MainCamera.")]
        [SerializeField] private Camera targetCamera;

        [Header("Bobbing")]
        [Min(0f)]
        [SerializeField] private float bobDistance = 0.15f;

        [Min(0.01f)]
        [Tooltip("Time for one complete up-and-down cycle.")]
        [SerializeField] private float bobCycleSeconds = 2f;

        private Transform cachedParent;
        private Vector3 authoredLocalPosition;
        private Quaternion authoredLocalRotation;
        private Vector3 authoredWorldPosition;
        private MotorbikeMount motorbikeMount;
        private bool initialized;

        private void Awake()
        {
            CaptureAuthoredTransform();
        }

        private void OnEnable()
        {
            if (!initialized)
            {
                CaptureAuthoredTransform();
            }
        }

        private void LateUpdate()
        {
            UpdateVisibility();

            if (!initialized || transform.parent != cachedParent)
            {
                CaptureAuthoredTransform();
            }

            float cycle = Mathf.Max(0.01f, bobCycleSeconds);
            float bob = Mathf.Sin(Time.time * (Mathf.PI * 2f / cycle)) * bobDistance;
            Vector3 anchorPosition = cachedParent != null
                ? cachedParent.TransformPoint(authoredLocalPosition)
                : authoredWorldPosition;

            transform.position = anchorPosition + Vector3.up * bob;

            Camera activeCamera = targetCamera;
            if (activeCamera == null || !activeCamera.isActiveAndEnabled)
            {
                activeCamera = Camera.main;
            }

            if (activeCamera != null)
            {
                transform.rotation = activeCamera.transform.rotation * authoredLocalRotation;
            }
        }

        private void OnDisable()
        {
            if (!initialized)
            {
                return;
            }

            if (cachedParent != null && transform.parent == cachedParent)
            {
                transform.localPosition = authoredLocalPosition;
                transform.localRotation = authoredLocalRotation;
            }
            else if (cachedParent == null)
            {
                transform.position = authoredWorldPosition;
                transform.rotation = authoredLocalRotation;
            }
        }

        private void CaptureAuthoredTransform()
        {
            cachedParent = transform.parent;
            authoredLocalPosition = transform.localPosition;
            authoredLocalRotation = transform.localRotation;
            authoredWorldPosition = transform.position;
            initialized = true;
        }

        private void UpdateVisibility()
        {
            if (motorbikeMount == null)
            {
                motorbikeMount = GetComponentInParent<MotorbikeMount>(true);
            }

            bool shouldBeVisible = motorbikeMount == null || !motorbikeMount.IsMounted;

            if (visualRoot != null && visualRoot.activeSelf != shouldBeVisible)
            {
                visualRoot.SetActive(shouldBeVisible);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            bobDistance = Mathf.Max(0f, bobDistance);
            bobCycleSeconds = Mathf.Max(0.01f, bobCycleSeconds);
        }
#endif
    }
}
