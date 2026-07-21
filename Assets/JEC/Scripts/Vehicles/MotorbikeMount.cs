using Invector.vCharacterController;
using rayzngames;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BicycleVehicle))]
public sealed class MotorbikeMount : Interactable
{
    [Header("Mount Positions")]
    [SerializeField] private Transform mountPoint;
    [SerializeField] private Transform dismountPoint;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Vector3 mountLocalPosition = new Vector3(0f, -0.29f, -0.31f);
    [SerializeField] private Vector3 mountLocalEulerAngles;
    [SerializeField] private Vector3 dismountLocalPosition = new Vector3(-1.25f, 0f, 0f);

    [Header("Input")]
    [SerializeField] private KeyCode dismountKey = KeyCode.F;
    [SerializeField, Min(0f)] private float inputCooldown = 0.25f;

    [Header("Optional Animation")]
    [SerializeField] private string mountedAnimatorState = "Free Locomotion";
    [SerializeField] private string locomotionAnimatorState = "Free Locomotion";

    private BicycleVehicle bicycle;
    private BikeControlsExample bikeControls;
    private Transform rider;
    private Transform riderOriginalParent;
    private Rigidbody riderRigidbody;
    private Collider[] riderColliders;
    private bool[] riderColliderStates;
    private vThirdPersonInput riderInput;
    private PlayerParkourController riderParkour;
    private Animator riderAnimator;
    private vThirdPersonCamera thirdPersonCamera;
    private bool riderInputWasEnabled;
    private bool riderParkourWasEnabled;
    private bool riderRigidbodyWasKinematic;
    private bool riderRigidbodyDetectedCollisions;
    private bool riderAnimatorAppliedRootMotion;
    private float riderAnimatorSpeed;
    private float nextInputTime;

    public bool IsMounted => rider != null;

    private void Awake()
    {
        bicycle = GetComponent<BicycleVehicle>();
        bikeControls = GetComponent<BikeControlsExample>();
        promptMessage = "오토바이에 탑승하려면 F를 누르세요.";
        SetBikeControl(false);
    }

    private void Update()
    {
        if (rider == null)
        {
            return;
        }

        ApplyRiderMountPose();

        if (Time.time >= nextInputTime && Input.GetKeyDown(dismountKey))
        {
            Dismount();
        }
    }

    public override void Interact(GameObject interactor)
    {
        if (interactor == null || Time.time < nextInputTime)
        {
            return;
        }

        if (rider == null)
        {
            Mount(interactor.transform);
        }
        else if (rider == interactor.transform)
        {
            Dismount();
        }
    }

    private void Mount(Transform newRider)
    {
        rider = newRider;
        riderOriginalParent = rider.parent;
        riderInput = rider.GetComponent<vThirdPersonInput>();
        riderParkour = rider.GetComponent<PlayerParkourController>();
        riderAnimator = rider.GetComponent<Animator>();
        riderRigidbody = rider.GetComponent<Rigidbody>();
        riderColliders = rider.GetComponentsInChildren<Collider>(true);
        riderColliderStates = new bool[riderColliders.Length];

        if (riderInput != null)
        {
            riderInputWasEnabled = riderInput.enabled;
            thirdPersonCamera = riderInput.tpCamera != null
                ? riderInput.tpCamera
                : Object.FindAnyObjectByType<vThirdPersonCamera>();
            riderInput.enabled = false;
        }

        if (riderParkour != null)
        {
            riderParkourWasEnabled = riderParkour.enabled;
            riderParkour.EndParkour();
            riderParkour.enabled = false;
        }

        if (riderRigidbody != null)
        {
            riderRigidbodyWasKinematic = riderRigidbody.isKinematic;
            riderRigidbodyDetectedCollisions = riderRigidbody.detectCollisions;
            riderRigidbody.linearVelocity = Vector3.zero;
            riderRigidbody.angularVelocity = Vector3.zero;
            riderRigidbody.isKinematic = true;
            riderRigidbody.detectCollisions = false;
        }

        for (int i = 0; i < riderColliders.Length; i++)
        {
            riderColliderStates[i] = riderColliders[i].enabled;
            riderColliders[i].enabled = false;
        }

        if (riderAnimator != null)
        {
            riderAnimatorAppliedRootMotion = riderAnimator.applyRootMotion;
            riderAnimatorSpeed = riderAnimator.speed;
            riderAnimator.applyRootMotion = false;
            FreezeAnimatorAtIdle(riderAnimator);
        }

        rider.SetParent(mountPoint != null ? mountPoint : transform, false);
        ApplyRiderMountPose();

        SetBikeControl(true);
        SetCameraTarget(cameraTarget != null ? cameraTarget : transform);
        promptMessage = "오토바이에서 내리려면 F를 누르세요.";
        nextInputTime = Time.time + inputCooldown;
    }

