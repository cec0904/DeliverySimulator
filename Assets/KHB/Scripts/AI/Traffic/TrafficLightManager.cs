using UnityEngine;
using System.Collections;

public class TrafficLightManager : MonoBehaviour
{
    [Header("신호등 연결 (동북 / 남서 등 2면 신호등들)")]
    [SerializeField] private TrafficLight[] trafficLights;

    [Header("신호 유지 시간 (초)")]
    [SerializeField] private float greenDuration = 10f;
    [SerializeField] private float yellowDuration = 3f;

    // ★ 추가: 교차로 내부에 진입한 차들이 빠져나갈 수 있는 여유 시간
    [SerializeField] private float allRedDuration = 2f;

    private void Start()
    {
        StartCoroutine(TrafficSignalLoop());
    }

    private IEnumerator TrafficSignalLoop()
    {
        while (true)
        {
            // 1단계: [Side A : 초록불] / [Side B : 빨간불]
            SetAllLights(TrafficLight.LightColor.Green, TrafficLight.LightColor.Red);
            yield return new WaitForSeconds(greenDuration);

            // 2단계: [Side A : 노란불] / [Side B : 빨간불] 
            // (수정됨: B는 계속 빨간불이어야 함)
            SetAllLights(TrafficLight.LightColor.Yellow, TrafficLight.LightColor.Red);
            yield return new WaitForSeconds(yellowDuration);

            // ★ 3단계 (추가): [Side A : 빨간불] / [Side B : 빨간불]
            // 양쪽 모두 정지. 늦게 진입한 A 차량이 교차로를 빠져나갈 시간을 줌
            SetAllLights(TrafficLight.LightColor.Red, TrafficLight.LightColor.Red);
            yield return new WaitForSeconds(allRedDuration);

            // 4단계: [Side A : 빨간불] / [Side B : 초록불]
            SetAllLights(TrafficLight.LightColor.Red, TrafficLight.LightColor.Green);
            yield return new WaitForSeconds(greenDuration);

            // 5단계: [Side A : 빨간불] / [Side B : 노란불] 
            // (수정됨: A는 계속 빨간불이어야 함)
            SetAllLights(TrafficLight.LightColor.Red, TrafficLight.LightColor.Yellow);
            yield return new WaitForSeconds(yellowDuration);

            // ★ 6단계 (추가): [Side A : 빨간불] / [Side B : 빨간불]
            // 양쪽 모두 정지. 늦게 진입한 B 차량이 빠져나갈 시간을 줌
            SetAllLights(TrafficLight.LightColor.Red, TrafficLight.LightColor.Red);
            yield return new WaitForSeconds(allRedDuration);
        }
    }

    private void SetAllLights(TrafficLight.LightColor sideA, TrafficLight.LightColor sideB)
    {
        if (trafficLights == null) return;

        for (int i = 0; i < trafficLights.Length; i++)
        {
            if (trafficLights[i] != null)
            {
                trafficLights[i].SetDualColors(sideA, sideB);
            }
        }
    }
}