using UnityEngine;
using System.Collections;

public class RockSpike : MonoBehaviour
{
    private float damage;
    private bool hasDealtDamage = false;
    [SerializeField] private float damageRadius = 2f;
    private ParticleSystem ps;
    
    public void Initialize(float damageAmount)
    {
        damage = damageAmount;
        ps = GetComponent<ParticleSystem>();
        
        // 파티클 시스템의 지속 시간에 맞춰 데미지 체크
        if (ps != null)
        {
            float particleDuration = ps.main.duration;
            StartCoroutine(ParticleBasedDamageCheck(particleDuration));
        }
        else
        {
            StartCoroutine(ContinuousDamageCheck());
        }
        
        Destroy(gameObject, 3f);
    }
    
    IEnumerator ParticleBasedDamageCheck(float duration)
    {
        float checkInterval = 0.1f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && !hasDealtDamage)
        {
            // 파티클이 활성화된 동안만 데미지 체크
            if (ps != null && ps.isPlaying)
            {
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, damageRadius);
                
                foreach (Collider hitCollider in hitColliders)
                {
                    if (hitCollider.CompareTag("Player") && !hasDealtDamage)
                    {
                        IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                        if (damageable != null)
                        {
                            damageable.TakeDamage(damage);
                            hasDealtDamage = true;
                            Debug.Log($"바위 송곳 파티클 데미지! 플레이어에게 {damage} 데미지!");
                            yield break;
                        }
                    }
                }
            }
            
            yield return new WaitForSeconds(checkInterval);
            elapsedTime += checkInterval;
        }
    }
    
    IEnumerator ContinuousDamageCheck()
    {
        float checkInterval = 0.1f;
        float maxDuration = 1f;
        float elapsedTime = 0f;
        
        while (elapsedTime < maxDuration && !hasDealtDamage)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, damageRadius);
            
            foreach (Collider hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player") && !hasDealtDamage)
                {
                    IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(damage);
                        hasDealtDamage = true;
                        Debug.Log($"바위 송곳 범위 데미지! 플레이어에게 {damage} 데미지!");
                        yield break;
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
