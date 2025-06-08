using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_Text loginMessageText;
    public TMP_Text registerMessageText;

    public FirebaseAuthManager authManager;
    
    public string nextSceneName = "Start"; // 로그인 성공 시 넘어갈 씬

    public async void OnClickLogin()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        bool success = await authManager.Login(email, password);
        if (success)
        {
            loginMessageText.text = "로그인 성공!";
            loginMessageText.color = Color.green;
            SceneManager.LoadScene(nextSceneName);
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            loginMessageText.text = "로그인 실패. 이메일과 비밀번호를 확인하세요.";
        }
    }

    public async void OnClickRegister()
    {
        string email = emailInput.text;
        string password = passwordInput.text;
        _ = authManager.Register(email, password);
        
        bool success = await authManager.Register(email, password);
        if (success)
        {
            registerMessageText.text = "회원가입 성공!";
        }
        else
        {
            registerMessageText.text = "회원가입 실패.";
        }
    }
}