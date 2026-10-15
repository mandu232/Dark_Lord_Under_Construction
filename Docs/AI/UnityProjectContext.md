# Unity project context

Verified 2026-10-15 while implementing new-game setup.

- Unity 6000.6.2f1, C# MonoBehaviour components grouped into DarkLord namespaces.
- Runtime assembly: Assets/Scripts/DarkLord.Runtime.asmdef. PlayMode tests: Assets/Tests/PlayMode.
- uGUI and TextMeshPro runtime UI; Input System keyboard/mouse input. Korean menu text follows existing project conventions.
- MainMenu -> save-slot selection -> new-game setup on the existing character screen -> Story (unless skipped) -> SampleScene.
- GameMenu owns menu state and pause restoration. CastleMenuPanorama animates UI stations using unscaled time.
- New-game setup reuses the main castle background and character; it replaces the right-side title/actions with difficulty, save name and story-skip controls. No ascent, blackout or exterior background is used.
- SaveNameInputField extends TMP_InputField for plain-name editing: rich-text rendering is off, edits use committed string indices, and IME owns preedit deletion/navigation. This avoids TMP 2.6.0 Backspace using composition mesh indices against the shorter stored string. Keep composition/delete regression coverage in NewGameSetupTests.
- NewGameSetupPanel prefab is installed in MainMenu; NewGameSetupAuthoring creates the initial prefab and wiring. Existing scene UI overrides must be preserved.
- GameSaveSession passes a copied NewGameOptions through the story, configures WaveDirector and serializes options with saves. Absent options in older version-1 saves mean normal difficulty. Save names are display names; stable slot1/2/3.json filenames remain compatible.
- Difficulty presets are Easy, Normal, Hard, Nightmare. Only preset selection, save name and opening-story skip are exposed in setup; tuning weights live in NewGameOptions.
- CurrencyWallet owns resources. WaveDirector owns invasions, health progression and wave-clear rewards.
- URP 17.6.0 is installed and referenced; quality levels supply the render pipeline asset (GraphicsSettings has no global override).
- Connected Unity tools verified against this exact project; compile, eval, screenshots and tests available.
- Existing workspace includes substantial uncommitted menu artwork, font, prefab and scene changes. Do not revert these.
