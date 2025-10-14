using UnityEngine;
using System.Collections.Generic;

public class AttackCollider : MonoBehaviour
{
    public float damageAmount;
    private bool hasDealtDamage = false;
    private IDamageable damageSource; // HoundAI 대신 범용 IDamageable 인터페이스 사용
    private HashSet<Collider> hitTargets = new HashSet<Collider>();

    public void SetDamageSource(IDamageable source, float damage)
    {
        damageSource = source;
        damageAmount = damage;
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
                    // 충돌 위치 계산
                    Vector3 hitPoint = other.ClosestPoint(transform.position);
                    Vector3 hitNormal = (transform.position - hitPoint).normalized;
                    
                    // 충돌 위치 정보와 함께 데미지 전달
                    damageable.TakeDamage(damageAmount, hitPoint, hitNormal);
                    hasDealtDamage = true;
                    Debug.Log($"플레이어에게 {damageAmount} 데미지! (위치: {hitPoint})");
                    
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
