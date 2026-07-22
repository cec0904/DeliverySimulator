using System.Collections;
using Invector.vCharacterController;
using UnityEngine;

public enum ParkourAction
{
    None,
    Climb,
    VaultToTop,
    VaultOver,
    VaultSlide
}

public enum VaultSlideType
{
    Top,
    Down
}

public struct ParkourTargetData
{
    public Collider obstacleCollider;
    public Vector3 frontPosition;
    public Vector3 topPosition;
    public Vector3 topExitPosition;
    public Vector3 landingPosition;
    public Vector3 slideEntryPosition;
    public Vector3 slideDirection;
    public Quaternion facingRotation;
    public float obstacleHeight;
    public float obstacleLength;
}

public class PlayerParkourController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Parkour Motion")]
    [SerializeField, Min(0f)] private float alignmentDuration = 0.12f;
    [SerializeField, Min(0.1f)] private float slideSpeed = 3.5f;
    [SerializeField, Min(0.05f)] private float slideExitClearance = 0.5f;
    [SerializeField, Min(0f)] private float vaultArcHeight = 0.45f;
    [SerializeField, Min(0f)] private float climbClearance = 0.3f;
    [SerializeField, Range(0.5f, 0.95f)] private float climbArrivalNormalizedTime = 0.78f;
    [SerializeField, Range(0.5f, 0.95f)] private float vaultToTopArrivalNormalizedTime = 0.64f;
    [SerializeField, Range(0.5f, 0.95f)] private float vaultArrivalNormalizedTime = 0.88f;
    [SerializeField, Min(0.1f)] private float animatorStateTimeout = 0.5f;
    [SerializeField, Min(0.01f)] private float arrivalDistance = 0.05f;

    private vThirdPersonController controller;
    private Coroutine parkourRoutine;
    private bool parkourActive;
    private Vector3 finalPosition;
    private Quaternion finalRotation;

    private void Awake()
    {

        animator = GetComponent<Animator>();
        controller = GetComponent<vThirdPersonController>();
    }

    public ParkourAction DivisionClimb(float obstacleHeight)
    {
        if (obstacleHeight >= 1.5f)
        {
            return ParkourAction.Climb;
        }

        return ParkourAction.None;
    }

    public ParkourAction DivisionVault(float obstacleHeight, float inputMagnitude)
    {
        if (obstacleHeight < 0.5f || obstacleHeight >= 1.5f)
        {
            return ParkourAction.None;
        }

        if (inputMagnitude <= 0.5f)
        {
            return ParkourAction.VaultToTop;
        }

        return ParkourAction.VaultOver;
    }

    public void StartParkour(ParkourAction action, ParkourTargetData target, VaultSlideType slideType = VaultSlideType.Top)
    {
  

        if (action == ParkourAction.None || controller == null || controller.isParkouring)
        {
            return;
        }

        if (parkourRoutine != null)
        {
            StopCoroutine(parkourRoutine);
        }

        parkourRoutine = StartCoroutine(RunParkour(action, target, slideType));
    }

    private IEnumerator RunParkour(ParkourAction action, ParkourTargetData target, VaultSlideType slideType)
    {

        parkourActive = true;
        finalRotation = target.facingRotation;

        controller.BeginParkour(target.obstacleCollider);
        yield return AlignToFront(target.frontPosition, target.facingRotation);

        if (!parkourActive)
        {
            yield break;
        }

        switch (action)
        {
            case ParkourAction.Climb:
                finalPosition = target.topPosition;
                yield return RunTopAction("StartClimb", target, climbClearance, climbArrivalNormalizedTime);
                break;

            case ParkourAction.VaultToTop:
                finalPosition = target.topPosition;
                yield return RunTopAction("StartVaultToTop", target, vaultArcHeight, vaultToTopArrivalNormalizedTime);
                break;

            case ParkourAction.VaultOver:
                finalPosition = target.landingPosition;
                yield return RunVaultOver(target);
                break;

            case ParkourAction.VaultSlide:
                yield return RunSlide(target, slideType);
                break;
        }

        if (parkourActive)
        {
            CompleteParkour();
        }
    }

    private IEnumerator AlignToFront(Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = controller.ParkourPosition;
        Quaternion startRotation = controller.ParkourRotation;
        float elapsed = 0f;

        while (parkourActive && elapsed < alignmentDuration)
        {
            elapsed += Time.deltaTime;
            float t = Smooth01(elapsed / Mathf.Max(0.0001f, alignmentDuration));
            Vector3 position = Vector3.Lerp(startPosition, targetPosition, t);
            Quaternion rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            controller.SetParkourPose(position, rotation);
            yield return null;
        }

        controller.SetParkourPose(targetPosition, targetRotation);
    }

    private IEnumerator RunTopAction(string stateName, ParkourTargetData target, float clearance, float arrivalNormalizedTime)
    {
        animator.CrossFadeInFixedTime(stateName, 0.1f, 0, 0f);
        yield return WaitForAnimatorState(stateName);

        Vector3 start = controller.ParkourPosition;
        Vector3 control = Vector3.Lerp(start, target.topPosition, 0.5f);
        control.y = Mathf.Max(start.y, target.topPosition.y) + clearance;

        while (parkourActive)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if (!state.IsName(stateName))
            {
                break;
            }

            float t = Smooth01(Mathf.InverseLerp(0f, arrivalNormalizedTime, state.normalizedTime));
            Vector3 position = QuadraticBezier(start, control, target.topPosition, t);

            controller.SetParkourPose(position, target.facingRotation);

            if (t >= 1f && HasArrived(target.topPosition))
            {
                CompleteParkour();
                yield break;
            }

            yield return null;
        }

        controller.SetParkourPose(target.topPosition, target.facingRotation);

        if (HasArrived(target.topPosition))
        {
            CompleteParkour();
        }
    }

    private IEnumerator RunVaultOver(ParkourTargetData target)
    {
        Debug.Log("Slide 호출");

        const string stateName = "StartVaultOver";

        animator.CrossFadeInFixedTime(stateName, 0.1f, 0, 0f);
        yield return WaitForAnimatorState(stateName);

        Vector3 start = controller.ParkourPosition;
        Vector3 control = Vector3.Lerp(start, target.landingPosition, 0.5f);
        control.y = Mathf.Max(start.y, target.topPosition.y) + vaultArcHeight;

        while (parkourActive)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if (!state.IsName(stateName))
            {
                break;
            }

            float t = Smooth01(Mathf.InverseLerp(0f, vaultArrivalNormalizedTime, state.normalizedTime));
            Vector3 position = QuadraticBezier(start, control, target.landingPosition, t);

            controller.SetParkourPose(position, target.facingRotation);

            if (t >= 1f && HasArrived(target.landingPosition))
            {
                CompleteParkour();
                yield break;
            }

            yield return null;
        }

        controller.SetParkourPose(target.landingPosition, target.facingRotation);

        if (HasArrived(target.landingPosition))
        {
            CompleteParkour();
        }
    }

    private IEnumerator RunSlide(ParkourTargetData target, VaultSlideType slideType)
    {
        animator.CrossFadeInFixedTime("StartSlide", 0.1f, 0, 0f);
        yield return WaitForAnimatorState("StartSlide");

        Vector3 start = controller.ParkourPosition;
        Vector3 slideDirection = target.slideDirection.normalized;

        while (parkourActive)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if (!state.IsName("StartSlide"))
            {
                break;
            }

            float t = Smooth01(Mathf.InverseLerp(0f, 0.65f, state.normalizedTime));
            Vector3 position = Vector3.Lerp(start, target.slideEntryPosition, t);

            controller.SetParkourPose(position, target.facingRotation);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        if (!parkourActive)
        {
            yield break;
        }

        controller.SetParkourPose(target.slideEntryPosition, target.facingRotation);
        animator.CrossFadeInFixedTime("SlideLoop", 0.08f, 0, 0f);
        yield return WaitForAnimatorState("SlideLoop");

        float distanceAfterCeiling = 0f;
        bool passedUnderCeiling = false;

        while (parkourActive)
        {
            float stepDistance = slideSpeed * Time.deltaTime;

            if (!controller.TryGetSlidePosition(controller.ParkourPosition, slideDirection, stepDistance, out Vector3 position))
            {
                yield return null;
                continue;
            }

            controller.SetParkourPose(position, target.facingRotation);

            if (controller.HasSlideCeiling(controller.ParkourPosition, target.obstacleCollider))
            {
                passedUnderCeiling = true;
                distanceAfterCeiling = 0f;
            }
            else if (passedUnderCeiling)
            {
                distanceAfterCeiling += stepDistance;

                if (distanceAfterCeiling >= slideExitClearance && controller.CanStandAt(controller.ParkourPosition))
                {
                    break;
                }
            }

            yield return null;
        }

        if (!parkourActive)
        {
            yield break;
        }

        animator.CrossFadeInFixedTime("EndSlide", 0.1f, 0, 0f);
        yield return WaitForAnimatorState("EndSlide");

        Vector3 endStart = controller.ParkourPosition;
        Vector3 endPosition = endStart + slideDirection * slideExitClearance;

        if (controller.TryGetSlidePosition(endStart, slideDirection, slideExitClearance, out Vector3 groundedEndPosition))
        {
            endPosition = groundedEndPosition;
        }

        while (parkourActive)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if (!state.IsName("EndSlide"))
            {
                break;
            }

            float t = Smooth01(Mathf.InverseLerp(0f, 0.7f, state.normalizedTime));
            Vector3 position = Vector3.Lerp(endStart, endPosition, t);

            controller.SetParkourPose(position, target.facingRotation);

            if (t >= 1f && controller.CanStandAt(controller.ParkourPosition))
            {
                finalPosition = controller.ParkourPosition;
                CompleteParkour();
                yield break;
            }

            yield return null;
        }

        controller.SetParkourPose(endPosition, target.facingRotation);

        if (controller.CanStandAt(controller.ParkourPosition))
        {
            finalPosition = controller.ParkourPosition;
            CompleteParkour();
        }
    }

    private IEnumerator WaitForAnimatorState(string stateName)
    {
        float elapsed = 0f;

        while (parkourActive && !animator.GetCurrentAnimatorStateInfo(0).IsName(stateName) && elapsed < animatorStateTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void EndParkour()
    {
        if (!parkourActive)
        {
            return;
        }

        CompleteParkour();
    }

    private void CompleteParkour()
    {
        if (!parkourActive)
        {
            return;
        }

        parkourActive = false;

        animator.InterruptMatchTarget(false);
        controller.SetParkourPose(finalPosition, finalRotation);
        controller.EndParkour();
        animator.CrossFadeInFixedTime("Free Locomotion", 0.1f, 0, 0f);
        parkourRoutine = null;
    }

    private bool HasArrived(Vector3 targetPosition)
    {
        return Vector3.Distance(controller.ParkourPosition, targetPosition) <= arrivalDistance;
    }

    private static float Smooth01(float value)
    {
        float t = Mathf.Clamp01(value);
        return t * t * (3f - 2f * t);
    }

    private static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
    }
}
