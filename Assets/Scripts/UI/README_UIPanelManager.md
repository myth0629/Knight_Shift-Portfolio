# UI 패널 관리 시스템 사용 가이드

## 개요
ESC 키를 누르면 열려있는 UI 패널을 우선적으로 닫고, 모든 패널이 닫혀있을 때만 옵션 패널이 열리는 시스템입니다.

## 구성 요소

### 1. UIPanelManager
- 싱글톤 패턴으로 구현된 UI 패널 관리자
- ESC 키 입력을 중앙에서 처리
- 등록된 패널들의 우선순위 관리

### 2. IUIPanel 인터페이스
```csharp
public interface IUIPanel
{
    bool IsOpen();  // 패널이 열려있는지 확인
    void Close();   // 패널 닫기
}
```

### 3. 구현된 패널들
- **StatusUI**: 캐릭터 스테이터스 패널 (I 키)
- **MapUIManager**: 맵 UI (M 키)
- **UpgradeConfirmPanel**: 업그레이드 확인 패널
- **SimpleUIPanel**: 간단한 GameObject 기반 패널용 래퍼

## Unity Editor 설정

### 1. UIPanelManager 추가
1. 씬에 빈 GameObject 생성 (이름: "UIPanelManager")
2. `UIPanelManager` 스크립트 추가
3. Inspector에서 설정:
   - **Option Panel**: 옵션/환경설정 패널 GameObject 연결
   - **Vcam**: CinemachineCamera 연결 (자동 탐색 가능)

### 2. 기존 패널 수정
#### PlayerUI
- ESC 키 처리가 UIPanelManager로 자동 위임됨
- 옵션 패널은 UIPanelManager의 Option Panel로 연결

#### 다른 패널들
- `StatusUI`, `MapUIManager`, `UpgradeConfirmPanel`은 자동으로 등록됨
- 별도 설정 필요 없음

## 새로운 패널 추가하기

### 방법 1: IUIPanel 직접 구현

```csharp
public class MyCustomPanel : MonoBehaviour, IUIPanel
{
    [SerializeField] private GameObject panelObject;

    private void Start()
    {
        // UIPanelManager에 등록
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.RegisterPanel(this);
        }
    }

    private void OnDestroy()
    {
        // 등록 해제
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.UnregisterPanel(this);
        }
    }

    public void Open()
    {
        panelObject.SetActive(true);
        
        // 패널 열릴 때 UIPanelManager에 알림
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.OnPanelOpened(this);
        }
    }

    // IUIPanel 인터페이스 구현
    public bool IsOpen()
    {
        return panelObject != null && panelObject.activeSelf;
    }

    public void Close()
    {
        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }
    }
}
```

### 방법 2: SimpleUIPanel 사용 (간단한 패널)

1. 패널 GameObject에 `SimpleUIPanel` 컴포넌트 추가
2. Inspector에서 설정:
   - **Panel Object**: 열고 닫을 GameObject
   - **Manage Cursor**: 커서 표시 여부 관리 (기본: true)
   - **Pause Game**: 게임 일시정지 여부 (기본: false)

3. 코드에서 사용:
```csharp
SimpleUIPanel panel = GetComponent<SimpleUIPanel>();
panel.Open();   // 패널 열기
panel.Close();  // 패널 닫기
panel.Toggle(); // 토글
```

## 동작 순서

1. **ESC 키 입력**
2. **UIPanelManager가 감지**
3. **등록된 패널 중 열린 패널 확인**
   - 열린 패널이 있으면 → **가장 최근에 열린 패널 닫기**
   - 모든 패널이 닫혀있으면 → **옵션 패널 토글**

### 예시 시나리오

#### 시나리오 1: 맵이 열려있을 때
```
1. M 키로 맵 열기
2. ESC → 맵 닫힘
3. ESC → 옵션 패널 열림
4. ESC → 옵션 패널 닫힘
```

#### 시나리오 2: 여러 패널이 열려있을 때
```
1. I 키로 스테이터스 창 열기
2. M 키로 맵 열기 (스테이터스 창은 그대로)
3. ESC → 맵 닫힘 (가장 마지막에 열림)
4. ESC → 스테이터스 창 닫힘
5. ESC → 옵션 패널 열림
```

#### 시나리오 3: 업그레이드 확인 패널
```
1. 업그레이드 슬롯 클릭
2. 확인 패널 열림
3. ESC → 확인 패널 닫힘 (구매 취소)
4. ESC → 옵션 패널 열림
```

## 주의사항

1. **UIPanelManager는 씬마다 하나만 존재해야 합니다**
2. **패널은 Start()에서 자동 등록되므로, Awake()에서는 UIPanelManager를 찾을 수 없을 수 있습니다**
3. **OnDestroy()에서 반드시 등록 해제해야 메모리 누수를 방지할 수 있습니다**
4. **옵션 패널은 UIPanelManager에서 관리하므로 직접 ESC 키를 처리하지 마세요**

## 기존 코드와의 호환성

### PlayerUI.cs
- ESC 키 처리: UIPanelManager가 없으면 기존 방식대로 작동
- 기존 프로젝트에서도 안전하게 사용 가능

### 개별 패널들
- 각 패널의 고유 단축키(I, M 등)는 그대로 작동
- ESC 키만 UIPanelManager가 처리

## 트러블슈팅

### 문제: ESC 키가 작동하지 않음
- UIPanelManager가 씬에 있는지 확인
- Option Panel이 연결되어 있는지 확인

### 문제: 패널이 자동으로 닫히지 않음
- 패널이 IUIPanel을 구현하는지 확인
- Start()에서 RegisterPanel()을 호출하는지 확인

### 문제: 패널 순서가 이상함
- OnPanelOpened()를 패널이 열릴 때마다 호출하는지 확인
- 여러 패널을 동시에 열 경우, 각각 OnPanelOpened() 호출 필요
