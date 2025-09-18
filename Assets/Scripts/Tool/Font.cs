using UnityEngine;
using UnityEditor;
using TMPro;

public class TMPFontChanger : EditorWindow
{
    private TMP_FontAsset oldFont;
    private TMP_FontAsset newFont;

    [MenuItem("Tools/Text/TMP Font Changer")]
    public static void ShowWindow()
    {
        GetWindow<TMPFontChanger>("TMP Font Changer");
    }

    private void OnGUI()
    {
        GUILayout.Label("모든 TextMeshPro 폰트 변경", EditorStyles.boldLabel);

        oldFont = (TMP_FontAsset)EditorGUILayout.ObjectField("기존 폰트 애셋", oldFont, typeof(TMP_FontAsset), false);
        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("새 폰트 애셋", newFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("프로젝트 전체 폰트 변경"))
        {
            if (oldFont == null || newFont == null)
            {
                EditorUtility.DisplayDialog("경고", "기존 폰트와 새 폰트를 모두 지정해야 합니다.", "확인");
                return;
            }

            if (EditorUtility.DisplayDialog("프로젝트 전체 폰트 변경",
                $"정말로 모든 '{oldFont.name}' 폰트를 '{newFont.name}' 폰트로 변경하시겠습니까?\n이 작업은 되돌릴 수 없으니, 프로젝트를 백업했는지 확인하세요.",
                "변경 실행", "취소"))
            {
                ChangeAllFonts();
            }
        }
    }

    private void ChangeAllFonts()
    {
        // 모든 프리팹에서 폰트 변경
        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
        int changedCount = 0;

        for (int i = 0; i < allPrefabs.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(allPrefabs[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            // 프리팹과 그 자식 오브젝트들의 모든 TMP 컴포넌트를 가져옴
            TextMeshProUGUI[] texts = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);

            foreach (var text in texts)
            {
                if (text.font == oldFont)
                {
                    text.font = newFont;
                    EditorUtility.SetDirty(prefab); // 변경사항 저장
                    changedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets(); // 변경된 모든 애셋 저장
        Debug.Log($"총 {changedCount}개의 프리팹 내 텍스트 폰트를 변경했습니다.");
        EditorUtility.DisplayDialog("완료", $"총 {changedCount}개의 프리팹 내 텍스트 폰트를 변경했습니다.", "확인");
    }
}