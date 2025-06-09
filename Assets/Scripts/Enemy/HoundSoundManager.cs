using UnityEngine;

public class HoundSoundManager : MonoBehaviour
{
    [Header("하운드 기본 공격 효과음")]
    [SerializeField] private AudioClip[] leftPawSounds;
    [SerializeField] private AudioClip[] rightPawSounds;
    [SerializeField] private AudioClip[] lickBiteSounds;
    [SerializeField] private AudioClip[] jumpAttackSounds;
    
    [Header("하운드 패턴 효과음")]
    [SerializeField] private AudioClip[] shieldPatternSounds;
    [SerializeField] private AudioClip[] phase2TransitionSounds;
    
    [Header("하운드 이동 효과음")]
    [SerializeField] private AudioClip[] footstepSounds;
    
    [Header("하운드 상태 효과음")]
    [SerializeField] private AudioClip[] deathSounds;
    [SerializeField] private AudioClip[] growlSounds; // 으르렁거리는 위협/경고 소리
    
    [Header("볼륨 설정")]
    [SerializeField] private float attackVolume = 0.8f;
    [SerializeField] private float specialVolume = 1.0f;
    [SerializeField] private float movementVolume = 0.6f;
    [SerializeField] private float patternVolume = 0.9f;
    
    private AudioSource audioSource;
    private HoundAI houndAI;
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        houndAI = GetComponent<HoundAI>();
    }
    
    void Start()
    {
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 50f;
        audioSource.volume = 1f;
    }
    
    // === Animation Event에서 호출할 공격 사운드 ===
    public void PlayLeftPawSound()
    {
        PlayRandomSound(leftPawSounds, attackVolume);
        Debug.Log("하운드 왼발 공격 효과음 재생!");
    }
    
    public void PlayRightPawSound()
    {
        PlayRandomSound(rightPawSounds, attackVolume);
        Debug.Log("하운드 오른발 공격 효과음 재생!");
    }
    
    public void PlayLickBiteSound()
    {
        PlayRandomSound(lickBiteSounds, attackVolume);
        Debug.Log("하운드 핥기/물기 공격 효과음 재생!");
    }
    
    public void PlayJumpAttackSound()
    {
        PlayRandomSound(jumpAttackSounds, specialVolume);
        Debug.Log("하운드 점프 공격 효과음 재생!");
    }
    
    // === 패턴 사운드 (코드에서 호출) ===
    public void PlayShieldPatternSound()
    {
        PlayRandomSound(shieldPatternSounds, patternVolume);
        Debug.Log("하운드 쉴드 패턴 효과음 재생!");
    }
    
    public void PlayPhase2TransitionSound()
    {
        PlayRandomSound(phase2TransitionSounds, specialVolume);
        Debug.Log("하운드 페이즈 2 전환 효과음 재생!");
    }
    
    // === 상태 사운드 ===
    public void PlayFootstepSound()
    {
        PlayRandomSound(footstepSounds, movementVolume);
    }
    
    public void PlayDeathSound()
    {
        PlayRandomSound(deathSounds, specialVolume);
        Debug.Log("하운드 사망 효과음 재생!");
    }
    
    public void PlayGrowlSound()
    {
        PlayRandomSound(growlSounds, attackVolume);
        Debug.Log("하운드 으르렁 효과음 재생!");
    }
    
    // === 페이즈별 볼륨 조정 ===
    public void PlayAttackSoundWithPhase(AudioClip[] sounds)
    {
        float volume = attackVolume;
        if (houndAI != null && houndAI.isPhase2)
        {
            volume *= 1.3f;
        }
        PlayRandomSound(sounds, volume);
    }
    
    // === 헬퍼 메서드 ===
    private void PlayRandomSound(AudioClip[] sounds, float volume)
    {
        if (sounds == null || sounds.Length == 0) return;
        
        AudioClip randomClip = sounds[Random.Range(0, sounds.Length)];
        if (randomClip != null)
        {
            audioSource.PlayOneShot(randomClip, volume);
        }
    }
    
    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
