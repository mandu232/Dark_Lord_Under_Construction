# Castle archive save screen

The save screen uses a distinct three-column archive layout rather than the settings panel layout. It retains the dark plum, burgundy and scarlet pixel-art style, with tall ledger frames, a bat crest, hanging chains and candles.

Generated artwork: `Assets/Art/UI/MainMenu/CastleArchive.png` (built-in image generation, reference: current CastlePanel.png). Prompt direction: three tall empty vertical archive cards, opaque full-screen gothic castle background, limited burgundy palette, no text, pixel-art edges.

Applied to `Assets/Prefabs/UI/SaveSlotPanel.prefab` and the MainMenu/SampleScene instances. Live text and existing save callbacks remain separate from the artwork. The selected card receives a translucent crimson tint. Texture import uses Point filtering, no mipmaps and no compression.

Validation: inspected 1280x720 Game view; verified all three slot callbacks and cancel/overwrite callbacks; opened and dismissed overwrite confirmation and returned to the main menu without writing save data. Preview: `Docs/ArchivePreview.png`.

`CastleSavePanel-v1.png` is an unused earlier draft.
