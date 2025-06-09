using UnityEngine;
public class HoundInvisibleWall : MonoBehaviour
{
    [Header("가상 벽 설정")]  
[SerializeField] float wallRadius = 1.3f;               // 최종 방어선    [SerializeField] string playerTag = "Player";
    
    private SphereCollider invisibleWall;
    
    void Start()
    {
        CreateInvisibleWall();
    }
    
    void CreateInvisibleWall()
    {
        // 새로운 GameObject 생성 (가상 벽용)
        GameObject wallObject = new GameObject("InvisibleWall");
        wallObject.transform.SetParent(transform);
        wallObject.transform.localPosition = Vector3.zero;
        
        // SphereCollider 추가
        invisibleWall = wallObject.AddComponent<SphereCollider>();
        invisibleWall.radius = wallRadius;
        invisibleWall.isTrigger = false; // 물리적 충돌
        
        // 물리 재질 생성 (미끄러지도록)
        PhysicsMaterial wallMaterial = new PhysicsMaterial("HoundWall");
        wallMaterial.dynamicFriction = 0.1f;        // 낮은 마찰력
        wallMaterial.staticFriction = 0.1f;        // 낮은 정적 마찰력
        wallMaterial.bounciness = 0.3f;            // 약간의 반발력
        wallMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
        wallMaterial.bounceCombine = PhysicsMaterialCombine.Average;
        
        invisibleWall.material = wallMaterial;
        
        // 레이어 설정 (선택사항)
        wallObject.layer = LayerMask.NameToLayer("Default");
        
        Debug.Log("하운드 가상 벽 생성 완료");
    }
    
    // 벽 크기 실시간 조정 (필요시)
    public void SetWallRadius(float newRadius)
    {
        wallRadius = newRadius;
        if (invisibleWall != null)
        {
            invisibleWall.radius = wallRadius;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wallRadius);
    }
}