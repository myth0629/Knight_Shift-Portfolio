using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponData weaponData;
    [SerializeField] private Collider damageCollider;
    [SerializeField] public string targetTag;
    [SerializeField] float damage;

    private List<Collider> targetsHitDuringSwing = new List<Collider>(); // 주석 해제

    public void ApplyWeaponTypeToAnimator(Animator animator)
    {
        animator.SetInteger("WeaponType", (int)weaponData.weaponType);
    }

    private void Awake()
    {
        damage = weaponData.damage;
        
        if (damageCollider == null)
        {
            damageCollider = GetComponent<Collider>();
        }
        DisableDamageCollider();
    }

    public void Init(float damage)
    {
        this.damage = damage;
    }

    // 애니메이션 이벤트
    public void EnableDamageCollider()
    {
        // 새로운 공격 시작 시, 이전에 맞았던 타겟 리스트 초기화
        targetsHitDuringSwing.Clear(); // 주석 해제
        damageCollider.enabled = true;
        Debug.Log("Weapon Collider Enabled");
    }

    // 애니메이션 이벤트
    public void DisableDamageCollider()
    {
        damageCollider.enabled = false;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.name);
        if (!damageCollider.enabled) return;

        // 자기 자신이나 이미 맞은 대상은 무시
        if (other == damageCollider || targetsHitDuringSwing.Contains(other))
        {
            return;
        }
        
        // 설정된 targetTag와 충돌한 대상의 태그 비교
        if (!string.IsNullOrEmpty(targetTag) && other.CompareTag(targetTag))
        {
            // IDamageable 인터페이스를 가진 컴포넌트 찾기
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                targetsHitDuringSwing.Add(other); // 히트 목록에 추가
                damageable.TakeDamage(damage);
            }
        }
    }
}