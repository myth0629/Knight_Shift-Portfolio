using UnityEngine;

/// <summary>
/// 데미지를 받을 때 충돌 위치에 이펙트를 생성하는 매니저
/// </summary>
public class HitEffectManager : MonoBehaviour
{
    public static HitEffectManager Instance { get; private set; }

    [Header("히트 이펙트 프리팹")]
    [SerializeField] private GameObject defaultHitEffectPrefab;
    [SerializeField] private GameObject criticalHitEffectPrefab;
    [SerializeField] private GameObject bloodEffectPrefab;
    
    [Header("이펙트 설정")]
    [SerializeField] private float effectLifetime = 2f;
    [SerializeField] private bool useObjectPooling = true;
    [SerializeField] private int poolSize = 20;
    
    private GameObject effectPool;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (useObjectPooling)
            {
                InitializePool();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool()
    {
        effectPool = new GameObject("EffectPool");
        effectPool.transform.SetParent(transform);
        
        // 기본 히트 이펙트 풀 생성
        if (defaultHitEffectPrefab != null)
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject effect = Instantiate(defaultHitEffectPrefab, effectPool.transform);
                effect.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 충돌 위치에 히트 이펙트 생성
    /// </summary>
    /// <param name="hitPosition">충돌 위치</param>
    /// <param name="hitNormal">충돌 표면의 법선 벡터</param>
    /// <param name="effectType">이펙트 타입 (0: 기본, 1: 크리티컬, 2: 피)</param>
    public void PlayHitEffect(Vector3 hitPosition, Vector3 hitNormal, int effectType = 0)
    {
        GameObject effectPrefab = GetEffectPrefab(effectType);
        
        if (effectPrefab == null)
        {
            Debug.LogWarning("히트 이펙트 프리팹이 설정되지 않았습니다!");
            return;
        }

        if (useObjectPooling)
        {
            GameObject effect = GetPooledEffect();
            if (effect != null)
            {
                ActivateEffect(effect, hitPosition, hitNormal);
            }
            else
            {
                // 풀에 사용 가능한 오브젝트가 없으면 새로 생성
                CreateAndPlayEffect(effectPrefab, hitPosition, hitNormal);
            }
        }
        else
        {
            CreateAndPlayEffect(effectPrefab, hitPosition, hitNormal);
        }
    }

    /// <summary>
    /// 충돌 지점(Collision)에서 히트 이펙트 생성
    /// </summary>
    public void PlayHitEffect(Collision collision, int effectType = 0)
    {
        if (collision.contactCount > 0)
        {
            ContactPoint contact = collision.GetContact(0);
            PlayHitEffect(contact.point, contact.normal, effectType);
        }
        else
        {
            PlayHitEffect(collision.transform.position, Vector3.up, effectType);
        }
    }

    /// <summary>
    /// 충돌 지점(Collider)에서 히트 이펙트 생성
    /// </summary>
    public void PlayHitEffectAtCollider(Collider collider, Vector3 attackerPosition, int effectType = 0)
    {
        // 공격자에서 피격자로 향하는 방향
        Vector3 direction = (collider.transform.position - attackerPosition).normalized;
        
        // 충돌 지점은 콜라이더의 가장 가까운 지점
        Vector3 hitPosition = collider.ClosestPoint(attackerPosition);
        
        PlayHitEffect(hitPosition, -direction, effectType);
    }

    private GameObject GetEffectPrefab(int effectType)
    {
        switch (effectType)
        {
            case 1:
                return criticalHitEffectPrefab != null ? criticalHitEffectPrefab : defaultHitEffectPrefab;
            case 2:
                return bloodEffectPrefab != null ? bloodEffectPrefab : defaultHitEffectPrefab;
            default:
                return defaultHitEffectPrefab;
        }
    }

    private GameObject GetPooledEffect()
    {
        if (effectPool == null) return null;

        foreach (Transform child in effectPool.transform)
        {
            if (!child.gameObject.activeInHierarchy)
            {
                return child.gameObject;
            }
        }
        
        return null;
    }

    private void ActivateEffect(GameObject effect, Vector3 position, Vector3 normal)
    {
        effect.transform.position = position;
        effect.transform.rotation = Quaternion.LookRotation(normal);
        effect.SetActive(true);

        // 파티클 시스템 재생
        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles)
        {
            ps.Clear();
            ps.Play();
        }

        // 일정 시간 후 비활성화
        StartCoroutine(DeactivateAfterTime(effect, effectLifetime));
    }

    private void CreateAndPlayEffect(GameObject prefab, Vector3 position, Vector3 normal)
    {
        GameObject effect = Instantiate(prefab, position, Quaternion.LookRotation(normal));
        
        // 일정 시간 후 삭제
        Destroy(effect, effectLifetime);
    }

    private System.Collections.IEnumerator DeactivateAfterTime(GameObject effect, float time)
    {
        yield return new WaitForSeconds(time);
        
        if (effect != null)
        {
            effect.SetActive(false);
        }
    }

    /// <summary>
    /// 특정 위치에 간단한 히트 이펙트 (법선 벡터 없이)
    /// </summary>
    public void PlayHitEffect(Vector3 position, int effectType = 0)
    {
        PlayHitEffect(position, Vector3.up, effectType);
    }
}
