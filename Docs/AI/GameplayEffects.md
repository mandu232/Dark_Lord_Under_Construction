# Gameplay effects integration — 2026-11-01

Assets/Resources/GameplayEffects.asset references the imported sheets directly; no duplicated textures or scene/prefab setup is required. GameplayEffectLibrary stores frame order, width, tint, duration and looping. Frames run left to right, then top to bottom for multi-row sheets.

| Trigger | Sheet / presentation |
| --- | --- |
| Lord melee / wide melee | red Slash01 / Slash02, rotated to attack direction |
| Blood nova | Blood_Curse, 7 world units wide, .6 seconds |
| Blood ward | Blood_Fire, follows lord for 2 seconds |
| Recruitment, including starting guards | Blood_Hole_Start followed by Blood_Hole_End |
| Lord mark newly applied | Blood_Curse on marked actor |
| Actual HP recovered, either faction | Holy/Heal; full HP and save restore do not trigger |
| Hero cleave / charge | HolySlash A / B; existing red danger telegraphs retained |
| Frost nova | blue-tinted HolyNova |
| Aegis / haste | HolyShield / subdued Blood_Fire, 3 / 6 seconds |

GameplayEffects owns one renderer pool per scene, capped at 48 renderers. Scaled time respects pause and hit stop. Renderers are reused, follow effects stop when their target is disabled/destroyed, and scene unloading destroys the pool. Damage, cooldown, mana and save formats are unchanged. Adjust visual tuning in the Resources asset. Unused sheets remain available for future matching abilities.

Validation:
- Unity 6000.6.2f1 compile passed; final Console ground truth has zero errors.
- GameplayEffectsTests: 2/2 passed. Covers all frame references/order, 80 requests capped at 48, pause, completion, renderer reuse, following, disabled owner and scene unload.
- CoreCombatLoopTests: 7/13 passed. All six failures reproduced with the six gameplay integration files temporarily replaced by their HEAD versions, then restored byte-for-byte from backups. Same failures: AttackHitsEnemyOnceDespiteMultipleCollidersAndHasCooldown, ClearOffersThreeUniqueRewardsPausesAndOnlyThenRecoversSurvivors, CoreUpgradeAddsOnlyNewCapacityAndStopsAtThree, InvasionRepairUsesBothGoldAndManaAndCannotUpgradeOrSummon, LordDeathLeavesCoreDormantWithItsOwnHealth, NonlethalDamageDoesNotMergeTheTwoHealthPools.
- Actual InGame TryNova and TryWard returned true; mid-animation blood effects visually inspected at Temp/pipeline-screenshots/gameplay-effects-blood.png. Not every other effect was visually inspected in combat.
- Returned to stopped MainMenu; no scene or gameplay save writes. Test-generated TMP caches restored.
## InGame visual tuning follow-up — 2026-11-01

Reviewed all ten remaining configured effects in the actual InGame Game View, including 130ms live playback samples and held intermediate/ending frames. Tested real PlayerCombat attack/offensive/support entry points, ActorHealth.Heal, AugmentStatus.MarkLord and MinionSummoning.CreateGuard. Hero effects used the real WaveInvader prefab at Rowen's 1.4 scale and InvaderTactics.ShowHeroEffect. This is controlled in-scene review, not a complete invasion playthrough. Tools/ReviewGameplayEffects.cs reproduces the review in Play Mode and changes runtime state only; leave Play Mode to discard it.

| Effect | Final tuning |
| --- | --- |
| Slash | width 2.6, .26s; down .15; all four cardinal directions visually checked |
| Wide slash | width 3.8 instead of 4.8, .32s instead of .4; forward offset .75 instead of 1 |
| Summon | 2.7 wide, half-height at slime feet; start + end sheets, .85s total; behind actor |
| Curse | 1.15 wide, .42s, .55 above origin, alpha .85; face remains readable |
| Heal | width 1.9, .6s, down .1; smaller sparkle envelope |
| Holy slash | width 3.8, .25s; down .2; checked on real hero |
| Holy charge | HolySlash B forward arc, 2.8 wide, height .65, blue-white tint; .35s matching charge |
| Frost nova | width 8 retained, .45s, ground offset -.55; behind actors |
| Aegis | width 3.2; .18s appearance, hold complete frame 5, .18s disappearance across 3s protection |
| Haste | width 1.6, height .55, ground offset -.65, alpha .5; behind actor, .2s final fade |

All effects now fade at the end instead of abruptly disappearing. Removed redundant vertical support traces; ally healing traces remain. Size, ground offset, height, draw depth, sustain frame, transition and fade are authored in the catalog. Damage/mana/cooldowns remain unchanged.

