using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string promptMessage = "상호작용하려면 F를 누르세요.";

    public virtual void Interact()
    {
        Debug.Log(gameObject.name + "과(와) 상호작용했습니다.");
    }

    public virtual void Interact(GameObject interactor)
    {
        Interact();
    }
}
