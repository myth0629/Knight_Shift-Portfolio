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

## 참고
- Firebase 연동으로 골드 저장/로드는 `PlayerDataManager`가 처리합니다.
- 효과는 자유롭게 확장(버프, 무기 지급, 스탯 상승 등) 가능하며 `ShopEffect`를 상속한 SO를 추가해 사용하세요.
