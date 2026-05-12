# Graph Report - Assets\Script  (2026-05-06)

## Corpus Check
- 45 files · ~27,409 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 995 nodes · 1836 edges · 44 communities (23 shown, 21 thin omitted)
- Extraction: 81% EXTRACTED · 19% INFERRED · 0% AMBIGUOUS · INFERRED: 345 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `5d1fec9c`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

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
- [[_COMMUNITY_Community 23|Community 23]]
- [[_COMMUNITY_Community 24|Community 24]]
- [[_COMMUNITY_Community 25|Community 25]]
- [[_COMMUNITY_Community 26|Community 26]]
- [[_COMMUNITY_Community 27|Community 27]]
- [[_COMMUNITY_Community 28|Community 28]]
- [[_COMMUNITY_Community 29|Community 29]]
- [[_COMMUNITY_Community 30|Community 30]]
- [[_COMMUNITY_Community 31|Community 31]]
- [[_COMMUNITY_Community 32|Community 32]]
- [[_COMMUNITY_Community 33|Community 33]]
- [[_COMMUNITY_Community 34|Community 34]]
- [[_COMMUNITY_Community 35|Community 35]]
- [[_COMMUNITY_Community 36|Community 36]]
- [[_COMMUNITY_Community 37|Community 37]]
- [[_COMMUNITY_Community 38|Community 38]]
- [[_COMMUNITY_Community 39|Community 39]]
- [[_COMMUNITY_Community 40|Community 40]]
- [[_COMMUNITY_Community 41|Community 41]]

## God Nodes (most connected - your core abstractions)
1. `CombatSystem` - 35 edges
2. `InventorySystem` - 30 edges
3. `InventorySystem` - 28 edges
4. `ServerConsole` - 26 edges
5. `CombatSystem` - 25 edges
6. `PlayerHealth` - 23 edges
7. `EquipmentSystem` - 22 edges
8. `FighterSkillExecutor` - 21 edges
9. `SkillSystem` - 21 edges
10. `NetworkManagerUI` - 21 edges

## Surprising Connections (you probably didn't know these)
- `ItemData` --references--> `int`  [EXTRACTED]
  Data/ItemData.cs → UI/InventoryStates/InventoryUIStates.cs
- `ItemData` --references--> `string`  [EXTRACTED]
  Data/ItemData.cs → UI/ServerConsole.cs
- `ItemData` --references--> `ItemType`  [EXTRACTED]
  Data/ItemData.cs → Player/RPG/Data/ItemDataSO.cs
- `ItemData` --references--> `ItemRarity`  [EXTRACTED]
  Data/ItemData.cs → Player/RPG/Data/ItemDataSO.cs
- `ItemData` --references--> `StatType`  [EXTRACTED]
  Data/ItemData.cs → Player/RPG/Data/ItemDataSO.cs

## Communities (44 total, 21 thin omitted)

### Community 0 - "Community 0"
Cohesion: 0.06
Nodes (8): BaseSkillExecutor, CombatSystem, FighterSkillExecutor, ISkillExecutor, MageSkillExecutor, PaladinSkillExecutor, PlayerMovement, SwordsmanSkillExecutor

### Community 1 - "Community 1"
Cohesion: 0.05
Nodes (7): EquipmentSystem, InventorySystem, CraftingUIState, ItemDatabase, Stat, StatModifier, StatSystem

### Community 2 - "Community 2"
Cohesion: 0.05
Nodes (11): InventoryUIController, CSS_RPG.UI, DebugUIState, EquipmentUIState, MainUIState, TradeInventorySelectUIState, TradeSearchUIState, TradeSessionUIState (+3 more)

### Community 3 - "Community 3"
Cohesion: 0.05
Nodes (12): CsvDatabase, LocalUserData, UserData, InventorySlot, ItemData, RecipeData, PlayerHealth, PlayerState (+4 more)

### Community 4 - "Community 4"
Cohesion: 0.08
Nodes (9): BaseSkillExecutor, Camera, PlayerState, PlayerVFXController, BaseSkillExecutor, FighterSkillExecutor, MageSkillExecutor, PaladinSkillExecutor (+1 more)

