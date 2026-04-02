# CSS_RPG: 아키텍처 및 상세 개발 로드맵 (개정판)

본 문서는 최근 완료된 기반 아키텍처 리팩토링(Editor 룰 대응 및 ISkillExecutor 추상화)을 반영하여, 앞으로 프로젝트를 어떤 구조로 확장해 나갈 것인지 단계별로 **가장 자세하게 정리한 최신 로드맵**입니다. 

---

## ✅ 1. 최근 완료된 개선 작업 (Completed)
- **Editor 위반 사항 수정**: 금지되었던 `[MenuItem]` 로직을 완전히 제거하고 S.O.L.I.D (단일 책임) 관점에 맞추어 `PlayerStateEditor` (CustomEditor) 인스펙터 구조로 성공적 이전 완료.
- **의존성 역전 및 개방-폐쇄(DIP/OCP)**: 전투 시스템(`CombatSystem`)에서 직접적으로 `FighterSkillExecutor`를 호출하던 구조를 버리고 `ISkillExecutor` 인터페이스를 통해 다형성으로 실행하도록 기반 분리 완료.
- **네트워크 레거시 제거**: 구형 `[ServerRpc(RequireOwnership=false)]` 구문을 NGO 최신 규격인 `[Rpc(InvokePermission.Everyone)]` 으로 일괄 전환하여 안전성 확보.

---

## 🚀 2. 상세 로드맵 (Actionable Roadmap)

앞으로의 개발은 아래의 세부 페이즈(Phase) 순서로 진행하는 것을 강력히 권장합니다. 각 단계 안에는 S.O.L.I.D 원칙을 지키기 위한 핵심 과제들이 담겨 있습니다.

### Phase 1: 기반 아키텍처 고도화 (Deepening the Core Mechanics)
가장 시급하며, 다른 콘텐츠들을 무한정 찍어내기 전에 완료해야 하는 뼈대 작업들입니다.

*   [x] **1-1. 타격 판정의 추상화 (`IDamageable` 도입) - 방금 완료!**
    *   **완료 내역**: 모든 피격 가능 객체가 상속받는 `IDamageable` 인터페이스를 정의했습니다. `CombatSystem`과 `FighterSkillExecutor`가 하드코딩된 `PlayerState` 대신 `IDamageable`을 찾아서 의존하도록 전부 교체 완료했습니다.
*   [x] **1-2. 전투 상태 머신 (FSM) 적용 - 방금 완료!**
    *   **완료 내역**: `CombatState` 열거형(Idle, BasicAttacking, SkillCasting, SkillExecuting, Stunned, Dead)을 신설하고 `CombatSystem`에서 중앙 관리하도록 수정. 단일 `isUsingSkill` boolean을 완전히 없애고 공격, 스킬 시전, 그리고 넉백 같은 피격 시 `Stunned` 등으로 상태 락(Lock)이 걸리도록 완벽한 FSM 구조 연결.
*   [ ] **1-3. 시각 / 사운드 효과 통합 (SFX + VFX)**
    *   스킬별 발동 효과음(오디오 클립)을 `SkillData` CSV에 연동하거나 매니저로 제어하여 64비트 도트 아케이드 감성을 후각/청각적으로 완성.

### Phase 2: 콘텐츠 수평 확장 (Horizontal Expansion - Classes & Skills)
Phase 1의 코어 뼈대가 완성되면 직업과 캐릭터수를 안전하게 복사/확장할 수 있습니다.

*   [x] **2-1. 타 직업 스킬 실행기 구현 (Mage / Swordman 등) - 방금 완료!**
    *   **완료 내역**: `ISkillExecutor`를 상속받는 `SwordsmanSkillExecutor`, `MageSkillExecutor` 스크립트를 새롭게 구현했습니다. 발도술(집중타격) 및 블리자드(다단장판) 같은 테스트용 코드를 내장했으며, `PlayerClass`에서 직업이 변경되면 `CombatSystem`이 이를 감지해 자동으로 기존 실행기를 파괴하고 새 실행기를 부착하도록 동적 바인딩 시스템을 구축했습니다.
*   [ ] **2-2. 신규 이펙트 추가 파이프라인 정립**
    *   무투가에 적용했던 것과 동일하게, 다른 직업들도 아케이드형 도트 스프라이트를 제작.
    *   `PlayerStateEditor`를 확장하여, 직업별 이펙트를 클릭 한 번에 Generate & Assign 하는 유틸리티 기능 강화.

