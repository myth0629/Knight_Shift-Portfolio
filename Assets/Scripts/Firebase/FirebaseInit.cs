using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;

public class FirebaseInit : MonoBehaviour
{
    public static FirebaseAuth Auth;
    public static FirebaseUser User;
    public static DatabaseReference DB;

    public static bool IsReady = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                var app = FirebaseApp.DefaultInstance;

                if (app.Options.DatabaseUrl == null)
                {
                    app.Options.DatabaseUrl = new System.Uri("https://knightshift-dmu-default-rtdb.firebaseio.com/");
                }

                Auth = FirebaseAuth.DefaultInstance;
                DB = FirebaseDatabase.DefaultInstance.RootReference;
                IsReady = true;

                Debug.Log("Firebase 전체 초기화 완료");
            }
            else
            {
                Debug.LogError("Firebase 초기화 실패: " + task.Result);
            }
        });
    }
}