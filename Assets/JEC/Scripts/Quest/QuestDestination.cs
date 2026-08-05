using UnityEngine;

public class QuestDestination : MonoBehaviour
{
    // 목적지 NPC마다 서로 다른 ID를 지정
    [SerializeField] private string destinationId;

    [SerializeField] private Transform deliveryPoint;
    [SerializeField] private Transform questUIAnchor;
    [SerializeField] private bool canReceiveDelivery = true;

    public string DestinationId => destinationId;
    public Transform QuestUIAnchor => questUIAnchor;
    public bool CanReceiveDelivery => canReceiveDelivery;

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
}