using UnityEngine;
using UnityEngine.Tilemaps;
namespace DarkLord.Dungeon
{
    public sealed partial class DungeonGrid
    {
        [Header("Dungeon artwork")]
        [SerializeField] private TileBase roomFloorTile, corridorCarpetTile, repeatingWallTile;
        [SerializeField] private TileBase[] stonePieces, carpetPieces, wallPieces;
        private Tilemap floorMap, floorShadeMap, carpetMap, cornerMap, shadowMap, wallMap, glowMap;
        private Tile carpetFill;
        private Sprite carpetFillSprite;
        private Tile ceilingTile;
        private Sprite ceilingSprite;
        private Tile[] roomWallPieces, bannerWallPieces;
        private Tile[] floorVariants, carpetCorners;
        private Tile[] carpetInnerCorners;
        private Tile wallShadow, pillarShadow, pillarShadowEnd, windowGlow;
        private Tile[] floorShades;
        private Transform artRoot;
        private bool deferArtRefresh;
        public bool UsesArtTiles => roomFloorTile != null && corridorCarpetTile != null && repeatingWallTile != null;
        private static Vector3Int ArtPosition(Vector2Int cell) => new Vector3Int(cell.x * 2, cell.y * 2, 0);
        public TileBase FloorArtAt(Vector2Int cell) => floorMap != null ? floorMap.GetTile(ArtPosition(cell)) : null;
        public TileBase CarpetArtAt(Vector2Int cell) => carpetMap != null ? carpetMap.GetTile(ArtPosition(cell)) : null;
        public TileBase WallArtAt(Vector2Int cell) => wallMap != null ? wallMap.GetTile(ArtPosition(cell)) : null;
        public TileBase CarpetPieceAt(Vector2Int pixelCell) => carpetMap != null ? carpetMap.GetTile(new Vector3Int(pixelCell.x, pixelCell.y, 0)) : null;
        private Tilemap ArtLayer(string name, int order)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(artRoot, false);
            var map = go.GetComponent<Tilemap>();
            map.tileAnchor = new Vector3(.5f, .5f, 0);
            go.GetComponent<TilemapRenderer>().sortingOrder = order;
            return map;
        }
        private void BuildArtTiles()
        {
            if (!UsesArtTiles) return;
            var root = new GameObject("Native 16px artwork", typeof(Grid));
            root.transform.SetParent(generated.transform, false);
            root.GetComponent<Grid>().cellSize = Vector3.one;
            artRoot = root.transform;
            floorMap = ArtLayer("Room stone floor", -32000);
            floorShadeMap = ArtLayer("Stone tile shading", -31999);
            carpetMap = ArtLayer("Continuous corridor carpet", -31999);
            cornerMap = ArtLayer("Carpet inner corners", -31998);
            shadowMap = ArtLayer("Wall and pillar shadows", -31997);
            wallMap = ArtLayer("North wall faces and brown wall tops", -31996);
            glowMap = ArtLayer("Window glow", -31995);
            wallShadow = MakeEffectTile(false, false, false);
            pillarShadow = MakeEffectTile(false, true, false);
            pillarShadowEnd = MakeEffectTile(false, true, true);
            windowGlow = MakeEffectTile(true, false, false);
            floorShades = MakeFloorShades();
            carpetInnerCorners = MakeInnerCorners();
            if (wallPieces != null && wallPieces.Length > 0 && wallPieces[0] is Tile wallSource && wallSource.sprite != null)
            {
                var texture = wallSource.sprite.texture;
                ceilingSprite = Sprite.Create(texture, new Rect(288, 240, 16, 16), new Vector2(.5f, .5f), 16);
                ceilingTile = ScriptableObject.CreateInstance<Tile>();
                ceilingTile.sprite = ceilingSprite;
                ceilingTile.colliderType = Tile.ColliderType.None;
                roomWallPieces = MakeWallPieces(texture, 192);
                bannerWallPieces = MakeWallPieces(texture, 96);
                floorVariants = MakeFloorVariants(texture);
                carpetCorners = MakePieces(texture, 112, 48, 2, 2);
            }
            // The new carpet has borders on both halves, but no separate plain center tile.
            // Stretch one solid-red pixel to fill interior cells without repeating its borders.
            if (corridorCarpetTile is Tile carpet && carpet.sprite != null)
            {
                var texture = carpet.sprite.texture;
                var rect = carpet.sprite.rect;
                carpetFillSprite = Sprite.Create(texture, new Rect(rect.x + 8, rect.y + 9, 1, 1), new Vector2(.5f, .5f), 1);
                carpetFill = ScriptableObject.CreateInstance<Tile>();
                carpetFill.sprite = carpetFillSprite;
                carpetFill.colliderType = Tile.ColliderType.None;
            }
            foreach (var p in new RectInt(0, 0, Layout.Width * 2, Layout.Height * 2).allPositionsWithin) PaintArtPiece(p);
        }
        private bool ArtFloor(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < Layout.Width * 2 && p.y < Layout.Height * 2 && Layout.IsFloor(new Vector2Int(p.x / 2, p.y / 2));
        private bool ArtCarpet(Vector2Int p)
        {
            if (!ArtFloor(p)) return false;
            var section = Construction.At(new Vector2Int(p.x / 2, p.y / 2));
            return section != null && !DungeonConstruction.IsRoom(section.Kind);
        }
        // Continue the floor one gameplay cell behind north walls so transparent pixels
        // in the wall foot reveal floor rather than the empty background.
        private bool BackedFloor(Vector2Int p) => ArtFloor(p) || ArtFloor(p + Vector2Int.down) || ArtFloor(p + Vector2Int.down * 2);
        private static TileBase Piece(TileBase[] pieces, int index, TileBase fallback) => pieces != null && index < pieces.Length && pieces[index] != null ? pieces[index] : fallback;
        private static Tile[] MakeWallPieces(Texture2D texture, int startX)
        {
            var pieces = new Tile[25];
            for (int row = 0; row < 5; row++)
                for (int column = 0; column < 5; column++)
                {
                    var tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = Sprite.Create(texture, new Rect(startX + column * 16, 240 - row * 16, 16, 16), new Vector2(.5f, .5f), 16);
                    tile.colliderType = Tile.ColliderType.None;
                    pieces[row * 5 + column] = tile;
                }
            return pieces;
        }
        private static Tile MakeEffectTile(bool glow, bool pillar, bool tail)
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float alpha;
                    if (glow)
                    {
                        float dx = (x - 7.5f) / 8f, dy = (y - 7.5f) / 8f;
                        alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)) * .34f;
                        texture.SetPixel(x, y, new Color(1f, .12f, .08f, alpha));
                    }
                    else
                    {
                        // Narrower steps soften the falloff while retaining the pixel-art silhouette.
                        float t = (y / 2 + 1) / 8f;
                        float strength = pillar ? .66f : .58f;
                        alpha = !pillar ? strength * t : tail ? t * strength * .125f : strength * (.125f + .875f * t);
                        texture.SetPixel(x, y, new Color(.015f, .01f, .04f, alpha));
                    }
                }
            texture.Apply();
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 16);
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }
        private static Tile[] MakeFloorShades()
        {
            var result = new Tile[4];
            for (int i = 0; i < result.Length; i++)
            {
                bool right = i % 2 == 1, bottom = i < 2;
                var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
                {
                    float side = right ? Mathf.Clamp01((x - 10f) / 5f) * .13f : 0;
                    float lower = bottom ? Mathf.Clamp01((5f - y) / 5f) * .15f : 0;
                    texture.SetPixel(x, y, new Color(.015f, .01f, .04f, Mathf.Max(side, lower)));
                }
                texture.Apply();
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 16);
                tile.colliderType = Tile.ColliderType.None;
                result[i] = tile;
            }
            return result;
        }
        private static Tile[] MakeInnerCorners()
        {
            var result = new Tile[4];
            for (int corner = 0; corner < 4; corner++)
            {
                var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color[256];
                bool right = (corner & 1) != 0, bottom = corner >= 2;
                for (int a = 0; a < 4; a++) for (int b = 0; b < 4; b++)
                {
                    int x = right ? 15 - a : a, y = bottom ? b : 15 - b;
                    if (a < 2 || b < 2) pixels[y * 16 + x] = new Color32(218, 139, 62, 255);
                }
                texture.SetPixels(pixels); texture.Apply();
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 16);
                tile.colliderType = Tile.ColliderType.None;
                result[corner] = tile;
            }
            return result;
        }
        private static void DestroyEffectTile(Tile tile)
        {
            if (tile == null) return;
            if (tile.sprite != null)
            {
                var texture = tile.sprite.texture;
                Destroy(tile.sprite);
                if (texture != null) Destroy(texture);
            }
            Destroy(tile);
        }
        private static Tile[] MakePieces(Texture2D texture, int startX, int topY, int width, int height)
        {
            var result = new Tile[width * height];
            for (int row = 0; row < height; row++)
                for (int col = 0; col < width; col++)
                {
                    var tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = Sprite.Create(texture, new Rect(startX + col * 16, topY - row * 16, 16, 16), new Vector2(.5f, .5f), 16);
                    tile.colliderType = Tile.ColliderType.None;
                    result[row * width + col] = tile;
                }
            return result;
        }
        private static Tile[] MakeFloorVariants(Texture2D texture)
        {
            var result = new Tile[16];
            for (int variant = 0; variant < 4; variant++)
            {
                var pieces = MakePieces(texture, 16 + variant % 2 * 48, 144 - variant / 2 * 48, 2, 2);
                System.Array.Copy(pieces, 0, result, variant * 4, 4);
            }
            return result;
        }
        private static uint ArtHash(int x, int y)
        {
            unchecked
            {
                uint value = (uint)x * 0x9E3779B9u ^ (uint)y * 0x85EBCA6Bu;
                value ^= value >> 16; value *= 0x7FEB352Du;
                value ^= value >> 15; value *= 0x846CA68Bu;
                return value ^ (value >> 16);
            }
        }
        private static void DestroyWallPieces(Tile[] pieces)
        {
            if (pieces == null) return;
            foreach (var tile in pieces)
            {
                if (tile == null) continue;
                if (tile.sprite != null) Destroy(tile.sprite);
                Destroy(tile);
            }
        }
        private bool WallFoot(Vector2Int p) => ArtFloor(p) && !ArtFloor(p + Vector2Int.up);
        // Distribute spans of three or four cells between single pillars, anchored at both ends.
        private int WallColumn(Vector2Int foot, out int segmentStart, out bool shortenedInterior)
        {
            shortenedInterior = false;
            int left = foot.x, right = foot.x;
            while (WallFoot(new Vector2Int(left - 1, foot.y))) left--;
            while (WallFoot(new Vector2Int(right + 1, foot.y))) right++;
            int distance = right - left;
            if (distance == 0) { segmentStart = left; return 0; }
            int intervals = Mathf.CeilToInt(distance / 4f);
            int shortLength = distance / intervals, longer = distance % intervals;
            int start = left;
            for (int i = 0; i < intervals; i++)
            {
                int end = start + shortLength + (i < longer ? 1 : 0);
                if (foot.x <= end)
                {
                    segmentStart = start;
                    if (foot.x == start) return 0;
                    if (foot.x == end) return end == right ? 4 : 0;
                    int offset = foot.x - start;
                    shortenedInterior = end - start < 4;
                    return end - start == 3 && offset == 2 ? 3 : offset;
                }
                start = end;
            }
            segmentStart = left;
            return 4;
        }
        private void PaintArtPiece(Vector2Int p)
        {
            if (p.x < 0 || p.y < 0 || p.x >= Layout.Width * 2 || p.y >= Layout.Height * 2) return;
            var position = new Vector3Int(p.x, p.y, 0);
            bool floor = ArtFloor(p);
            uint floorRoll = ArtHash(p.x / 2, p.y / 2) % 15;
            int variant = floorRoll < 12 ? 0 : (int)floorRoll - 11;
            int floorIndex = variant * 4 + (1 - p.y % 2) * 2 + p.x % 2;
            floorMap.SetTile(position, BackedFloor(p) ? (floorVariants != null ? floorVariants[floorIndex] : Piece(stonePieces, (1 - p.y % 2) * 3 + p.x % 2, roomFloorTile)) : null);
            var section = Construction.At(new Vector2Int(p.x / 2, p.y / 2));
            floorMap.SetTileFlags(position, TileFlags.None);
            floorMap.SetColor(position, section != null && section.Kind == ConstructionKind.StorageRoom
                ? new Color(1f, .82f, .55f) : section != null && section.Kind == ConstructionKind.AlchemyRoom
                ? new Color(.8f, .65f, 1f) : Color.white);
            floorShadeMap.SetTile(position, floor && !ArtCarpet(p) ? floorShades[(p.y % 2) * 2 + p.x % 2] : null);
            // The source carpet runs vertically. Rotate its edge pieces for horizontal runs.
            TileBase carpetTile = null;
            TileBase cornerTile = null;
            float rotation = 0;
            if (ArtCarpet(p))
            {
                bool left = !ArtCarpet(p + Vector2Int.left), right = !ArtCarpet(p + Vector2Int.right);
                bool up = !ArtCarpet(p + Vector2Int.up), down = !ArtCarpet(p + Vector2Int.down);
                if ((left || right) && (up || down) && carpetCorners != null)
                    carpetTile = carpetCorners[(up ? 0 : 2) + (left ? 0 : 1)];
                else if (left || right)
                    carpetTile = Piece(carpetPieces, (p.y % 2 == 0 ? 6 : 0) + (left ? 0 : 2), corridorCarpetTile);
                else if (up || down)
                {
                    carpetTile = Piece(carpetPieces, (p.x % 2 == 0 ? 6 : 0) + (up ? 0 : 2), corridorCarpetTile);
                    rotation = -90;
                }
                else
                {
                    carpetTile = carpetFill != null ? carpetFill : corridorCarpetTile;
                    if (!ArtCarpet(p + Vector2Int.up + Vector2Int.left)) cornerTile = carpetInnerCorners[0];
                    else if (!ArtCarpet(p + Vector2Int.up + Vector2Int.right)) cornerTile = carpetInnerCorners[1];
                    else if (!ArtCarpet(p + Vector2Int.down + Vector2Int.left)) cornerTile = carpetInnerCorners[2];
                    else if (!ArtCarpet(p + Vector2Int.down + Vector2Int.right)) cornerTile = carpetInnerCorners[3];
                }
            }
            carpetMap.SetTile(position, carpetTile);
            carpetMap.SetTileFlags(position, TileFlags.None);
            carpetMap.SetTransformMatrix(position, Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotation)));
            cornerMap.SetTile(position, cornerTile);
            TileBase shadow = null;
            if (floor && WallFoot(p))
            {
                int column = WallColumn(p, out _, out _);
                shadow = column == 0 || column == 4 ? pillarShadow : wallShadow;
            }
            else if (floor && WallFoot(p + Vector2Int.up))
            {
                int column = WallColumn(p + Vector2Int.up, out _, out _);
                shadow = column == 0 || column == 4 ? pillarShadowEnd : null;
            }
            shadowMap.SetTile(position, shadow);
            TileBase wall = null;
            bool litWindow = false;
            if (!floor)
            {
                wall = ceilingTile != null ? ceilingTile : repeatingWallTile;
                // Upright faces exist only above open floor. Side/bottom walls remain solid top faces.
                for (int depth = 1; depth <= 5; depth++)
                {
                    var foot = p + Vector2Int.down * depth;
                    if (!ArtFloor(foot)) continue;
                    int wallColumn = WallColumn(foot, out int segmentStart, out bool shortenedInterior);
                    int index = (5 - depth) * 5 + wallColumn;
                    var owner = Construction.At(new Vector2Int(foot.x / 2, foot.y / 2));
                    bool corridor = owner != null && !DungeonConstruction.IsRoom(owner.Kind);
                    litWindow = corridor && !shortenedInterior && wallColumn == 2 && depth <= 3;
                    if ((!corridor || shortenedInterior) && roomWallPieces != null) wall = roomWallPieces[index];
                    else if (corridor && bannerWallPieces != null && ArtHash(segmentStart, foot.y) % 5 == 0) wall = bannerWallPieces[index];
                    else wall = Piece(wallPieces, index, repeatingWallTile);
                    break;
                }
            }
            wallMap.SetTile(position, wall);
            glowMap.SetTile(position, litWindow ? windowGlow : null);
        }
        private void RefreshArtTile(Vector2Int cell)
        {
            if (deferArtRefresh || !UsesArtTiles || floorMap == null) return;
            // Excavation changes carpet boundaries and the five rows of upright wall above it.
            for (int y = -2; y <= 6; y++)
                for (int x = 0; x < Layout.Width * 2; x++) PaintArtPiece(new Vector2Int(x, cell.y * 2 + y));
        }
        private void RefreshAllArtTiles()
        {
            if (!UsesArtTiles || floorMap == null) return;
            foreach (var p in new RectInt(0, 0, Layout.Width * 2, Layout.Height * 2).allPositionsWithin) PaintArtPiece(p);
        }
    }
}

