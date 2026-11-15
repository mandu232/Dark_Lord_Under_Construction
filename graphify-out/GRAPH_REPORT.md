# Graph Report - Scripts  (2026-11-11)

## Corpus Check
- 153 files · ~96,402 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 162 file(s) not represented in the graph (top: .meta 161, .asmdef 1)
- WARNING: 162 of 315 files seen (51%) have no supported extractor and are missing from this graph entirely. It may look complete while representing only part of the codebase.

## Summary
- 2812 nodes · 8130 edges · 120 communities (84 shown, 36 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 739 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Build Items & Inventory
- Room Buffs & Guards
- Gameplay Effect Kinds
- UI Color & Graphics
- Construction & Garrisons
- Combat Effects & Hit Steps
- Corpse Workers & Animation
- Actor Health & Tactics
- Dungeon Grid Core
- Expansion & Layout
- UI Namespace Imports
- Wave Director & Recovery
- Game Menu Flow
- HUD & Inspection Panels
- Run Economy Equipment
- Facility Placement
- Scroll Panels
- Top-Down Motor
- Settings Menu
- Minion Summoning & Merging
- Expansion Mode Input
- Corpse Harvest
- Tech Research Rules
- Actor Feedback Files
- Actor Animation States
- Difficulty Options
- World Overlays & Markers
- Construction Panel
- Resource Building
- Minion Roles
- Gameplay & Player Imports
- Combat Augments
- Invader Tactics Roles
- Opening Story Cuts
- Dungeon Namespace Imports
- Starting Tutorial Dialogue
- Alchemy Station Persistence
- Building Lookup
- Minion Inspection Panel
- Invasion Alert
- Depth Sorting
- NPC Movement Blocking
- Death Loot
- Wallet & HUD Orbs
- Story Layout
- Daily Items & Recovery
- Facility Coverage Display
- Wave Rewards
- Main Menu Parallax
- Audio Settings
- Castle Panorama & Defeat
- Core & Rift Visuals
- Region Catalog
- Alchemy Exchange
- Effects Render Pass
- UI Drag Handlers
- Slider Handles
- Minion Behaviour & Support
- Shadows & Lighting
- Placement Camera
- Loading Screen
- Alchemy Jobs
- Alchemy Start & Exchange
- Runtime Resets
- Augment Status
- Hit Flash & Damage Numbers
- UI Images & Pixel Scale
- Minion Commands
- Pointer Hover UI
- Build Item Buttons
- Invader Actions
- Minion Tier & Capacity Labels
- Rowen Animation Clips
- Alchemy Room Panel
- Save Slots
- Wave Coroutines
- Building Income & Repair
- Corpse Summon Stacks
- Spike Trap
- Wave Progression
- Hit Stop Kinds
- Player Expressions
- Tutorial Wave Flow
- Wave Formation
- Tech Catalog
- Wave States
- Camera Follow
- Main Menu Buttons
- Minion Skins
- Actor State Enum
- Text Input & Selection
- Opening Collapse FX
- Support Spells
- Lord Upgrades
- Wave HUD
- Boss Setup
- Offensive Spells
- Attack Styles
- Main Menu Title
- Language Service
- Placement Bounce
- Tutorial Setup
- Tutorial Training Steps
- Menu Screen States
- Invader Races
- Tech Node States
- Minion Traits
- Inspection Health Bar

## God Nodes (most connected - your core abstractions)
1. `DungeonGrid` - 217 edges
2. `MinionController` - 184 edges
3. `ActorHealth` - 180 edges
4. `WaveDirector` - 168 edges
5. `MinionSummoning` - 131 edges
6. `RunEconomy` - 110 edges
7. `InvaderTactics` - 105 edges
8. `PlayerCombat` - 94 edges
9. `ResourceBuilding` - 91 edges
10. `TopDownMotor` - 87 edges

## Surprising Connections (you probably didn't know these)
- `PreparationAllowed` --references--> `PlayerCombat`  [EXTRACTED]
  Dungeon/DungeonExpansion.cs → Player/PlayerCombat.cs
- `MenuOwnsPause` --references--> `GameMenu`  [EXTRACTED]
  Gameplay/StartingTutorialPause.cs → UI/GameMenu.cs
- `MinionSummoning` --defines--> `WorkerRecruitmentAvailable`  [EXTRACTED]
  Persistence/GameSaveAdapters.cs → Player/CorpseWorkerSummoning.cs
- `MinionSummoning` --defines--> `GuaranteesBloodSlime`  [EXTRACTED]
  Persistence/GameSaveAdapters.cs → Player/MinionSummoning.cs
- `MinionController` --references--> `ActorState`  [EXTRACTED]
  Minions/InvaderArcher.cs → Actors/ActorAnimation.cs

## Import Cycles
- None detected.

## Communities (120 total, 36 thin omitted)

### Community 0 - "Build Items & Inventory"
Cohesion: 0.05
Nodes (24): BuildCategory, Buildings, Corridors, Decorations, Entrances, Minions, Rooms, Traps (+16 more)

### Community 1 - "Room Buffs & Guards"
Cohesion: 0.04
Nodes (44): MinionController, MinionBuffIndicator, ActiveBuffCount, IsVisible, Anchor, AttackCooldownRemaining, AttackDamage, AttackInterval (+36 more)

### Community 2 - "Gameplay Effect Kinds"
Cohesion: 0.03
Nodes (72): GameplayEffectKind, Aegis, BloodAura, BloodBite, BloodBolt, BloodBurst, BloodEmpower, BloodHit (+64 more)

### Community 3 - "UI Color & Graphics"
Cohesion: 0.06
Nodes (14): GrowthFeedback, IsLevelUpPlaying, IsPickupPlaying, MinionRecoveryIndicator, IsVisible, TraitProcFeedback, IsPlaying, Trait (+6 more)

### Community 4 - "Construction & Garrisons"
Cohesion: 0.07
Nodes (22): DungeonConstruction, GarrisonAreas, ConstructionKind, AlchemyRoom, Corridor, HorizontalCorridor, LargeRoom, Room (+14 more)

### Community 5 - "Combat Effects & Hit Steps"
Cohesion: 0.05
Nodes (30): CanInteractRecruitment, GuaranteesBloodSlime, PlayerCombat, AttackFacing, BlocksTutorialInput, CanPrepare, CanSpendSummonStack, CanSummon (+22 more)

### Community 6 - "Corpse Workers & Animation"
Cohesion: 0.06
Nodes (17): CorpseWorker, Health, IsAlive, WorkerMiner, ActiveJob, Available, Dungeon, PendingCount (+9 more)

### Community 7 - "Actor Health & Tactics"
Cohesion: 0.06
Nodes (26): ActorHealth, BaseMagicResistance, BasePhysicalResistance, ContactCollider, ContactHalfExtent, CurrentHealth, Experience, ExperienceToNextLevel (+18 more)

### Community 8 - "Dungeon Grid Core"
Cohesion: 0.07
Nodes (17): DungeonGrid, Inventory, LastConstructionRewardDay, AlchemyStations, UsesArtTiles, Augments, RecruitmentFacilities, CanMoveCoreNow (+9 more)

### Community 9 - "Expansion & Layout"
Cohesion: 0.08
Nodes (19): DungeonCell, Bedrock, Floor, Rock, DungeonLayout, ActiveCore, Core, Entrance (+11 more)

### Community 11 - "Wave Director & Recovery"
Cohesion: 0.05
Nodes (35): WaveDirector, AutoMinionRecoveryEnabled, BlocksTutorialInput, CanUseFacilities, InCombat, Tutorial, ActiveBoss, Alchemy (+27 more)

### Community 12 - "Game Menu Flow"
Cohesion: 0.09
Nodes (8): GameMenu, OwnsPause, State, PausePanelBindings, SaveSlotMode, Continue, New, Save

### Community 13 - "HUD & Inspection Panels"
Cohesion: 0.09
Nodes (9): CharacterPanel, Economy, IsVisible, Option, CombatHud, EconomyPanel, IsVisible, InteractionUiSkin (+1 more)

### Community 14 - "Run Economy Equipment"
Cohesion: 0.06
Nodes (30): RunEconomy, EquippedAttack, EquippedOffensive, EquippedSupport, AttackBonus, BlocksGameplayInput, BloodDebt, CanChangeLoadout (+22 more)

### Community 15 - "Facility Placement"
Cohesion: 0.09
Nodes (9): ConstructionResult, Disconnected, NeedsCorridor, Occupied, OutsideMap, Success, TooCloseToRoom, Unavailable (+1 more)

### Community 16 - "Scroll Panels"
Cohesion: 0.11
Nodes (6): CoreTechPanel, IsVisible, Selected, TooltipText, TooltipVisible, TreeScroll

### Community 17 - "Top-Down Motor"
Cohesion: 0.08
Nodes (15): Slow, TopDownMotor, BlockedBy, EffectiveMoveSpeed, Facing, IgnoreActorBlocking, IgnoreMinionBlocking, MovementMultiplier (+7 more)

### Community 18 - "Settings Menu"
Cohesion: 0.06
Nodes (14): SettingsPanelBindings, CancelButton, CharacterVolumeLabel, CharacterVolumeSlider, EffectsVolumeLabel, EffectsVolumeSlider, FullscreenLabel, LanguageLabel (+6 more)

### Community 19 - "Minion Summoning & Merging"
Cohesion: 0.08
Nodes (18): MinionSummoning, WorkerCapacity, WorkerCount, Workers, ArmySize, BlocksGameplayInput, Dungeon, FallenCount (+10 more)

### Community 20 - "Expansion Mode Input"
Cohesion: 0.08
Nodes (11): DungeonExpansion, BlocksGameplayInput, DemolitionMode, Dungeon, ExpansionMode, IsDraggingCorridor, IsMovingCore, IsPlacementView (+3 more)

### Community 21 - "Corpse Harvest"
Cohesion: 0.08
Nodes (14): CorpseSaveData, CorpseHarvest, BlocksGameplayInput, Carried, Corpses, Dungeon, InTutorial, IsDragging (+6 more)

### Community 22 - "Tech Research Rules"
Cohesion: 0.08
Nodes (26): TechId, Aegis, Armor, BloodLance, BloodLeech, BloodNova, BloodTithe, CoreArmor (+18 more)

### Community 24 - "Actor Animation States"
Cohesion: 0.09
Nodes (8): ActorAnimation, ExpressionTime, IsExpressionPlaying, LastRequested, LastRequestedAt, DialogueIdleSnapshot, PlayerMovement, DodgeInputDirection

### Community 25 - "Difficulty Options"
Cohesion: 0.09
Nodes (16): GameDifficulty, Easy, Hard, Nightmare, Normal, NewGameOptions, AutomaticWaves, DifficultyName (+8 more)

### Community 26 - "World Overlays & Markers"
Cohesion: 0.11
Nodes (8): AlchemyReadyOverlay, CorpseHarvestHud, TutorialGuideGraphic, TutorialTargetHighlight, HighlightRect, IsOffscreen, IsVisible, Target

### Community 27 - "Construction Panel"
Cohesion: 0.12
Nodes (5): ConstructionPanel, ArtPixelSize, Category, DetailDescription, IsVisible

### Community 28 - "Resource Building"
Cohesion: 0.07
Nodes (22): ResourceBuilding, AttackBonusPercent, BuffIcon, BuffRadius, CanReceiveCorpse, Cell, ConstructionCost, Design (+14 more)

### Community 29 - "Minion Roles"
Cohesion: 0.08
Nodes (19): MinionRole, AcidSlime, BladeSlime, BloodArcher, BloodGolem, BloodKnight, BloodSlime, BloodSupport (+11 more)

### Community 30 - "Gameplay & Player Imports"
Cohesion: 0.15
Nodes (3): DarkLord.Gameplay, DarkLord.Persistence, DarkLord.Player

### Community 32 - "Combat Augments"
Cohesion: 0.17
Nodes (8): CombatAugments, CanTrigger, LastTrigger, LastTriggeredId, LastTriggerTime, ThornCooldown, TributeCharges, Augments

### Community 33 - "Invader Tactics Roles"
Cohesion: 0.08
Nodes (19): InvaderTactics, IsArcher, HeroArmored, HeroPace, IsHero, PhaseTwo, UsesRegionalChampionArt, Action (+11 more)

### Community 34 - "Opening Story Cuts"
Cohesion: 0.13
Nodes (5): Cut, OpeningStory, IsTransitioning, IsTyping, Story

### Community 36 - "Starting Tutorial Dialogue"
Cohesion: 0.07
Nodes (25): StartingTutorial, Active, BlocksPlayerInput, Cinematic, Collapsed, DialogueExpression, DialogueOpen, Elapsed (+17 more)

### Community 37 - "Alchemy Station Persistence"
Cohesion: 0.12
Nodes (6): InvaderEncirclement, AlchemyJobSaveData, BuildingSaveData, DungeonSaveData, SectionSaveData, TrapSaveData

### Community 39 - "Minion Inspection Panel"
Cohesion: 0.15
Nodes (9): MinionInspectionPanel, CanInspect, HealthBar, Hovered, IsOpen, Selected, StatsText, Title (+1 more)

### Community 40 - "Invasion Alert"
Cohesion: 0.11
Nodes (8): InvasionAlert, IsVisible, PresentedWave, InvasionRingGraphic, SummonPresentation, IsVisible, PresentedName, PresentedSprite

### Community 41 - "Depth Sorting"
Cohesion: 0.13
Nodes (5): ActorDepthSorting, FacilityDepthSorting, CorpseAbsorptionEffect, GoblinMerchantVisual, SpeakerSprite

### Community 42 - "NPC Movement Blocking"
Cohesion: 0.13
Nodes (10): InvaderDungeonRoute, BreachTarget, CanApproachTargetDirectly, Dungeon, HasWaypoint, LockedRoom, LockOrder, NextWaypoint (+2 more)

### Community 43 - "Death Loot"
Cohesion: 0.13
Nodes (9): DeathLoot, Collected, Kind, Blood, Crystal, Experience, Gold, LootSaveData (+1 more)

### Community 46 - "Daily Items & Recovery"
Cohesion: 0.11
Nodes (3): WaveProgression, WaveMember, Number

### Community 47 - "Facility Coverage Display"
Cohesion: 0.14
Nodes (9): FacilityCoverageDisplay, IsVisible, Outline, BuildingInspectionPanel, CanInspect, HealthBar, IsOpen, Selected (+1 more)

### Community 48 - "Wave Rewards"
Cohesion: 0.16
Nodes (4): RewardUiSkin, WaveRewardPanel, CanSelectReward, IsVisible

### Community 49 - "Main Menu Parallax"
Cohesion: 0.19
Nodes (3): MainMenuParallax, StartingTutorialPresentation, IsTyping

### Community 51 - "Audio Settings"
Cohesion: 0.16
Nodes (7): AudioCategory, Character, Effects, Music, AudioCategoryVolume, GameSettings, Defaults

### Community 52 - "Castle Panorama & Defeat"
Cohesion: 0.19
Nodes (3): CastleMenuPanorama, DefeatPresentation, Playing

### Community 53 - "Core & Rift Visuals"
Cohesion: 0.14
Nodes (7): DungeonCoreVisual, IsCollapsing, EntranceRiftVisual, Clip, GameplayEffectLibrary, RowenAnimator, DeathSprite

### Community 54 - "Region Catalog"
Cohesion: 0.17
Nodes (8): RegionCatalog, Count, FinalDay, RegionDefinition, IsHolyKingdom, NextFormation, WaveFormation, CurrentRegion

### Community 55 - "Alchemy Exchange"
Cohesion: 0.11
Nodes (12): AlchemyExchange, BlocksGameplayInput, CanUse, Dungeon, IsInAlchemyRoom, IsOpen, Message, NearbyStation (+4 more)

### Community 56 - "Effects Render Pass"
Cohesion: 0.11
Nodes (3): EffectsPass, GameplayEffectsRendererFeature, PassData

### Community 59 - "Minion Behaviour & Support"
Cohesion: 0.15
Nodes (7): MinionBehaviour, Aggressive, Defensive, MinionSupport, Cooldown, IsCasting, MinionSupportSaveData

### Community 60 - "Shadows & Lighting"
Cohesion: 0.17
Nodes (3): ActorContactShadow, SpriteProjectedShadow, TorchShadowLight

### Community 61 - "Placement Camera"
Cohesion: 0.22
Nodes (3): PlacementCameraPan, IsActive, PlacementCameraZoom

### Community 62 - "Loading Screen"
Cohesion: 0.20
Nodes (3): SceneLoadingScreen, IsLoading, StorySceneController

### Community 63 - "Alchemy Jobs"
Cohesion: 0.15
Nodes (7): AlchemyStation, Cell, GoldOutput, HasJob, IsReady, Message, Remaining

### Community 64 - "Alchemy Start & Exchange"
Cohesion: 0.24
Nodes (5): Duration, CurrencyType, DarkCrystals, Gold, Souls

### Community 66 - "Augment Status"
Cohesion: 0.13
Nodes (5): AugmentStatus, Empowered, LordMarked, TrapBranded, AugmentStatusSaveData

### Community 70 - "Minion Commands"
Cohesion: 0.16
Nodes (4): MinionCommands, MinionRelocationVisuals, CarriedSprite, GroundShadow

### Community 73 - "Invader Actions"
Cohesion: 0.13
Nodes (15): InvaderAction, Aiming, Charging, Healing, HeroCleave, Judging, Leaping, Ready (+7 more)

### Community 74 - "Minion Tier & Capacity Labels"
Cohesion: 0.13
Nodes (6): MinionTierMarker, MinionShot, MenuLightAnimation, RoomCapacityLabel, CountText, PlacementAllowed

### Community 75 - "Rowen Animation Clips"
Cohesion: 0.13
Nodes (15): Clip, Attack, Cast, Cleave, Dash, Die, Hit, Idle (+7 more)

### Community 76 - "Alchemy Room Panel"
Cohesion: 0.19
Nodes (5): AlchemyRoomPanel, BloodInteractable, CollectInteractable, CrystalInteractable, IsVisible

### Community 80 - "Building Income & Repair"
Cohesion: 0.18
Nodes (4): CurrencyWallet, DarkCrystals, Gold, Souls

### Community 81 - "Corpse Summon Stacks"
Cohesion: 0.18
Nodes (6): InvaderCorpse, AssignedWorker, Consumed, Day, Role, Visual

### Community 82 - "Spike Trap"
Cohesion: 0.19
Nodes (7): SpikeTrap, AttackInterval, AttackSpeed, CooldownRemaining, Damage, SlowDuration, SlowMultiplier

### Community 83 - "Wave Progression"
Cohesion: 0.15
Nodes (3): NextEnemyCount, NextEnemyHealth, NextWaveRegion

### Community 84 - "Hit Stop Kinds"
Cohesion: 0.18
Nodes (7): ImpactKind, Combo, Heavy, Light, Projectile, Spell, PlayerHitStop

### Community 85 - "Player Expressions"
Cohesion: 0.15
Nodes (12): PlayerExpression, Accept, Blinking, Crying, Decline, Down, DialogueEntry, TutorialPortraitMood (+4 more)

### Community 87 - "Wave Formation"
Cohesion: 0.18
Nodes (9): InvaderRole, Archer, Charger, Cleric, Healer, HolyKnight, Paladin, Shieldbearer (+1 more)

### Community 88 - "Tech Catalog"
Cohesion: 0.28
Nodes (4): TechCatalog, TechDefinition, TechTreeSaveData, EconomySaveData

### Community 90 - "Wave States"
Cohesion: 0.18
Nodes (9): WaveState, Active, Defeated, Preparing, Relocating, RewardSelection, Victorious, WaveSaveData (+1 more)

### Community 91 - "Camera Follow"
Cohesion: 0.24
Nodes (3): CameraFollow, Rumble, Sway

### Community 94 - "Minion Skins"
Cohesion: 0.33
Nodes (3): MinionSkin, MinionSkinAsset, LoadingScreenSkin

### Community 95 - "Actor State Enum"
Cohesion: 0.20
Nodes (10): ActorState, Attack, Dash, Die, Hit, Idle, Roll, Run (+2 more)

### Community 100 - "Support Spells"
Cohesion: 0.24
Nodes (6): SupportSpell, Aegis, Haste, Mend, Ward, SupportLabel

### Community 101 - "Lord Upgrades"
Cohesion: 0.22
Nodes (4): LordUpgrade, Power, Speed, Vitality

### Community 105 - "Offensive Spells"
Cohesion: 0.28
Nodes (5): AttackSpell, BloodNova, FrostNova, None, OffensiveLabel

### Community 106 - "Attack Styles"
Cohesion: 0.28
Nodes (5): AttackStyle, BloodLance, Slash, WideSlash, AttackLabel

### Community 107 - "Main Menu Title"
Cohesion: 0.33
Nodes (4): MainMenuTitle, TitleLanguage, English, Korean

### Community 111 - "Tutorial Training Steps"
Cohesion: 0.25
Nodes (8): TutorialTrainingStep, Complete, Corridor, Deliver, Entrance, None, Pickup, Summon

### Community 112 - "Menu Screen States"
Cohesion: 0.25
Nodes (8): ScreenState, Main, NewGameSetup, Pause, Playing, Quit, Settings, Slots

### Community 115 - "Invader Races"
Cohesion: 0.33
Nodes (6): InvaderRace, Elf, Emberborn, Frostkin, Human, Orc

### Community 116 - "Tech Node States"
Cohesion: 0.33
Nodes (5): TechNodeState, Available, Complete, Locked, Unaffordable

### Community 117 - "Minion Traits"
Cohesion: 0.40
Nodes (5): MinionTrait, Acid, Magic, Melee, None

### Community 119 - "Inspection Health Bar"
Cohesion: 0.50
Nodes (3): InspectionHealthBar, Label, Ratio

## Knowledge Gaps
- **705 isolated node(s):** `Idle`, `Run`, `Hit`, `Attack`, `Die` (+700 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 884 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **36 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DungeonGrid` connect `Dungeon Grid Core` to `Build Items & Inventory`, `Room Buffs & Guards`, `UI Color & Graphics`, `Construction & Garrisons`, `Combat Effects & Hit Steps`, `Corpse Workers & Animation`, `Actor Health & Tactics`, `Expansion & Layout`, `Wave Director & Recovery`, `Run Economy Equipment`, `Facility Placement`, `Scroll Panels`, `Top-Down Motor`, `Minion Summoning & Merging`, `Expansion Mode Input`, `Corpse Harvest`, `Resource Building`, `Gameplay & Player Imports`, `Dungeon Art Tiles`, `Combat Augments`, `Dungeon Namespace Imports`, `Starting Tutorial Dialogue`, `Alchemy Station Persistence`, `Building Lookup`, `Minion Inspection Panel`, `NPC Movement Blocking`, `Death Loot`, `Daily Items & Recovery`, `Facility Coverage Display`, `Main Menu Parallax`, `Castle Panorama & Defeat`, `Core & Rift Visuals`, `Alchemy Exchange`, `Placement Camera`, `Loading Screen`, `Alchemy Jobs`, `UI Images & Pixel Scale`, `Minion Tier & Capacity Labels`, `Core Data Files`, `Building Income & Repair`, `Spike Trap`, `Tech Catalog`, `Minion Skins`, `Opening Collapse FX`, `Tutorial Setup`?**
  _High betweenness centrality (0.161) - this node is a cross-community bridge._
- **What connects `Idle`, `Run`, `Hit` to the rest of the system?**
  _705 weakly-connected nodes found - possible missing edges._
- **Should `Build Items & Inventory` be split into smaller, more focused modules?**
  _Cohesion score 0.05189189189189189 - nodes in this community are weakly interconnected._
- **Why does `ActorHealth` connect `Actor Health & Tactics` to `Build Items & Inventory`, `Room Buffs & Guards`, `UI Color & Graphics`, `Construction & Garrisons`, `Combat Effects & Hit Steps`, `Corpse Workers & Animation`, `Dungeon Grid Core`, `Wave Director & Recovery`, `Run Economy Equipment`, `Facility Placement`, `Top-Down Motor`, `Minion Summoning & Merging`, `Expansion Mode Input`, `Corpse Harvest`, `Actor Animation States`, `Resource Building`, `Combat Augments`, `Invader Tactics Roles`, `Starting Tutorial Dialogue`, `Alchemy Station Persistence`, `Building Lookup`, `Depth Sorting`, `NPC Movement Blocking`, `Death Loot`, `Wallet & HUD Orbs`, `Daily Items & Recovery`, `Facility Coverage Display`, `Main Menu Parallax`, `Motor Steering`, `Core & Rift Visuals`, `Alchemy Exchange`, `Minion Behaviour & Support`, `Augment Status`, `Ranged Aiming`, `Hit Flash & Damage Numbers`, `Minion Commands`, `Minion Tier & Capacity Labels`, `Core Data Files`, `Building Income & Repair`, `Spike Trap`, `Hit Stop Kinds`, `Tech Catalog`, `Tutorial Battle Checks`, `Boss Effects`, `Tutorial Setup`?**
  _High betweenness centrality (0.102) - this node is a cross-community bridge._
- **Should `Room Buffs & Guards` be split into smaller, more focused modules?**
  _Cohesion score 0.03554239170677527 - nodes in this community are weakly interconnected._
- **Why does `InvaderTactics` connect `Invader Tactics Roles` to `Room Buffs & Guards`, `Gameplay Effect Kinds`, `UI Color & Graphics`, `Corpse Workers & Animation`, `Actor Health & Tactics`, `Dungeon Grid Core`, `Top-Down Motor`, `Corpse Harvest`, `Actor Feedback Files`, `Actor Animation States`, `Depth Sorting`, `NPC Movement Blocking`, `Main Menu Parallax`, `Motor Steering`, `Core & Rift Visuals`, `Region Catalog`, `Ranged Aiming`, `Invader Actions`, `Minion Tier & Capacity Labels`, `Core Data Files`, `Hit Stop Kinds`, `Wave Formation`, `Minion Skins`, `Invader Soldier Art`, `Boss Effects`, `Boss Setup`, `Invader Races`?**
  _High betweenness centrality (0.095) - this node is a cross-community bridge._
- **Should `Gameplay Effect Kinds` be split into smaller, more focused modules?**
  _Cohesion score 0.027777777777777776 - nodes in this community are weakly interconnected._