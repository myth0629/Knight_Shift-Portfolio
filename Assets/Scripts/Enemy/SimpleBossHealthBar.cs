using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SimpleBossHealthBar : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image backgroundBorder; // 테두리
    
    [Header("색상 설정")]
    [SerializeField] private Color healthColor = new Color(0.8f, 0.1f, 0.1f); // 진한 빨간색
    [SerializeField] private Color borderColor = new Color(0.2f, 0.2f, 0.2f, 1f); // 어두운 테두리
    
    [Header("애니메이션 설정")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float healthUpdateSpeed = 2f;
    
    private CanvasGroup canvasGroup;
    private float currentHealth;
    private float maxHealth;
    private bool isVisible = false;
    
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        // 초기 설정
        canvasGroup.alpha = 0f;
        
        // 색상 초기화
        if (healthFill != null) healthFill.color = healthColor;
        if (backgroundBorder != null) backgroundBorder.color = borderColor;
    }
    
    public void ShowBossHealthBar(string bossName, float maxHp, float currentHp)
    {
        gameObject.SetActive(true);
        
        // 보스 이름 설정
        if (bossNameText != null)
        {
            bossNameText.text = bossName;
            bossNameText.fontSize = 24;
            bossNameText.color = Color.white;
        }
        
        maxHealth = maxHp;
        currentHealth = currentHp;
        
        // 슬라이더 초기화
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        
        StartCoroutine(FadeInEffect());
        isVisible = true;
    }
    
    public void UpdateHealth(float newHealth)
    {
        if (!isVisible) return;
        
        currentHealth = newHealth;
        
        // 부드러운 체력 감소 애니메이션
        StartCoroutine(SmoothHealthUpdate(newHealth));
    }
    
    IEnumerator FadeInEffect()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeInDuration;
            
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    IEnumerator SmoothHealthUpdate(float targetHealth)
    {
        if (healthSlider == null) yield break;
        
        float startHealth = healthSlider.value;
        float elapsed = 0f;
        float duration = Mathf.Abs(targetHealth - startHealth) / maxHealth * healthUpdateSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            healthSlider.value = Mathf.Lerp(startHealth, targetHealth, progress);
            yield return null;
        }
        
        healthSlider.value = targetHealth;
    }
    
    public void HideBossHealthBar()
    {
        if (isVisible)
        {
            StartCoroutine(FadeOutEffect());
            isVisible = false;
        }
    }
    
    IEnumerator FadeOutEffect()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