    private void ApplyRiderMountPose()
    {
        if (rider == null)
        {
            return;
        }

        rider.localPosition = mountPoint != null ? Vector3.zero : mountLocalPosition;
        rider.localRotation = mountPoint != null
            ? Quaternion.identity
            : Quaternion.Euler(mountLocalEulerAngles);
    }

    private void Dismount()
    {
        Transform departingRider = rider;
        Vector3 exitPosition = dismountPoint != null
            ? dismountPoint.position
            : transform.TransformPoint(dismountLocalPosition);
        Quaternion exitRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        SetBikeControl(false);
        departingRider.SetParent(riderOriginalParent, true);
        departingRider.SetPositionAndRotation(exitPosition, exitRotation);

        for (int i = 0; i < riderColliders.Length; i++)
        {
            if (riderColliders[i] != null)
            {
                riderColliders[i].enabled = riderColliderStates[i];
            }
        }

        if (riderRigidbody != null)
        {
            riderRigidbody.isKinematic = riderRigidbodyWasKinematic;
            riderRigidbody.detectCollisions = riderRigidbodyDetectedCollisions;
        }

        if (riderAnimator != null)
        {
            riderAnimator.speed = riderAnimatorSpeed;
            riderAnimator.applyRootMotion = riderAnimatorAppliedRootMotion;
            PlayStateIfAvailable(riderAnimator, locomotionAnimatorState);
            riderAnimator.Update(0f);
        }

        if (riderParkour != null)
        {
            riderParkour.enabled = riderParkourWasEnabled;
        }

        if (riderInput != null)
        {
            riderInput.enabled = riderInputWasEnabled;
        }

        SetCameraTarget(departingRider);
        rider = null;
        promptMessage = "오토바이에 탑승하려면 F를 누르세요.";
        nextInputTime = Time.time + inputCooldown;
    }

    private void SetBikeControl(bool active)
    {
        if (bikeControls != null)
        {
            bikeControls.controllingBike = active;
            bikeControls.enabled = active;
        }

        bicycle.horizontalInput = 0f;
        bicycle.verticalInput = 0f;
        bicycle.braking = !active;
        bicycle.InControl(true);

        if (!active)
        {
            bicycle.ConstrainRotation(true);
        }
    }

    private void SetCameraTarget(Transform target)
    {
        if (thirdPersonCamera == null)
        {
            thirdPersonCamera = Object.FindAnyObjectByType<vThirdPersonCamera>();
        }

        if (thirdPersonCamera != null && target != null)
        {
            thirdPersonCamera.SetMainTarget(target);
        }
    }

    private void FreezeAnimatorAtIdle(Animator animator)
    {
        animator.SetFloat(Animator.StringToHash("InputMagnitude"), 0f);
        animator.SetFloat(Animator.StringToHash("InputHorizontal"), 0f);
        animator.SetFloat(Animator.StringToHash("InputVertical"), 0f);
        animator.SetBool(Animator.StringToHash("IsSprinting"), false);
        animator.SetBool(Animator.StringToHash("IsStrafing"), false);

        PlayStateIfAvailable(animator, mountedAnimatorState);
        animator.Update(0f);
        animator.speed = 0f;
    }

    private static void PlayStateIfAvailable(Animator animator, string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        int stateHash = Animator.StringToHash(stateName);
        if (animator.HasState(0, stateHash))
        {
            animator.Play(stateHash, 0, 0f);
            return;
        }

        int fullPathHash = Animator.StringToHash("Base Layer." + stateName);
        if (animator.HasState(0, fullPathHash))
        {
            animator.Play(fullPathHash, 0, 0f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 mountPosition = mountPoint != null
            ? mountPoint.position
            : transform.TransformPoint(mountLocalPosition);
        Gizmos.DrawWireSphere(mountPosition, 0.15f);

        Gizmos.color = Color.yellow;
        Vector3 exitPosition = dismountPoint != null
            ? dismountPoint.position
            : transform.TransformPoint(dismountLocalPosition);
        Gizmos.DrawWireSphere(exitPosition, 0.15f);
    }
}
