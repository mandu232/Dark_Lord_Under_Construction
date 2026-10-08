# Dungeon tile artwork

Source: Assets/Art/Dungeon/DungeonTiles.png (the supplied image, unchanged).

All rendered tile pieces are native 16×16 sprites at 16 PPU with identity transforms, Point filtering, no mipmaps or compression. This matches the Player sprite's 16 PPU and unit scale. Each existing 32px gameplay cell contains four visual pieces on a separate one-unit Grid; movement, construction, room capacity and collision coordinates remain unchanged.

Stone floor repeats the source 3×3 pieces without resizing. Corridor carpet uses the source nine pieces: red interiors connect and gold trim appears only along the boundary of connected corridor carpet. Rooms have stone and no carpet.

Solid areas use the source plain brown top surface. Only solid tiles immediately above floor show the source upright wall: five native 16px rows form the original 80px height, with pillars at the ends. Side and lower boundaries remain brown, giving the supplied slightly oblique viewpoint without changing the gameplay grid.

Excavation refreshes neighboring carpet edges and wall faces above the changed floor. Save restoration reconstructs all visual layers. Artwork references are editable on DungeonGrid in SampleScene.

Floor and corridor carpet extend one gameplay cell (32px) behind north walls. Transparent pixels at the wall foot reveal the continued floor. This visual backing does not change layout or collision.

