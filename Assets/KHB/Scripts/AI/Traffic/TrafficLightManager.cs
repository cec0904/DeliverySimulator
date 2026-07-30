using UnityEngine;
using System.Collections;

public class TrafficLightManager : MonoBehaviour
{

    [Header("신호등 연결 (동북 / 남서 등 2면 신호등들)")]
    [SerializeField] private TrafficLight[] trafficLights;

    [Header("신호 유지 시간 (초)")]
    [SerializeField] private float greenDuration = 3f;  // 초록불 유지 시간
    [SerializeField] private float yellowDuration = 3f;  // 노란불 유지 시간

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
            SetAllLights(TrafficLight.LightColor.Yellow, TrafficLight.LightColor.Yellow);
            yield return new WaitForSeconds(yellowDuration);

            // 3단계: [Side A : 빨간불] / [Side B : 초록불]
            SetAllLights(TrafficLight.LightColor.Red, TrafficLight.LightColor.Green);
            yield return new WaitForSeconds(greenDuration);

            // 4단계: [Side A : 빨간불] / [Side B : 노란불]
            SetAllLights(TrafficLight.LightColor.Yellow, TrafficLight.LightColor.Yellow);
            yield return new WaitForSeconds(yellowDuration);
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
