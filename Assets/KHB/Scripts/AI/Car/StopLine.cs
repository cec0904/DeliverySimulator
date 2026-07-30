using UnityEngine;



[RequireComponent(typeof(BoxCollider))]
public class StopLine : MonoBehaviour
{
    [SerializeField] private TrafficLight targetTrafficLight;
    [SerializeField] private bool isSideA = true;



    public TrafficLight.LightColor GetCurrentSignal()
    {
        
        if (targetTrafficLight == null) return TrafficLight.LightColor.Green;
        return isSideA ? targetTrafficLight.CurrentSideAColor : targetTrafficLight.CurrentSideBColor;
    }
}