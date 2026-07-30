using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    public enum LightColor { Green, Yellow, Red }

    [Header("렌더러 설정")]
    [SerializeField] private MeshRenderer lightRenderer; // 신호등 불빛 메쉬 렌더러

    [SerializeField] private int sideA_RedIndex = 1;
    [SerializeField] private int sideA_YellowIndex = 2;
    [SerializeField] private int sideA_GreenIndex = 3;

    [SerializeField] private int sideB_RedIndex = 4;
    [SerializeField] private int sideB_YellowIndex = 5;
    [SerializeField] private int sideB_GreenIndex = 6;

    [Header("불빛 켜진 머티리얼 (Lighting)")]
    [SerializeField] private Material greenOnMat;   // Green lighting
    [SerializeField] private Material yellowOnMat;  // Yellow lighting
    [SerializeField] private Material redOnMat;     // Red lighting

    [Header("불빛 꺼진 머티리얼 (Off)")]
    [SerializeField] private Material greenOffMat;  // Green
    [SerializeField] private Material yellowOffMat; // Yellow
    [SerializeField] private Material redOffMat;    // Red

    public LightColor CurrentSideAColor { get; private set; } = LightColor.Red;
    public LightColor CurrentSideBColor { get; private set; } = LightColor.Red;

    private void Reset()
    {
        lightRenderer = GetComponent<MeshRenderer>();
    }

    public void SetDualColors(LightColor sideAColor, LightColor sideBColor)
    {
        CurrentSideAColor = sideAColor;
        CurrentSideBColor = sideBColor;

        if (lightRenderer == null) return;

        Material[] mats = lightRenderer.sharedMaterials;

        if (mats.Length >= 7)
        {
            // 1. 모든 슬롯을 우선 꺼짐 머티리얼로 초기화
            mats[sideA_GreenIndex] = greenOffMat;
            mats[sideA_YellowIndex] = yellowOffMat;
            mats[sideA_RedIndex] = redOffMat;

            mats[sideB_GreenIndex] = greenOffMat;
            mats[sideB_YellowIndex] = yellowOffMat;
            mats[sideB_RedIndex] = redOffMat;

            // 2. 면 A (Side A) 색상 켜기
            switch (sideAColor)
            {
                case LightColor.Green: mats[sideA_GreenIndex] = greenOnMat; break;
                case LightColor.Yellow: mats[sideA_YellowIndex] = yellowOnMat; break;
                case LightColor.Red: mats[sideA_RedIndex] = redOnMat; break;
            }

            // 3. 면 B (Side B) 색상 켜기
            switch (sideBColor)
            {
                case LightColor.Green: mats[sideB_GreenIndex] = greenOnMat; break;
                case LightColor.Yellow: mats[sideB_YellowIndex] = yellowOnMat; break;
                case LightColor.Red: mats[sideB_RedIndex] = redOnMat; break;
            }

            // 변경된 머티리얼 적용
            lightRenderer.materials = mats;
        }
    }
}