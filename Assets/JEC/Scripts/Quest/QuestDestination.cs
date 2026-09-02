using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum QuestNpcGender
{
    Auto,
    Male,
    Female
}

public class QuestDestination : Interactable
{
    private const string FemaleIdleClipName = "idle_f_2_190f";
    private const string MaleIdleClipName = "idle_m_2_220f";
    private const string PhoneTalkingClipName = "idle_phoneTalking_180f";
    private const string SelfCheckClipName = "idle_selfcheck_1_300f";

    // City M Animator의 두 상태를 성별 기본 idle과 목적지별 특수 idle 슬롯으로 사용합니다.
    private const string TemplateBaseStateName = MaleIdleClipName;
    private const string TemplateSpecialStateName = SelfCheckClipName;

    private static readonly Dictionary<string, AnimationClip> IdleClips = new();
    private static RuntimeAnimatorController idleTemplateController;

    private Animator destinationAnimator;

    // 목적지 NPC마다 서로 다른 ID를 지정
    [SerializeField] private string destinationId;

    [SerializeField] private string displayName;

    [SerializeField] private Transform deliveryPoint;
    [SerializeField] private Transform questUIAnchor;
    [SerializeField] private bool canReceiveDelivery = true;

    [Header("NPC 표시")]
    [SerializeField] private QuestNpcGender markerGender = QuestNpcGender.Auto;

    [Header("목적지 NPC 대기 애니메이션")]
    [SerializeField, Min(0.1f)] private float minBaseIdleDuration = 8f;
    [SerializeField, Min(0.1f)] private float maxBaseIdleDuration = 15f;
    [SerializeField, Min(0f)] private float idleCrossFadeDuration = 0.35f;

    [Header("상호작용 범위")]
    [SerializeField] private Vector3 interactionColliderCenter = new(0f, 0.9f, 0f);
    [SerializeField] private float interactionColliderHeight = 1.8f;
    [SerializeField] private float interactionColliderRadius = 0.4f;

    [Header("퀘스트 목록")]
    [SerializeField] private PlayerQuestList playerQuestList;

