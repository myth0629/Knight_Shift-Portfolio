# 상점 패널 구현 가이드

## 개요
- 스크립터블 오브젝트 `ShopItemData`로 아이템의 이름/설명/가격/아이콘(옵션)을 정의합니다.
- 각 아이템은 `ShopItemSlot`에 바인딩되며, 슬롯에 마우스를 올리면 `ShopDescriptionPanel`에 설명 텍스트가 표시됩니다.
- `ShopPanelController`는 여러 아이템을 받아 슬롯 프리팹을 동적으로 생성합니다.

## 준비물
- TextMeshPro 패키지 및 TMP 텍스트 컴포넌트
- EventSystem + GraphicRaycaster가 존재하는 Canvas

## 설정 절차
1. 프로젝트 뷰에서 마우스 우클릭 → Create → DMU → Shop → Shop Item Data 로 아이템 SO를 생성합니다.
   - itemName, description, price, icon(선택)을 입력합니다.
2. Hierarchy의 상점 UI 패널 아래에 다음 프리팹/오브젝트를 구성합니다.
   - DescriptionPanel 오브젝트에 `ShopDescriptionPanel`을 붙이고 TMP_Text를 연결합니다.
   - Grid 또는 Vertical Layout이 붙은 `SlotsParent` 오브젝트를 만듭니다.
3. 슬롯 프리팹(버튼 또는 패널 형태)을 만들고 다음을 배치합니다.
   - 아이템 이름용 TMP_Text → `ShopItemSlot.nameText`에 연결
   - 가격용 TMP_Text → `ShopItemSlot.priceText`에 연결
   - 아이콘용 Image(선택) → `ShopItemSlot.iconImage`에 연결
   - 그리고 루트에 `ShopItemSlot` 컴포넌트를 부착합니다.
4. `ShopPanelController`를 상점 패널 루트에 붙입니다.
   - `slotsParent`에 2번의 SlotsParent Transform을 연결
   - `slotPrefab`에 3번의 슬롯 프리팹을 연결
   - `items` 리스트에 1번에서 만든 ShopItemData들을 드래그하여 등록
5. 실행 시 아이템 슬롯에 마우스를 올리면 설명 패널이 활성화되며 설명 텍스트가 표시됩니다. 마우스를 떼면 비활성화됩니다.

## 참고
- `ShopItemSlot`은 scene 내 첫 번째 `ShopDescriptionPanel`을 자동 탐색하지만, 다중 패널 구성 시 명시적으로 참조를 연결해 주세요.
- 가격 포맷팅, 구매 로직, 재화 차감 등은 이후 확장 포인트입니다.
