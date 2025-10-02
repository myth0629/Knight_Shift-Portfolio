using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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