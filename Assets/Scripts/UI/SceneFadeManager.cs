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
                fadePanel.gameObject.SetActive(true);
                
                // Fade Panel이 최상위에 표시되도록 설정
                Canvas canvas = fadePanel.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = 999; // 최상위 레이어
                }
            }
            
            // 씬 로드 이벤트 구독
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
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
        // 게임 시작 시 페이드 인 효과
        if (fadePanel != null)
        {
            StartCoroutine(FadeIn());
        }
    }
    
    /// <summary>
    /// 씬이 로드될 때마다 호출되는 메서드
    /// </summary>
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // 새 씬 로드 후 페이드 인 효과
        if (fadePanel != null)
        {
            // 페이드 아웃 상태라면 페이드 인 시작
            Color currentColor = fadePanel.color;
            if (currentColor.a > 0.5f) // 화면이 어두운 상태라면
            {
                Debug.Log($"Scene loaded: {scene.name}, starting fade in...");
                StartCoroutine(FadeIn());
            }
        }
    }

    /// <summary>
    /// 페이드 아웃 효과 (화면이 어두워짐)
    /// </summary>
    public IEnumerator FadeOut()
    {
        if (fadePanel == null || isFading)
        {
            Debug.LogWarning("Fade panel is null or already fading!");
            yield break;
        }

        isFading = true;
        fadePanel.gameObject.SetActive(true);

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
        isFading = false;
    }

    /// <summary>
    /// 페이드 인 효과 (화면이 밝아짐)
    /// </summary>
    public IEnumerator FadeIn()
    {
        if (fadePanel == null || isFading)
        {
            Debug.LogWarning("Fade panel is null or already fading!");
            yield break;
        }

        isFading = true;
        fadePanel.gameObject.SetActive(true);

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
        isFading = false;
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
