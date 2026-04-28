# Graph Report - C:\Users\User\OneDrive - 제주대학교\바탕 화면\취미\유니티\게임\CSS_RPG\Assets\Script  (2026-04-28)

## Corpus Check
- 42 files · ~59,421 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 462 nodes · 922 edges · 23 communities detected
- Extraction: 70% EXTRACTED · 30% INFERRED · 0% AMBIGUOUS · INFERRED: 272 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- [[_COMMUNITY_Community 0|Community 0]]
- [[_COMMUNITY_Community 1|Community 1]]
- [[_COMMUNITY_Community 2|Community 2]]
- [[_COMMUNITY_Community 3|Community 3]]
- [[_COMMUNITY_Community 4|Community 4]]
- [[_COMMUNITY_Community 5|Community 5]]
- [[_COMMUNITY_Community 6|Community 6]]
- [[_COMMUNITY_Community 7|Community 7]]
- [[_COMMUNITY_Community 8|Community 8]]
- [[_COMMUNITY_Community 9|Community 9]]
- [[_COMMUNITY_Community 10|Community 10]]
- [[_COMMUNITY_Community 11|Community 11]]
- [[_COMMUNITY_Community 12|Community 12]]
- [[_COMMUNITY_Community 13|Community 13]]
- [[_COMMUNITY_Community 14|Community 14]]
- [[_COMMUNITY_Community 15|Community 15]]
- [[_COMMUNITY_Community 16|Community 16]]
- [[_COMMUNITY_Community 17|Community 17]]
- [[_COMMUNITY_Community 18|Community 18]]
- [[_COMMUNITY_Community 19|Community 19]]
- [[_COMMUNITY_Community 20|Community 20]]
- [[_COMMUNITY_Community 21|Community 21]]
- [[_COMMUNITY_Community 22|Community 22]]

## God Nodes (most connected - your core abstractions)
1. `InventorySystem` - 27 edges
2. `CombatSystem` - 24 edges
3. `FighterSkillExecutor` - 21 edges
4. `NetworkManagerUI` - 20 edges
5. `EquipmentSystem` - 19 edges
6. `PlayerTradeSystem` - 18 edges
7. `PlayerHealth` - 18 edges
8. `PaladinSkillExecutor` - 16 edges
9. `RoundManager` - 15 edges
10. `SkillSystem` - 14 edges

## Surprising Connections (you probably didn't know these)
- `CsvDatabase` --inherits--> `MonoBehaviour`  [EXTRACTED]
  C:\Users\User\OneDrive - 제주대학교\바탕 화면\취미\유니티\게임\CSS_RPG\Assets\Script\Manager\CsvDatabaseManager.cs →   _Bridges community 6 → community 10_
- `ItemDatabase` --inherits--> `MonoBehaviour`  [EXTRACTED]
  C:\Users\User\OneDrive - 제주대학교\바탕 화면\취미\유니티\게임\CSS_RPG\Assets\Script\Manager\ItemDatabase.cs →   _Bridges community 10 → community 3_
- `BaseSkillExecutor` --inherits--> `MonoBehaviour`  [EXTRACTED]
  C:\Users\User\OneDrive - 제주대학교\바탕 화면\취미\유니티\게임\CSS_RPG\Assets\Script\Player\RPG\BaseSkillExecutor.cs →   _Bridges community 10 → community 1_
- `CombatSystem` --inherits--> `MonoBehaviour`  [EXTRACTED]
  C:\Users\User\OneDrive - 제주대학교\바탕 화면\취미\유니티\게임\CSS_RPG\Assets\Script\Player\RPG\CombatSystem.cs →   _Bridges community 10 → community 4_
- `SkillSystem` --inherits--> `MonoBehaviour`  [EXTRACTED]
  C:\Users\User\OneDrive - 제주대학교\바탕 화면\취미\유니티\게임\CSS_RPG\Assets\Script\Player\RPG\SkillSystem.cs →   _Bridges community 10 → community 8_

## Communities

### Community 0 - "Community 0"
Cohesion: 0.05
Nodes (14): EquipmentSystem, IInventoryUIState, CraftingUIState, CSS_RPG.UI, DebugUIState, EquipmentUIState, InventoryUIState, MainUIState (+6 more)

