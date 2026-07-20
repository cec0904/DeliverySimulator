using Invector.vCharacterController;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
public enum ParkourAction
{
    None,
    Climb,
    VaultToTop,
    VaultOver,
    VaultSlide
}

public enum VaultSlideType
{
    Top,
    Down
}

public class PlayerParkourController : MonoBehaviour
{

    [SerializeField] private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // 1. 파쿠르가 가능한 장애물이 앞에 있는지 확인



    // 2. 높이 확인 
    // 예 : 장애물의 높이가 1.5f 이상이다 ? climb
    // 장애물의 높이가 1.5f 미만이다 ? vault
    public ParkourAction DivisionParkour(float obstacleHeight, float obstacleLength, float inputMagnitude)
    {
        if (obstacleHeight >= 1.5f)
        {
            return ParkourAction.Climb;
        }
        if (obstacleHeight >= 0.5f)
        {
            return DivisionVault(obstacleLength, inputMagnitude);
        }

        return ParkourAction.None;

    }




    // 3. 현재 플레이어의 속도에 따라서 VaultToTop 을 할건지 VaultOver, VaultSlide 할건지
    // 보통 걷는 속도일때만 VaultToTop을 할 것이다.
    public ParkourAction DivisionVault(float obstacleLength, float inputMagnitude)
    {

        if (inputMagnitude <= 0.5f)
        {
            return ParkourAction.VaultToTop;
        }

        if (obstacleLength <= 1.2f)
        {
            return ParkourAction.VaultOver;
        }
        return ParkourAction.VaultSlide;
    }
    // 필요한 애니메이션 일단 4개
    // 애니메이션 뿐 아니라 위치보정 해주기
    // StartClimb -> 장애물 위에 (보통 벽이니까)
    // StartVaultToTop -> 장애물 위에
    // StartVaultOver -> 장애물 건너 바닥
    // StartVaultSlide -> 장애물 슬라이드 후 다시 일어서기. 이거는 장애물위를 슬라이드 하거나 아래쪽으로 슬라이드 하거나 둘 다 활용 가능

    public void StartParkour(ParkourAction action, VaultSlideType slideType = VaultSlideType.Top)
    {
        switch (action)
        {
            case ParkourAction.Climb:
                StartClimb();
                break;
            case ParkourAction.VaultToTop:
                StartVaultToTop();
                break;
            case ParkourAction.VaultOver:
                StartVaultOver();
                break;
            case ParkourAction.VaultSlide:
                StartSlide(slideType);
                break;
        }

    }



    public void StartClimb()
    {
        animator.CrossFadeInFixedTime("StartClimb", 0.1f);
    }

    public void StartVaultToTop()
    {
        animator.CrossFadeInFixedTime("StartVaultToTop", 0.1f);
    }

    public void StartVaultOver()
    {
        animator.CrossFadeInFixedTime("StartVaultOver", 0.1f);
    }

    public void StartSlide(VaultSlideType slideType)
    {

        animator.CrossFadeInFixedTime("StartSlide", 0.1f);


        if (slideType == VaultSlideType.Top)
        {
            Debug.Log("장애물 위쪽 슬라이드");
        }

        else
        {
            Debug.Log("장애물 아래쪽 슬라이드");
        }
    }



}