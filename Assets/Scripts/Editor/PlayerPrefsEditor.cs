using UnityEngine;
using UnityEditor;

/// <summary>
/// PlayerPrefs를 쉽게 관리하기 위한 에디터 도구
/// </summary>
public class PlayerPrefsEditor : EditorWindow
{
    [MenuItem("Tools/PlayerPrefs Manager")]
    public static void ShowWindow()
    {
        GetWindow<PlayerPrefsEditor>("PlayerPrefs Manager");
    }

    private void OnGUI()
    {
        GUILayout.Label("PlayerPrefs 관리", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        // 현재 저장된 스테이지 레벨 표시
        int currentStage = PlayerPrefs.GetInt("StageLevel", 1);
        GUILayout.Label($"현재 스테이지: {currentStage}", EditorStyles.helpBox);
        
        GUILayout.Space(10);
        
        // 스테이지 1로 리셋 버튼
        if (GUILayout.Button("스테이지를 1로 리셋", GUILayout.Height(30)))
        {
            PlayerPrefs.SetInt("StageLevel", 1);
            PlayerPrefs.Save();
            Debug.Log("[PlayerPrefsEditor] 스테이지를 1로 리셋했습니다.");
            
            // StageManager가 있으면 즉시 반영
            if (StageManager.Instance != null)
            {
                StageManager.Instance.ResetToStage1();
            }
        }
        
        GUILayout.Space(5);
        
        // 스테이지 2로 설정 버튼 (테스트용)
        if (GUILayout.Button("스테이지를 2로 설정 (테스트)", GUILayout.Height(30)))
        {
            PlayerPrefs.SetInt("StageLevel", 2);
            PlayerPrefs.Save();
            Debug.Log("[PlayerPrefsEditor] 스테이지를 2로 설정했습니다.");
        }
        
        GUILayout.Space(20);
        
        // 모든 PlayerPrefs 삭제 버튼
        GUI.color = Color.red;
        if (GUILayout.Button("모든 PlayerPrefs 삭제 ⚠️", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("경고", 
                "모든 PlayerPrefs 데이터를 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다!", 
                "삭제", "취소"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("[PlayerPrefsEditor] 모든 PlayerPrefs를 삭제했습니다.");
            }
        }
        GUI.color = Color.white;
        
        GUILayout.Space(20);
        
        // 새로고침 버튼
        if (GUILayout.Button("새로고침"))
        {
            Repaint();
        }
    }
}
