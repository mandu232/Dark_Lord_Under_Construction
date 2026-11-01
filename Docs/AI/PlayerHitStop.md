# Player hit stop — 2026-11-01

The existing uniform 45ms PlayerHitStop is now an attack-specific impact response. PlayerCombat.DealDamage requests outgoing impact only when authoritative HP actually decreases. ActorHealth's existing hook handles direct incoming player damage. Passive core pulses and NPC-only combat no longer request a global hit stop.

| Impact | Real-time duration |
| --- | --- |
| Basic slash | 35ms |
| Second combo slash | 45ms |
| Wide slash | 55ms |
| Blood / frost nova | 50ms |
| Blood lance | 25ms |
| Killing blow | +15ms, capped at 70ms |
| Player takes damage | 45ms |
| Player hit by a committed charge / hero cleave | 55ms |

During the impact window, time runs at 3% of the previous scale, retaining compatibility with gameplay methods that require a positive clock. Unscaled time ends the pulse and restores the previous speed. The existing brief camera shake remains. Visual effects and animations naturally pause with the scaled clock; no damage, cooldown, mana, audio or save data changes are needed.

Same-frame AoE hits select only the strongest duration, measured from the first hit. They never add durations. Later frames cannot extend a running impact, and new pulses have a 140ms real-time refractory period. Misses, blocks, unchanged HP, dead/disabled players and a disabled PlayerHitStop component do not start pulses. Disabling PlayerCombat cancels its owned pulse. External pauses/speed changes are preserved; existing GameMenu/tutorial/reward CancelActive hooks remain compatible.

Changed runtime files: PlayerHitStop.cs, PlayerCombat.cs, PlayerCombatTechnology.cs. No Inspector/prefab/scene setup is required.

Validation:
- Unity compilation succeeded.
- PlayerHitStopTests: 9/9 passed. Actual nova damage on 20 targets, no cumulative AoE freeze, repeated same-frame killing impacts, refractory window, misses, ward blocks, NPC/passive damage, restoration of an existing .5 speed, pause/external clock ownership, disabled component/combat and reward pause on a death callback.
- Actual Nightmare wave 5 through the real scheduler: 19 mixed invaders and seven defenders, using the existing review-only higher friendly HP. A dense 8-second segment observed 12 impact pulses, .691s total sampled slowdown, longest sample 73.1ms. Another segment observed 3 pulses, .203s total, longest 71.4ms. End-of-frame sampling can exceed the authored 70ms maximum by one frame. This is not a frame-rate performance benchmark.
- Live GameMenu pause during an incoming hit: impact scale .03 -> paused 0 -> resumed 1. The same encounter reached zero enemies and RewardSelection at scale 0.
- Returned to stopped MainMenu at scale 1, without writing scene/gameplay saves. Generated TMP font caches restored. No full player build was run.