Evidence: Temp/pipeline-screenshots/effects-before-*.png, effects-final-*.png and effects-live-*.png. Includes actual hero, four-direction melee, summon closing phase, and shield/haste/heal overlap. Original blood nova/ward setup was retained.

Final validation: Unity compile passed; GameplayEffectsTests 3/3 passed, including shield opening/holding/expiration, pool pause/reuse/follow/cleanup and all catalog references/frame ordering. Returned to stopped MainMenu, no scene/gameplay save writes; generated TMP caches restored.
## Actual wave overlap and original color — 2026-11-01

Confirmed root cause: DungeonAtmosphere's ColorAdjustments applies exposure -0.3, contrast +12, filter (1,.79,.8) and saturation -8, plus a dark red Vignette. Global Light2D is white at intensity 1. Merely switching to an unlit material would still receive this post-processing. A frozen Heal on/off comparison is captured in effects-grade-before.png and effects-grade-diagnostic-off.png.

GameplayEffectsRendererFeature on Assets/Settings/Renderer2D.asset now draws tagged sprite effects and minion/ability traces after the grade. It uses a scene-camera R8 actor alpha mask, published through SetGlobalTextureAfterPass and consumed with UseGlobalTexture. The same raster orientation is used by both passes; raw SV_POSITION / _ScaledScreenParams is essential here (the post-processing UV-flip helper flips the mask incorrectly). Materials are serialized/referenced assets, preserving them in builds. ActorDepthSorting registers enabled body renderers so the pass can draw their current sprite silhouettes without scanning the scene each frame. Source texture RGB and authored per-spell tints remain; the dungeon profile and normal actor/background rendering are unchanged.

Silhouettes attenuate covering effect alpha by 90%. Large blood nova alpha is .5, blood ward .4 with width 1.9 and lower anchor, slash .8, wide slash .7, holy slash .8, holy charge .75, heal .85, curse/summon .7, frost .65, Aegis .75, haste .3. Aegis now uses white tint so its original gold remains. Repeated Heal/Curse requests on one actor retain a single active visual; gameplay healing is unaffected. MinionShot uses the shared original-color trace material instead of creating/destroying a material per shot.

Actual wave review: Tools/ReviewWaveEffects.cs enters the real Nightmare wave-5 scheduler (prior waves marked completed only to jump to this scenario), spawning 19 mixed invaders with their normal roles, pathfinding, attacks, healing and cooldowns. Four frontline defenders plus one archer and two supports fight near the entrance. Review-only higher friendly HP keeps the encounter available for observation; player casts obey normal mana/cooldowns. This is a controlled real-wave combat scenario, not a fresh campaign or balance test.

Before tuning, peak active sprite VFX was 11. After tuning, a full encounter reached the same peak 11, then defeated all 19 invaders and entered RewardSelection after roughly 21 seconds of simulated combat; all four enemy roles were observed. A second run captured the original-color, masked blood nova during the ongoing wave. Artifacts: effects-wave-before.png, effects-wave-after-8s.png, effects-wave-after-16s.png, effects-wave-blood-overlap.png in Temp/pipeline-screenshots. Characters remained identifiable while attacks, healing and shields overlapped.

Validation:
- Unity compile passed and final shader diagnostics were empty.
- GameplayEffectsTests 4/4 passed, including repeated-heal suppression and reuse, shield timing, frame references and bounded pool lifecycle.
- Tools/ValidateGameplayEffectRendering.cs passed on actual GPU rendering with a deliberately strong red volume: effect RGB delta on/off = 0.0000, green effect = 1.000, blue trace = 1.000, background green changed 0.000 -> 0.996. An actor inside the green effect retained graded red: RGBA(.706,.345,0,1), proving silhouette protection.
- Render Graph review: all used pass data initialized; mask published/declared explicitly; output attachment read/write for alpha blending; no color-source sampling/blit or depth/normal copies; static render functions; no persistent graph handles. GPU validation covered both a normal render-texture camera and the actual pixel-perfect InGame camera visually. Renderer feature references and feature map are saved.
- Renderer2D serialization also removed deprecated URP fields during its normal Editor save. No atmosphere, scene, physics/layer or save-data settings were changed. Returned to stopped MainMenu with clock 1; temporary combat state discarded and generated font caches restored. Full player build was not run.
- Final GPU rerun uses a depth-backed 128x128 camera target, as required by Render Graph. Passed with no new Console warnings/errors; final green background change 0.000 -> 0.996. Clock is 1 and MainMenu is stopped.
