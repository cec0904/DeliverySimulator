using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        //Debug.Log("버튼이 클릭되었습니다! 씬 로드를 시도합니다.");
        SceneManager.LoadScene("MainScene_v03");
    }

    // [게임 종료] 버튼 클릭 시 실행할 함수
    public void QuitGame()
    {
        //Debug.Log("종료 버튼이 클릭되었습니다! ");

#if UNITY_EDITOR
        // 유니티 에디터 상에서 테스트할 때
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // 빌드된 게임(실제 실행 파일)에서 종료할 때
            Application.Quit();
#endif
    }
}
