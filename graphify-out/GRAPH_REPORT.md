# Graph Report - Scripts  (2026-11-01)

## Corpus Check
- 18 files · ~0 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 352 nodes · 848 edges · 11 communities (9 shown, 2 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 20 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Community 0
- Community 1
- Community 2
- Community 3
- Community 4
- Community 5
- Community 6
- Community 7
- Community 8
- Community 9
- Community 10

## God Nodes (most connected - your core abstractions)
1. `ClimateOperatorManager` - 44 edges
2. `WorldMapMonitorController` - 35 edges
3. `ClimateRestorationManager` - 26 edges
4. `Greybox` - 25 edges
5. `ClimateRestorationUI` - 24 edges
6. `WorldMapPin` - 24 edges
7. `BlindChoiceDialogueUI` - 23 edges
8. `PlayerInteractor` - 23 edges
9. `ContinentCrisisSpawner` - 19 edges
10. `ContinentType` - 19 edges

## Surprising Connections (you probably didn't know these)
- `BlindChoiceDialogueUI` --references--> `UIBar`  [EXTRACTED]
  COP31/BlindChoiceDialogueUI.cs → COP31/Greybox.cs
- `BlindChoiceDialogueUI` --references--> `PressButton`  [EXTRACTED]
  COP31/BlindChoiceDialogueUI.cs → COP31/PressButton.cs
- `ClimateRestorationUI` --references--> `PressButton`  [EXTRACTED]
  COP31/ClimateRestorationUI.cs → COP31/PressButton.cs
- `WorldMapPin` --references--> `UIBar`  [EXTRACTED]
  COP31/WorldMapPin.cs → COP31/Greybox.cs
- `HudController` --references--> `PlayerInteractor`  [EXTRACTED]
  COP31/HudController.cs → COP31/PlayerInteractor.cs

## Import Cycles
- None detected.

## Communities (11 total, 2 thin omitted)

### Community 0 - "Community 0"
Cohesion: 0.11
Nodes (4): Greybox, Font, PressButton, SceneAutoBuilder

### Community 1 - "Community 1"
Cohesion: 0.08
Nodes (15): BlindChoice, ContinentQuestion, ContinentQuestionDatabase, ContinentType, Africa, Antarctica, Asia, Europe (+7 more)

### Community 2 - "Community 2"
Cohesion: 0.09
Nodes (13): PlantedTree, RestorationRegion, TotalTrees, TreeGrowthStage, MatureForest, Sapling, YoungTree, TreeType (+5 more)

### Community 3 - "Community 3"
Cohesion: 0.08
Nodes (22): ClimateOperatorManager, EndReason, Halk, Iklim, Instance, LastMessage, LastMessageTime, MissedCount (+14 more)

### Community 4 - "Community 4"
Cohesion: 0.10
Nodes (10): Interactable, HoverLabel, PlayerInteractor, Hovered, Instance, IsCursorLocked, WorldMapScreenInteractable, LevelManager (+2 more)

### Community 5 - "Community 5"
Cohesion: 0.09
Nodes (15): ClimateRestorationManager, Instance, StateContract, TruckStatus, EnRouteToRegion, IdleInDepot, PlantingOnSite, ReturningToDepot (+7 more)

### Community 6 - "Community 6"
Cohesion: 0.12
Nodes (6): ContinentCrisisSpawner, Instance, WorldMapPin, IsActive, MaxDuration, TimeRemaining

### Community 7 - "Community 7"
Cohesion: 0.17
Nodes (5): ClimateRestorationUI, Instance, UIBar, HudController, Instance

### Community 10 - "Community 10"
Cohesion: 0.17
Nodes (11): ChoiceType, FinancialIncentive, HardSanction, PartialConcession, ProblemType, Factory, Food, Pollution (+3 more)

## Knowledge Gaps
- **69 isolated node(s):** `Instance`, `Instance`, `Money`, `Halk`, `Iklim` (+64 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 111 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ClimateOperatorManager` connect `Community 3` to `Community 0`, `Community 1`, `Community 2`, `Community 4`, `Community 7`, `Community 8`?**
  _High betweenness centrality (0.206) - this node is a cross-community bridge._
- **What connects `Instance`, `Instance`, `Money` to the rest of the system?**
  _69 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.11422527763088314 - nodes in this community are weakly interconnected._
- **Why does `WorldMapMonitorController` connect `Community 1` to `Community 0`, `Community 2`, `Community 4`, `Community 8`, `Community 9`?**
  _High betweenness centrality (0.146) - this node is a cross-community bridge._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.08163265306122448 - nodes in this community are weakly interconnected._
- **Why does `ClimateRestorationManager` connect `Community 5` to `Community 0`, `Community 1`, `Community 2`, `Community 4`, `Community 8`?**
  _High betweenness centrality (0.138) - this node is a cross-community bridge._
- **Should `Community 2` be split into smaller, more focused modules?**
  _Cohesion score 0.0919661733615222 - nodes in this community are weakly interconnected._