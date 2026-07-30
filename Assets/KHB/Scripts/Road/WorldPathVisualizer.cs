using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class WorldPathVisualizer : MonoBehaviour
{
    [Header("참조 설정")]
    public Transform playerTransform; // 플레이어 위치
    private LineRenderer lineRenderer;
    private NavMeshPath navMeshPath;

    private Vector3 currentTargetPosition;
    private bool hasTarget = false;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        navMeshPath = new NavMeshPath();
    }

    void Update()
    {
        if (!hasTarget || playerTransform == null) return;

        // 1. 플레이어 위치부터 목표 지점까지 NavMesh 경로 계산
        if (NavMesh.CalculatePath(playerTransform.position, currentTargetPosition, NavMesh.AllAreas, navMeshPath))
        {
            if (navMeshPath.status == NavMeshPathStatus.PathComplete || navMeshPath.status == NavMeshPathStatus.PathPartial)
            {
                lineRenderer.enabled = true;
                lineRenderer.positionCount = navMeshPath.corners.Length;

                // LineRenderer가 지면에 묻히지 않도록 Y축 살짝 올림 (+0.2f)
                for (int i = 0; i < navMeshPath.corners.Length; i++)
                {
                    Vector3 point = navMeshPath.corners[i];
                    point.y += 0.2f;
                    lineRenderer.SetPosition(i, point);
                }
            }
        }
        else
        {
            ClearPath();
        }
    }

    // 목표 지점 설정
    public void SetDestination(Vector3 targetWorldPos)
    {
        currentTargetPosition = targetWorldPos;
        hasTarget = true;
    }

    // 경로 지우기
    public void ClearPath()
    {
        hasTarget = false;
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
    }

    // 계산된 NavMesh 경로 지점들 반환 (UI 라인용)
    public Vector3[] GetPathCorners()
    {
        if (hasTarget && navMeshPath != null)
        {
            return navMeshPath.corners;
        }
        return null;
    }
}