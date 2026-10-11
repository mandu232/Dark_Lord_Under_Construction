# Gothic title logos

Created with built-in image_gen using the user supplied title reference for style.

- Assets/Art/UI/MainMenu/GothicTitle-English.png: DARK LORD / UNDER CONSTRUCTION
- Assets/Art/UI/MainMenu/GothicTitle-Korean.png: 마왕 / 공사 중
- Docs/GothicTitle-Preview.png: both assets composited over a dark background for inspection only.

Prompt: standalone transparent game title, scarlet high-contrast gothic calligraphy, elongated swashes, diamond terminals and restrained antique ivory/blush flowers; exact title text as above; no scenery, bevel or watermark. English refinement requested opaque crisp scarlet letter fills and transparent negative space. Korean uses the first generation, which retained correct flowers and legible Hangul. Both deliverables retain generated alpha. RGB-only viewers may show color bleed in transparent pixels; composited preview confirms the visible artwork.

New standalone assets; existing title scene reference has not been replaced.

## Applied flowerless titles (2026-10-10)

Built-in image_gen edits removed all flowers and reconstructed the covered lettering. Korean prompt replaces the title with exactly `마왕님은` / `공사중`, retaining crimson high-contrast gothic strokes, pointed serifs and swashes. English prompt preserves `DARK LORD` / `UNDER CONSTRUCTION`. Both request genuine transparent alpha and no additional ornaments.

Current assets: `Assets/Art/UI/MainMenu/GothicTitle-English-NoFlowers.png` and `GothicTitle-Korean-NoFlowers.png`.

Applied to MainMenu.prefab and scene menu instances. Default: Korean. Select Crimson Title > Main Menu Title > Language to switch between Korean and English; both layers switch together. Existing settings language remains Korean-only.

White Depth uses TitleWhiteSilhouette.mat and TitleSilhouette.shader, drawing the same sprite alpha in white behind the red artwork at (5,-5). Reuses MainMenuParallax unchanged: white travel 3, red travel 6, response 4, unscaled-time smoothing and pointer/focus behavior identical to background. Title center is preserved, height expanded for the two-line artwork. Child images do not intercept clicks. Existing GameMenu title visibility hides both layers for modal panels.

Validation: compilation completed without errors; both language variants inspected at 1280x720; runtime layer displacement verified and disabling restored white to (5,-5) and red to (0,0). Play-mode language switch restored to Korean. Game view previews: Docs/TitleKoreanPreview.png and Docs/TitleEnglishPreview.png.
