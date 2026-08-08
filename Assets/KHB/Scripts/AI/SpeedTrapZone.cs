using rayzngames;
using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor; // EditorGUIUtility 및 Handles 사용
#endif

public class SpeedTrapZone : MonoBehaviour
{
    [Header("속도 제한 설정")]
    [Tooltip("제한 속도 (km/h)")]
    public float speedLimitKmh = 50f;

    [Header("경찰 지원 호출 범위")]
    [Tooltip("과속 감지 시 신호를 받아 출동할 주변 경찰차 수색 반경")]
    public float policeSearchRadius = 100f;

    [Tooltip("재감지 쿨타임 (초)")]
    public float cooldownTime = 5f;
    private bool isCooldown = false;


    private void OnTriggerStay(Collider other)
    {

        if (isCooldown) return;

        // 1. 감지된 오브젝트에서 플레이어(자전거/차량) 가져오기
        BicycleVehicle bicycle = other.GetComponentInParent<BicycleVehicle>();

        if (bicycle != null)
        {
            // km/h 속도 계산
            float speedKmh = bicycle.currentSpeedKmh;

            // 2. 제한 속도 초과 판정
            if (speedKmh > speedLimitKmh)
            {
                Debug.Log($"[단속 감지] 속도 위반! ({speedKmh:F1} km/h)");

                // 3. 근처 대기 중인 경찰차(Car)를 찾아서 상태 변경 및 출동 명령
                bool dispatched = DispatchNearestPolice(bicycle);

                if (dispatched)
                {
                    StartCoroutine(CooldownRoutine());
                }
            }
        }
    }
    /// <summary>
    /// //////////////
    /// </summary>
    ///////////////
    private bool DispatchNearestPolice(BicycleVehicle targetPlayer)
    {
        // 주변 콜라이더 탐색
        Collider[] hits = Physics.OverlapSphere(transform.position, policeSearchRadius);
        PoliceCarAI nearestPolice = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            PoliceCarAI police = hit.GetComponentInParent<PoliceCarAI>();

            // 핵심: 경찰차가 존재하고, 'Idle(대기)' 상태인 경찰차만 후보로 선정!
            if (police != null && police.currentState == PoliceCarAI.PoliceState.Idle)
            {
                float dist = Vector3.Distance(transform.position, police.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestPolice = police;
                }
            }
        }

        // 가장 가까운 대기 중 경찰차가 있으면 출동시킴
        if (nearestPolice != null)
        {
            // 이 함수 내부에서 nearestPolice.currentState = PoliceState.Intercept 가 수행됨
            nearestPolice.StartIntercept(targetPlayer);
            return true;
        }

        Debug.Log("주변에 출동 가능한 대기 상태(Idle)의 경찰차가 없습니다.");
        return false;
    }

    private IEnumerator CooldownRoutine()
    {
        isCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        isCooldown = false;
    }


    #region 시각화 (Gizmos)

    private void OnDrawGizmos()
    {
        // Collider 컴포넌트 가져오기
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        // 1. 감지 구역 칼라 설정 (기본: 반투명 노란색)
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.3f);

        // 2. Collider 종류(Box/Sphere)에 따른 영역 채우기 및 외곽선 그리기
        if (col is BoxCollider box)
        {
            // 오브젝트의 Transform 위치/회전/스케일 반영
            Gizmos.matrix = transform.localToWorldMatrix;

            // 영역 박스
            Gizmos.DrawCube(box.center, box.size);

            // 외곽선 (진한 노란색)
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.9f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Vector3 globalCenter = transform.TransformPoint(sphere.center);
            float maxScale = Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
            float globalRadius = sphere.radius * maxScale;

            Gizmos.DrawSphere(globalCenter, globalRadius);

            Gizmos.color = new Color(1f, 0.8f, 0f, 0.9f);
            Gizmos.DrawWireSphere(globalCenter, globalRadius);
        }

        // Gizmos 행렬 원복
        Gizmos.matrix = Matrix4x4.identity;

#if UNITY_EDITOR
        // 3. 씬 뷰에 제한 속도 텍스트(GUI) 표 시
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;
        style.fontSize = 13;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        Vector3 labelPosition = transform.position + Vector3.up * 1.5f;
        Handles.Label(labelPosition, $"[Speed Limit: {speedLimitKmh} km/h]", style);
#endif
    }

    private void OnDrawGizmosSelected()
    {
        // 오브젝트를 클릭(선택)했을 때: 지원 경찰 수색 반경(빨간색 원) 표시
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, policeSearchRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, policeSearchRadius);
    }

    #endregion

}
