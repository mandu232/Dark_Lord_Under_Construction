# Character contact shadows

Latest revision: ellipse width and height have been increased by 40%, keeping the same foot anchors and alpha/held-state behavior. See `Docs/AI/SharedVitalsAndShadows.md`.

Added 2026-10-31 to the InGame player and the BloodSlimeGuard, BloodArcher, BloodSupport, GuardTemplate and WaveInvader prefabs. Existing projected player shadow is preserved.

Each actor has a small soft black ellipse at its authored idle-foot position. `Tools/SetupCharacterContactShadows.cs` measures opaque pixels from the idle sprite, compensates for trimmed texture rectangles and padded animation frames, and sizes the ellipse from the bottom band of the silhouette. This keeps the ground anchor stable across animated poses and flips. All shadows use one shared `ActorContactShadow` material and the existing full-rect white lighting quad. Actor SortingGroups keep the shadow immediately behind its body.

`ActorContactShadow` follows body visibility and alpha, hides while a minion is held (the existing carried-minion preview has its own ground shadow), and reactivates on release/reenable. It allocates no sprites, textures or material instances per frame. Source character art and gameplay physics are unchanged.

Verified actual Game View with the player and four minions; verified an offscreen invader instance follows movement and alpha, hides when its body is hidden or held, and restores after release and disable/enable. All seven runtime checks passed. Unity compiled and the contact shader is supported with zero diagnostics; Console errors zero. Play Mode was stopped before this task and returned to stopped after temporary validation. No gameplay saves were written. Screenshot: `Docs/AI/CharacterContactShadowsInGame.png`.
