using System.Collections.Generic;
using UnityEngine;

// 각 무기 프리팹에 할당
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

    /// <summary>
    /// 강화 레벨을 고려한 실제 데미지 계산
    /// </summary>
    public float GetActualDamage()
    {
        if (weaponData == null)
        {
            return damage;
        }

        // WeaponUpgradeSystem에서 현재 강화 레벨 가져오기
        int upgradeLevel = 0;
        if (WeaponUpgradeSystem.Instance != null)
        {
            upgradeLevel = WeaponUpgradeSystem.Instance.GetLevel(weaponData);
        }

        // 기본 데미지 + 강화 레벨당 20% 증가
        float baseDamage = weaponData.damage;
        float upgradeMultiplier = 1f + (upgradeLevel * 0.2f);
        float finalDamage = baseDamage * upgradeMultiplier;

        return finalDamage;
    }

    // 애니메이션 이벤트
    public void EnableDamageCollider()
    {
        // 새로운 공격 시작 시, 이전에 맞았던 타겟 리스트 초기화
        targetsHitDuringSwing.Clear();
        damageCollider.enabled = true;
    }

    // 애니메이션 이벤트
    public void DisableDamageCollider()
    {
        damageCollider.enabled = false;
    }
    
    private void OnTriggerEnter(Collider other)
    {
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
                
                // 충돌 위치 계산 (무기 위치에서 타겟으로의 가장 가까운 지점)
                Vector3 hitPoint = other.ClosestPoint(damageCollider.transform.position);
                Vector3 hitNormal = (damageCollider.transform.position - hitPoint).normalized;
                
                // 강화 레벨이 반영된 실제 데미지 계산
                float actualDamage = GetActualDamage();
                
                // 충돌 위치 정보와 함께 데미지 전달
                damageable.TakeDamage(actualDamage, hitPoint, hitNormal);
                
                // 디버그 로그 (강화 반영 확인용)
                if (WeaponUpgradeSystem.Instance != null && weaponData != null)
                {
                    int level = WeaponUpgradeSystem.Instance.GetLevel(weaponData);
                    Debug.Log($"[Weapon] {weaponData.weaponName} +{level} 공격! 기본 데미지: {weaponData.damage}, 실제 데미지: {actualDamage}");
                }
            }
        }
    }
}