# 보스 스테이지 자동 씬 전환 시스템

## 개요
보스 스테이지에서 Boss 태그를 가진 오브젝트(보스)가 사라지면 자동으로 페이드 효과와 함께 다음 씬으로 전환되는 시스템입니다.

---

## Unity 에디터 설정 방법

### 1단계: BossStageManager 오브젝트 생성

보스 스테이지 씬(예: Boss_Hound, Boss_Golem)에서:

1. **빈 GameObject 생성**
   - Hierarchy에서 우클릭 → Create Empty
   - 이름: "BossStageManager"

2. **BossStageManager 스크립트 추가**
   - `Assets/Scripts/Boss/BossStageManager.cs` 드래그 앤 드롭

3. **Inspector 설정**
   ```
   Next Scene Name: "start2"  (전환할 씬 이름)
   Delay Before Transition: 3  (보스 사망 후 대기 시간, 초)
   Boss Tag: "Boss"  (보스 오브젝트의 태그)
   Check Interval: 0.5  (보스 존재 확인 주기, 초)
   ```

### 2단계: 보스 오브젝트에 Boss 태그 설정

1. **보스 오브젝트 선택** (HoundAI, GolemAI 등)
2. **Inspector 상단의 Tag 드롭다운 클릭**
3. **"Boss" 태그 선택**
   - Boss 태그가 없다면: Add Tag → + 버튼 → "Boss" 입력 → Save

### 3단계: SceneFadeManager 확인

SceneFadeManager가 씬에 있는지 확인:
- 있으면: 자동으로 페이드 효과 적용 ✅
- 없으면: 페이드 없이 즉시 전환 (경고 로그 출력)

---

## 동작 방식

### 1. 보스 감지
```
씬 시작 → Boss 태그 오브젝트 탐색 → 0.5초마다 존재 확인
```

### 2. 보스 사망 감지
```
보스 체력 0 → Die() 호출 → Destroy(gameObject, 4~5f)
→ BossStageManager가 감지 (보스 오브젝트 == null)
```

### 3. 씬 전환 프로세스
```
1. 보스 사망 감지
2. 3초 대기 (승리 연출, 아이템 획득 등)
3. 페이드 아웃 효과 시작
4. "start2" 씬 로드
5. 페이드 인 효과 (SceneFadeManager.OnSceneLoaded에서 자동)
```

---

## 코드 흐름

### BossStageManager
```csharp
Start()
    └─ FindBossObject()  // Boss 태그 찾기
    └─ CheckBossStatus()  // 코루틴 시작
        └─ 0.5초마다 보스 존재 확인
            └─ 보스 없음 감지
                └─ TransitionToNextScene()
                    ├─ 3초 대기
                    ├─ FadeOut()
                    ├─ LoadScene("start2")
                    └─ FadeIn() (자동)
```

### HoundAI / GolemAI
```csharp
TakeDamage()
    └─ currentHp <= 0
        └─ Die()
            ├─ isDead = true
            ├─ 사망 애니메이션
            ├─ 콜라이더 비활성화
            ├─ 골드 지급
            └─ Destroy(gameObject, 4~5f)  // ⭐ BossStageManager가 감지
```

---

## 커스터마이징

### 다른 씬으로 전환
```csharp
// Inspector에서 변경
Next Scene Name: "MainMenu"  // 원하는 씬 이름

// 또는 코드로 변경
BossStageManager manager = FindFirstObjectByType<BossStageManager>();
manager.SetTransitionDelay(5f);  // 5초 대기
```

### 수동 트리거
```csharp
// 특정 조건에서 강제로 씬 전환
BossStageManager manager = FindFirstObjectByType<BossStageManager>();
manager.TriggerSceneTransition();
```

### 대기 시간 조정
```csharp
// 승리 연출이 긴 경우
Delay Before Transition: 5  // 5초로 증가

// 빠르게 전환하고 싶은 경우
Delay Before Transition: 1  // 1초로 감소
```

---

## 디버깅

### Console 로그 메시지

✅ **정상 동작**
```
보스 감지: Hound
보스가 사라졌습니다! 씬 전환 시작...
페이드 효과와 함께 start2 씬으로 전환
```

⚠️ **경고**
```
'Boss' 태그를 가진 보스 오브젝트를 찾을 수 없습니다!
→ 보스 오브젝트에 Boss 태그 설정 확인

SceneFadeManager를 찾을 수 없습니다. 즉시 씬 전환합니다.
→ SceneFadeManager가 씬에 있는지 확인
```

### 문제 해결

**문제: 보스가 죽어도 씬 전환이 안 됨**
1. 보스 오브젝트에 "Boss" 태그 설정 확인
2. BossStageManager가 씬에 있는지 확인
3. Console에서 에러 메시지 확인

**문제: 페이드 효과가 없음**
1. SceneFadeManager가 씬에 있는지 확인
2. SceneFadeManager.Instance가 null인지 확인

**문제: 너무 빨리/늦게 전환됨**
1. `Delay Before Transition` 값 조정
2. 보스의 Destroy 시간 확인 (현재 4~5초)

---

## 적용된 씬 목록

### 현재 자동 전환 지원
- ✅ Boss_Hound 씬 (HoundAI)
- ✅ Boss_Golem 씬 (GolemAI)
- ✅ 기타 Boss 태그를 가진 모든 보스 씬

### 수동 설정 필요
각 보스 스테이지 씬에서:
1. BossStageManager GameObject 생성
2. Boss 태그 확인
3. Next Scene Name 설정

---

## 타임라인 예시

```
00:00 - 보스 전투 시작
  ↓
01:30 - 플레이어가 보스 처치
  ↓
01:30 - Die() 호출, 사망 애니메이션 시작
  ↓
01:30 - 골드 획득 메시지
  ↓
01:34 - Destroy(gameObject, 4f) 실행
  ↓
01:34 - BossStageManager 감지
  ↓
01:37 - 3초 대기 완료
  ↓
01:37 - 페이드 아웃 시작 (1초)
  ↓
01:38 - "start2" 씬 로드
  ↓
01:38 - 페이드 인 시작 (1초 + 0.5초 검은 화면)
  ↓
01:39.5 - 새 씬에서 게임 계속
```

---

## 확장 가능성

### 승리 UI 표시
```csharp
private IEnumerator TransitionToNextScene()
{
    isTransitioning = true;
    
    // 승리 UI 표시
    VictoryUI.Instance?.Show();
    
    yield return new WaitForSeconds(delayBeforeTransition);
    
    // 페이드 아웃 & 씬 전환
    // ...
}
```

### 보스별 다른 씬 전환
```csharp
[SerializeField] private BossSceneMapping[] bossSceneMappings;

[System.Serializable]
public class BossSceneMapping
{
    public string bossName;
    public string nextSceneName;
}

// Hound 보스 → start2
// Golem 보스 → start3
```

---

## 요약

✅ **자동 감지**: Boss 태그 오브젝트 자동 탐색
✅ **부드러운 전환**: 페이드 효과 + 대기 시간
✅ **간편한 설정**: Inspector에서 모든 설정 가능
✅ **디버그 지원**: 상세한 로그 메시지
✅ **확장 가능**: 승리 UI, 보스별 씬 등 추가 가능
