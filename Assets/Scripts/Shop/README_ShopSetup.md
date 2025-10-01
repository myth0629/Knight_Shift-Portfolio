# 상점 패널 구현 가이드

## 개요
- 스크립터블 오브젝트 `ShopItemData`로 아이템의 이름/설명/가격/아이콘(옵션) 및 구매 시 적용할 `effects`를 정의합니다.
- 각 아이템은 `ShopItemSlot`에 바인딩되며, 슬롯에 마우스를 올리면 `ShopDescriptionPanel`에 설명 텍스트가 표시됩니다.
- 슬롯을 클릭하면 `ShopConfirmDialog` 확인창이 떠서 구매 여부를 묻고, 확인 시 재화 차감 및 효과 적용이 진행됩니다.

## 준비물
- TextMeshPro 패키지 및 TMP 텍스트 컴포넌트
- EventSystem + GraphicRaycaster가 존재하는 Canvas

## 설정 절차
1. 아이템 데이터 생성
   - Project 뷰 → Create → DMU → Shop → Shop Item Data
   - `itemName`, `description`, `price`, `icon(선택)` 입력
   - `effects` 배열에 아래의 효과 SO들을 드래그로 추가
2. 설명 패널 구성
   - `ShopDescriptionPanel` 컴포넌트를 DescriptionPanel 오브젝트에 부착
   - `descriptionText`에 TMP_Text 연결
3. 슬롯 프리팹 구성
   - 루트에 `ShopItemSlot` 부착 (동시에 `Button` 컴포넌트를 추가해야 함)
   - 이름 TMP_Text → `nameText` 연결, 가격 TMP_Text → `priceText` 연결, 아이콘 Image(선택) → `iconImage` 연결
   - `descriptionPanel`은 씬의 `ShopDescriptionPanel`을 자동으로 찾지만 명시 연결 권장
   - `confirmDialog`에 4번에서 만든 확인창 연결
   - `Button.onClick`은 스크립트에서 자동으로 연결되므로 별도 이벤트 등록은 필요 없음
4. 구매 확인창 구성
   - 빈 UI 패널을 만들고 `ShopConfirmDialog` 부착
   - 메시지 TMP_Text, 확인/취소 Button을 각각 `messageText`, `confirmButton`, `cancelButton`에 연결
   - 필요시 `root`에 패널 루트 오브젝트 연결(미지정 시 자기 자신)
5. 상점 패널 컨트롤러(선택)
   - `ShopPanelController`를 사용하면 `items` 리스트로 슬롯을 동적 생성 가능

## 효과 시스템(예시)
- `ShopEffect`(추상 SO): `Apply(GameObject player)`를 구현해 플레이어에게 효과 적용
- 예시: `HealEffect`
  - Create → DMU → Shop → Effects → Heal
  - `healHp`, `healSp` 수치를 지정하면 구매 시 해당만큼 회복됩니다.

## 런타임 동작
- 슬롯 Hover → 설명 패널 표시/숨김
- 슬롯 Click → 확인창 표시 → 확인 시
  - `PlayerDataManager.SpendGold(price)`로 결제 시도(부족하면 취소)
  - 결제 성공 시 `effects` 순회하며 `Apply(player)` 호출
  - `PlayerUI.UpdateGold()`로 골드 UI 갱신
  - `disableAfterPurchase`가 true면 슬롯 비활성화(버튼이 있으면 interactable=false, 없으면 오브젝트 Disable)

## 스탯 업그레이드 상점
- `StatUpgradeShop`는 Start 씬 전용 스탯 상점 기능을 제공합니다. 슬롯별로 체력/스태미나 최대치를 올리며, 구매마다 비용이 곱셈식으로 증가합니다.

### 필수 컴포넌트
1. `StatUpgradeShop`
   - `goldText`: 현재 골드 표시 TMP_Text
   - `warningText`: 골드 부족 등 메시지를 띄워줄 TMP_Text (선택)
   - `playerStatus`: MaxHp/MaxSp를 보유한 `PlayerStatus`
   - `playerData`: 골드를 관리하는 `PlayerDataManager`
   - `playerUI`: 골드/스탯 UI를 갱신할 `PlayerUI`
   - `statusUI`: 체력/스태미나 바를 갱신할 `StatusUI` (선택)
   - `confirmDialog`: 기존 확인창(`ShopConfirmDialog`) 재사용 가능
   - 씬 이름이 `Start`인지 확인하여 자동으로만 활성화됩니다.
2. `StatUpgradeSlot` (여러 개 배치)
   - `shop`: 위에서 만들어둔 `StatUpgradeShop` 참조
   - `statType`: `MaxHp` 또는 `MaxSp` 중 선택
   - `nameText`, `levelText`, `priceText`: 각 슬롯의 TMP_Text 연결
   - `upgradeButton`: Button 컴포넌트
   - `baseCost`: 첫 구매 비용, `costMultiplier`: 비용 증가 배율 (예: 1.5)
   - `bonusPerLevel`: 레벨당 증가 수치 (예: 체력 +10)

### 동작 흐름
- 슬롯의 업그레이드 버튼 클릭 → 확인창에서 구매 확정 → 골드 차감 및 스탯 보너스 저장 → UI 자동 갱신.
- 슬롯은 현재 레벨과 다음 비용을 UI에 표시하며, 골드 부족/최대 레벨 도달 시 버튼이 비활성화됩니다.
- 구매 정보는 `PlayerPrefs`에 저장되어 재접속 시 유지되며, 보너스는 `PlayerStatUpgradeApplier`가 적용합니다.

### 보너스 자동 적용
- 플레이어 프리팹(또는 씬 내 Player)에 `PlayerStatUpgradeApplier`를 추가하면 씬 로드 시 저장된 보너스가 즉시 적용됩니다.
- `applyOnStart`(기본값 true)로 자동 적용 제어, `refillHpOnApply` / `refillSpOnApply` 옵션으로 최대치 상승 시 현재 체력/스태미나를 채울 수 있습니다.
- 적용 후 `PlayerUI`와 `StatusUI`(연결한 경우)가 최신 값으로 갱신됩니다.

### UI 연동 팁
- 기존 Shop UI 안에서 섹션을 분리하거나 별도 Canvas를 사용해도 무방합니다.
- 골드 부족 메시지는 자동으로 2초 동안 표시되며, 필요시 `StatUpgradeShop.warningDuration`으로 조정하세요.
- 슬롯 프리팹을 재사용하면 일반 상점 아이템처럼 손쉽게 확장 가능합니다 (타 스탯 추가 시 `PlayerStatType` enum에 항목 추가).

## 참고
- Firebase 연동으로 골드 저장/로드는 `PlayerDataManager`가 처리합니다.
- 효과는 자유롭게 확장(버프, 무기 지급, 스탯 상승 등) 가능하며 `ShopEffect`를 상속한 SO를 추가해 사용하세요.
