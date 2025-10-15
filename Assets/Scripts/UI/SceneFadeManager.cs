using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬 전환 시 페이드 인/아웃 효과를 관리하는 싱글톤 매니저
/// </summary>
public class SceneFadeManager : MonoBehaviour
{
    public static SceneFadeManager Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private Image fadePanel;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private Color fadeColor = Color.black;

    private bool isFading = false;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Fade Panel 초기 설정
            if (fadePanel != null)
            {
                fadePanel.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
                fadePanel.raycastTarget = false; // 초기화 시 레이캐스트 차단 해제
                fadePanel.gameObject.SetActive(true);

                // Fade Panel이 최상위에 표시되도록 설정
                Canvas canvas = fadePanel.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = 999; // 최상위 레이어
                }

                Debug.Log("[SceneFadeManager] Initialized - raycastTarget set to FALSE");
            }

            // 씬 로드 이벤트 구독
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
        
        if(fadePanel == null)
        {
            fadePanel = GameObject.Find("Fade Panel").GetComponent<Image>();
        }
    }

    private void OnDestroy()
    {
        // 씬 로드 이벤트 구독 해제
        if (Instance == this)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void Start()
    {
        // 게임 최초 시작 시에는 페이드 효과 없이 바로 시작
        // 씬 전환 시에만 OnSceneLoaded에서 페이드 인이 작동함
        if (fadePanel != null)
        {
            // 초기 상태: 투명하고 레이캐스트 비활성화
            fadePanel.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadePanel.raycastTarget = false;
            Debug.Log("[SceneFadeManager] Start - No fade effect on initial game start");
        }
    }

    private void Update()
    {
        // 안전장치: 페이드가 끝났는데도 raycastTarget이 활성화되어 있으면 강제 해제
        if (fadePanel != null && !isFading)
        {
            // 화면이 완전히 투명한데 raycastTarget이 켜져있으면 버그
            if (fadePanel.color.a < 0.01f && fadePanel.raycastTarget)
            {
                fadePanel.raycastTarget = false;
                Debug.LogWarning("[SceneFadeManager] Bug detected! Force disabled raycastTarget (alpha is near 0 but raycast was enabled)");
            }
        }
    }
    
    /// <summary>
    /// 씬이 로드될 때마다 호출되는 메서드
    /// </summary>
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // 씬 전환 시마다 Fade Panel 재검색 (Missing 방지)
        if (fadePanel == null)
        {
            GameObject fadePanelObj = GameObject.Find("Fade Panel");
            if (fadePanelObj != null)
            {
                fadePanel = fadePanelObj.GetComponent<Image>();
                if (fadePanel != null)
                {
                    Debug.Log($"[OnSceneLoaded] Found Fade Panel in scene: {scene.name}");
                    
                    // Fade Panel이 최상위에 표시되도록 설정
                    Canvas canvas = fadePanel.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        canvas.sortingOrder = 999;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[OnSceneLoaded] Fade Panel not found in scene: {scene.name}");
                return;
            }
        }
        
        // 새 씬 로드 후 페이드 인 효과
        if (fadePanel != null)
        {
            // 페이드 아웃 상태라면 페이드 인 시작
            Color currentColor = fadePanel.color;
            if (currentColor.a > 0.5f) // 화면이 어두운 상태라면
            {
                Debug.Log($"[OnSceneLoaded] Scene: {scene.name}, alpha: {currentColor.a:F2}, starting fade in...");
                StartCoroutine(FadeIn());
            }
            else
            {
                // 화면이 이미 밝은 상태라면 raycastTarget만 확실히 해제
                fadePanel.raycastTarget = false;
                Debug.Log($"[OnSceneLoaded] Scene: {scene.name}, already bright (alpha: {currentColor.a:F2}), ensuring raycast disabled");
            }
        }
    }

    /// <summary>
    /// 페이드 아웃 효과 (화면이 어두워짐)
    /// </summary>
    public IEnumerator FadeOut()
    {
        if (fadePanel == null)
        {
            Debug.LogWarning("[FadeOut] Fade panel is null!");
            yield break;
        }

        if (isFading)
        {
            Debug.LogWarning("[FadeOut] Already fading! Waiting for current fade to complete...");
            // 이미 페이딩 중이면 완료될 때까지 대기
            float timeout = 3f; // 3초 타임아웃 (페이드 최대 시간보다 길게)
            float elapsed = 0f;
            while (isFading && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (isFading)
            {
                Debug.LogError($"[FadeOut] Fade timeout after {timeout} seconds! Forcing fade state reset.");
                isFading = false;
                
                // 패널 상태 강제 리셋
                if (fadePanel != null)
                {
                    fadePanel.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
                    fadePanel.raycastTarget = false;
                }
            }
            else
            {
                Debug.Log("[FadeOut] Previous fade completed, proceeding with new fade.");
            }
        }

        isFading = true;
        fadePanel.gameObject.SetActive(true);
        fadePanel.raycastTarget = true; // 페이드아웃 시작 시 레이캐스트 차단 활성화
        Debug.Log("[FadeOut] Started - Raycast blocking enabled");

        float elapsedTime = 0f;
        Color startColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        Color endColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeOutDuration);
            fadePanel.color = Color.Lerp(startColor, endColor, alpha);
            yield return null;
        }

        fadePanel.color = endColor;
        // FadeOut 완료 후에도 raycastTarget은 유지 (씬 전환 중이므로)
        isFading = false;
        Debug.Log("[FadeOut] Complete - raycastTarget still enabled for scene transition");
    }

    /// <summary>
    /// 페이드 인 효과 (화면이 밝아짐)
    /// </summary>
    public IEnumerator FadeIn()
    {
        if (fadePanel == null)
        {
            Debug.LogWarning("Fade panel is null!");
            yield break;
        }

        if (isFading)
        {
            Debug.LogWarning("Already fading! Skipping FadeIn.");
            yield break;
        }

        isFading = true;
        fadePanel.gameObject.SetActive(true);
        fadePanel.raycastTarget = true; // 페이드인 시작 시 레이캐스트 차단 활성화
        Debug.Log("[FadeIn] Started - Raycast blocking enabled");

        // 페이드인 시작 전 0.5초 대기 (검은 화면 유지)
        yield return new WaitForSeconds(0.5f);

        float elapsedTime = 0f;
        Color startColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        Color endColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            fadePanel.color = Color.Lerp(startColor, endColor, alpha);
            yield return null;
        }

        fadePanel.color = endColor;
        fadePanel.raycastTarget = false; // 페이드인 완료 후 레이캐스트 차단 해제
        isFading = false;
        Debug.Log("[FadeIn] Complete - Raycast blocking DISABLED");
    }

    /// <summary>
    /// 페이드 아웃 후 페이드 인 효과 (씬 전환용)
    /// </summary>
    public IEnumerator FadeOutAndIn(float delayBetween = 0f)
    {
        yield return StartCoroutine(FadeOut());
        
        if (delayBetween > 0f)
        {
            yield return new WaitForSeconds(delayBetween);
        }
        
        yield return StartCoroutine(FadeIn());
    }

    /// <summary>
    /// 즉시 페이드 아웃 (페이드 효과 없이)
    /// </summary>
    public void SetFadeOut()
    {
        if (fadePanel != null)
        {
            fadePanel.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
            fadePanel.gameObject.SetActive(true);
            fadePanel.raycastTarget = true; // 즉시 페이드아웃 시 레이캐스트 차단
            Debug.Log("[SetFadeOut] Instant fade out - Raycast blocking enabled");
        }
    }

    /// <summary>
    /// 즉시 페이드 인 (페이드 효과 없이)
    /// </summary>
    public void SetFadeIn()
    {
        if (fadePanel != null)
        {
            fadePanel.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadePanel.gameObject.SetActive(true);
            fadePanel.raycastTarget = false; // 즉시 페이드인 시 레이캐스트 차단 해제
            Debug.Log("[SetFadeIn] Instant fade in - Raycast blocking DISABLED");
        }
    }

    /// <summary>
    /// 현재 페이드 중인지 확인
    /// </summary>
    public bool IsFading()
    {
        return isFading;
    }

    /// <summary>
    /// 페이드 지속 시간 설정
    /// </summary>
    public void SetFadeDuration(float fadeIn, float fadeOut)
    {
        fadeInDuration = Mathf.Max(0.1f, fadeIn);
        fadeOutDuration = Mathf.Max(0.1f, fadeOut);
    }

    /// <summary>
    /// 페이드 색상 설정
    /// </summary>
    public void SetFadeColor(Color color)
    {
        fadeColor = color;
    }
}
