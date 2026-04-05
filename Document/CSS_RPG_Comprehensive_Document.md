# CSS_RPG 프로젝트 종합 아키텍처 및 로드맵 가이드

본 문서는 프로젝트 기획부터 현재까지의 제작 과정을 총망라하여 **로드맵 진행 현황**, **씬 및 오브젝트 구조**, **핵심 스크립트의 체계 및 연관 관계**, 그리고 **네트워크 데이터 처리 흐름**을 정리한 종합 가이드입니다. 

---

## 1. 씬 (Scene) 및 기본 환경
*   **Scene**: `Assets/Scenes/SampleScene.unity` 단일 씬 기반.
*   **NGO 연동**: 이 단일 씬 안에서 NetworkManager를 통해 호스트/클라이언트를 띄우고, 접속 시 미리 지정된 `Player` 프리팹을 클라이언트 권한(Object Authority)으로 스폰하는 구조입니다.

---

## 2. 로드맵 진행 현황 (History & To-Do)

### ✅ 제작 완료 파트 (Phase 1 ~ 4)
*   **[Phase 1] 타격 시스템 및 FSM 코어 분리**: `IDamageable` 인터페이스 분리를 통해 모든 오브젝트가 유연하게 타격을 받을 수 있도록 리팩토링. `CombatState`를 도입하여 공격, 넉백, 스턴 상황에서 락(Lock)이 걸리도록 상태 제어 완비.
*   **[Phase 2] 직업별 스킬 다형성 (DIP)**: 하드코딩된 전투 스크립트를 버리고 `ISkillExecutor` 인터페이스를 거치게 하여 전직(무투가, 검사, 마법사, 성기사) 시 동적으로 전투 스크립트가 갈아끼워지는 유연한 다중 클래스 체계 완성.
*   **[Phase 3] Data-Driven 아이템 & UI 구축**: 기존 레거시 재료 방식을 버리고 엑셀(CSV)을 읽어오는 `ItemDatabase` / `RecipeDatabase` 싱글톤 적용. 무한 스크롤 배열 방식의 `InventorySystem`과 `EquipmentSystem`을 구현. 또한 이를 시각화하기 위해 `InventoryUIController`라는 순수 C# 상태 머신을 적용하여 N키 UI 제어와 분리 관리 처리.
*   **[Phase 4] 플레이어 간 아이템 거래**: `PlayerTradeSystem`을 도입하여 서버에 접속한 플레이어의 아이디를 직접 조회하고, 1:1로 아이템을 올리고 안전하게 교환하는 네트워킹 확립.

### 🚀 향후 제작 진행 파트 (Next Roadmap)
*   **[Phase 5: 시각적 피드백 통합]**: 화면 위로 데미지가 떠오르는 **플로팅 데미지 텍스트(Floating Damage UI)** 연동. 직업별로 새로 제작될 64비트 아케이드풍 파티클을 Inspector에 추가/세팅.
*   **[Phase 6: 필드 아이템화 (Looting)]**: 무조건 인벤토리로 습득되는 현재 드롭 방식을 교체하여, 아이템이 필드 3D 맵에 (`FieldItem`) 떨어지고 F키를 눌러 상호작용하는 체계 완성.
*   **[Phase 7: 게임 모드화 (Round Manager 고도화)]**: 인간 vs 좀비 진형을 나누어 숙주 좀비 감염 등의 라운드 기반 규칙 정립.

---

## 3. 핵심 스크립트 정리 및 계층 연관 관계

아래는 `Assets/Script/` 내부의 폴더 및 스크립트를 기능군으로 묶은 것입니다. (S.O.L.I.D 원칙 지향)

