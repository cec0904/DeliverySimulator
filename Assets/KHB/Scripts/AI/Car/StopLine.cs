using UnityEngine;



[RequireComponent(typeof(BoxCollider))]
public class StopLine : MonoBehaviour
{
    [SerializeField] private TrafficLight targetTrafficLight;
    [SerializeField] private bool isSideA = true;

    private void Awake()
    {
        // 게임 실행 시(Play 모드) 눈에 보이는 빨간색 메시를 자동으로 숨깁니다.
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }

    public TrafficLight.LightColor GetCurrentSignal()
    {
        
        if (targetTrafficLight == null) return TrafficLight.LightColor.Green;
        return isSideA ? targetTrafficLight.CurrentSideAColor : targetTrafficLight.CurrentSideBColor;
    }
}