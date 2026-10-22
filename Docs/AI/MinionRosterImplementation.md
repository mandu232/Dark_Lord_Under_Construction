# Minion roster — 2026-10-22

Recruitment offers Blood Slime (frontline, HP100/attack10), Blood Archer (혈침 슬라임, HP60/magic attack14/range6/cooldown1.25s) and Blood Support (수혈 슬라임, HP70/magic attack4/range3.5/cooldown1.6s). All cost20 mana on valid placement, sharing capacity and existing placement checks.

Support heals another injured minion14 every3.5s within4.5 world units, prioritizing missing-health ratio in its assigned room. Terrain blocks healing. A .55s cast interrupts on damage or orders/relocation; self, worker and lord healing are excluded. Magic facility attack bonuses scale healing. Ranged attacks retain normal windup, cooldown and terrain checks. Shot lines last .16s; damage resolves at windup completion (hitscan, no pending projectile state). Existing animations are reused with color tints, role badges and shot/heal lines.

All attacks share CombatAugments: lord marks, minion slow and kill tribute apply to the new roles. Blood Pact (ID9, 혈막의 맹약) now empowers the recipient's next attack after support healing as well as ward healing. No procs during preparation or pause.

Resources/MinionRoster.asset references two new prefabs, authored by Tools/SetupMinionRoster.cs. RecruitmentPanel displays stats and suggested augments using existing UI frame/button sprites. Forecast hides during recruitment. Inspection includes role and healing stats.

New games put the starting recruitment facility at Core+(1,1). Loads move a recognizable old initial-room-center station to an empty core cell, preserving damage/ruins/income state and avoiding occupied posts. Extra custom stations and missing stations remain authoritative; terrain stays unchanged.

Optional save fields preserve role, attack cooldown and healing state. Missing roles default to Blood Slime. Empty JsonUtility support objects are accepted for legacy actors; meaningful support state on other roles and invalid values are rejected. Restored recruits belong to the gameplay scene.

Validation: Unity compilation passed. MinionRoster Play Mode category38/38 passed (8 roster,10 augment,10 facility,7 save,3 storage/recruitment). Covers ranged hits/walls, augmentation procs, healing priority/interruption/pause, facility traits, mixed-party saves, selection/cost and migration. Actual1920x1080 Game View reviewed at Temp/minion-roster-preview.png. No player build or full-suite success claimed; complete-run balance needs playtesting.
