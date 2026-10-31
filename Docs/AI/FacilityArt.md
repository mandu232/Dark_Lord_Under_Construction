# Facility artwork

Crimson-source lighting revision: `Tools/SetupFacilityLighting.cs` now generates a separate 48×48 Gaussian halo mask from only the crimson pixels of each of the 11 facilities and the core. The mask follows the actual red pattern in the artwork; it is a rendering helper under Materials, not a change to the 32×32 palette-limited source art. The source emission, source halo and faint floor reflection share a smooth 2.8-second pulse between 25% and 100% emission. Floor strength is now 0.035 and its position follows the horizontal centroid of the red pixels. A separate contact-shadow material copies only the bottom band of each sprite, shifted down one source pixel, for a thin dark rim under the base. The original longer projected shadow is preserved. The earlier floor-dominant lighting settings below are superseded.

Authoring order when changing source art: run `SetupFacilityArt.Run`, `SetupDungeonCoreArt.Run`, then `SetupFacilityLighting.Run`. The final step rebuilds source-shaped masks and updates existing live facilities/core without stopping Play Mode. All three light materials must retain `_PulseTime = -1` for live animation; the hidden override is only used to compare deterministic bright/dim screenshots.

Verified all 12 prefabs contain their own 48×48 source halo and foot contact shadow. Four light/shadow shaders are supported with zero shader diagnostics. Actual Game View inspected at bright (0.7s) and dim (2.1s) phases, then live pulsing was restored. Offscreen ruin/intact state checks confirmed new effects disappear and restore with the intact parent. Gameplay player position, wallet, core health/cell, guard count and wave state matched the pre-edit checkpoint; Play Mode was never stopped. Console errors: zero. Screenshots: `Docs/AI/CrimsonSourceBright.png`, `Docs/AI/CrimsonSourceDim.png`.

Latest lighting revision: floor illumination is now wider and softer, using strength 0.13, scale 2.2 and continuous smoothstep falloff; the earlier pixel-stepped falloff has been superseded. Core artwork and relocation details are in `Docs/AI/DungeonCoreRelocation.md`.

Generated with the built-in image generation tool on 2026-10-31. Final exports are 32 × 32 RGBA PNGs, use a shared palette with at most 12 RGBA values including transparency, and use binary alpha. All original source images remain under the generated-images directory; project assets are standalone PNGs.

## Files and subjects

- `Assets/Sprite/Dungeon/Facilities/ArcaneFocus.png`: arcane focusing pedestal: a floating violet crystal held between gothic iron prongs on a squat stone plinth.
- `Assets/Sprite/Dungeon/Facilities/AcidVat.png`: acid cultivation vat: a squat round iron cauldron filled with lime-green bubbling acid, reinforced dark stone base.
- `Assets/Sprite/Dungeon/Facilities/MeleeTotem.png`: melee combat totem: two crossed short silver swords mounted on a dark stone training shrine.
- `Assets/Sprite/Dungeon/Facilities/WarBanner.png`: war banner: torn deep crimson hanging flag with a tiny ivory horn emblem on a bronze pole and heavy stone foot.
- `Assets/Sprite/Dungeon/Facilities/IronWard.png`: iron ward: a large blue-gray iron shield embedded upright in a squat carved stone pedestal.
- `Assets/Sprite/Dungeon/Facilities/ArcaneWard.png`: magic ward: a teal runic ring around a small violet crystal mounted on a squat stone pedestal.
- `Assets/Sprite/Dungeon/Facilities/GoldMint.png`: gold mint: squat gothic coin press with a brass gear and a small stack of gold coins on stone workbench.
- `Assets/Sprite/Dungeon/Facilities/BloodAltar.png`: blood altar: gothic stone altar basin filled with crimson blood, with two short ivory candles.
- `Assets/Sprite/Dungeon/Facilities/BloodstoneExtractor.png`: bloodstone extractor: compact iron mining clamp surrounding a jagged bright crimson crystal on a rock base.
- `Assets/Sprite/Dungeon/Facilities/MinionRecruitment.png`: minion summoning station: small gothic summoning arch with a circular crimson portal and two horned stone pillars.
- `Assets/Sprite/Dungeon/Facilities/MinionQuarters.png`: minion quarters: compact dark wooden bunk bed with a crimson blanket and a small iron helmet at its foot.

## Front-facing revision and installed effects

GoldMint, MinionQuarters, WarBanner and BloodstoneExtractor were repainted using the built-in image tool with their existing sprites as edit targets. The final views face straight forward: horizontal front edges, vertical supports, no angled side faces or diagonal footprints. GoldMint centers its gear above the front coin press; MinionQuarters clearly shows two horizontal bunk beds; WarBanner hangs flat toward the viewer; BloodstoneExtractor uses matching clamps around an upright crimson crystal. Other facility sprites were retained. Updated PNGs remain 32 × 32 with 10 RGBA values each, including transparency; the common palette remains limited to 12 entries.

