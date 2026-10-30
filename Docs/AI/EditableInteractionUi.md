# Editable interaction UI prefabs

- Minion information: Assets/Prefabs/UI/InGame/MinionInspectionPanel.prefab
- Recruitment: Assets/Resources/UI/RecruitmentPanel.prefab
- Alchemy: Assets/Resources/UI/AlchemyRoomPanel.prefab
- Merchant: Assets/Resources/UI/EconomyPanel.prefab

Open each prefab in Prefab Mode. Enable its window child temporarily to preview hidden windows, then edit its RectTransforms, text styles, frame images, and button Sprite State. Controllers bind gameplay actions and update live values; they no longer recreate or restyle these windows. Preserve serialized component references when replacing UI objects. The minion stats ScrollRect and viewport are authored in its prefab.

GameHud instantiates the three facility prefabs from Resources/UI. TutorialTargetHighlight finds recruitment in the HUD children. CoreTechPanel has its original appearance; interaction styling is no longer applied to its root, tooltip, or buttons.

Validation: Unity compilation passed; each facility instantiated once with no missing serialized references. Runtime configuration preserved deliberately changed window dimensions for all three. Button states produced BoxButton_0 / BoxButton_1 / BoxButton_1 / BoxButton_0 for idle / hover / selected / deselected. Console had no runtime errors. Existing AlchemyRoomTests failed 7/7 in setup at line 38 (room placement returned Occupied); those tests did not reach UI checks.


## Shared pixel density

UiSpritePixelScale on scoped sliced Images matches their source-pixel dimensions to the construction book (root Canvas width / 405), taking the sprite PPU and Canvas reference PPU into account. Serialized button sizes provide enough space for slice borders. The density component never modifies RectTransforms at runtime or in Prefab Mode. This controls density only; authored positions, text styles and gameplay actions remain prefab-owned. CoreTechPanel is excluded.

The legacy construction CastleButton artwork had zero vertical slice borders, which stretched its pixels vertically regardless of PPU. ConstructionCastleButton.asset reuses that texture with local vertical borders; the shared original sprite is unchanged. ValidateUiPixelScale.Run checks the generated corner mesh against texture UV distances. All 44 scoped runtime meshes passed at Canvas widths 1920 and 1280. Unity compilation and runtime Console had no errors.


## Inspection layout consistency

The authored BuildingInspectionPanel appearance previously lived as overrides inside GameHud: Repair was (60,60), 300x76 there, but (0,20), 400x52 in the source prefab. Its presentation overrides and hierarchy additions/removals are now applied to BuildingInspectionPanel.prefab, preserving gameplay bindings in GameHud. Minion and building window layouts and all five serialized buttons match the source prefab in Play Mode (positions, dimensions, anchors and pivots). UiSpritePixelScale no longer calls SetSizeWithCurrentAnchors; authored RectTransforms remain authoritative. The 44 scoped pixel-mesh checks still pass at the standard Canvas reference size.

## Hidden minion stats regression

MinionInspectionPanel.SetupStatsScroll now searches inactive parents. The prefab starts with Information hidden, so the former default GetComponentInParent search missed the authored ScrollRect and created a nested ScrollRect inside its Content. The nested viewport received zero height and concealed all stats. Reusing the authored viewport restores stats display and scrolling without changing the prefab layout or close button geometry. InGame capture verified both the top stats and bottom movement/room-capacity rows. MinionInspectionLayoutTests.HiddenAuthoredViewportIsReusedAndCanScrollStats passed 1/1 in Play Mode.

## Authored close position and stats inset

Removed the hardcoded Close-button transform block from InteractionUiSkin.ApplyInspection and removed inherited RectTransform overrides from the MinionInspectionPanel instance in GameHud. The minion source prefab owns the layout. Stats Scroll now starts at x=80 rather than 59, with width=360, aligning the text with the heading and health value. Close itself retains its authored (60,28), 300x56.89 transform. Play Mode compares the actual source and instance values after opening. The regression test now deliberately supplies a different close-button position/size before Awake and verifies both survive initialization unchanged, alongside the hidden viewport/scroll checks (1/1 passed).
