using Firebase.Auth;
using UnityEngine;
using System.Threading.Tasks;

public class FirebaseAuthManager : MonoBehaviour
{
    
    public async Task<bool> Register(string email, string password, string passwordConfirm)
    {
        // 입력값 유효성 검사
        if (string.IsNullOrEmpty(email))
        {
            Debug.LogError("회원가입 실패: 이메일을 입력해주세요.");
            return false;
        }

        if (string.IsNullOrEmpty(password))
        {
            Debug.LogError("회원가입 실패: 비밀번호를 입력해주세요.");
            return false;
        }

        if (string.IsNullOrEmpty(passwordConfirm))
        {
            Debug.LogError("회원가입 실패: 비밀번호 확인을 입력해주세요.");
            return false;
        }

        // 비밀번호 일치 검사
        if (password != passwordConfirm)
        {
            Debug.LogError("회원가입 실패: 비밀번호가 일치하지 않습니다.");
            return false;
        }

        // 비밀번호 길이 검사
        if (password.Length < 6)
        {
            Debug.LogError("회원가입 실패: 비밀번호는 최소 6자 이상이어야 합니다.");
            return false;
        }

        try
        {
            var userCredential = await FirebaseInit.Auth.CreateUserWithEmailAndPasswordAsync(email, password);
            FirebaseInit.User = userCredential.User;
            Debug.Log($"회원가입 성공: {FirebaseInit.User.Email}");
            return true;
        }
        catch (System.Exception e)
        {
             Debug.LogError($"회원가입 실패: {e.Message}");
            return false;
        }
    }

    public async Task<bool> Login(string email, string password)
    {
        try
        {
            var userCredential = await FirebaseInit.Auth.SignInWithEmailAndPasswordAsync(email, password);
            FirebaseInit.User = userCredential.User;
            Debug.Log($"로그인 성공: {FirebaseInit.User.Email}");
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"로그인 실패: {e.Message}");
            return false;
        }
    }

    public void Logout()
    {
        FirebaseInit.Auth.SignOut();
        Debug.Log("로그아웃 완료");
    }
}