### Community 1 - "Community 1"
Cohesion: 0.13
Nodes (4): BaseSkillExecutor, FighterSkillExecutor, ISkillExecutor, PaladinSkillExecutor

### Community 2 - "Community 2"
Cohesion: 0.06
Nodes (8): LobbyUIBuilder, LobbyUIElements, NetworkBehaviour, NetworkManagerUI, PlayerAnimation, PlayerAuthentication, PlayerCamera, PlayerLifecycleManager

### Community 3 - "Community 3"
Cohesion: 0.1
Nodes (2): InventorySystem, ItemDatabase

### Community 4 - "Community 4"
Cohesion: 0.11
Nodes (4): BaseSkillExecutor, CombatSystem, MageSkillExecutor, SwordsmanSkillExecutor

### Community 5 - "Community 5"
Cohesion: 0.09
Nodes (5): IDamageable, PlayerHealth, PlayerVFXController, DamageAccumulator, ServerConsole

### Community 6 - "Community 6"
Cohesion: 0.1
Nodes (7): CsvDatabase, LocalUserData, UserData, InventorySlot, ItemData, RecipeData, PlayerExperience

### Community 7 - "Community 7"
Cohesion: 0.14
Nodes (2): PlayerState, RoundManager

### Community 8 - "Community 8"
Cohesion: 0.13
Nodes (3): InventoryUIController, SkillData, SkillSystem

### Community 9 - "Community 9"
Cohesion: 0.22
Nodes (1): PlayerTradeSystem

### Community 10 - "Community 10"
Cohesion: 0.17
Nodes (3): InputHandle, MonoBehaviour, UIGameHUDRuntime

### Community 11 - "Community 11"
Cohesion: 0.26
Nodes (1): PlayerClass

### Community 12 - "Community 12"
Cohesion: 0.31
Nodes (1): PlayerMovement

### Community 13 - "Community 13"
Cohesion: 0.39
Nodes (1): AutoPlayerSetup

### Community 14 - "Community 14"
Cohesion: 0.29
Nodes (2): Stat, StatModifier

### Community 15 - "Community 15"
Cohesion: 0.29
Nodes (2): CSS_RPG.UI, IInventoryUIState

### Community 16 - "Community 16"
Cohesion: 0.47
Nodes (2): Editor, PlayerStateEditor

### Community 17 - "Community 17"
Cohesion: 0.5
Nodes (2): NetworkAnimator, OwnerNetworkAnimator

### Community 18 - "Community 18"
Cohesion: 0.5
Nodes (2): NetworkTransform, OwnerNetworkTransform

### Community 19 - "Community 19"
Cohesion: 0.5
Nodes (1): IDamageable

### Community 20 - "Community 20"
Cohesion: 0.5
Nodes (1): ISkillExecutor

### Community 21 - "Community 21"
Cohesion: 0.67
Nodes (1): FixPlayerPrefab

### Community 22 - "Community 22"
Cohesion: 1.0
Nodes (0): 

## Knowledge Gaps
- **10 isolated node(s):** `InventorySlot`, `UserData`, `LocalUserData`, `SkillData`, `StatModifier` (+5 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **Thin community `Community 22`** (1 nodes): `CombatState.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `CombatSystem` connect `Community 4` to `Community 1`, `Community 5`, `Community 7`, `Community 10`, `Community 11`?**
  _High betweenness centrality (0.088) - this node is a cross-community bridge._
- **Why does `InventorySystem` connect `Community 3` to `Community 0`, `Community 2`?**
  _High betweenness centrality (0.086) - this node is a cross-community bridge._
- **Why does `NetworkManagerUI` connect `Community 2` to `Community 10`?**
  _High betweenness centrality (0.081) - this node is a cross-community bridge._
- **What connects `InventorySlot`, `UserData`, `LocalUserData` to the rest of the system?**
  _10 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.05 - nodes in this community are weakly interconnected._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.13 - nodes in this community are weakly interconnected._
- **Should `Community 2` be split into smaller, more focused modules?**
  _Cohesion score 0.06 - nodes in this community are weakly interconnected._