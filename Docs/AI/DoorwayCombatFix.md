# Room/corridor combat fix — 2026-10-28

## Confirmed causes

- InvaderDungeonRoute retains a locked room while its defenders live. Knockback can move an invader into the corridor, but confined FindPath rejects an outside start. The route then has neither target nor waypoints, so the invader never returns.
- Registered guards rejected every target outside their assigned room. An enemy within melee reach across the doorway could therefore be ignored, and the room-sized scan could miss it altogether.

## Changes

- Keep the uncleared-room lock and route a displaced invader back to the first reachable cell in that room before resuming its confined combat path.
- Expand the guard acquisition area by its attack reach. Permit targets within attack range on walkable corridor cells cardinally adjacent to the room. Existing line-of-sight checks still gate acquisition and damage; guards do not pursue targets into corridors or other rooms.
- Permit an invader to fight a nearby corridor blocker at its locked-room doorway. Protected buildings remain excluded.
- No scenes, prefabs, settings, packages or save schemas changed.

## Evidence

Play Mode regressions in DungeonConstructionTests:

1. DoorwayCombatRecoversAfterInvaderIsPushedOutsideLockedRoom
2. DoorwayCombatGuardHitsEnemyAcrossBoundaryWithoutLeavingRoom
3. DoorwayCombatActualKnockbackReturnsInvaderToUnclearedRoom

All three fail on the original runtime code and pass with the fix. The third applies actual motor knockback, checks corridor displacement, then verifies walking back into the room and resumed damage. InvaderNavigatesTurnAndClearsTwoGuardsBeforeNextRoom also passes.

The StarterDungeon category ran 25 tests: original runtime 17 passed / 8 failed; fixed runtime 20 passed / 5 failed. All 17 original passes remain passes. The same five unrelated failures exist in both runs:

- InitialSectionsPreserveCoreAndSevenBySevenRoomConnects
- InvaderVisitsConnectedSideRoomBeforeAttackingCore
- StorageRoomTests.ConstructionStacksOnlyCompletedWarehousesAndRestoresCapsAndHud
- StorageRoomTests.RecruitmentUsesFSelectionAndEscapeWithoutSpendingOrPausing
- StorageRoomTests.StartingRoomHasFreeRecruitmentFacilityAndRestoreNeverDuplicatesIt

The first two expect starting side rooms that the current construction initializer no longer creates. The other three concern initial recruitment/storage setup. They were not changed by this fix. Final restored runtime files match the tested fixed version byte-for-byte; Unity recompilation completed with no errors. Source diff whitespace checks passed.


## Follow-up: five historical test failures resolved

The five failures were stale fixtures/assertions rather than new runtime regressions:

- InitialSectionsPreserveCoreAndSevenBySevenRoomConnects expected four initial sections and an 11x5 hall. It now verifies the current two-section layout (7x7 core room and the entrance-to-core hall, three cells wide) and retains the large-room construction and defender-routing checks.
- InvaderVisitsConnectedSideRoomBeforeAttackingCore assumed a free starting side room. It now builds a connected room explicitly, checks that the invader does not select the core first, and walks every route waypoint to assert arrival at the side-room center.
- ConstructionStacksOnlyCompletedWarehousesAndRestoresCapsAndHud assumed free starting storage and unlimited room items. It now begins with base capacities 1000/200/50, grants two storage items as test inputs, and verifies queued versus completed capacity, stacking, save restoration, all three HUD labels, and preservation of saved over-cap balances after removing storage bonuses.
- RecruitmentUsesFAndEscapeWithoutSpendingOrPausing formerly stood at the core, whose nearer F interaction correctly opens core research. It now stands nearer the recruitment facility, checks the actual RandomSummonButton with zero corpse stacks, and preserves the F/Escape, no-spending, no-placement and no-pause assertions. Temporary input settings follow the established DontUnloadUnusedAsset cleanup pattern.
- StartingRoomHasFreeRecruitmentFacilityAndRestoreNeverDuplicatesIt expected the removed post-summon placement flow. It now frees one of the four starting guard slots, funds two corpse stacks, clicks the actual random summon button, and verifies immediate spawn, consumption of exactly one stack, unchanged mana, persisted stack/health/guards, and idempotent facility restoration.

Storage tests select Easy/manual-wave mode to isolate the tested interactions from unrelated automatic invasion timing. No runtime behavior, scenes, prefabs or production settings were changed in this follow-up.

Final validation: Unity compilation completed without errors. StarterDungeon Play Mode category passed **25/25**, zero failed, zero skipped (21.03 seconds), including all five corrected tests and the three doorway regressions. Focused source diff whitespace checks passed.