### 📊 [Data / Manager 계층] - 데이터 및 영구 저장소
*   `Data/ItemData.cs`: 아이템 정의 레코드.
*   `Manager/ItemDatabase.cs`: 하드코딩 탈피의 핵심. 게임 런타임 시작 시 엑셀 CSV(`ItemDatabase.csv`, `RecipeDatabase.csv`) 파일 두 개를 로드 및 파싱하여 각 스크립트들에게 글로벌 정보를 제공합니다.
*   `Manager/CsvDatabaseManager.cs`: 플레이어의 레벨, 획득한 아이템 등을 컴퓨터 내 로컬 스토리지(`PlayerData.csv`)에 영구적으로 물리 저장.

### 🏃‍♂️ [Player / Movement 계층] - 이동 및 네트워크 렌더링
*   `InputHandle.cs` & `PlayerMovement.cs`: 물리 처리. 사용자의 키보드 값을 입력받아 Character Controller상에 반영.
*   `OwnerNetworkAnimator.cs` & `OwnerNetworkTransform.cs`: NGO의 호스트/클라이언트 권한 구조 아래서, 부드럽고 딜레이 없는 위치 및 애니메이션 정보 동기화.

### ⚔️ [Player / RPG 계층] - 전투 루프 및 직업, 스탯 코어
*   `CombatSystem.cs`: 최상위 무력 충돌 통제 계열. 
*   `ISkillExecutor.cs` & `직업이름+SkillExecutor.cs`: CombatSystem의 명령을 받아 실제로 어떤 이펙트나 데미지를 뽑아낼지 처리합니다. 
*   `IDamageable.cs` & `PlayerState.cs`: 타격을 입었을 때 HP 조율/감염 등을 분배. 
*   `PlayerExperience.cs` & `PlayerClass.cs`: 레벨/클래스 전직 및 경험치 루틴 제어.

### 🎒 [Player / Item 계층] - 아이템 로직 (인벤토리/거래)
*   `InventorySystem.cs`: 데이터를 들고 있는 물리적 인벤토리 매니저. 서버권한(ServerRpc)에 의해 검증된 후 동작.
*   `EquipmentSystem.cs`: 인벤토리에서 넘어온 장비를 활성화.
*   `PlayerTradeSystem.cs`: 다른 플레이어 클라이언트를 색인하고, 교환용 데이터를 동기화 한 뒤 조건(양측 레디) 충족 시 서버측에서 물물교환 처리.

### 🖥️ [UI 계층] - 프론트엔드 (화면 노출)
*   `UIBuilder.cs`: 코드 기반에서 화면 레이아웃 박스나 폰트를 동적으로 그리는 에디터 유틸리티.
*   `UIGameHUD.cs`: 런타임 상에서 그려진 화면(기초 박스 및 텍스트)을 관리.
*   `InventoryUIController.cs`: 방대한 UIGameHUD를 단일 시스템이 조작하는 복잡성을 떨어뜨리기 위해 분리된 UI 상태 머신. (이 클래스는 MonoBehaviour를 상속하지 않는 순수 C# 디자인으로 제작됨)

---

## 4. 데이터 플로우 및 처리 체계 방식

1.  **네트워크 클라이언트 사이드 예측 (Client Auth)**:
    이동과 애니메이션 갱신은 내가 버튼을 눌렀을 때 렉이 발생하지 않도록 **Client가 우선 결정**하고 서버가 중계합니다.
2.  **보안 및 검증 체계 (Server Auth)**:
    아이템을 먹고, 사용하고, 강화하고, 데미지를 주는 로직들은 인가되지 않은 해킹 조작을 막기 위해 철저히 **ServerRpc**를 타고 호스트가 처리한 다음 다시 쏘아줍니다. (NGO 특성 적용)
3.  **데이터 주도 (Data-Driven Workflow)**:
    `EquipmentSystem`, `InventorySystem`, `CombatSystem` 등은 내부에 하드코딩된 변수 값을 들고 있지 않습니다. 수정은 오직 제공된 CSV(엑셀) 파일만 건드리면 게임 전체의 장비 능력과 확률, 쿨타임이 바뀌도록 설계하여 S.O.L.I.D의 OCP(개방 폐쇄 원칙)를 관철합니다.
