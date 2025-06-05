using Firebase.Auth;
using UnityEngine;
using System.Threading.Tasks;

public class FirebaseAuthManager : MonoBehaviour
{
    public async Task<bool> Register(string email, string password)
    {
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