Edit prompt for each of these four subjects: repaint the attached same facility in STRICT FRONT VIEW, looking straight at its front elevation; nearly bilateral symmetry except small natural details; preserve gothic dark stone/iron, crimson and bronze style and purpose; exact logical 32 × 32 grid with 2-pixel transparent margins; chunky clusters, no fine details, no turned side faces, isometric perspective, diagonal footprint, text, frame, ground, baked shadows, baked glow or antialiasing; upper-left highlights and the same 11 opaque palette colors plus transparency. Built-in image generation was used, followed by the same palette-constrained export.

Every facility prefab now includes a shared additive `FacilityRedGlow` material on an overlay of its source sprite. The shader selects crimson pixels by hue, excludes bronze/green/gray, preserves source brightness differences and adds a short pixel halo without requiring bloom. This is a rendering effect; no extra colors are written into the palette-constrained artwork. Both intact and ruined visuals have a silhouette shadow using the existing `SpriteProjectedShadow` implementation and a shared facility shadow material. Its new local ground-anchor field defaults to the original character anchor, preserving existing player shadows. Facility shadows use the source sprite's foot position.

Runtime verification: inspected all 11 designs on the actual InGame stone floor, including front-facing replacements, brightened red details and projected floor shadows. All 12 instances (11 test facilities and the starting summon facility) had emission and intact/ruin shadow renderers. Destroying a facility disabled emission and intact shadow and enabled the ruin shadow; repairing it reversed those states. Shader diagnostics and Console errors: zero. Temporary placement and camera changes were discarded. Screenshot: `Docs/AI/FacilityLightingInGame.png`.

## Final shared palette

Fully transparent plus #11151D, #202B33, #354750, #87918B, #DDD2AA, #4A1E30, #8D293D, #CF4B55, #AD783F, #6F9A67, #8A77A0. Source artwork is sampled onto a 28-pixel maximum footprint inside a 32-pixel canvas, centered horizontally with a two-pixel bottom margin, then mapped to this palette. No dithering, partial alpha, compression, mipmaps, or smoothing. Unity import: 16 PPU, point filtering, center pivot, full-rect mesh.

## Generation prompts

One built-in generation call was made per subject above. Each prompt requested: one standalone facility sprite, also used as its inventory icon; dark gothic dungeon style with charcoal blue-gray masonry, deep crimson and muted bronze; orthographic three-quarter top-down view showing front and top rather than an isometric diamond; centered transparent background; exact logical 32 × 32 pixel grid; chunky clusters, readable silhouette, upper-left lighting and a one-pixel dark outline; no text, border, floor tile, gradients, antialiasing, soft glow, noise or translucent shadows. The subject for each call is listed above. The source prompt palette also included #52656B, which was removed in the final palette to keep transparency within the 12-value limit.

## Installation and verification

Ground placement revision: all facility feet now anchor at cell-centre Y − 0.25 world units (4 source pixels below the centre of the 32px/2-unit tile). Intact artwork moves up 0.625 units, while its emission and projected shadow follow the same transform. Ruins use the same ground contact. New `Crimson floor light` children use a shared additive floor-light shader and a full-rect 32×16 white lighting quad, with a pixel-stepped elliptical falloff across a 2.8×1.4-unit footprint. The light is centered at the facility foot, renders above floor artwork and below facility shadows, and switches off with the intact parent on ruin. Artwork and UI icon PNGs are unchanged and retain the shared 12-entry palette.

Verified all 11 prefab foot and floor-light centres at local cell Y −0.25, with light order −31994 below shadow order −91. Inspected the user's active InGame recruitment facility after updating its visuals in place, without stopping Play Mode or replacing run state. An isolated offscreen clone confirmed floor light deactivation in ruin state and restoration when intact. Shader/Console errors: zero. Screenshot: `Docs/AI/FacilityGroundLightInGame.png`.

`Tools/Art/FinalizeFacilityArt.ps1` exports and validates the PNGs from a source manifest using Windows PowerShell. `Tools/SetupFacilityArt.cs` runs in the Unity Editor to configure import settings and assign the same sprite to each construction item icon and its prefab's Intact SpriteRenderer. Previous tint and resource-symbol overlays are disabled because those details are part of the authored sprite. Existing buff effect symbols remain assigned to the skill icon set. Collider, production, buff and recruitment data are preserved. Existing ruined-building artwork is preserved.

Validated all 11 exported PNGs and imported Unity textures at exactly 32 × 32 with 10–12 RGBA values each. Their combined palette has 12 entries including transparency. Each item icon matches its prefab appearance, all full-health renderer tints are white, and all imports use 16 PPU with point filtering. Inspected the actual InGame view with all 11 facilities on existing stone floor and the construction book open. Destroyed and repaired an AcidVat at runtime: intact → existing ruins → authored AcidVat sprite restored. Console errors: zero. Temporary facilities, inventory and camera changes were discarded on leaving Play Mode; no scene or save writes. Enlarged nearest-neighbor preview: `Docs/AI/FacilityArtPreview.png`.
