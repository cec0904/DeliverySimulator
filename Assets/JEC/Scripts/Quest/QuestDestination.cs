using UnityEngine;

public enum QuestNpcGender
{
    Auto,
    Male,
    Female
}

public class QuestDestination : Interactable
{
    // 목적지 NPC마다 서로 다른 ID를 지정
    [SerializeField] private string destinationId;

    [SerializeField] private string displayName;

    [SerializeField] private Transform deliveryPoint;
    [SerializeField] private Transform questUIAnchor;
    [SerializeField] private bool canReceiveDelivery = true;

    [Header("NPC 표시")]
    [SerializeField] private QuestNpcGender markerGender = QuestNpcGender.Auto;

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

        if (playerQuestList == null)
        {
            playerQuestList = FindAnyObjectByType<PlayerQuestList>();
        }
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
