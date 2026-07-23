using UnityEngine;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class RiderBikeIKState : MonoBehaviour
{
    [SerializeField] private string riderRigObjectName = "RiderIK";
    [SerializeField] private string leftElbowHintName = "LeftElbowHint";
    [SerializeField] private string rightElbowHintName = "RightElbowHint";

    private Rig riderRig;
    private RigBuilder rigBuilder;
    private Animator animator;
    private Transform leftHandTarget;
    private Transform rightHandTarget;
    private Transform leftFootTarget;
    private Transform rightFootTarget;
    private Transform leftElbowHint;
    private Transform rightElbowHint;
    private Transform leftKneeHint;
    private Transform rightKneeHint;

    private void OnEnable()
    {
        ResolveRig();
        rigBuilder = GetComponent<RigBuilder>();
        animator = GetComponent<Animator>();
        ResolveHumanoidTargets();

        // A zero-weight TwoBoneIK job still touches its stream handles. The
        // Humanoid Animator IK callback below replaces this graph at runtime.
        if (rigBuilder != null)
        {
            rigBuilder.enabled = false;
        }

        ConnectArmHint("LeftArmIK", leftElbowHintName);
        ConnectArmHint("RightArmIK", rightElbowHintName);
        ResolveHumanoidTargets();
    }

    private void Awake()
    {
        ResolveRig();
        SetMounted(false);
    }

    public bool IsMounted { get; private set; }

    public void BindBikeTargets(
        Transform newLeftHandTarget,
        Transform newRightHandTarget,
        Transform newLeftFootTarget,
        Transform newRightFootTarget,
        Transform newLeftElbowHint,
        Transform newRightElbowHint,
        Transform newLeftKneeHint,
        Transform newRightKneeHint)
    {
        leftHandTarget = newLeftHandTarget;
        rightHandTarget = newRightHandTarget;
        leftFootTarget = newLeftFootTarget;
        rightFootTarget = newRightFootTarget;
        leftElbowHint = newLeftElbowHint;
        rightElbowHint = newRightElbowHint;
        leftKneeHint = newLeftKneeHint;
        rightKneeHint = newRightKneeHint;
    }

    public void SetMounted(bool mounted)
    {
        IsMounted = mounted;

        ResolveRig();

        if (riderRig != null)
            riderRig.weight = 0f;

        if (rigBuilder == null)
        {
            rigBuilder = GetComponent<RigBuilder>();
        }

        if (rigBuilder == null)
        {
            return;
        }

        rigBuilder.Clear();
        rigBuilder.enabled = false;
    }

    private void ResolveRig()
    {
        if (riderRig != null)
        {
            return;
        }

        Rig[] rigs = GetComponentsInChildren<Rig>(true);
        for (int i = 0; i < rigs.Length; i++)
        {
            if (rigs[i].gameObject.name == riderRigObjectName)
            {
                riderRig = rigs[i];
                return;
            }
        }

        if (rigs.Length > 0)
        {
            riderRig = rigs[0];
        }
    }

    private void ResolveHumanoidTargets()
    {
        if (riderRig == null)
            return;

        foreach (TwoBoneIKConstraint constraint in riderRig.GetComponentsInChildren<TwoBoneIKConstraint>(true))
        {
            TwoBoneIKConstraintData data = constraint.data;
            switch (constraint.gameObject.name)
            {
                case "Hand_L":
                case "LeftArmIK":
                    leftHandTarget = data.target;
                    leftElbowHint = data.hint;
                    break;
                case "Hand_R":
                case "RightArmIK":
                    rightHandTarget = data.target;
                    rightElbowHint = data.hint;
                    break;
                case "Feet_L":
                case "LeftLegIK":
                    leftFootTarget = data.target;
                    leftKneeHint = data.hint;
                    break;
                case "Feet_R":
                case "RightLegIK":
                    rightFootTarget = data.target;
                    rightKneeHint = data.hint;
                    break;
            }
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !animator.isHuman)
            return;

        float weight = IsMounted ? 1f : 0f;
        ApplyGoal(AvatarIKGoal.LeftHand, leftHandTarget, weight);
        ApplyGoal(AvatarIKGoal.RightHand, rightHandTarget, weight);
        ApplyGoal(AvatarIKGoal.LeftFoot, leftFootTarget, weight);
        ApplyGoal(AvatarIKGoal.RightFoot, rightFootTarget, weight);
        ApplyHint(AvatarIKHint.LeftElbow, leftElbowHint, weight);
        ApplyHint(AvatarIKHint.RightElbow, rightElbowHint, weight);
        ApplyHint(AvatarIKHint.LeftKnee, leftKneeHint, weight);
        ApplyHint(AvatarIKHint.RightKnee, rightKneeHint, weight);
    }

    private void ApplyGoal(AvatarIKGoal goal, Transform target, float weight)
    {
        float appliedWeight = target != null ? weight : 0f;
        animator.SetIKPositionWeight(goal, appliedWeight);
        animator.SetIKRotationWeight(goal, appliedWeight);
        if (appliedWeight <= 0f)
            return;

        animator.SetIKPosition(goal, target.position);
        animator.SetIKRotation(goal, target.rotation);
    }

    private void ApplyHint(AvatarIKHint hintGoal, Transform hint, float weight)
    {
        float appliedWeight = hint != null ? weight : 0f;
        animator.SetIKHintPositionWeight(hintGoal, appliedWeight);
        if (appliedWeight > 0f)
            animator.SetIKHintPosition(hintGoal, hint.position);
    }

    private bool ConnectArmHint(string constraintObjectName, string hintObjectName)
    {
        if (riderRig == null)
        {
            return false;
        }

        TwoBoneIKConstraint[] constraints = riderRig.GetComponentsInChildren<TwoBoneIKConstraint>(true);
        TwoBoneIKConstraint constraint = null;
        for (int i = 0; i < constraints.Length; i++)
        {
            if (constraints[i].gameObject.name == constraintObjectName)
            {
                constraint = constraints[i];
                break;
            }
        }

        if (constraint == null)
        {
            return false;
        }

        Transform hint = FindChildByName(riderRig.transform, hintObjectName);
        if (hint == null)
        {
            hint = CreateElbowHint(constraint, hintObjectName, constraintObjectName.StartsWith("Left"));
        }

        if (constraint.data.hint == hint)
        {
            return false;
        }

        TwoBoneIKConstraintData data = constraint.data;
        data.hint = hint;
        data.hintWeight = 1f;
        constraint.data = data;
        return true;
    }

    private Transform CreateElbowHint(TwoBoneIKConstraint constraint, string hintObjectName, bool isLeft)
    {
        GameObject hintObject = new GameObject(hintObjectName);
        Transform hint = hintObject.transform;
        hint.SetParent(riderRig.transform, true);

        Transform elbow = constraint.data.mid;
        Vector3 outward = isLeft ? -transform.right : transform.right;
        hint.position = elbow.position + outward * 0.45f - transform.forward * 0.15f;
        hint.rotation = transform.rotation;
        return hint;
    }

    private static Transform FindChildByName(Transform root, string objectName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == objectName)
            {
                return children[i];
            }
        }

        return null;
    }
}
