using UnityEngine;

public class JumpAttackAreaDamage : MonoBehaviour
{
    private float damage;
    private float radius;
    private bool hasDamageApplied = false;

    public void Initialize(float areaDamage, float areaRadius)
    {
        damage = areaDamage;
        radius = areaRadius;
        
        ApplyAreaDamage();
        
        // 2초 후에 이 게임 오브젝트를 파괴합니다.
        Destroy(gameObject, 2f);
    }

    private void ApplyAreaDamage()
    {
        if (hasDamageApplied) return;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, LayerMask.GetMask("Player"));
        
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                var damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                    Debug.Log($"점프 공격 범위 데미지 적용: {damage}");
                }
            }
        }
        hasDamageApplied = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
