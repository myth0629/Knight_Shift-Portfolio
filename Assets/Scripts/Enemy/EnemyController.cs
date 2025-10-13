using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float currentHp;
    [SerializeField] private int dropGold = 10;
    [SerializeField] private float attackDamage = 20f; // 무기 초기화용

    [Header("Flags (읽기전용)")] public bool isDead = false;
    
    [Header("피격 사운드")]
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private float hitSoundVolume = 0.5f;

    private Weapon weapon;
    private Animator animator;
    private PlayerDataManager playerData;
    private NavMeshAgent navmesh;
    private AudioSource audioSource;
    private EnemySound enemySound;


    private void Awake()
    {
        weapon = GetComponentInChildren<Weapon>();
        animator = GetComponent<Animator>();
        playerData = FindFirstObjectByType<PlayerDataManager>();
        navmesh = GetComponent<NavMeshAgent>();
        enemySound = GetComponent<EnemySound>();
        
        // AudioSource 초기화
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D 사운드
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = 30f;
        }
    }

    void Start()
    {
        currentHp = Mathf.Clamp(currentHp <= 0 ? maxHp : currentHp, 0, maxHp);
        if (weapon != null) weapon.Init(attackDamage);
    }

    // 이동 / 추격 / 공격 판단 로직은 Behavior Tree(EnemyBehaviorTree)에서 수행.
    // 이 스크립트는 체력/사망/무기 콜라이더 제어만 담당.
    
    public void TakeDamage(float damageAmount)
    {
        Debug.Log("Skeleton Damage Taken: " + damageAmount);
        currentHp -= damageAmount;
        
        // 피격 사운드 재생
        PlayHitSound();
        
        if (!isDead && animator != null) animator.SetTrigger("Hit");
        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
        }
    }
    
    private void PlayHitSound()
    {
        if (hitSounds != null && hitSounds.Length > 0 && audioSource != null && !isDead)
        {
            AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, hitSoundVolume);
            }
        }
    }
    
    void Die()
    {
        if (isDead) return;
        isDead = true;

        enemySound?.PlayDeathSound();

        if (animator != null) animator.SetTrigger("Death");
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        navmesh.enabled = false;
        
        // 골드 지급: 플레이어 데이터 시스템이 존재할 때만
        playerData?.AddGold(dropGold);

        LockOnSystem lockOn = FindFirstObjectByType<LockOnSystem>();
        if (lockOn != null)
        {
            Debug.Log("Unlock");
            lockOn.Unlock();
        }
        Destroy(gameObject, 3f);
    }
    
    public void EnableWeaponCollider()
    {
        weapon.EnableDamageCollider();
    }
    
    public void DisableWeaponCollider()
    {
        weapon.DisableDamageCollider();
    }
}
