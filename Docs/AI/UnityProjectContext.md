# Unity project context

Inspected 2026-11-03. Unity 6000.6.2f1; PC single-player 2D; URP 2D, Input System, uGUI/TMP (README and project configuration).

Startup: MainMenu → Story → InGame. Runtime code lives under Assets/Scripts, editor tooling under Assets/Editor, PlayMode tests under Assets/Tests/PlayMode. MonoBehaviour controllers use explicit Configure methods and events. WaveDirector owns invasion membership and death notifications. CorpseHarvest owns corpse creation/delivery. CurrencyWallet owns capped currency balances. Player and core share ActorHealth through a health owner. MinionController is shared by allied guards and hostile invaders, so faction checks are required.

Persistence: GameSaveSession composes snapshots; GameSaveData contains additive serializable DTOs; GameSaveAdapters restores guards and waves. Preserve existing health maxima when restoring experience to avoid applying level bonuses twice. ActorHealth owns experience; direct hostile fatal damage credits an allied minion source. DeathLoot owns world pickups and their snapshots. Corpse delivery grants only summon stacks.

Connected Unity MCP supports Editor status, compilation, Console and PlayMode tests. Active project path verified against workspace. Runtime play and save compatibility need validation after gameplay changes; no scene/prefab wiring is required for the death-loot feature.

GarrisonAt resolves completed rooms and corridors for minion relocation, capacity and patrol. RoomAt remains room-only for construction, core, recruitment and bed capacity. Corridor capacity is 2 per authored section; beds affect only their room. Combat facility buffs use live position within BuffRadius (default 6 world units), preserving same-design deduplication. WaveSaveData.minionRecoveryDisabled is additive and defaults to enabled for old saves; recovery indicators appear only on paid healing.

ConnectedGarrisons derives cached four-neighbor completed corridor components from construction Revision. GarrisonAt/GarrisonAreas use these components for shared 2-unit capacity, placement and patrol. Authored Sections and saves retain original construction pieces. Room boundaries do not join corridor components. Irregular components must use GarrisonAt membership rather than Bounds containment for movement.
