using UnityEngine;
using System.Collections;

public class JumpAreaDamage : MonoBehaviour
{
    private float damage;
    private float radius;
    private float duration;
    private bool isDamageActive = false;
    
    private float lastDamageTime = 0f;
    private float damageInterval = 0.3f; // 간격을 줄여서 더 자주 체크
    
    // 이미 데미지를 받은 플레이어 추적
    private bool hasDealtDamage = false;

    public void Initialize(float areaDamage, float areaRadius, float effectDuration)
    {
        damage = areaDamage;
        radius = areaRadius;
        duration = effectDuration;
        
        isDamageActive = true;
        hasDealtDamage = false;
        
        Debug.Log($"=== JumpAreaDamage 초기화 ===");
        Debug.Log($"위치: {transform.position}, 데미지: {damage}, 반경: {radius}");
        
        // 다중 방식으로 데미지 체크
        StartCoroutine(MultipleCheckMethods());
    }
    
    IEnumerator MultipleCheckMethods()
    {
        // 방법 1: 즉시 체크
        yield return new WaitForFixedUpdate();
        CheckForPlayersInRange("즉시체크");
        
        // 방법 2: 짧은 지연 후 체크
        yield return new WaitForSeconds(0.1f);
        CheckForPlayersInRange("0.1초후");
        
        // 방법 3: 지속적인 체크
        float elapsedTime = 0f;
        int checkCount = 0;
        
        while (elapsedTime < duration && isDamageActive && !hasDealtDamage)
        {
            checkCount++;
            CheckForPlayersInRange($"지속체크{checkCount}");
            
            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;
        }
        
        isDamageActive = false;
        Debug.Log($"=== 데미지 체크 종료 - 데미지 적용됨: {hasDealtDamage} ===");
    }
    
    void CheckForPlayersInRange(string checkType)
    {
        if (hasDealtDamage) return; // 이미 데미지를 줬으면 중단
        
        // 여러 방법으로 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            Debug.Log($"[{checkType}] 플레이어 거리: {distance:F2}m (반경: {radius}m)");
            
            if (distance <= radius)
            {
                Debug.Log($"★ [{checkType}] 플레이어가 범위 내에 있음!");
                
                // 방법 1: IDamageable 인터페이스
                IDamageable damageable = player.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                    hasDealtDamage = true;
                    Debug.Log($"★★★ [{checkType}] IDamageable로 {damage} 데미지 적용! ★★★");
                    return;
                }
                
                // 방법 2: 직접 컴포넌트 찾기
                MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour script in scripts)
                {
                    var takeDamageMethod = script.GetType().GetMethod("TakeDamage");
                    if (takeDamageMethod != null)
                    {
                        takeDamageMethod.Invoke(script, new object[] { damage });
                        hasDealtDamage = true;
                        Debug.Log($"★★★ [{checkType}] {script.GetType().Name}로 {damage} 데미지 적용! ★★★");
                        return;
                    }
                }
                
                Debug.LogError($"[{checkType}] 데미지 적용 실패 - TakeDamage 메서드를 찾을 수 없음");
            }
        }
        
        // OverlapSphere로도 체크
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
        Debug.Log($"[{checkType}] OverlapSphere: {hitColliders.Length}개 콜라이더 발견");
        
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player") && !hasDealtDamage)
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                    hasDealtDamage = true;
                    Debug.Log($"★★★ [{checkType}] OverlapSphere로 {damage} 데미지 적용! ★★★");
                    return;
                }
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isDamageActive ? Color.red : Color.gray;
        Gizmos.DrawWireSphere(transform.position, radius);
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            Gizmos.color = distance <= radius ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.transform.position);
        }
    }
}
