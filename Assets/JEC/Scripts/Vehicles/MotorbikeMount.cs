using Invector.vCharacterController;
using rayzngames;
using UnityEngine;
using UnityEngine.Animations.Rigging;

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
    [SerializeField] private string mountedAnimatorState = "Free Crouch";
    [SerializeField] private string locomotionAnimatorState = "Free Locomotion";

    private BicycleVehicle bicycle;
    private BikeControlsExample bikeControls;
    private Rigidbody bikeRigidbody;
    private RigidbodyConstraints bikeConstraintsBeforeMount;
    private BikeIKTargets bikeIKTargets;
    private RiderBikeIKState riderBikeIKState;
    private Transform rider;
    private Transform riderOriginalParent;
    private Rigidbody riderRigidbody;
    private Collider[] riderColliders;
    private bool[] riderColliderStates;
    private vThirdPersonInput riderInput;
    private vThirdPersonMotor[] riderControllers;
    private bool[] riderControllerStates;
    private bool[] riderMovementLockStates;
    private bool[] riderRotationLockStates;
    private PlayerParkourController riderParkour;
    private Animator riderAnimator;
    private vThirdPersonCamera thirdPersonCamera;
    private bool riderCharacterInputWasLocked;
    private bool riderInputWasEnabled;
    private bool riderParkourWasEnabled;
    private bool riderRigidbodyWasKinematic;
    private bool riderRigidbodyDetectedCollisions;
    private bool riderAnimatorAppliedRootMotion;
    private float riderAnimatorSpeed;
    private float nextInputTime;

    private Transform packagedRiderPose;
    private Transform packagedLeftHandTarget;
    private Transform packagedRightHandTarget;
    private Transform packagedLeftFootTarget;
    private Transform packagedRightFootTarget;
    private Transform packagedLeftElbowHint;
    private Transform packagedRightElbowHint;
    private Transform packagedLeftKneeHint;
    private Transform packagedRightKneeHint;

    public bool IsMounted => rider != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AddMountSupportToPackagedBikeRigs()
    {
        BicycleVehicle[] bicycles = Object.FindObjectsByType<BicycleVehicle>(FindObjectsInactive.Include);

        for (int i = 0; i < bicycles.Length; i++)
        {
            BicycleVehicle candidate = bicycles[i];
            if (candidate.GetComponent<MotorbikeMount>() != null)
            {
                continue;
            }

            Transform candidateTransform = candidate.transform;
            if (FindChildByName(candidateTransform, "Rag_Rig_URP") != null &&
                FindChildByName(candidateTransform, "Hand_L_target") != null &&
                FindChildByName(candidateTransform, "Hand_R_target") != null &&
                FindChildByName(candidateTransform, "Feet_L_target") != null &&
                FindChildByName(candidateTransform, "Feet_R_target") != null)
            {
                candidate.gameObject.AddComponent<MotorbikeMount>();
            }
        }
    }

    private void Awake()
    {
        bicycle = GetComponent<BicycleVehicle>();
        bikeControls = GetComponent<BikeControlsExample>();
        bikeRigidbody = GetComponent<Rigidbody>();
        bikeIKTargets = GetComponent<BikeIKTargets>();
        ResolvePackagedRigReferences();
        DisablePackagedRider();

        if (bikeIKTargets != null)
        {
            bikeIKTargets.enabled = false;
        }

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

    private void FixedUpdate()
    {
        if (rider == null || bikeRigidbody == null)
        {
            return;
        }

        // Keep steering around Y, but stop the scooter from pitching or rolling over.
        RigidbodyConstraints mountedConstraints = bikeConstraintsBeforeMount | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        mountedConstraints &= ~RigidbodyConstraints.FreezeRotationY;
        bikeRigidbody.constraints = mountedConstraints;

        Vector3 angularVelocity = bikeRigidbody.angularVelocity;
        angularVelocity.x = 0f;
        angularVelocity.z = 0f;
        bikeRigidbody.angularVelocity = angularVelocity;
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

    // 오토바이 탑승
    private void Mount(Transform newRider)
    {
        rider = newRider;
        riderOriginalParent = rider.parent;
        riderInput = rider.GetComponent<vThirdPersonInput>();
        riderControllers = rider.GetComponents<vThirdPersonMotor>();
        riderControllerStates = new bool[riderControllers.Length];
        riderMovementLockStates = new bool[riderControllers.Length];
        riderRotationLockStates = new bool[riderControllers.Length];
        riderParkour = rider.GetComponent<PlayerParkourController>();
        riderAnimator = rider.GetComponent<Animator>();
        riderRigidbody = rider.GetComponent<Rigidbody>();
        riderBikeIKState = rider.GetComponent<RiderBikeIKState>();
        if (riderBikeIKState == null)
        {
            riderBikeIKState = rider.gameObject.AddComponent<RiderBikeIKState>();
        }

        riderColliders = rider.GetComponentsInChildren<Collider>(true);
        riderColliderStates = new bool[riderColliders.Length];



        if (riderInput != null)
        {
            riderInputWasEnabled = riderInput.enabled;
            riderCharacterInputWasLocked = riderInput.lockCharacterInput;

            thirdPersonCamera = riderInput.tpCamera != null ? riderInput.tpCamera : Object.FindAnyObjectByType<vThirdPersonCamera>();
            riderInput.lockCharacterInput = true;
        }

        if (riderParkour != null)
        {
            riderParkourWasEnabled = riderParkour.enabled;
            riderParkour.EndParkour();
            riderParkour.enabled = false;
        }

        for (int i = 0; i < riderControllers.Length; i++)
        {
            vThirdPersonMotor controller = riderControllers[i];
            riderControllerStates[i] = controller.enabled;
            riderMovementLockStates[i] = controller.lockMovement;
            riderRotationLockStates[i] = controller.lockRotation;

            ResetRiderControllerState(controller);
            controller.lockMovement = true;
            controller.lockRotation = true;
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

        if (bikeRigidbody != null)
        {
            bikeConstraintsBeforeMount = bikeRigidbody.constraints;
            bikeRigidbody.angularVelocity = Vector3.zero;
        }

        if (bikeIKTargets != null)
        {
            bikeIKTargets.enabled = true;
            bikeIKTargets.ApplyTargets();
        }

        if (packagedRiderPose != null)
        {
            riderBikeIKState.BindBikeTargets(
                packagedLeftHandTarget,
                packagedRightHandTarget,
                packagedLeftFootTarget,
                packagedRightFootTarget,
                packagedLeftElbowHint,
                packagedRightElbowHint,
                packagedLeftKneeHint,
                packagedRightKneeHint);
        }

        riderBikeIKState.SetMounted(true);

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

        if (mountPoint != null)
        {
            rider.localPosition = Vector3.zero;
            rider.localRotation = Quaternion.identity;
        }
        else if (packagedRiderPose != null)
        {
            rider.localPosition = packagedRiderPose.localPosition;
            rider.localRotation = packagedRiderPose.localRotation;
        }
        else
        {
            rider.localPosition = mountLocalPosition;
            rider.localRotation = Quaternion.Euler(mountLocalEulerAngles);
        }
    }

    // 오토바이 하차
    private void Dismount()
    {
        Transform departingRider = rider;

        Vector3 exitPosition = dismountPoint != null
            ? dismountPoint.position
            : transform.TransformPoint(dismountLocalPosition);

        Quaternion exitRotation =
            Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        if (riderBikeIKState != null)
        {
            riderBikeIKState.SetMounted(false);
        }

        if (bikeIKTargets != null)
        {
            bikeIKTargets.enabled = false;
        }

        SetBikeControl(false);

        if (bikeRigidbody != null)
        {
            bikeRigidbody.angularVelocity = Vector3.zero;
            bikeRigidbody.constraints = bikeConstraintsBeforeMount;
        }

        departingRider.SetParent(riderOriginalParent, true);
        departingRider.SetPositionAndRotation(exitPosition, exitRotation);

        // Restore controller input state before re-enabling player input.
        for (int i = 0; i < riderControllers.Length; i++)
        {
            if (riderControllers[i] == null)
            {
                continue;
            }

            ResetRiderControllerState(riderControllers[i]);
            riderControllers[i].lockMovement = riderMovementLockStates[i];
            riderControllers[i].lockRotation = riderRotationLockStates[i];
            riderControllers[i].enabled = riderControllerStates[i];
        }

        // Restore Rigidbody and collider participation in the physics world.
        if (riderRigidbody != null)
        {
            riderRigidbody.isKinematic = riderRigidbodyWasKinematic;
            riderRigidbody.detectCollisions = riderRigidbodyDetectedCollisions;
            riderRigidbody.linearVelocity = Vector3.zero;
            riderRigidbody.angularVelocity = Vector3.zero;
        }

        for (int i = 0; i < riderColliders.Length; i++)
        {
            if (riderColliders[i] != null)
            {
                riderColliders[i].enabled = riderColliderStates[i];
            }
        }

        Physics.SyncTransforms();

        if (riderRigidbody != null && !riderRigidbody.isKinematic)
        {
            riderRigidbody.WakeUp();
        }

        // Animator 복구
        if (riderAnimator != null)
        {
            riderAnimator.speed = riderAnimatorSpeed;
            riderAnimator.applyRootMotion = riderAnimatorAppliedRootMotion;

            riderAnimator.SetFloat("InputMagnitude", 0f);
            riderAnimator.SetFloat("InputHorizontal", 0f);
            riderAnimator.SetFloat("InputVertical", 0f);
            riderAnimator.SetBool("IsSprinting", false);

            riderAnimator.Update(0f);
        }

        if (riderParkour != null)
        {
            riderParkour.enabled = riderParkourWasEnabled;
        }

        if (riderInput != null)
        {
            riderInput.lockCharacterInput = riderCharacterInputWasLocked;
            riderInput.enabled = riderInputWasEnabled;
        }

        SetCameraTarget(departingRider);

        rider = null;
        promptMessage = "오토바이에 탑승하려면 F를 누르세요.";
        nextInputTime = Time.time + inputCooldown;
    }

    private static void ResetRiderControllerState(vThirdPersonMotor controller)
    {
        controller.input = Vector3.zero;
        controller.inputSmooth = Vector3.zero;
        controller.moveDirection = Vector3.zero;
        controller.isJumping = false;
        controller.isSprintJumping = false;
        controller.isSprinting = false;
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
        bicycle.InControl(active);

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

       // PlayStateIfAvailable(animator, mountedAnimatorState);
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

    private void ResolvePackagedRigReferences()
    {
        packagedRiderPose = FindChildByName(transform, "Rag_Rig_URP");
        if (packagedRiderPose == null)
        {
            return;
        }

        packagedLeftHandTarget = FindChildByName(packagedRiderPose, "Hand_L_target");
        packagedRightHandTarget = FindChildByName(packagedRiderPose, "Hand_R_target");
        packagedLeftFootTarget = FindChildByName(packagedRiderPose, "Feet_L_target");
        packagedRightFootTarget = FindChildByName(packagedRiderPose, "Feet_R_target");
        packagedLeftElbowHint = FindChildByName(packagedRiderPose, "Hand_L_hint");
        packagedRightElbowHint = FindChildByName(packagedRiderPose, "Hand_R_hint");
        packagedLeftKneeHint = FindChildByName(packagedRiderPose, "Feet_L_hint");
        packagedRightKneeHint = FindChildByName(packagedRiderPose, "Feet_R_hint");
    }

    private void DisablePackagedRider()
    {
        if (packagedRiderPose == null)
        {
            return;
        }

        Renderer[] renderers = packagedRiderPose.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }

        Collider[] colliders = packagedRiderPose.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Animator packagedAnimator = packagedRiderPose.GetComponent<Animator>();
        if (packagedAnimator != null)
        {
            packagedAnimator.enabled = false;
        }

        RigBuilder packagedRigBuilder = packagedRiderPose.GetComponent<RigBuilder>();
        if (packagedRigBuilder != null)
        {
            packagedRigBuilder.Clear();
            packagedRigBuilder.enabled = false;
        }
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
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
