using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damageAmount);
    
    /// <summary>
    /// 충돌 위치 정보와 함께 데미지를 받습니다
    /// </summary>
    /// <param name="damageAmount">데미지 양</param>
    /// <param name="hitPoint">충돌 지점</param>
    /// <param name="hitNormal">충돌 표면의 법선 벡터</param>
    void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 hitNormal)
    {
        // 기본 구현: 기존 TakeDamage 호출
        TakeDamage(damageAmount);
    }
}