### Phase 3: 게임 루프 및 UI/UX 시각화 (Game Loop & Front-End)
멀티플레이어 액션이 눈으로 명확히 보이게 프론트엔드를 구성하는 단계입니다.

*   [ ] **3-1. 실시간 전투 피드백 UI 연동**
    *   머리 위의 둥둥 떠다니는 데미지 텍스트(Floating Damage) 구현 (ClientRpc 타격 위치 반영).
    *   현재 백그라운드로만 돌아가는 스킬 쿨타임(`SkillSystem.currentCooldown`)을 화면 하단의 `UIGameHUD`의 슬롯 아이콘 위에 텍스트와 음영으로 실시간 표출.
*   [ ] **3-2. RoundManager 고도화 (인간 vs 좀비 룰 정립)**
    *   이벤트 시스템 연동: 누군가 죽으면(`OnAnyZombieDied`) 아이템이 떨어지거나 경험치가 오르게 연동.
    *   좀비 감염, 숙주 선정, 라운드 시간 타이머를 화면 상단 UI에 동기화 브로드캐스트.

### Phase 4: 인벤토리 및 엑셀(CSV) 기반 아이템 시스템 (Inventory & Items)
게임의 성장 요소와 파밍을 제공하는 시스템으로, 기획의 확장성과 유지보수 편의를 위해 데이터 주도(Data-Driven) 구조로 설계합니다. (Phase 3의 UI 작업과 맞물리거나 그 직후에 진행하는 것을 최우선으로 권장힙니다.)

*   [ ] **4-1. 엑셀(CSV) 기반 데이터베이스 구축 (Data-Driven Items)**
    *   **계획**: 스킬을 엑셀로 관리했듯, 아이템(ID, 이름, 아이콘, 획득 시 효과, 드롭 확률 등)을 엑셀 파일(CSV 형태)로 먼저 정리합니다.
    *   런타임 시 스크립트로 엑셀 파일을 읽어와 파싱한 뒤 인게임 `ItemData`로 자동 전환하게 합니다. 유니티를 열지 않고도 엑셀만 수정하면 시스템이 반영되는 구조입니다.
*   [ ] **4-2. 아이템 드롭 루팅 체계 (Looting System)**
    *   Phase 1-1에서 구축한 `IDamageable` 오브젝트(좀비, 보스, 상자 등)가 죽으면, 서버에서 엑셀 기반의 확률표를 굴려 필드 위에 3D 아이템 프리팹을 뿌립니다.
    *   플레이어와 Trigger 충돌 혹은 상호작용(F키)을 통해 아이템의 NetworkOwnership을 넘겨받아 획득 처리를 합니다.
*   [ ] **4-3. MVC 기반 인벤토리 슬롯 동기화**
    *   `InventorySystem`에서 획득한 데이터를 배열/딕셔너리로 들고 있고, `UIInventory`가 이걸 바라보고 자동으로 슬롯 이미지를 갱신하도록 설계합니다.

---

## 3. 요약: 다음 작업 제안 (What to do Next?)
가장 근간이 되는 아키텍처 작업인 **Phase 1-1(IDamageable)** 및 **Phase 1-2(FSM 도입)** 에 이어서 직업 확장 프레임워크인 **Phase 2-1**까지 모두 성공적으로 완료되었습니다! 이제 전직만 하면 전용 스킬이 나가는 다중 클래스 RPG 구조가 성립되었습니다.

다음으로 진행하실 개발 목표를 아래 중에서 골라주세요!
1. **[Phase 4. 인벤토리 및 엑셀(CSV) 시스템]**: 죽은 대상이 엑셀에 정의된 데이터 확률에 따라 아이템 프리팹을 실시간으로 드롭하고 이를 루팅하는 인벤토리 기틀 잡기 (가장 강력히 추천)
2. **[Phase 2-2. 신규 이펙트 추가 파이프라인]**: 방금 완성한 검사/마법사를 위한 아케이드 도트풍 이펙트를 배정하고 인스펙터 자동화 스크립트 기능 추가
3. **[Phase 3-1. 게임 UI 실시간 연동]**: HUD 하단의 스킬 슬롯 동기화 및 적 타격 시 발생하는 데미지 플로팅 텍스트(Floating Damage) 시각화
