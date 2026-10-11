# Title character idle

The Crimson Repose image now uses `Assets/Art/UI/MainMenu/CharacterIdle.mat` and `Assets/Shaders/MenuCharacterIdle.shader`. Original artwork is unchanged.

Local UV deformation adds a 0.9-degree slow arm/glass sway and subtle shoulder/head breathing. The softly bounded masks exclude the chair and desk. This is a subtle illustration animation, not a separately rigged arm or a full drinking animation. UV masks are specific to the existing standalone CrimsonRepose texture and must be adjusted if the art changes; do not pack it into an atlas.

The existing MenuLightAnimation component provides unscaled time with a private material instance; the existing mouse parallax remains independent. Applied to MainMenu prefab and scene instances. Material inspector controls: Arm Sway Degrees, Breathing UV Distance, Idle Speed. Set the first two to zero to disable deformation.

Validation: shader compilation produced no errors; Game view inspected at 1280x720; runtime clock advances while Time.timeScale is zero; runtime material is separate from the asset. Preview: Docs/CharacterIdlePreview.png.

User adjustment: increased arm sway from 0.9 to 1.2 degrees and breathing distance from 0.0012 to 0.0016 (about 33% stronger). Speed unchanged. Saved material values verified.
