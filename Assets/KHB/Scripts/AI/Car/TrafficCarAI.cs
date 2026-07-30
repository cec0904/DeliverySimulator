using UnityEngine;

public class TrafficCarAI : MonoBehaviour
{
    public enum CarState { Move, Stop }

    [Header("차량 설정")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float stopDistance = 3f; // 정지선/앞차와의 안전거리
    [SerializeField] private float sensorDistance = 15f; // 감지 레이캐스트 거리

    [Header("센서 레이어 설정")]
    [SerializeField] private LayerMask obstacleLayer; // 앞차, 정지선 등이 포함된 레이어

    private CarState currentState = CarState.Move;

    private void Update()
    {
        // 1. 전방 감지 수행
        CheckForwardSensor();

        // 2. 상태에 따른 주행 제어
        HandleMovement();
    }

    private void CheckForwardSensor()
    {
        Debug.Log(" raycast 감지");

        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        // 차량 전방으로 Raycast 발사
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, sensorDistance, obstacleLayer))
        {
            Debug.DrawLine(rayStart, hit.point, Color.red, 0.1f);
            // A. 전방에 '앞차'가 감지된 경우
            if (hit.collider.CompareTag("Car"))
            {
                Debug.Log("차 감지");
                if (hit.distance <= stopDistance)
                {
                    Debug.Log($" 남은 거리: {hit.distance:F2}m (목표: {stopDistance}m 이하)");

                    currentState = CarState.Stop;
                    return;
                }
            }

            // B. 전방에 '정지선'이 감지된 경우
            if (hit.collider.CompareTag("StopLine"))
            {
                // 정지선에 연결된 TrafficLight 스크립트 가져오기
                StopLine stopLine = hit.collider.GetComponent<StopLine>();
                if (stopLine != null)
                {
                    TrafficLight.LightColor currentSignal = stopLine.GetCurrentSignal();

                    // 노란불이거나 빨간불이고, 정지선 근처에 도달했다면 정지
                    if (currentSignal == TrafficLight.LightColor.Red || currentSignal == TrafficLight.LightColor.Yellow)
                    {
                        Debug.Log($"[정지선 감지] 신호: {currentSignal} | 남은 거리: {hit.distance:F2}m (목표: {stopDistance}m 이하)");

                        if (hit.distance <= stopDistance)
                        {
                            Debug.Log("정지선에 멈춤");

                            currentState = CarState.Stop;
                            return;
                        }
                    }
                }
            }
        }
        // 장애물이나 제지 신호가 없으면 주행 상태 유지
        currentState = CarState.Move;
    }

    private void HandleMovement()
    {
        if (currentState == CarState.Move)
        {
            // 전진 (실제 프로젝트에서는 NavMeshAgent.isStopped = false 등을 사용)
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
        else if (currentState == CarState.Stop)
        {
            // 정지 (NavMeshAgent.isStopped = true)
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 에디터 뷰에서 센서 레이캐스트 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * sensorDistance);
    }
}