    public string DestinationId => destinationId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName)
        ? gameObject.name
        : displayName;
    public QuestNpcGender MarkerGender => ResolveMarkerGender();
    public Transform QuestUIAnchor => questUIAnchor;
    public bool CanReceiveDelivery => canReceiveDelivery;

    private void Awake()
    {
        EnsureInteractionCollider();

        destinationAnimator = FindDestinationAnimator();

        if (destinationAnimator != null)
        {
            // 기존 CityPeople.Start가 전체 애니메이션 셔플을 시작하기 전에 차단합니다.
            StopCityPeopleAnimationShuffle(destinationAnimator);
        }

        if (playerQuestList == null)
        {
            playerQuestList = FindAnyObjectByType<PlayerQuestList>();
        }
    }

    private void Start()
    {
        ConfigureDestinationIdleAnimation();
    }

    public override string GetPromptMessage(GameObject interactor)
    {
        if (!canReceiveDelivery || playerQuestList == null ||
            !playerQuestList.HasQuestReadyForDeliveryAt(this))
        {
            return string.Empty;
        }

        return $"<color=#FFD36A>F키</color>를 눌러 {DisplayName}에게 물건을 전달하세요";
    }

    public override void Interact(GameObject interactor)
    {
        if (!canReceiveDelivery)
        {
            Debug.Log($"[{name}] 현재 배달을 받을 수 없습니다.", this);

            return;
        }

        if (playerQuestList == null)
        {
            Debug.LogError($"[{name}] PlayerQuestList를 찾을 수 없습니다.", this);

            return;
        }

        int deliveredCount = playerQuestList.TryDeliverQuestsAt(this);

        if (deliveredCount == 0)
        {
            Debug.Log($"[{name}] 이 목적지에 전달할 물건이 없습니다.", this);

            return;
        }

        Debug.Log($"[{name}] 물건 {deliveredCount}개를 전달했습니다.", this);
    }

    public Vector3 GetDeliveryPosition()
    {
        if (deliveryPoint != null)
        {
            return deliveryPoint.position;
        }

        return transform.position;
    }

    public void SetCanReceiveDelivery(bool value)
    {
        canReceiveDelivery = value;
    }

    private QuestNpcGender ResolveMarkerGender()
    {
        if (markerGender != QuestNpcGender.Auto)
        {
            return markerGender;
        }

        string hierarchyName = GetHierarchyName().ToLowerInvariant();

        if (hierarchyName.Contains("female") ||
            hierarchyName.Contains("woman") ||
            hierarchyName.Contains("girl") ||
            hierarchyName.Contains("city f animator"))
        {
            return QuestNpcGender.Female;
        }

        return QuestNpcGender.Male;
    }

    private void ConfigureDestinationIdleAnimation()
    {
        Animator animator = destinationAnimator != null
            ? destinationAnimator
            : FindDestinationAnimator();

        if (animator == null)
        {
            Debug.LogWarning($"[{name}] 목적지 NPC Animator를 찾을 수 없습니다.", this);
            return;
        }

        StopCityPeopleAnimationShuffle(animator);
        CacheIdleAnimationAssets();

        bool isMale = ResolveAnimationGender(animator);
        string genderIdleName = isMale ? MaleIdleClipName : FemaleIdleClipName;

        if (idleTemplateController == null ||
            !IdleClips.TryGetValue(genderIdleName, out AnimationClip genderIdleClip) ||
            !IdleClips.TryGetValue(PhoneTalkingClipName, out AnimationClip phoneTalkingClip) ||
            !IdleClips.TryGetValue(SelfCheckClipName, out AnimationClip selfCheckClip))
        {
            Debug.LogError(
                $"[{name}] 목적지 idle 애니메이션 구성을 찾을 수 없습니다. " +
                $"필요 클립: {genderIdleName}, {PhoneTalkingClipName}, {SelfCheckClipName}",
                this
            );
            return;
        }

        // 목적지 하나마다 phoneTalking/selfcheck 중 정확히 하나만 선택합니다.
        AnimationClip specialIdleClip = Random.value < 0.5f
            ? phoneTalkingClip
            : selfCheckClip;

        AnimatorOverrideController destinationController =
            new AnimatorOverrideController(idleTemplateController);

        destinationController[MaleIdleClipName] = genderIdleClip;
        destinationController[SelfCheckClipName] = specialIdleClip;

        animator.runtimeAnimatorController = destinationController;
        animator.applyRootMotion = false;

        StartCoroutine(PlayAssignedIdleSequence(animator, specialIdleClip));
    }

    private Animator FindDestinationAnimator()
    {
        Animator animator = GetComponent<Animator>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        return animator;
    }

    private bool ResolveAnimationGender(Animator animator)
    {
        CitizenAI citizen = animator.GetComponent<CitizenAI>();

        if (citizen == null)
        {
            citizen = animator.GetComponentInParent<CitizenAI>();
        }

        // CitizenAI가 있는 NPC는 요청한 isMale 값을 그대로 사용합니다.
        return citizen != null
            ? citizen.IsMale
            : ResolveMarkerGender() == QuestNpcGender.Male;
    }

    private static void StopCityPeopleAnimationShuffle(Animator animator)
    {
        CityPeople.CityPeople cityPeople = animator.GetComponent<CityPeople.CityPeople>();

        if (cityPeople == null)
        {
            cityPeople = animator.GetComponentInParent<CityPeople.CityPeople>();
        }

        if (cityPeople == null)
        {
            return;
        }

        cityPeople.StopAllCoroutines();
        cityPeople.enabled = false;
    }

    private static void CacheIdleAnimationAssets()
    {
        if (idleTemplateController != null &&
            IdleClips.ContainsKey(FemaleIdleClipName) &&
            IdleClips.ContainsKey(MaleIdleClipName) &&
            IdleClips.ContainsKey(PhoneTalkingClipName) &&
            IdleClips.ContainsKey(SelfCheckClipName))
        {
            return;
        }

        IdleClips.Clear();
        idleTemplateController = null;

        Animator[] animators = FindObjectsByType<Animator>(
            FindObjectsInactive.Include
        );

        foreach (Animator animator in animators)
        {
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;

            if (controller == null)
            {
                continue;
            }

            bool hasTemplateBase = false;
            bool hasTemplateSpecial = false;

            foreach (AnimationClip clip in controller.animationClips)
            {
                if (clip == null)
                {
                    continue;
                }

                if (clip.name == FemaleIdleClipName ||
                    clip.name == MaleIdleClipName ||
                    clip.name == PhoneTalkingClipName ||
                    clip.name == SelfCheckClipName)
                {
                    IdleClips[clip.name] = clip;
                }

                hasTemplateBase |= clip.name == TemplateBaseStateName;
                hasTemplateSpecial |= clip.name == TemplateSpecialStateName;
            }

            if (idleTemplateController == null && hasTemplateBase && hasTemplateSpecial)
            {
                idleTemplateController = controller;
            }
        }
    }

    private IEnumerator PlayAssignedIdleSequence(
        Animator animator,
        AnimationClip specialIdleClip)
    {
        float minDuration = Mathf.Min(minBaseIdleDuration, maxBaseIdleDuration);
        float maxDuration = Mathf.Max(minBaseIdleDuration, maxBaseIdleDuration);

        while (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.CrossFade(
                TemplateBaseStateName,
                idleCrossFadeDuration,
                0,
                Random.value
            );

            yield return new WaitForSeconds(Random.Range(minDuration, maxDuration));

            animator.CrossFadeInFixedTime(
                TemplateSpecialStateName,
                idleCrossFadeDuration,
                0,
                0f
            );

            yield return new WaitForSeconds(Mathf.Max(0.1f, specialIdleClip.length));
        }
    }

    private string GetHierarchyName()
    {
        string result = gameObject.name;
        Transform current = transform.parent;

        while (current != null)
        {
            result += $" {current.name}";
            current = current.parent;
        }

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            result += $" {child.name}";
        }

        foreach (Animator animator in GetComponentsInChildren<Animator>(true))
        {
            if (animator.avatar != null)
            {
                result += $" {animator.avatar.name}";
            }

            if (animator.runtimeAnimatorController != null)
            {
                result += $" {animator.runtimeAnimatorController.name}";
            }
        }

        return result;
    }

    private void EnsureInteractionCollider()
    {
        if (TryGetComponent(out Collider _))
        {
            return;
        }

        CapsuleCollider interactionCollider = gameObject.AddComponent<CapsuleCollider>();
        interactionCollider.isTrigger = true;
        interactionCollider.center = interactionColliderCenter;
        interactionCollider.radius = Mathf.Max(0.01f, interactionColliderRadius);
        interactionCollider.height = Mathf.Max(
            interactionCollider.radius * 2f,
            interactionColliderHeight
        );
    }
}
