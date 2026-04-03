# 프로젝트 클래스 다이어그램 (Class Diagram)

이 문서는 **CSS_RPG** 프로젝트의 주요 클래스 구조와 관계를 Mermaid 클래스 다이어그램으로 시각화한 것입니다. 프로젝트의 확장성과 유지보수를 위해 객체지향 설계 원칙(SOLID)이 적용된 구조를 확인하실 수 있습니다.

## 1. 핵심 플레이어 아키텍처 (Core Player Architecture)

플레이어 캐릭터는 여러 컴포넌트의 조합으로 구성되며, 각 컴포넌트는 독립적인 책임을 가집니다.

![핵심 아키텍처 다이어그램](file:///c:/Users/User/OneDrive%20-%20%EC%A0%9C%EC%A3%BC%EB%8C%80%ED%95%99%EA%B5%90/%EB%B0%94%ED%83%95%20%ED%99%94%EB%A9%B4/%EC%B7%A8%EB%AF%B8/%EC%9C%A0%EB%8B%88%ED%8B%B0/%EA%B2%8C%EC%9E%84/CSS_RPG/Assets/Document/Images/CoreArchitecture.png)

<details>
<summary>Mermaid 소스 코드 보기</summary>

```mermaid
classDiagram
    class NetworkBehaviour { <<Unity>> }
    class MonoBehaviour { <<Unity>> }
    class IDamageable { <<Interface>> }

    class PlayerState {
        +NetworkVariable currentTeam
        +NetworkVariable currentHealth
        +TakeDamage(amount, killerId)
        +HealServerRpc(amount)
    }

    class PlayerMovement {
        +float walkSpeed
        +float runSpeed
        +ApplyForcedMovement(velocity, duration)
        +ApplySlowClientRpc(ratio, duration)
    }

    class PlayerClass {
        +NetworkVariable currentClass
        +NetworkVariable awakeningLevel
        +ChangeClass(newClass)
        +SetAwakening(level)
    }

    class StatSystem {
        +Dictionary stats
        +GetStat(type)
        +AddModifier(type, mod)
    }

    class SkillSystem {
        +SkillData[] currentSkills
        +TryUseSkill(index)
        +LoadSkillsForClass(classType)
    }

    class CombatSystem {
        +CombatState CurrentState
        +ExecuteSkillAttack(index)
        +CalculateDamage(multiplier, target)
    }

    NetworkBehaviour <|-- PlayerState
    NetworkBehaviour <|-- PlayerMovement
    NetworkBehaviour <|-- PlayerClass
    MonoBehaviour <|-- StatSystem
    MonoBehaviour <|-- SkillSystem
    MonoBehaviour <|-- CombatSystem
    IDamageable <|.. PlayerState

    CombatSystem --> PlayerState : Reference
    CombatSystem --> PlayerClass : Reference
    CombatSystem --> StatSystem : Reference
    CombatSystem --> SkillSystem : Reference
    SkillSystem --> PlayerClass : Reference
    PlayerMovement --> CombatSystem : Check UsingSkill
```
</details>

---

## 2. 전투 시스템 및 스킬 전략 (Combat & Skill Strategy)

전투 시스템은 `ISkillExecutor` 인터페이스를 통해 직업별 스킬 로직을 분리하여 구현하였습니다 (OCP, DIP 적용).

![전투 전략 다이어그램](file:///c:/Users/User/OneDrive%20-%20%EC%A0%9C%EC%A3%BC%EB%8C%80%ED%95%99%EA%B5%90/%EB%B0%94%ED%83%95%20%ED%99%94%EB%A9%B4/%EC%B7%A8%EB%AF%B8/%EC%9C%A0%EB%8B%88%ED%8B%B0/%EA%B2%8C%EC%9E%84/CSS_RPG/Assets/Document/Images/CombatStrategy.png)

<details>
<summary>Mermaid 소스 코드 보기</summary>

```mermaid
classDiagram
    class ISkillExecutor {
        <<Interface>>
        +Initialize(combat, state)
        +ExecuteSkill(index, skill)
    }

    class CombatSystem {
        -ISkillExecutor currentSkillExecutor
        +UpdateSkillExecutor()
    }

    class FighterSkillExecutor {
        +ExecuteSkill(index, skill)
    }

    class PaladinSkillExecutor {
        +int ShieldEnergy
        +AddShieldEnergy(amount)
        +ExecuteSkill(index, skill)
    }

    class MageSkillExecutor { }
    class SwordsmanSkillExecutor { }

    ISkillExecutor <|.. FighterSkillExecutor
    ISkillExecutor <|.. PaladinSkillExecutor
    ISkillExecutor <|.. MageSkillExecutor
    ISkillExecutor <|.. SwordsmanSkillExecutor
    
    CombatSystem --> ISkillExecutor : Delegates Skills
    MonoBehaviour <|-- FighterSkillExecutor
    MonoBehaviour <|-- PaladinSkillExecutor
```
</details>

---

## 3. 데이터 모델 및 관리 (Data Models & Management)

관통력, 치명타 등 복잡한 스탯 연산과 CSV 기반 데이터 관리를 담당하는 구조입니다.

```mermaid
classDiagram
    class Stat {
        +float BaseValue
        +float Value
        -List modifiers
        +AddModifier(mod)
    }

    class StatModifier {
        +float Value
        +bool IsMultiplicative
        +object Source
    }

    class SkillData {
        +string skillName
        +float cooldownTime
        +float damageMultiplier
        +bool isReady
    }

    class StatSystem {
        +Dictionary stats
    }

    class SkillSystem {
        +SkillData[] currentSkills
    }

    StatSystem *-- Stat : Contains
    SkillSystem *-- SkillData : Contains
    Stat o-- StatModifier : Managed By

    class RoundManager {
        +List players
        +RegisterPlayer(player)
    }

    class CsvDatabaseManager {
        +SaveUser(data)
        +LoadUser(id)
    }

    MonoBehaviour <|-- RoundManager
    MonoBehaviour <|-- CsvDatabaseManager
```

---

## 4. UI 시스템 (UI System)

코드 기반 UI 생성(`UIBuilder`)과 실시간 정보 업데이트(`UIGameHUD`)를 담당합니다.

```mermaid
classDiagram
    class UIBuilder {
        +CreateGameHUD(pClass)
        +CreateInventory()
    }

    class UIGameHUD {
        +UpdateSkillUI(index, name, cd, maxCd)
        +UpdateClassName(name)
    }

    class UISettingsManager { }

    MonoBehaviour <|-- UIGameHUD
    MonoBehaviour <|-- UISettingsManager
    UIGameHUD --> UIBuilder : Uses for Creation
```
