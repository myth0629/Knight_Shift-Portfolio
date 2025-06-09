using UnityEngine;

public class ParticleProjectile : MonoBehaviour
{
    [Header("투사체 설정")]
    [SerializeField] private float projectileSize = 0.5f; // 투사체 크기
    [SerializeField] private float collisionRadius = 0.5f; // 충돌 감지 반경
    private Vector3 moveDirection;
    private float speed;
    private float damage;
    private float lifetime;
    private ParticleSystem particles;
    private bool hasHit = false;
    
    public void Initialize(Vector3 direction, float projectileSpeed, float projectileDamage, float life)
    {
        moveDirection = direction;
        speed = projectileSpeed;
        damage = projectileDamage;
        lifetime = life;
        
        particles = GetComponent<ParticleSystem>();
        if (particles != null)
        {
            // 파티클 설정
            var main = particles.main;
            main.startLifetime = 2f;
            main.startSpeed = 5f;
            main.startSize = 1f;
            main.startColor = Color.red;
            main.scalingMode = ParticleSystemScalingMode.Local;
            var collision = particles.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.lifetimeLoss = 1f; // 충돌 시 즉시 사라짐
            collision.bounceMultiplier = 0f; // 반발 없음
            collision.dampenMultiplier = 0f; // 감속 없음

            // 충돌할 레이어 설정 (모든 레이어와 충돌)
            collision.collidesWith = -1; // 모든 레이어
            
            // Shape 설정
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.2f;
            shape.angle = 10f;
            
            particles.Play();
        }
        
        // 수명 후 자동 삭제
        Destroy(gameObject, lifetime);
    }
    
    void Update()
    {
        if (!hasHit)
        {
            // 투사체 이동
            transform.position += moveDirection * speed * Time.deltaTime;
            
            // 충돌 감지 (Sphere Cast)
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, 0.3f, moveDirection, out hit, speed * Time.deltaTime))
            {
                if (hit.transform.CompareTag("Player"))
                {
                    // 플레이어 데미지
                    IDamageable damageable = hit.transform.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(damage);
                        Debug.Log($"투사체 데미지 적용! {damage} 데미지");
                    }
                    
                    HitEffect();
                }
                else if (!hit.transform.CompareTag("Enemy") && !hit.transform.CompareTag("Projectile"))
                {
                // Enemy와 Projectile 태그가 아닌 모든 오브젝트와 충돌 시 사라짐
                Debug.Log($"투사체가 {hit.transform.name}에 충돌하여 사라짐");
                HitEffect();
                }
            }
        }
    }
    
    void HitEffect()
    {
        hasHit = true;
        
        // 파티클 정지
        if (particles != null)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // 또는 더 확실하게
            particles.Clear(); // 모든 파티클 즉시 제거
            particles.gameObject.SetActive(false); // 파티클 시스템 비활성화
        }
        
        Destroy(gameObject, 0.1f); // 0.1초 후 삭제 (거의 즉시)
    }
}
