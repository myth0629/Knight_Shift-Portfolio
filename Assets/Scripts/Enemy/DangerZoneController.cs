using UnityEngine;
using System.Collections;

public class DangerZoneController : MonoBehaviour
{
    private Vector3 safeZoneCenter; // 랜덤 안전지대 중심 (Transform 대신 Vector3)
    private float safeRadius;
    private float damage;
    private float damageInterval;
    private Transform player;
    
    private bool isDamageActive = false;
    private ParticleSystem dangerParticles;

    // 검색 결과 4번 적용: 고정된 안전지대 중심 사용
    public void Initialize(Vector3 center, float radius, float zoneDamage, float interval, Transform playerTransform)
    {
        safeZoneCenter = center; // Vector3로 고정된 중심점
        safeRadius = radius;
        damage = zoneDamage;
        damageInterval = interval;
        player = playerTransform;
        
        dangerParticles = GetComponent<ParticleSystem>();
        
        StartDangerZone();
        Debug.Log($"위험지역 초기화 - 안전지대 중심: {safeZoneCenter}, 반경: {safeRadius}m");
    }

    void StartDangerZone()
    {
        isDamageActive = true;
        
        if (dangerParticles != null)
        {
            var main = dangerParticles.main;
            main.startLifetime = 2f;
            main.startSpeed = 0f;
            
            var shape = dangerParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 1f, 40f); // 40x40 범위
            
            var emission = dangerParticles.emission;
            emission.rateOverTime = 25;
            
            dangerParticles.Play();
        }
        
        StartCoroutine(DamageLoop());
        Debug.Log("랜덤 안전지대 기반 위험지역 데미지 시작!");
    }

    IEnumerator DamageLoop()
    {
        while (isDamageActive && player != null)
        {
            CheckPlayerInDangerZone();
            yield return new WaitForSeconds(damageInterval);
        }
    }

    void CheckPlayerInDangerZone()
    {
        if (player == null) return;
        
        // 플레이어와 랜덤 안전지대 중심 간의 거리 계산
        float distanceFromSafeZone = Vector3.Distance(player.position, safeZoneCenter);
        
        // 플레이어가 랜덤 안전지대 밖에 있으면 데미지
        if (distanceFromSafeZone > safeRadius)
        {
            IDamageable damageable = player.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                Debug.Log($"위험지역 데미지! 플레이어가 랜덤 안전지대 밖에 있음 (거리: {distanceFromSafeZone:F1}m > {safeRadius}m)");
            }
        }
        else
        {
            Debug.Log($"플레이어가 랜덤 안전지대 내부에 있음 (거리: {distanceFromSafeZone:F1}m <= {safeRadius}m)");
        }
    }

    public void StopDangerZone()
    {
        isDamageActive = false;
        
        if (dangerParticles != null)
        {
            dangerParticles.Stop();
        }
        
        Debug.Log("랜덤 안전지대 위험지역 데미지 중지!");
    }

    void OnDestroy()
    {
        StopDangerZone();
    }

    void OnDrawGizmosSelected()
    {
        // 랜덤 안전지대 표시 (초록색)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(safeZoneCenter, safeRadius);
        
        // 위험지역 표시 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(40f, 2f, 40f));
    }
}
