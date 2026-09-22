# Main menu and settings

`Assets/Scenes/MainMenu.unity` is the separate title scene and the first enabled build scene. Open this scene and press Play to start from the menu; opening SampleScene directly remains useful for gameplay development and tests. MainMenu contains the menu canvas, camera and Input System EventSystem, with no gameplay objects. SampleScene retains only the pause/settings/confirmation panels and its own EventSystem.

- **Begin your reign** asynchronously loads SampleScene as a fresh dungeon. SampleScene starts playing immediately. **Esc** pauses/resumes. Closing the minion popup consumes Esc before the pause menu.
- **Main menu** opens a confirmation before unloading the dungeon and loading MainMenu. Dungeon progress is not saved; settings persist between scenes and launches.
- **Settings** offers master volume, borderless fullscreen/windowed display, graphics quality and vSync. Apply & Save commits preferences to `DarkLord.Settings.*` PlayerPrefs. Cancel/Esc discards the draft; Defaults edits only the draft.
- Display mode changes take effect in standalone builds; the editor Game View does not change desktop display mode. Master volume controls AudioListener and needs audio sources to be audible.
- Menu screens pause simulation and audio, hide the HUD and IMGUI gameplay hints, and restore the prior time scale/audio/cursor/HUD state on resume or disable.
- Quit opens a confirmation that explains dungeon progress is not persisted. Confirm stops Play Mode in the editor or quits a standalone build.
- Mouse, arrow-key UI navigation and Enter submission use InputSystemUIInputModule. Buttons select an initial focus on each panel.

All menu and HUD TMP text uses the supplied PF Stardust 3.0 fonts from `Assets/Font`: Regular for body text, Bold for buttons/status, ExtraBold for large titles. Static ASCII atlases have matching dynamic Korean fallbacks, cleared on build. TMP's default font is Stardust Regular. The summon/mining IMGUI uses `Stardust Game UI.guiskin` with the original Regular/Bold TTFs. No localization or third-party art packages are added. The menu illustration is editable uGUI geometry.

The title screen now uses the supplied `Assets/Sprite/BackGround/MainMenu_Bg.png` and the two existing slices in `Assets/Sprite/Ui/Button/Button-Sheet.png`. The 480x270 composition is displayed at 4x design size with a centered 16:9 frame, point-filtered uncompressed sprites, and Korean Stardust buttons on the right. Pointer hover or up/down selection moves the arrow and shows the underline for the selected item. The former title/illustration objects remain inactive for editing. `Tools > Dark Lord > Apply Main Menu Sprites` reapplies this style to the existing buttons without replacing their actions.

Hover animation uses the 11 existing slices of `MainMenu_Button.png`, ordered top to bottom (0 through 10). Pointer enter restarts the reveal at 15 fps; the last frame holds until exit. Exit or panel disable restores frame 0. Playback uses unscaled time because the title menu pauses simulation. The animation includes the arrow and underline, so the old static underline is hidden. Keyboard focus retains the text highlight. `Apply Main Menu Hover Animation` updates these references without changing button layout; playback speed is editable on each MainMenuButtonStyle.

`Tools > Dark Lord > Setup Main Menu and Settings` configures both scenes, startup order and fonts while preserving existing panels. Runtime behavior lives in `Assets/Scripts/UI/GameMenu.cs`, preference persistence in `GameSettings.cs`, and authoring in `Assets/Editor/MenuSceneSetup.cs`, `MainMenuSetup.cs` and `StardustFontSetup.cs`. Setup closes the saved gameplay authoring scene to avoid duplicate EventSystems.
