# Graph Report - Pixamor  (2026-10-31)

## Corpus Check
- Corpus is ~12,950 words - fits in a single context window. You may not need a graph.

## Summary
- 742 nodes · 1351 edges · 65 communities (33 shown, 32 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 24 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Cluster_0
- Cluster_1
- Cluster_2
- Cluster_3
- Cluster_4
- Cluster_5
- Cluster_6
- Cluster_7
- Cluster_8
- Cluster_9
- Cluster_11
- Cluster_12
- Cluster_13
- Cluster_14
- Cluster_15
- Cluster_16
- Cluster_17
- Cluster_18
- Cluster_19
- Cluster_20
- Cluster_21
- Cluster_22
- Cluster_23
- Cluster_24
- Cluster_25
- Cluster_26
- Cluster_27
- Cluster_28
- Cluster_29
- Cluster_30
- Cluster_31
- Cluster_32
- Cluster_33
- Cluster_34
- Cluster_35
- Cluster_36
- Cluster_37
- Cluster_38
- Cluster_39
- Cluster_40
- Cluster_41
- Cluster_42
- Cluster_43
- Cluster_44
- Cluster_45
- Cluster_46
- Cluster_47
- Cluster_48
- Cluster_49
- Cluster_50
- Cluster_51
- Cluster_52
- Cluster_53
- Cluster_54
- Cluster_55
- Cluster_56
- Cluster_57
- Cluster_58
- Cluster_59
- Cluster_60
- Cluster_61
- Cluster_62
- Cluster_63
- Cluster_64

## God Nodes (most connected - your core abstractions)
1. `ClimateStateManager` - 49 edges
2. `DeskPuzzleController` - 47 edges
3. `CountryNode` - 44 edges
4. `DialogueAndChoiceSystem` - 24 edges
5. `ProblemType` - 22 edges
6. `Greybox` - 22 edges
7. `SceneAutoBuilder` - 20 edges
8. `Pixamor.COP31` - 16 edges
9. `ToggleSwitch` - 16 edges
10. `HudController` - 15 edges

## Surprising Connections (you probably didn't know these)
- `ClimateStateManager` --references--> `GameState`  [EXTRACTED]
  Assets/Scripts/COP31/ClimateStateManager.cs → Assets/Scripts/COP31/ClimateTypes.cs
- `ClimateStateManager` --references--> `CountryNode`  [EXTRACTED]
  Assets/Scripts/COP31/ClimateStateManager.cs → Assets/Scripts/COP31/CountryNode.cs
- `CountryNode` --references--> `ProblemType`  [EXTRACTED]
  Assets/Scripts/COP31/CountryNode.cs → Assets/Scripts/COP31/ClimateTypes.cs
- `PuzzleRequest` --references--> `CountryNode`  [EXTRACTED]
  Assets/Scripts/COP31/ClimateTypes.cs → Assets/Scripts/COP31/CountryNode.cs
- `DeskPuzzleController` --references--> `PuzzleRequest`  [EXTRACTED]
  Assets/Scripts/COP31/DeskPuzzleController.cs → Assets/Scripts/COP31/ClimateTypes.cs

## Import Cycles
- None detected.

## Communities (65 total, 32 thin omitted)

### Community 0 - "Cluster_0"
Cohesion: 0.08
Nodes (11): Greybox, Font, UIBar, HudController, Instance, PressButton, SceneAutoBuilder, ToggleSwitch (+3 more)

### Community 1 - "Cluster_1"
Cohesion: 0.07
Nodes (12): HoldValve, IsHeld, Interactable, HoverLabel, PlayerInteractor, Hovered, IsCursorLocked, Sfx (+4 more)

### Community 2 - "Cluster_2"
Cohesion: 0.05
Nodes (44): dependencies, depth, source, version, dependencies, depth, source, version (+36 more)

### Community 3 - "Cluster_3"
Cohesion: 0.05
Nodes (44): dependencies, depth, source, version, dependencies, depth, source, version (+36 more)

### Community 4 - "Cluster_4"
Cohesion: 0.09
Nodes (9): SfxType, Alarm, Click, Fail, Lever, Open, Success, Pixamor (+1 more)

### Community 5 - "Cluster_5"
Cohesion: 0.11
Nodes (8): DeskPuzzleController, CanStartEmergency, Instance, IsEmergencyActive, IsMainPuzzleActive, HatchModule, FullyOpen, IsOpen

### Community 6 - "Cluster_6"
Cohesion: 0.11
Nodes (11): NumpadProtocol, Budget, ZeroWaste, ProblemInfo, ProblemType, Factory, Food, Pollution (+3 more)

### Community 7 - "Cluster_7"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, version, depth, source, version, dependencies (+22 more)

### Community 8 - "Cluster_8"
Cohesion: 0.08
Nodes (20): ClimateStateManager, ActiveCrisisCount, AllProblemsMinimal, Climate, Countries, DateLabel, EndReason, GameOverBar (+12 more)

### Community 9 - "Cluster_9"
Cohesion: 0.13
Nodes (14): ChoiceType, FinancialIncentive, HardSanction, PartialConcession, PuzzleRequest, PuzzleType, Emergency, Numpad (+6 more)

### Community 11 - "Cluster_11"
Cohesion: 0.14
Nodes (12): CountryNode, Anger, AverageBar, CountryName, CrisisMessage, CrisisProblem, EmergencyActive, HoverLabel (+4 more)

### Community 12 - "Cluster_12"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, version, dependencies, depth, source, version (+9 more)

### Community 13 - "Cluster_13"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, version, dependencies, depth, source, version (+9 more)

### Community 14 - "Cluster_14"
Cohesion: 0.12
Nodes (17): dependencies, depth, source, version, dependencies, depth, source, version (+9 more)

### Community 15 - "Cluster_15"
Cohesion: 0.14
Nodes (3): ReadmeEditor, Readme, Section

### Community 16 - "Cluster_16"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, url, version, depth, source, version (+8 more)

### Community 17 - "Cluster_17"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 18 - "Cluster_18"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 19 - "Cluster_19"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, version, dependencies, depth, source, version (+5 more)

### Community 20 - "Cluster_20"
Cohesion: 0.17
Nodes (11): com.unity.modules.animation@1.0.0, com.unity.modules.audio@1.0.0, com.unity.modules.director@1.0.0, com.unity.modules.physics@1.0.0, com.unity.modules.video@1.0.0, com.unity.modules.animation, com.unity.modules.audio, com.unity.modules.physics (+3 more)

### Community 21 - "Cluster_21"
Cohesion: 0.17
Nodes (12): dependencies, com.unity.ide.rider@3.1.0, com.unity.modules.androidjni@1.0.0, com.unity.modules.jsonserialize@1.0.0, com.unity.modules.unityanalytics@1.0.0, com.unity.modules.unitywebrequesttexture@1.0.0, com.unity.modules.wind@1.0.0, com.unity.modules.jsonserialize (+4 more)

### Community 22 - "Cluster_22"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, version, dependencies, depth, source, version (+3 more)

### Community 23 - "Cluster_23"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, version, dependencies, depth, source, version (+3 more)

### Community 24 - "Cluster_24"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, version, dependencies, depth, source, version (+3 more)

### Community 25 - "Cluster_25"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, version, dependencies, depth, source, version (+3 more)

### Community 26 - "Cluster_26"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, version, dependencies, depth, source, version (+3 more)

### Community 27 - "Cluster_27"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.collab-proxy

### Community 28 - "Cluster_28"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.ide.rider

### Community 29 - "Cluster_29"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.accessibility

### Community 30 - "Cluster_30"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.tetgen

### Community 31 - "Cluster_31"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.wind

### Community 32 - "Cluster_32"
Cohesion: 0.40
Nodes (5): depth, source, version, com.unity.render-pipelines.universal-config@17.6.0, com.unity.render-pipelines.universal-config

### Community 33 - "Cluster_33"
Cohesion: 0.50
Nodes (4): GameState, Lost, Playing, Won

## Knowledge Gaps
- **74 isolated node(s):** `Instance`, `Climate`, `Population`, `Mental`, `Money` (+69 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 323 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **32 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `dependencies` connect `Cluster_17` to `Cluster_2`, `Cluster_3`, `Cluster_7`, `Cluster_12`, `Cluster_13`, `Cluster_14`, `Cluster_16`, `Cluster_18`, `Cluster_19`, `Cluster_22`, `Cluster_23`, `Cluster_24`, `Cluster_25`, `Cluster_26`, `Cluster_27`, `Cluster_28`, `Cluster_29`, `Cluster_30`, `Cluster_31`, `Cluster_32`?**
  _High betweenness centrality (0.163) - this node is a cross-community bridge._
- **What connects `Instance`, `Climate`, `Population` to the rest of the system?**
  _74 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Cluster_0` be split into smaller, more focused modules?**
  _Cohesion score 0.0761904761904762 - nodes in this community are weakly interconnected._
- **Why does `CountryNode` connect `Cluster_11` to `Cluster_0`, `Cluster_1`, `Cluster_4`, `Cluster_5`, `Cluster_6`, `Cluster_8`, `Cluster_9`, `Cluster_10`?**
  _High betweenness centrality (0.048) - this node is a cross-community bridge._
- **Should `Cluster_1` be split into smaller, more focused modules?**
  _Cohesion score 0.06553911205073996 - nodes in this community are weakly interconnected._
- **Why does `ClimateStateManager` connect `Cluster_8` to `Cluster_0`, `Cluster_33`, `Cluster_1`, `Cluster_4`, `Cluster_6`, `Cluster_10`, `Cluster_11`?**
  _High betweenness centrality (0.045) - this node is a cross-community bridge._
- **Should `Cluster_2` be split into smaller, more focused modules?**
  _Cohesion score 0.04756871035940803 - nodes in this community are weakly interconnected._