using UnityEngine;
using System.Collections;

public class JumpAreaDamage : MonoBehaviour
{
    private float damage;
    private float radius;
    private bool hasDamageApplied = false; // 데미지가 한 번만 적용되도록 체크

    public void Initialize(float areaDamage, float areaRadius)
    {
        damage = areaDamage;
        radius = areaRadius;
        Debug.Log($"===== 점프 공격 범위 데미지 초기화 =====");
        Debug.Log($"데미지: {damage}");
        Debug.Log($"범위: {radius}m");
        Debug.Log($"파티클 위치: {transform.position}");
        Debug.Log($"파티클 스케일: {transform.localScale}");
        
        // 파티클 시스템 정보 출력
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            var shape = ps.shape;
            Debug.Log($"파티클 시스템: {ps.name}");
            Debug.Log($"Shape 타입: {shape.shapeType}");
            Debug.Log($"Shape 반경: {shape.radius}");
            Debug.Log($"Shape 스케일: {shape.scale}");
        }
        
        ApplyAreaDamage();
    }

    // 데미지를 지연시키는 새로운 초기화 메서드
    public void InitializeWithDelay(float areaDamage, float areaRadius, float delay)
    {
        damage = areaDamage;
        radius = areaRadius;
        Debug.Log($"===== 지연 범위 데미지 초기화 (딜레이: {delay}초) =====");
        StartCoroutine(ApplyDamageAfterDelay(delay));
    }

    private IEnumerator ApplyDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ApplyAreaDamage();
    }
    private void ApplyAreaDamage()
    {
        if (hasDamageApplied) return;

        Debug.Log($"범위 데미지 체크 시작 - 반경: {radius}m");
        
        // 구 형태로 범위 내 플레이어 체크
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, LayerMask.GetMask("Player"));
        
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                var damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                    Debug.Log($"범위 데미지 적용: {damage}");
                }
            }
        }

        hasDamageApplied = true;
        
        // 파티클 효과가 끝나면 오브젝트 제거
        // 코루틴을 사용하여 파티클이 끝난 후 오브젝트를 제거합니다.
        StartCoroutine(DestroyAfterParticles());
    }

    private IEnumerator DestroyAfterParticles()
    {
        ParticleSystem ps = GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            // 파티클 시스템의 모든 파티클이 사라질 때까지 기다립니다.
            yield return new WaitWhile(() => ps.IsAlive(true));
        }
        else
        {
            // 파티클 시스템이 없는 경우를 대비해 기본 2초 대기
            yield return new WaitForSeconds(2f);
        }

        // 파티클 재생이 완전히 끝난 후 오브젝트를 제거합니다.
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        // 실제 데미지 범위를 빨간색으로 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
        
        // 파티클 시스템의 범위를 노란색으로 표시
        Gizmos.color = Color.yellow;
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            var shape = ps.shape;
            if (shape.shapeType == ParticleSystemShapeType.Sphere || 
                shape.shapeType == ParticleSystemShapeType.Circle)
            {
                Gizmos.DrawWireSphere(ps.transform.position, shape.radius);
            }
            else if (shape.shapeType == ParticleSystemShapeType.Box)
            {
                Gizmos.DrawWireCube(ps.transform.position, shape.scale);
            }
        }
    }
}