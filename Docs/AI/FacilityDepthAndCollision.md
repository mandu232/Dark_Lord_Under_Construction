# Raised facilities and collision

Occlusion update: FacilityDepthSorting caches the scene's player once and fades raised art/emission to 45% opacity over about 0.11 seconds only when the living player is behind and its visible footprint overlaps the facility. Side/front exit restores original opacity, as does disabling the component. Original RGB/damage tint and authored alpha are preserved. Ground lights and shadows are excluded. FacilityDepth tests now pass 3/3, including fade/restore and unchanged ground-light alpha. Actual composited Game View verified in FacilityOcclusion.png; stopped MainMenu restored.

2026-10-31

- Eleven facility prefabs use solid 1.25 x 0.5 BoxCollider2D footprints instead of the small central circle. ResourceBuilding retains its existing destruction/repair handling: ruined footprints are disabled, repairs restore them.
- FacilityDepthSorting updates raised sprites and their local emission/shadow offsets with the same 16 steps/world-unit order used by ActorDepthSorting. Floor light remains in its original terrain order. It follows transform changes, including core relocation.
- Depth/footprint centre is +0.5625 local Y: the facility ground is -0.25 below tile centre; existing player root/collision origin sits 13 source pixels above the visible feet. This matches the established actor coordinate convention without changing movement/navigation or actor colliders.
- DungeonCoreVisual uses the same sorting. BindCore installs the same solid footprint on the core actor. SetupFacilityDepth.Run authors the eleven prefabs and core visual; SetupDungeonCoreArt retains depth setup when reauthoring.

Validation: Unity compilation passed. FacilityDepth Play Mode tests passed 2/2 (solid walking collision, no building displacement, ruins passage, front/back ordering, moved facility ordering, floor-light preservation). Composited Game View inspected with the actual player behind and in front: FacilityBehind.png / FacilityFront.png. Console showed no current errors/warnings. CoreRelocation filters returned zero tests, so no relocation test pass is claimed. Editor restored to stopped MainMenu.
