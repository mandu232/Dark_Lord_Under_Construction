# Pixel vitals and Easy wave button

2026-10-31

- HP and MP use the existing point-filtered SliderTrack sprite with sliced caps, red/blue fills, pixel highlight/shade strips, and numeric values only.
- WaveStartButtonPresentation lives on GameHud and controls the existing Start Wave Button. Only manual (Easy) preparation shows it; tutorials and active/completed waves hide it. Gameplay-blocking panels and construction disable interaction.
- Existing CastleButton_0 supplies the button art. The obsolete Castle Frame Image is disabled because it covered the new sprite with a white rectangle.
- Resources/WaveRewardSkin stores both sprite references. No new raster assets are generated.

Validation: compilation passed; SharedCoreHealth Play Mode category passed 10/10, including all four difficulties, button activation/click, numeric labels, sprite assignments and hidden obsolete overlay. Actual composited Game View checked in Easy (PixelVitalsEasy.png) and Normal (button hidden). Editor returned to stopped MainMenu.
