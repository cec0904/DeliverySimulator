using UnityEngine;

public class WaypointNode : MonoBehaviour
{
    public WaypointNode nextNode; // 다음으로 갈 노드
    public WaypointNode branchNode; // 교차로에서 갈라질 노드 (선택)

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.5f);
        if (nextNode != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, nextNode.transform.position);
        }
    }
}