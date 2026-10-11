# Continuous castle menu

MainMenu scene now uses a continuous castle panorama. The title is centered between two UI stations: save slots on the left and settings on the right. CastleMenuPanorama moves the background, foreground and panels using unscaled time over 0.85 seconds with smooth start/stop. Reversals begin at the current position. Station controls remain blocked during travel. Title artwork remains independent of mouse movement; background and character retain their small pointer parallax.

User artwork copied without pixel changes:
- Assets/Art/UI/MainMenu/CastleArchive-Open.png from CastleArchive_2.png
- Assets/Art/UI/MainMenu/CastlePanel-Open.png from CastlePanel.png
Both have transparent interiors, Point filtering and uncompressed import. Settings has a faint dark backing for readability. Save cards have translucent fills and thin crimson borders. Original source images remain untouched.

CrimsonCastle-Panorama.png was created using built-in image_gen, referencing CrimsonCastle.png. Prompt: extend the castle to both sides into one continuous 3:1 interior, matching crimson/plum silhouette line art, central stained glass, connected dim left archive hall and right arched chamber, empty UI space, no people or text. The generated center is a matching repaint rather than a pixel-identical outpaint.

Title-scene changes are scene overrides; gameplay pause/settings presentation is unchanged. GameMenu's optional panorama reference is only assigned in MainMenu. SettingsPanelView externalPresentation disables its independent fade only for this station.

Validation: script compilation passed; inspected Main, Settings and Slots at 1280x720, transparent art reveals the same moving castle; fast Settings/Main/Slots/Main navigation returned to Main while timescale remained zero. No save data was written. Original light overlay used coordinates of the old castle and is hidden if attached below that background; character idle remains active.

Transition tint fix: parent input locks temporarily use normalColor for enabled Selectables' disabledColor. Restore original disabled tint on arrival; genuinely disabled controls retain their disabled appearance. Verified settings buttons remain input-blocked with matching normal tint during transition, and input returns on arrival. Compilation passed.
