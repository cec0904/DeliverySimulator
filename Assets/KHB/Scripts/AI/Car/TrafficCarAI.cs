using UnityEngine;

public class TrafficCarAI : MonoBehaviour
{
    public enum CarState { Move, Stop }

    [Header("차량 설정")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float stopDistance = 3f; // 정지선/앞차와의 안전거리
    private float carStopDistance; 

    [SerializeField] private float sensorDistance = 15f; // 감지 레이캐스트 거리

    [Header("바퀴 및 시각 효과")]
    [SerializeField] private Transform[] wheels;
    [SerializeField] private float wheelRotateSpeed = 200f; 
    [SerializeField] private Vector3 wheelAxis = Vector3.right; 

    [Header("센서 레이어 설정")]
    [SerializeField] private LayerMask obstacleLayer; // 앞차, 정지선 등이 포함된 레이어

    [Header("정지선 통과 예외 설정")]
    [SerializeField] private float stopLineIgnoreDuration = 5.0f; // 정지선 통과 후 무시할 시간 (초)
    private float stopLineIgnoreTimer = 0f;

    private CarState currentState = CarState.Move;

    private void OnEnable()
    {
        carStopDistance = stopDistance * 2f;
        stopLineIgnoreTimer = 0f; // [추가] 풀에서 차를 꺼낼 때 타이머 리셋
        int trafficLayer = LayerMask.NameToLayer("traffic");
        if (trafficLayer != -1)
        {
            obstacleLayer = 1 << trafficLayer;
        }
        else
        {
            Debug.LogWarning("[TrafficCarAI] 'Traffic' 레이어가 프로젝트 Settings에 존재하지 않습니다!");
        }
    }

    private void Update()
    {

        if (stopLineIgnoreTimer > 0f)
        {
            stopLineIgnoreTimer -= Time.deltaTime;
        }

        CheckForwardSensor();

        HandleMovement();
    }

    private void CheckForwardSensor()
    {

        RaycastHit hit;
        // 차량 전방으로 Raycast 발사
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, sensorDistance, obstacleLayer))
        {
            // A. 전방에 '앞차'가 감지된 경우
            if (hit.collider.CompareTag("Car"))
            {
                if (hit.distance <= carStopDistance)
                {

                    currentState = CarState.Stop;
                    return;
                }
            }

            // B. 전방에 '정지선'이 감지된 경우
            if (hit.collider.CompareTag("StopLine"))
            {
                if (stopLineIgnoreTimer > 0f)
                {
                    currentState = CarState.Move;
                    return;
                }

                StopLine stopLine = hit.collider.GetComponent<StopLine>();
                if (stopLine != null)
                {
                    TrafficLight.LightColor currentSignal = stopLine.GetCurrentSignal();

                    // 노란불이거나 빨간불이고, 정지선 근처에 도달했다면 정지
                    if (currentSignal == TrafficLight.LightColor.Red || currentSignal == TrafficLight.LightColor.Yellow)
                    {
                        
                        if (hit.distance <= stopDistance)
                        {

                            currentState = CarState.Stop;
                            return;
                        }
                    }
                    else if (currentSignal == TrafficLight.LightColor.Green)
                    {
                        // 초록불을 받고 정지선 근처에 도달하면 타이머 작동
                        if (hit.distance <= stopDistance)
                        {
                            stopLineIgnoreTimer = stopLineIgnoreDuration;
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
            RotateWheels();

        }
        else if (currentState == CarState.Stop)
        {
            // 정지 (NavMeshAgent.isStopped = true)
        }
    }
    private void RotateWheels()
    {
        if (wheels == null || wheels.Length == 0) return;

        // 속도 기반 회전량 계산
        float rotationAmount = moveSpeed * wheelRotateSpeed * Time.deltaTime;

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] != null)
            {
                wheels[i].Rotate(wheelAxis * rotationAmount, Space.Self);
            }
        }
    }

}