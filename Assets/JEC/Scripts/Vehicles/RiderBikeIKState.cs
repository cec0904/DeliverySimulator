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
    private bool rigRequiresRebuild;

    private void OnEnable()
    {
        ResolveRig();
        rigRequiresRebuild = ConnectArmHint("LeftArmIK", leftElbowHintName) |
                             ConnectArmHint("RightArmIK", rightElbowHintName);
    }

    private void Awake()
    {
        ResolveRig();
        SetMounted(false);
    }

    private void Start()
    {
        if (!rigRequiresRebuild)
        {
            return;
        }

        rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder != null)
        {
            // The hint Transform is part of the Animation Rigging job data, so a
            // graph built before the hint was assigned has to be rebuilt once.
            rigBuilder.Clear();
            rigBuilder.Build();
        }
    }

    public bool IsMounted { get; private set; }

    public void SetMounted(bool mounted)
    {
        IsMounted = mounted;

        ResolveRig();

        if (riderRig != null)
        {
            riderRig.weight = mounted ? 1f : 0f;
        }
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
