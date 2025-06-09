using UnityEngine;
using System.Collections.Generic;

public class AttackCollider : MonoBehaviour
{
    public float damageAmount;
    private bool hasDealtDamage = false;
    private HoundAI damageSource;
    private HashSet<Collider> hitTargets = new HashSet<Collider>();

    public void SetDamageSource(HoundAI source)
    {
        damageSource = source;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"공격 콜라이더 트리거 감지: {other.name}, 태그: {other.tag}");
        
        if (other.CompareTag("Player") && !hasDealtDamage)
        {
            // Character Controller는 GetComponent로 직접 접근
            CharacterController playerController = other.GetComponent<CharacterController>();
            if (playerController != null)
            {
                IDamageable damageable = other.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damageAmount);
                    hasDealtDamage = true;
                    Debug.Log($"플레이어에게 {damageAmount} 데미지!");
                    
                    // 0.5초 후 다시 데미지 가능하도록 (연속 공격 방지)
                    Invoke(nameof(ResetDamage), 0.5f);
                }
            }
        }
    }
    
    void ResetDamage()
    {
        hasDealtDamage = false;
    }
    
    void OnEnable()
    {
        hasDealtDamage = false;
    }
}
