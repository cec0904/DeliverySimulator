using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string promptMessage = "상호작용하려면 F를 누르세요.";

    // 플레이어가 F 키를 눌렀을 때 실행될 진짜 행동
    public virtual void Interact()
    {
        // 이 부분은 기본 동작입니다. 상속받아서 다르게 꾸밀 수도 있습니다.
        Debug.Log(gameObject.name + "와(과) 상호작용했습니다!");

    }
}
