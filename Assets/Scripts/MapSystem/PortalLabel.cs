using UnityEngine;
using TMPro;

namespace MapSystem
{
    /// <summary>
    /// 포탈 프리팹에 텍스트 라벨을 표시하는 컴포넌트
    /// 포탈 생성 시 포탈 이름을 텍스트로 표시합니다.
    /// 
    /// 사용법:
    /// 1. 포탈 프리팹에 이 스크립트를 추가
    /// 2. TMP_Text 컴포넌트를 자식 오브젝트로 추가하거나 직접 할당
    /// 3. 포탈 생성 시 SetPortalName(string name) 호출
    /// </summary>
    public class PortalLabel : MonoBehaviour
    {
        [Header("Portal Label Settings")]
        [SerializeField, Tooltip("포탈 이름을 표시할 TMP_Text 컴포넌트 (자동 할당 가능)")]
        private TMP_Text portalNameText;
        
        [SerializeField, Tooltip("기본 포탈 이름 (설정되지 않은 경우 사용)")]
        private string defaultPortalName = "Portal";
        
        [Header("Text Style Settings")]
        [SerializeField, Tooltip("텍스트 색상")]
        private Color textColor = Color.white;
        
        [SerializeField, Tooltip("텍스트 크기")]
        private float fontSize = 24f;
        
        [SerializeField, Tooltip("텍스트가 카메라를 바라보도록 설정")]
        private bool lookAtCamera = true;
        
        private Camera mainCamera;
        
        private void Awake()
        {
            // TMP_Text가 할당되지 않은 경우 자동으로 찾기
            if (portalNameText == null)
            {
                AutoAssignTextComponent();
            }
            
            // 메인 카메라 참조
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }
        }
        
        private void Start()
        {
            // 초기 설정 적용
            ApplyTextSettings();
            
            // 기본 이름이 설정되어 있다면 표시
            if (!string.IsNullOrEmpty(defaultPortalName))
            {
                SetPortalName(defaultPortalName);
            }
        }
        
        private void Update()
        {
            // 카메라를 바라보도록 설정된 경우
            if (lookAtCamera && mainCamera != null && portalNameText != null)
            {
                // 텍스트가 카메라를 바라보도록 회전
                Vector3 directionToCamera = mainCamera.transform.position - transform.position;
                transform.LookAt(transform.position + directionToCamera);
            }
        }
        
        /// <summary>
        /// 포탈 이름을 설정합니다
        /// </summary>
        /// <param name="portalName">표시할 포탈 이름</param>
        public void SetPortalName(string portalName)
        {
            if (portalNameText != null)
            {
                portalNameText.text = portalName;
            }
            else
            {
                Debug.LogWarning($"PortalLabel: TMP_Text 컴포넌트가 할당되지 않았습니다. 포탈 이름 '{portalName}'을 표시할 수 없습니다.");
            }
        }
        
        /// <summary>
        /// 현재 설정된 포탈 이름을 반환합니다
        /// </summary>
        /// <returns>현재 포탈 이름</returns>
        public string GetPortalName()
        {
            return portalNameText != null ? portalNameText.text : string.Empty;
        }
        
        /// <summary>
        /// 텍스트 색상을 변경합니다
        /// </summary>
        /// <param name="color">새로운 텍스트 색상</param>
        public void SetTextColor(Color color)
        {
            textColor = color;
            if (portalNameText != null)
            {
                portalNameText.color = color;
            }
        }
        
        /// <summary>
        /// 텍스트 크기를 변경합니다
        /// </summary>
        /// <param name="size">새로운 텍스트 크기</param>
        public void SetFontSize(float size)
        {
            fontSize = size;
            if (portalNameText != null)
            {
                portalNameText.fontSize = size;
            }
        }
        
        /// <summary>
        /// 텍스트 표시/숨김을 설정합니다
        /// </summary>
        /// <param name="visible">표시 여부</param>
        public void SetVisible(bool visible)
        {
            if (portalNameText != null)
            {
                portalNameText.gameObject.SetActive(visible);
            }
        }
        
        /// <summary>
        /// TMP_Text 컴포넌트를 자동으로 찾아서 할당합니다
        /// </summary>
        private void AutoAssignTextComponent()
        {
            // 1. 자식 오브젝트에서 TMP_Text 찾기
            portalNameText = GetComponentInChildren<TMP_Text>();
            
            // 2. 찾지 못한 경우 이름으로 찾기
            if (portalNameText == null)
            {
                Transform textTransform = transform.Find("Text") ?? 
                                        transform.Find("PortalText") ?? 
                                        transform.Find("Label");
                
                if (textTransform != null)
                {
                    portalNameText = textTransform.GetComponent<TMP_Text>();
                }
            }
            
            // 3. 여전히 찾지 못한 경우 경고
            if (portalNameText == null)
            {
                Debug.LogWarning($"PortalLabel: {gameObject.name}에서 TMP_Text 컴포넌트를 찾을 수 없습니다. " +
                               "자식 오브젝트에 TMP_Text가 있는지 확인하거나 직접 할당해주세요.");
            }
            else
            {
                Debug.Log($"PortalLabel: TMP_Text 컴포넌트를 자동으로 찾았습니다: {portalNameText.gameObject.name}");
            }
        }
        
        /// <summary>
        /// 텍스트 스타일 설정을 적용합니다
        /// </summary>
        private void ApplyTextSettings()
        {
            if (portalNameText != null)
            {
                portalNameText.color = textColor;
                portalNameText.fontSize = fontSize;
                
                // 텍스트 정렬 설정 (중앙 정렬)
                portalNameText.alignment = TextAlignmentOptions.Center;
                
                // 텍스트가 잘리지 않도록 설정
                portalNameText.enableAutoSizing = true;
                portalNameText.fontSizeMin = fontSize * 0.5f;
                portalNameText.fontSizeMax = fontSize * 1.5f;
            }
        }
        
        /// <summary>
        /// Inspector에서 값이 변경될 때 호출 (에디터 전용)
        /// </summary>
        private void OnValidate()
        {
            if (portalNameText != null)
            {
                ApplyTextSettings();
            }
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 컴포넌트 추가 시 자동 설정 (에디터 전용)
        /// </summary>
        private void Reset()
        {
            AutoAssignTextComponent();
            ApplyTextSettings();
        }
        #endif
    }
}
