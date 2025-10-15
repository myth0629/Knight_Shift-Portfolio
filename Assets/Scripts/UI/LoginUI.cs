using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField LoginEmailInput;
    public TMP_InputField LoginPasswordInput;
    public TMP_InputField RegisterEmailInput;
    public TMP_InputField RegisterPasswordInput;
    public TMP_InputField RegisterPasswordConfirmInput;
    public TMP_Text loginMessageText;
    public TMP_Text registerMessageText;

    public FirebaseAuthManager authManager;
    
    public string nextSceneName = "Start"; // 로그인 성공 시 넘어갈 씬

    public async void OnClickLogin()
    {
        string email = LoginEmailInput.text;
        string password = LoginPasswordInput.text;

        bool success = await authManager.Login(email, password);
        if (success)
        {
            loginMessageText.text = "로그인 성공!";
            loginMessageText.color = Color.green;
            
            // 페이드 효과와 함께 씬 전환 (코루틴 시작)
            StartCoroutine(LoadSceneWithFade(nextSceneName));
        }
        else
        {
            loginMessageText.text = "로그인 실패. 이메일과 비밀번호를 확인하세요.";
            loginMessageText.color = Color.red;
        }
    }

    /// <summary>
    /// 페이드 효과와 함께 씬을 로드하는 코루틴
    /// </summary>
    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        // SceneFadeManager가 있으면 페이드 효과 사용
        if (SceneFadeManager.Instance != null)
        {
            Debug.Log($"[LoginUI] 페이드 아웃 시작 - {sceneName} 씬으로 이동");
            
            // 페이드 아웃 실행
            yield return StartCoroutine(SceneFadeManager.Instance.FadeOut());
            
            // 커서 상태 변경
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // 게임 타이머 시작 (로그인 후 게임 시작)
            if (GameTimer.Instance != null)
            {
                GameTimer.Instance.ResetTimer(); // 기존 타이머 리셋
                GameTimer.Instance.StartTimer(); // 새 타이머 시작
                Debug.Log("[LoginUI] 게임 타이머 시작");
            }
            else
            {
                Debug.LogWarning("[LoginUI] GameTimer를 찾을 수 없습니다!");
            }
            
            // 비동기 씬 로딩으로 변경 (로딩 중 멈춤 방지)
            Debug.Log($"[LoginUI] {sceneName} 씬 로딩 시작...");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            
            // 씬 로딩이 완료될 때까지 대기
            while (!asyncLoad.isDone)
            {
                Debug.Log($"[LoginUI] 로딩 진행률: {asyncLoad.progress * 100}%");
                yield return null;
            }
            
            Debug.Log($"[LoginUI] {sceneName} 씬 로딩 완료!");
            
            // 페이드 인은 SceneFadeManager의 OnSceneLoaded에서 자동으로 처리됨
        }
        else
        {
            // SceneFadeManager가 없으면 바로 전환
            Debug.LogWarning("[LoginUI] SceneFadeManager를 찾을 수 없습니다. 즉시 씬 전환합니다.");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // 비동기 로딩 사용
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
    }

    public async void OnClickRegister()
    {
        string email = RegisterEmailInput.text;
        string password = RegisterPasswordInput.text;
        string passwordConfirm = RegisterPasswordConfirmInput.text;
        
        bool success = await authManager.Register(email, password, passwordConfirm);
        if (success)
        {
            registerMessageText.text = "회원가입 성공!";
            registerMessageText.color = Color.green;
        }
        else
        {
            registerMessageText.text = "회원가입 실패. 입력 내용을 확인하세요.";
            registerMessageText.color = Color.red;
        }
    }
}