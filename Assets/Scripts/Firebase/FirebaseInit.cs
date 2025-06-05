using Firebase;
using Firebase.Auth;
using UnityEngine;

public class FirebaseInit : MonoBehaviour
{
    public static FirebaseAuth Auth;
    public static FirebaseUser User;

    private void Awake()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                Auth = FirebaseAuth.DefaultInstance;
                Debug.Log("Firebase 초기화 성공");
            }
            else
            {
                Debug.LogError($"Firebase 초기화 실패: {dependencyStatus}");
            }
        });
    }
}
