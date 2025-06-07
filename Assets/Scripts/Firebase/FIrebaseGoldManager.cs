using Firebase.Database;
using Firebase.Auth;
using UnityEngine;
using System.Threading.Tasks;
using Firebase;

public class FirebaseGoldManager : MonoBehaviour
{
    public static FirebaseGoldManager Instance { get; private set; }
    public int CurrentGold { get; private set; }
    public bool IsGoldLoaded { get; private set; } = false;
    private DatabaseReference dbRef;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // 원할 경우
    }

    private async void Start()
    {
        if (!FirebaseInit.IsReady) return;

        dbRef = FirebaseInit.DB;
        if (dbRef != null)
        {
            Debug.Log("[Firebase] dbRef가 초기화되었습니다.");
        }
        
        if (FirebaseInit.User != null)
        {
            await LoadGold();
        }
    }

    // 골드 저장
    public async Task SaveGold(int gold)
    {
        string uid = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid)) return;

        await dbRef.Child("Users").Child(uid).Child("gold").SetValueAsync(gold);
        Debug.Log($"[Firebase] 골드 {gold} 저장 완료");
    }

    // 골드 불러오기
    public async Task<int> LoadGold()
    {
        if (dbRef == null)
        {
            Debug.LogError("[Firebase] dbRef가 초기화되지 않았습니다.");
            return 0;
        }

        string uid = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("[Firebase] 로그인된 유저가 없습니다.");
            return 0;
        }

        var snapshot = await dbRef.Child("Users").Child(uid).Child("gold").GetValueAsync();
        if (snapshot.Exists && int.TryParse(snapshot.Value.ToString(), out int gold))
        {
            
            CurrentGold = gold;
            IsGoldLoaded = true;
            Debug.Log($"[Firebase] 불러온 골드: {gold}");
            return gold;
        }

        IsGoldLoaded = true;
        return 0;
    }
}