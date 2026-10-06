# Dungeon construction and room progression

Updated 2026-10-06.

The current **B** menu integrates rooms, guards and traps in a bottom panel. Completed sections spawn their selected design prefab. See [Unified construction](UnifiedConstruction.md) for asset setup and the updated menu.

## Playing

- No worker is required or summonable. **B → Minions** selects guard placement.
- Open **[B] Construction** from the bottom-left HUD button or the **B** key.
- Select **Room (5 × 5)**, **Large room (7 × 7)**, or **Corridor (width 3)**. Corridor placement uses press-drag-release from existing completed floor; see [Corridor dragging](CorridorDragging.md).
- Hover over rock to preview the whole footprint. Green permits placement; red indicates an invalid footprint . Click to reserve the footprint.
- Corridors connect to existing or planned sections along a cardinal edge. Rooms must connect to a corridor. Overlap, bedrock, map boundaries, and isolated footprints are rejected.
- Reserved tiles flash briefly, crumble into fragments, and open one by one from adjacent floor. DungeonGrid owns this animation; the default tileBreakInterval is 0.045 seconds. Connected plans can be queued before the previous section is complete. Pausing freezes construction.
- B/Esc closes the window without cancelling queued construction. WASD/arrow movement works while the unified menu is open, including guard placement. Guard commands remain blocked. Esc closes the window before opening pause.
- Construction is currently free and has no save persistence. No individual-tile mining input remains in the player UI.
- Right-click mass movement and Q rally are removed. E returns guards to posts. Right-click cancels placement and restores the construction menu/camera.

## Invaders and guards

Invaders navigate cardinal floor paths through **completed** sections. Partially excavated sections remain closed to invaders until the full footprint is complete.

Invaders visit connected non-core rooms before attacking the core, choosing the nearest reachable unvisited room. This includes side branches, so a room beside the original entrance corridor is not skipped. An empty room can be crossed after arrival; an occupied room confines the invader until all active living guards inside that room have been eliminated. Invaders prioritize a blocking room guard and retain their combat target instead of repeatedly switching between guards.

Guard detection for a room defence post is restricted to enemies inside that room. Guards do not chase an approaching invader out into the corridor. E return still works; room occupancy is determined by guards' current positions. The player and dungeon core do not count as room guards.

Combat damage, windup, hit interruptions, and wave health scaling retain their existing rules. Routing does not guarantee invaders win a fight; surviving defenders can hold a room. Invaders can traverse an already visited room again, but living guards encountered there still block progression.

Invaders also attack nearby players physically blocking their path. This does not release a guarded room or let them pursue a blocker into a different room.

## Ownership

- `DungeonConstruction` owns section footprints, reservations, completion and cardinal path rules. It does not excavate cells.
- `DungeonGrid.Construction` exposes the construction model and room-defender registry. `DungeonLayout.CellChanged` marks full sections complete.
- `DungeonExpansion` owns B/placement input and queues footprint cells on `DungeonGrid`.
- `ConstructionPanel` displays the uGUI window and binds its five buttons. The scene hierarchy is installed by **Tools > Dark Lord > Setup Construction UI**, which is idempotent and preserves the existing HUD.
- `InvaderDungeonRoute` selects room guards, confines routes and visits connected rooms. `MinionController` retains movement, attack and animation ownership.
- `WaveDirector` starts the routed assault using its assigned dungeon and core references. The one-argument `Assault` API retains the existing unrestricted assault behavior for other callers.

## Validation

PlayMode construction coverage includes overlap/boundary/connectivity rejection, reservation without instant floor creation, partial-section route exclusion, completed-section connectivity, sequential worker-free completion and pause, gating on the last living guard, side-room visitation, and real movement/combat through two connected rooms.

2026-10-06: all **71 PlayMode tests passed**, including construction/menu/summoning, removed right-click movement, legacy worker, legacy combat, physics, animation, settings, and wave progression tests. The construction window was also inspected in the live Game view at 1920 × 1080.

## Room spacing

Rooms (5x5, 7x7 and the initial core room) require at least one intervening tile between footprints. Edge and diagonal corner contact are rejected, including rooms still queued for construction. Exactly one tile of separation is allowed; corridors may occupy the gap to connect rooms. Invalid previews stay red and TryPlan returns TooCloseToRoom without reserving a section.
