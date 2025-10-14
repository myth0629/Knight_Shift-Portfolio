# 히트 이펙트 시스템 가이드

## 개요
모든 적과 플레이어가 데미지를 받을 때 충돌 위치에 자동으로 이펙트가 표시되는 시스템입니다.

---

## Unity 에디터 설정

### 1. HitEffectManager 생성

1. **빈 GameObject 생성**
   - Hierarchy에서 우클릭 → Create Empty
   - 이름: "HitEffectManager"

2. **HitEffectManager 스크립트 추가**
   - `Assets/Scripts/Effects/HitEffectManager.cs` 드래그 앤 드롭

3. **이펙트 프리팹 설정** (Inspector)
   ```
   Default Hit Effect Prefab: 기본 히트 이펙트 (불꽃, 충격파 등)
   Critical Hit Effect Prefab: 크리티컬 히트 이펙트 (선택)
   Blood Effect Prefab: 피 이펙트 (플레이어용)
   ```

4. **설정값 조정**
   ```
   Effect Lifetime: 2f (이펙트 지속 시간)
   Use Object Pooling: true (성능 최적화)
   Pool Size: 20 (풀 크기)
   ```

---

## 이펙트 프리팹 제작

### 추천 구조
```
HitEffect (GameObject)
├── ParticleSystem 1 (불꽃)
├── ParticleSystem 2 (연기)
└── Light (선택사항)
```

### 파티클 시스템 설정
1. **Duration**: 1-2초
2. **Looping**: OFF (반복 없음)
3. **Play On Awake**: ON
4. **Stop Action**: Disable

---

## 구현된 기능

### ✅ 자동 적용 완료
다음 시스템에서 자동으로 히트 이펙트가 표시됩니다:

#### 플레이어
- ✅ **PlayerStatus**: 데미지 받을 때 피 이펙트 (effectType: 2)

#### 일반 적
- ✅ **EnemyController**: 스켈레톤 등 일반 적 (effectType: 0)

#### 보스
- ✅ **HoundAI**: 하운드 보스 (effectType: 0)

#### 공격 시스템
- ✅ **Weapon.cs**: 플레이어 무기 공격
- ✅ **AttackCollider.cs**: 적의 근접 공격

---

## 이펙트 타입

```csharp
0: 기본 이펙트 (defaultHitEffectPrefab) - 적에게 사용
1: 크리티컬 이펙트 (criticalHitEffectPrefab)
2: 피 이펙트 (bloodEffectPrefab) - 플레이어에게 사용
```

---

## 추가 적용이 필요한 스크립트

아직 적용되지 않은 스크립트에 수동으로 추가하세요:

### BearAI.cs
```csharp
public void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 hitNormal)
{
    currentHp -= damageAmount;
    
    // 히트 이펙트 추가
    if (HitEffectManager.Instance != null)
    {
        HitEffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, 0);
    }
    
    // ... 기존 로직
}
```

### GolemAI.cs
```csharp
public void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 hitNormal)
{
    currentHp -= damageAmount;
    
    // 히트 이펙트 추가
    if (HitEffectManager.Instance != null)
    {
        HitEffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, 0);
    }
    
    // ... 기존 로직
}
```

---

## 코드 사용 예시

### 방법 1: 충돌 위치 정보와 함께 (정확한 위치)
```csharp
// 무기 충돌에서 사용
Vector3 hitPoint = other.ClosestPoint(transform.position);
Vector3 hitNormal = (transform.position - hitPoint).normalized;

damageable.TakeDamage(damage, hitPoint, hitNormal);
```

### 방법 2: 기본 위치 (위치 정보 없을 때)
```csharp
// 기존 코드와 호환
damageable.TakeDamage(damage);
// 내부적으로 기본 위치에 이펙트 생성
```

### 방법 3: HitEffectManager 직접 호출
```csharp
// 특정 위치에 이펙트만 생성
HitEffectManager.Instance.PlayHitEffect(position, normal, effectType);
```

---

## 성능 최적화

### 오브젝트 풀링 (기본 활성화)
- 이펙트를 미리 생성하여 재사용
- 런타임 중 Instantiate/Destroy 최소화
- Pool Size 조정으로 최적화 가능

### 권장 설정
```
동시 전투 적 수가 많은 경우: Pool Size = 30-50
일반적인 경우: Pool Size = 20
성능이 중요한 모바일: Pool Size = 10-15
```

---

## 디버깅

### Console 로그 확인
```
"Damage Taken: [데미지] at [위치]" - 위치 정보와 함께 데미지 받음
"Damage Taken: [데미지]" - 위치 정보 없이 데미지 받음
```

### 이펙트가 안 보일 때
1. HitEffectManager가 씬에 있는지 확인
2. 이펙트 프리팹이 할당되었는지 확인
3. 파티클 시스템이 정상 작동하는지 확인
4. Console에서 에러 메시지 확인

---

## 확장 가능성

### 다양한 이펙트 추가
```csharp
// 불 속성 공격
HitEffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, 3);

// 얼음 속성 공격
HitEffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, 4);
```

### HitEffectManager에 추가
```csharp
[SerializeField] private GameObject fireHitEffectPrefab;
[SerializeField] private GameObject iceHitEffectPrefab;

// GetEffectPrefab 메서드에 case 추가
case 3: return fireHitEffectPrefab;
case 4: return iceHitEffectPrefab;
```

---

## 요약

✅ **자동 적용**: PlayerStatus, EnemyController, HoundAI, Weapon, AttackCollider
📝 **수동 적용 필요**: BearAI, GolemAI (위 코드 참고)
🎨 **이펙트 설정**: Unity Inspector에서 프리팹 할당
⚡ **성능**: 오브젝트 풀링으로 최적화됨
