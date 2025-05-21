using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace MapSystem
{
    public class PortalBehavior : MonoBehaviour
    {
        [Header("Portal Settings")]
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private float hoverAmplitude = 0.2f;
        [SerializeField] private float hoverFrequency = 0.5f;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject portalEffectPrefab;
        [SerializeField] private Color activeColor = Color.cyan;
        [SerializeField] private Color inactiveColor = Color.gray;
        
        [Header("Interaction")]
        [SerializeField] private float interactionDistance = 2.0f;
        [SerializeField] private GameObject interactionPrompt;
        
        // 참조
        private MapNode nodeData;
        private MapUIManager mapUIManager;
        private Renderer portalRenderer;
        private Transform visualTransform;
        private GameObject portalEffect;
        
        // 상태
        private bool isAccessible = false;
        private bool playerInRange = false;
        private Vector3 initialPosition;
        
        // 초기화
        public void Initialize(MapNode node, MapUIManager manager)
        {
            nodeData = node;
            mapUIManager = manager;
            initialPosition = transform.position;
            
            // 렌더러 및 시각적 요소 참조 설정
            portalRenderer = GetComponentInChildren<Renderer>();
            visualTransform = transform.Find("PortalVisual");
            
            if (visualTransform == null && transform.childCount > 0)
            {
                visualTransform = transform.GetChild(0);
            }
            
            // 포탈 이펙트 생성
            if (portalEffectPrefab != null)
            {
                portalEffect = Instantiate(portalEffectPrefab, transform);
                portalEffect.transform.localPosition = Vector3.zero;
            }
            
            // 상호작용 프롬프트 초기화
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
            
            // 노드 타입에 따라 포탈 외관 조정
            SetupPortalAppearance();
        }
        
        // 노드 타입에 따라 포탈 외관 설정
        private void SetupPortalAppearance()
        {
            if (portalRenderer == null) return;
            
            // 노드 타입에 따라 크기, 색상 등 조정
            switch (nodeData.nodeType)
            {
                case NodeType.Battle:
                    transform.localScale = Vector3.one;
                    SetPortalColor(Color.red);
                    break;
                case NodeType.Shop:
                    transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                    SetPortalColor(Color.green);
                    break;
                case NodeType.Camp:
                    transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                    SetPortalColor(Color.yellow);
                    break;
                case NodeType.Elite:
                    transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                    SetPortalColor(new Color(0.8f, 0.2f, 0.8f)); // 보라색
                    break;
                case NodeType.Boss:
                    transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    SetPortalColor(new Color(0.8f, 0.1f, 0.1f)); // 진한 빨강
                    break;
                case NodeType.Start:
                    transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                    SetPortalColor(Color.white);
                    break;
            }
        }
        
        // 포탈 색상 설정
        private void SetPortalColor(Color baseColor)
        {
            if (portalRenderer == null) return;
            
            // 머티리얼이 있는지 확인
            Material portalMaterial = portalRenderer.material;
            if (portalMaterial != null)
            {
                // 접근 가능 여부에 따라 색상 조정
                Color finalColor = isAccessible ? baseColor : Color.Lerp(baseColor, inactiveColor, 0.7f);
                
                // 머티리얼에 색상 적용 (이미션 또는 베이스 컬러)
                if (portalMaterial.HasProperty("_EmissionColor"))
                {
                    portalMaterial.SetColor("_EmissionColor", finalColor * 2.0f);
                    portalMaterial.EnableKeyword("_EMISSION");
                }
                
                if (portalMaterial.HasProperty("_Color"))
                {
                    portalMaterial.SetColor("_Color", finalColor);
                }
            }
        }
        
        // 접근성 업데이트
        public void UpdateAccessibility(bool accessible)
        {
            isAccessible = accessible;
            
            // 시각적 상태 업데이트
            SetupPortalAppearance();
            
            // 이펙트 활성화/비활성화
            if (portalEffect != null)
            {
                portalEffect.SetActive(isAccessible);
            }
        }
        
        private void Update()
        {
            // // 포탈 회전 애니메이션
            // if (visualTransform != null)
            // {
            //     visualTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            // }
            
            // 포탈 호버링 애니메이션
            float hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            transform.position = initialPosition + new Vector3(0, hoverOffset, 0);
            
            // 플레이어와의 상호작용 체크
            CheckPlayerInteraction();
        }
        
        // 플레이어와의 상호작용 체크
        private void CheckPlayerInteraction()
        {
            if (!isAccessible) return;
            
            // 플레이어 찾기 (태그 또는 레이어로 식별)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                bool wasInRange = playerInRange;
                playerInRange = distance <= interactionDistance;
                
                // 상호작용 프롬프트 표시/숨김
                if (interactionPrompt != null)
                {
                    interactionPrompt.SetActive(playerInRange);
                }
                
                // 상호작용 범위에 들어왔을 때
                if (playerInRange && !wasInRange)
                {
                    // 상호작용 힌트 표시
                    Debug.Log($"플레이어가 {GetNodeTypeName(nodeData.nodeType)} 포탈에 접근했습니다.");
                }
                
                // 상호작용 키 입력 감지
                if (playerInRange && Input.GetKeyDown(KeyCode.E))
                {
                    ActivatePortal();
                }
            }
        }
        
        // 포탈 활성화 (노드 선택)
        private void ActivatePortal()
        {
            if (!isAccessible) return;
            
            Debug.Log($"{GetNodeTypeName(nodeData.nodeType)} 포탈 활성화 - 씬 로드: {nodeData.sceneName}");
            
            // 맵 컨트롤러에 노드 선택 알림
            MapController.Instance.SelectNode(nodeData.id);
            
            // 씬 로드
            if (!string.IsNullOrEmpty(nodeData.sceneName))
            {
                // 씬 로드 전 현재 노드 ID 저장 (상태 복원용)
                PlayerPrefs.SetInt("CurrentNodeID", nodeData.id);
                PlayerPrefs.Save();
                
                // 씬 로드 전 필요한 데이터 저장
                StartCoroutine(LoadSceneWithDelay(nodeData.sceneName));
            }
        }
        
        // 씬 로드 코루틴 (지연 효과 추가)
        private IEnumerator LoadSceneWithDelay(string sceneName)
        {
            // 포탈 이펙트 강화 또는 전환 이펙트 표시
            if (portalEffect != null)
            {
                portalEffect.transform.localScale = Vector3.one * 2.0f;
            }
            
            // 지연 시간
            yield return new WaitForSeconds(1.0f);
            
            try
            {
                // 씬 로드
                SceneManager.LoadScene(sceneName);
            }
            catch (Exception e)
            {
                Debug.LogError($"씬 로드 오류: {e.Message}");
                
                // 씬이 존재하지 않는 경우 임시 조치
                if (SceneManager.GetActiveScene().name != "Battle")
                {
                    SceneManager.LoadScene("Battle");
                }
            }
        }
        
        // 노드 타입에 따른 이름 반환
        private string GetNodeTypeName(NodeType type)
        {
            switch (type)
            {
                case NodeType.Battle: return "전투";
                case NodeType.Shop: return "상점";
                case NodeType.Camp: return "캠프";
                case NodeType.Boss: return "보스 전투";
                case NodeType.Start: return "시작";
                default: return "";
            }
        }
        
        // 기즈모 표시 (디버그용)
        private void OnDrawGizmos()
        {
            // 상호작용 범위 표시
            Gizmos.color = isAccessible ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }
}
