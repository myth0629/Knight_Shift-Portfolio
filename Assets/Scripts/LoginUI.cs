using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    public FirebaseAuthManager authManager;
    
    public string nextSceneName = "Start"; // 로그인 성공 시 넘어갈 씬

    public async void OnClickLogin()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        bool success = await authManager.Login(email, password);
        if (success)
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    public void OnClickRegister()
    {
        string email = emailInput.text;
        string password = passwordInput.text;
        _ = authManager.Register(email, password);
    }
}