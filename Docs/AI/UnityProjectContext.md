# Unity project context

Verified 2026-10-15 while implementing new-game setup.

- Unity 6000.6.2f1, C# MonoBehaviour components grouped into DarkLord namespaces.
- Runtime assembly: Assets/Scripts/DarkLord.Runtime.asmdef. PlayMode tests: Assets/Tests/PlayMode.
- uGUI and TextMeshPro runtime UI; Input System keyboard/mouse input. Korean menu text follows existing project conventions.
- MainMenu -> save-slot selection -> new-game setup on the existing character screen -> Story (unless skipped) -> InGame.
- GameMenu owns menu state and pause restoration. CastleMenuPanorama animates UI stations using unscaled time.
- GameMenu scene transitions reset the global clock and audio pause after releasing menu pause, including direct gameplay startup when opening-story skip is enabled. Successful save notices expire after 3 unscaled seconds; repeated saves restart the timer and error messages remain visible.
- New-game setup reuses the main castle background and character; it replaces the right-side title/actions with difficulty, save name and story-skip controls. No ascent, blackout or exterior background is used.
- SaveNameInputField extends TMP_InputField for plain-name editing: rich-text rendering is off, edits use committed string indices, and IME owns preedit deletion/navigation. This avoids TMP 2.6.0 Backspace using composition mesh indices against the shorter stored string. Keep composition/delete regression coverage in NewGameSetupTests.
- UI prefabs are grouped under Assets/Prefabs/UI/MainMenu (title/setup), InGame (HUD and gameplay menus), Shared (settings/save slots), and Story (opening story). Preserve their GUIDs when moving assets.
- NewGameSetupPanel prefab is installed in MainMenu; NewGameSetupAuthoring creates the initial prefab and wiring. Existing scene UI overrides must be preserved.
- GameSaveSession passes a copied NewGameOptions through the story, configures WaveDirector and serializes options with saves. Absent options in older version-1 saves mean normal difficulty. Save names are display names; stable slot1/2/3.json filenames remain compatible.
- Difficulty presets are Easy, Normal, Hard, Nightmare. Only preset selection, save name and opening-story skip are exposed in setup; tuning weights live in NewGameOptions.
- CurrencyWallet owns resources. WaveDirector owns invasions, health progression and wave-clear rewards.
- 2026-10-19: WaveDirector adds/configures PlayerCombat on its lord in Start. Combat owns mana, melee/nova/ward/dodge and core repair/health upgrades. GameHud installs CombatHud at runtime; serialized scene/prefab changes are unnecessary.
- Waves now end at five by default: clear income -> paused three-choice reward -> living actor recovery -> Preparing/Victorious. WaveState numeric values 0/1/2 remain unchanged; Victorious/RewardSelection are appended. Saves preserve combat and pending rewards; legacy missing combat uses full mana and no upgrades.
- Lord/core HP are separate. Core destruction kills the lord even through invulnerability; lord death leaves core HP intact. Heal never resurrects. See ConceptGameplayComparison.md for controls, tuning and remaining design differences.
- URP 17.6.0 is installed and referenced; quality levels supply the render pipeline asset (GraphicsSettings has no global override).
- Connected Unity tools verified against this exact project; compile, eval, screenshots and tests available.
- Existing workspace includes substantial uncommitted menu artwork, font, prefab and scene changes. Do not revert these.

- 2026-10-20: ResourceBuilding now supports room auras (six authored facilities), sharing placement, ruins, invader targeting, repair and save identity with production buildings. Multiple facilities may occupy distinct empty cells in each non-core room; aura bonuses add across unique prefab designs and identical copies apply once. Invader routes skip ruins and clear every surviving facility before leaving. MinionController resolves the aura from its physical room and traits at use time; leaving, disabling or destroying it removes bonuses immediately. BloodSlimeGuard has Magic/Acid/Melee traits and Magic damage. ActorHealth applies matching Physical/Magic percentage resistance (clamped 0–100); player melee and spike traps are Physical, nova is Magic. Facility attack bonuses: matching trait +30%, universal +20%; wards +25 percentage points resistance. Cost 100, repair 50, HP 180. RoomBuffFacilitySetup authors assets and registers construction/save catalogs. ResourceBuildingTests cover all six, filters, room transitions, destruction/repair, saves and damage types.

- MinionInspectionPanel displays final attack/resistances with base values and effective facility increases (resistance capped at 100%), plus a sorted list of unique active facility names/effects. The list is populated by the same GetRoomBonuses pass used for combat, keyed by prefab identity. Runtime uGUI scroll content preserves the existing stats region and controls; regular refresh preserves scrolling, opening resets to the top. InspectionListsUniqueBuffsAndSeparatesBaseFromEffectiveStats covers duplicates, cap, destruction/repair, room exit and scrolling.
