using UnityEngine;
using System.Collections;
using System.Collections.Generic; // List를 사용하기 위해 추가

public class RockSpike : MonoBehaviour
{
    private float damage;
    private List<Collider> sharedHitTargets; // 공유되는 피격 대상 리스트
    private bool hasDealtDamageLocally = false; // 이 스파이크 인스턴스가 피해를 입혔는지 여부
    [SerializeField] private float damageRadius = 2f;
    private ParticleSystem ps;
    
    // Initialize 메서드를 수정하여 공유 리스트를 받습니다.
    public void Initialize(float damageAmount, List<Collider> hitList)
    {
        damage = damageAmount;
        sharedHitTargets = hitList;
        ps = GetComponent<ParticleSystem>();
        
        if (ps != null)
        {
            float particleDuration = ps.main.duration;
            StartCoroutine(DamageCheckCoroutine(particleDuration));
        }
        else
        {
            // 파티클 시스템이 없는 경우를 대비한 대체 로직
            StartCoroutine(DamageCheckCoroutine(1f));
        }
        
        Destroy(gameObject, 3f);
    }
    
    // 두 개의 유사한 코루틴을 하나로 통합하고 로직을 수정합니다.
    IEnumerator DamageCheckCoroutine(float duration)
    {
        float checkInterval = 0.1f;
        float elapsedTime = 0f;
        
        // 이 스파이크가 아직 피해를 입히지 않았고, 전체 지속 시간보다 적게 실행되었다면 반복
        while (elapsedTime < duration && !hasDealtDamageLocally)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, damageRadius);
            
            foreach (Collider hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    // 공유 리스트가 존재하고, 아직 이 플레이어를 타격하지 않았다면
                    if (sharedHitTargets != null && !sharedHitTargets.Contains(hitCollider))
                    {
                        IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                        if (damageable != null)
                        {
                            damageable.TakeDamage(damage);
                            sharedHitTargets.Add(hitCollider); // 공유 리스트에 플레이어 추가
                            hasDealtDamageLocally = true;      // 이 스파이크는 이제 비활성화
                            Debug.Log($"바위 송곳 데미지! 플레이어에게 {damage} 데미지! (공유 리스트에 추가됨)");
                            yield break; // 이 스파이크의 데미지 체크 코루틴 종료
                        }
                    }
                }
            }
            
            yield return new WaitForSeconds(checkInterval);
            elapsedTime += checkInterval;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}