### Community 5 - "Community 5"
Cohesion: 0.06
Nodes (5): LobbyUIBuilder, LobbyUIElements, NetworkManagerUI, PlayerAuthentication, PlayerExperience

### Community 6 - "Community 6"
Cohesion: 0.05
Nodes (15): Animator, CharacterController, CombatSystem, IDamageable, InputHandle, List<SlowData>, PlayerAnimation, PlayerCamera (+7 more)

### Community 7 - "Community 7"
Cohesion: 0.06
Nodes (12): IInventoryUIState, CraftingUIState, CSS_RPG.UI, DebugUIState, EquipmentUIState, InventoryUIState, MainUIState, TradeInventorySelectUIState (+4 more)

### Community 8 - "Community 8"
Cohesion: 0.06
Nodes (7): Dictionary, InputHandle, CsvDatabase, ItemDatabase, MonoBehaviour, StatSystem, InventoryUIController

### Community 9 - "Community 9"
Cohesion: 0.07
Nodes (12): Button, Dropdown, GameObject, InputField, CSS_RPG.UI, InventoryView, Queue, ScrollRect (+4 more)

### Community 10 - "Community 10"
Cohesion: 0.11
Nodes (4): PlayerClass, PlayerHealth, SkillSystem, UIGameHUDRuntime

### Community 19 - "Community 19"
Cohesion: 0.17
Nodes (3): PlayerLifecycleManager, PlayerAuthentication, UserData

### Community 20 - "Community 20"
Cohesion: 0.17
Nodes (10): InventorySlot, RecipeData, RecipeMaterial, int, LocalUserData, UserData, LootDropData, string (+2 more)

### Community 21 - "Community 21"
Cohesion: 0.16
Nodes (7): bool, FixPlayerPrefab, float, object, SkillData, Stat, StatModifier

### Community 22 - "Community 22"
Cohesion: 0.15
Nodes (7): RecipeDataSO, ItemDataSO, ItemDataSO, List, RecipeDataSO, RecipeMaterial, ScriptableObject

### Community 23 - "Community 23"
Cohesion: 0.25
Nodes (3): Editor, PlayerStateEditor, PlayerStateEditor

### Community 24 - "Community 24"
Cohesion: 0.18
Nodes (3): LootDrop, NetworkVariable, PlayerExperience

### Community 25 - "Community 25"
Cohesion: 0.27
Nodes (7): ItemData, ItemDataSO, ItemRarity, ItemSlot, ItemType, Sprite, StatType

### Community 30 - "Community 30"
Cohesion: 0.29
Nodes (3): OwnerNetworkAnimator, NetworkAnimator, OwnerNetworkAnimator

### Community 31 - "Community 31"
Cohesion: 0.33
Nodes (3): NetworkBehaviour, PlayerLifecycleManager, PlayerLifecycleManager

### Community 33 - "Community 33"
Cohesion: 0.29
Nodes (3): OwnerNetworkTransform, NetworkTransform, OwnerNetworkTransform

## Knowledge Gaps
- **33 isolated node(s):** `InventorySystem`, `PlayerMovement`, `Animator`, `OwnerNetworkAnimator`, `Vector3` (+28 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **21 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `int` connect `Community 20` to `Community 4`, `Community 6`, `Community 7`, `Community 8`, `Community 9`, `Community 12`, `Community 18`, `Community 22`, `Community 25`?**
  _High betweenness centrality (0.114) - this node is a cross-community bridge._
- **Why does `SkillSystem` connect `Community 10` to `Community 4`, `Community 6`, `Community 7`, `Community 8`, `Community 21`?**
  _High betweenness centrality (0.068) - this node is a cross-community bridge._
- **Why does `CombatSystem` connect `Community 11` to `Community 0`, `Community 4`, `Community 6`, `Community 8`, `Community 10`, `Community 15`, `Community 21`?**
  _High betweenness centrality (0.067) - this node is a cross-community bridge._
- **What connects `InventorySystem`, `PlayerMovement`, `Animator` to the rest of the system?**
  _33 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.06 - nodes in this community are weakly interconnected._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.05 - nodes in this community are weakly interconnected._
- **Should `Community 2` be split into smaller, more focused modules?**
  _Cohesion score 0.05 - nodes in this community are weakly interconnected._