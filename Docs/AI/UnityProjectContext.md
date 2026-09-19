# Unity project context

Updated 2026-09-17. Confirmed from ProjectSettings, Packages, SampleScene, and first-party assets.

- Unity 6000.6.0f1, PC single-player, 2D top-down. URP 17.6 with Renderer2D is selected through QualitySettings (GraphicsSettings fallback is empty).
- Input System 1.20 is active exclusively. Reuse Assets/Settings/InputSystem_Actions.inputactions, Player/Move. As of 2026-09-18 the user replaced the witch with Legacy Red. The active player controller is Assets/Gameplay/PlayerDirectional.controller, a copy of the user's Player.controller retaining action states. Float Speed controls Locomotion/Idle_Default and Walk; FacingX/FacingY select five sprite-sheet rows with horizontal mirroring for eight directions. ActorAnimation supports both legacy integer speed and float Speed.
- Art convention: 16 pixels per unit; one 16x16 tile occupies one world unit. Existing witch sprites already use 16 PPU and Point filtering.
- Main/build scene: Assets/Scenes/SampleScene.unity. It originally contained Player, Main Camera, Global Light 2D. No prior gameplay code or architecture documentation existed.
- Runtime code: Assets/Scripts, assembly DarkLord.Runtime. Small MonoBehaviour components own movement, health, animation presentation, and minion decisions. Editor-only setup and validation are under Assets/Editor. PlayMode tests have their own assembly.
- No networking or persistence is implemented. No tilemap/navigation system exists yet. Minion steering stops at configured obstacles; full dungeon pathfinding is a later integration.
- Unity Pipeline 0.7.0-exp.1 is installed. The existing enabled Codex `unity` MCP entry launches Unity CLI (`unity mcp`). On 2026-09-17 a real stdio MCP handshake discovered 151 tools; editor_status, console_status, and get_scene_hierarchy succeeded against this project (Unity 6000.6.0f1, SampleScene, zero console errors/warnings). Restricted shell discovery can fail; use an approved elevated command for local connection checks, without weakening sandbox settings. Explicitly target this project with `--project-path` for CLI/MCP calls. Never rewrite scene/prefab YAML behind the running editor.
- Setup command: Tools > Dark Lord > Setup Player and Minions. It preserves the existing witch controller and adds two placeholder melee guards and a 16 PPU URP pixel camera. Reference resolution 320x180 is a provisional setup default, not a user-selected art requirement.
- Validation command: Tools > Dark Lord > Run Gameplay Tests. Results: Logs/GameplayTests.xml. See Docs/GameplaySetup.md for controls and animation wiring.
- Do not change existing animation clips or regenerate the minion controller after the user adds artwork. Setup reuses existing generated assets.
