using System;
using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponData weaponData;
    [SerializeField] private Collider damageCollider;
    private float damage;
    
    //private List<Collider> targetsHitDuringSwing = new List<Collider>();
    
    public void ApplyWeaponTypeToAnimator(Animator animator)
    {
        animator.SetInteger("WeaponType", (int)weaponData.weaponType);
    }

    private void Awake()
    {
        // Collider가 비활성화된 상태로 시작하도록 설정 (선택적)
        if (damageCollider == null)
        {
            damageCollider = GetComponent<Collider>(); // 콜라이더 자동 할당 시도
        }
        // 시작 시에는 콜라이더 비활성화
        DisableDamageCollider();
    }

    private void Start()
    {
        damage = weaponData.damage;
    }

    // 애니메이션 이벤트
    public void EnableDamageCollider()
    {
        // 새로운 공격 시작 시, 이전에 맞았던 타겟 리스트 초기화
        //targetsHitDuringSwing.Clear();
        damageCollider.enabled = true;
        Debug.Log("Weapon Collider Enabled");
    }

    // 애니메이션 이벤트
    public void DisableDamageCollider()
    {
        damageCollider.enabled = false;
        // Debug.Log("Weapon Collider Disabled");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.name);
        if (!damageCollider.enabled) return;

        // 자기 자신이나 이미 맞은 대상은 무시
        if (other == damageCollider) //|| targetsHitDuringSwing.Contains(other))
        {
            return;
        }
        
        if (other.CompareTag("Enemy"))
        {
            Skeleton skeleton = other.GetComponent<Skeleton>();
            if (skeleton != null)
            {
                skeleton.TakeDamage(damage);
            }
        }
    }
}
