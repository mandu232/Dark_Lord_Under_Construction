using UnityEngine;
using UnityEngine.Tilemaps;
namespace DarkLord.Dungeon
{
    public sealed partial class DungeonGrid
    {
        [Header("Dungeon artwork")]
        [SerializeField] private TileBase roomFloorTile, corridorCarpetTile, repeatingWallTile;
        [SerializeField] private TileBase[] stonePieces, carpetPieces, wallPieces;
        [SerializeField] private Texture2D storageWallTexture;
        [SerializeField] private Sprite windowLightSprite;
        [SerializeField] private Material windowLightMaterial;
        [SerializeField] private GameObject wallTorchPrefab;
        private readonly System.Collections.Generic.Dictionary<Vector2Int, GameObject> wallTorches = new System.Collections.Generic.Dictionary<Vector2Int, GameObject>();
        private Tilemap floorMap, floorShadeMap, carpetMap, cornerMap, shadowMap, wallMap, glowMap, wallJointMap;
        private Tile carpetFill;
        private Sprite carpetFillSprite;
        private Tile ceilingTile;
        private Sprite ceilingSprite;
        private Tile[] roomWallPieces, bannerWallPieces, verticalWallPieces, southWallPieces;
        private Tile[][] compactWallPieces, compactFacePieces;
        private Tile[][][] storageWallFaces;
        private Tile[] floorVariants, carpetCorners;
        private Tile[] carpetInnerCorners;
        private Tile wallShadow, pillarShadow, pillarShadowEnd, windowGlow;
        private Tile windowFloorLight;
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
            wallJointMap = ArtLayer("Inner wall corner backing", -31997);
            wallMap = ArtLayer("North wall faces and brown wall tops", -31996);
            glowMap = ArtLayer("Window glow", -31995);
            wallShadow = MakeEffectTile(false, false, false);
            pillarShadow = MakeEffectTile(false, true, false);
            pillarShadowEnd = MakeEffectTile(false, true, true);
            if (windowLightSprite != null && windowLightMaterial != null)
            {
                windowFloorLight = ScriptableObject.CreateInstance<Tile>();
                windowFloorLight.sprite = windowLightSprite;
                windowFloorLight.color = new Color(1f, .055f, .025f, .55f);
                windowFloorLight.colliderType = Tile.ColliderType.None;
                // The 16x32 mask reaches down two art cells from the wall/floor boundary.
                windowFloorLight.transform = Matrix4x4.Translate(new Vector3(0, -.5f, 0));
                glowMap.GetComponent<TilemapRenderer>().sharedMaterial = windowLightMaterial;
            }
            else windowGlow = MakeEffectTile(true, false, false);
            floorShades = MakeFloorShades();
            if (wallPieces != null && wallPieces.Length > 0 && wallPieces[0] is Tile wallSource && wallSource.sprite != null)
            {
                var texture = wallSource.sprite.texture;
                ceilingSprite = Sprite.Create(texture, new Rect(288, 240, 16, 16), new Vector2(.5f, .5f), 16);
                ceilingTile = ScriptableObject.CreateInstance<Tile>();
                ceilingTile.sprite = ceilingSprite;
                ceilingTile.colliderType = Tile.ColliderType.None;
                roomWallPieces = MakeWallPieces(texture, 192);
                bannerWallPieces = MakeWallPieces(texture, 96);
                verticalWallPieces = MakePieces(texture, 208, 144, 1, 8);
                southWallPieces = MakePieces(texture, 240, 144, 5, 1);
                // Only the upper eight pixels are the cornice; the lower half is opaque
                // background in the source and would hide the shaft at inner corners.
                for (int column = 0; column < southWallPieces.Length; column++)
                {
                    if (Application.isPlaying) Destroy(southWallPieces[column].sprite);
                    else DestroyImmediate(southWallPieces[column].sprite);
                    southWallPieces[column].sprite = Sprite.Create(texture, new Rect(240 + column * 16, 152, 16, 8), new Vector2(.5f, 0), 16);
                }
                compactFacePieces = MakeCompactFaces(texture, 192, 176);
                compactWallPieces = new Tile[5][];
                for (int height = 1; height < 5; height++)
                {
                    compactWallPieces[height] = new Tile[height];
                    float sliceHeight = 80f / height;
                    for (int row = 0; row < height; row++)
                    {
                        var tile = ScriptableObject.CreateInstance<Tile>();
                        tile.sprite = Sprite.Create(texture, new Rect(208, 32 + (height - row - 1) * sliceHeight, 16, sliceHeight), new Vector2(.5f, .5f), 16);
                        tile.transform = Matrix4x4.Scale(new Vector3(1, height / 5f, 1));
                        tile.colliderType = Tile.ColliderType.None;
                        compactWallPieces[height][row] = tile;
                    }
                }
                floorVariants = MakeFloorVariants(texture);
                carpetCorners = MakePieces(texture, 112, 48, 2, 2);
                carpetInnerCorners = MakePieces(texture, 64, 48, 2, 2);
            }
            if (storageWallTexture != null)
            {
                storageWallFaces = new Tile[3][][];
                for (int style = 0; style < storageWallFaces.Length; style++)
                {
                    int startX = 16 + style * 96;
                    var faces = MakeCompactFaces(storageWallTexture, startX, 0);
                    faces[5] = MakeWallPieces(storageWallTexture, startX, 64);
                    storageWallFaces[style] = faces;
                }
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
            if (!stagedPresentation) PaintOccupiedArt();
        }

        private void PaintOccupiedArt()
        {
            var routine = PaintOccupiedArtRoutine();
            while (routine.MoveNext()) { }
        }

        private System.Collections.IEnumerator PaintOccupiedArtRoutine()
        {
            var budget = System.Diagnostics.Stopwatch.StartNew();
            int width = Layout.Width * 2, height = Layout.Height * 2;
            // Distant rock needs only a ceiling tile, not eight layers of per-cell API calls.
            var detailed = new bool[width * height];
            var ceiling = ceilingTile != null ? ceilingTile : repeatingWallTile;
            floorMap.ClearAllTiles(); floorShadeMap.ClearAllTiles(); carpetMap.ClearAllTiles();
            cornerMap.ClearAllTiles(); shadowMap.ClearAllTiles(); glowMap.ClearAllTiles(); wallJointMap.ClearAllTiles();
            // Submit ceilings in rows so a large map cannot monopolize a loading frame.
            var ceilingRow = new TileBase[width];
            for (int x = 0; x < width; x++) ceilingRow[x] = ceiling;
            for (int y = 0; y < height; y++)
            {
                wallMap.SetTilesBlock(new BoundsInt(0, y, 0, width, 1, 1), ceilingRow);
                if (budget.ElapsedMilliseconds >= 8) { yield return null; budget.Restart(); }
            }
            foreach (var cell in new RectInt(0, 0, Layout.Width, Layout.Height).allPositionsWithin)
            {
                if (!Layout.IsFloor(cell)) continue;
                // Include side/south caps and the five-piece tall north cornice.
                for (int y = Mathf.Max(0, cell.y * 2 - 1); y <= Mathf.Min(height - 1, cell.y * 2 + 6); y++)
                    for (int x = Mathf.Max(0, cell.x * 2 - 1); x <= Mathf.Min(width - 1, cell.x * 2 + 2); x++)
                        detailed[y * width + x] = true;
            }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    if (detailed[y * width + x]) PaintArtPiece(new Vector2Int(x, y));
                if (budget.ElapsedMilliseconds >= 8) { yield return null; budget.Restart(); }
            }
            // A full redraw can remove torches outside the remaining floor footprint.
            foreach (var pair in wallTorches)
                if (!detailed[pair.Key.y * width + pair.Key.x] && pair.Value != null) pair.Value.SetActive(false);
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
        private static Tile[][] MakeCompactFaces(Texture2D texture, int startX, int bottomY)
        {
            var faces = new Tile[6][];
            for (int height = 1; height < 5; height++)
            {
                faces[height] = new Tile[height * 5];
                float slice = 80f / height;
                for (int row = 0; row < height; row++)
                    for (int column = 0; column < 5; column++)
                    {
                        var tile = ScriptableObject.CreateInstance<Tile>();
                        tile.sprite = Sprite.Create(texture, new Rect(startX + column * 16, bottomY + (height - row - 1) * slice, 16, slice), new Vector2(.5f, .5f), 16);
                        tile.transform = Matrix4x4.Scale(new Vector3(1, height / 5f, 1));
                        tile.colliderType = Tile.ColliderType.None;
                        faces[height][row * 5 + column] = tile;
                    }
            }
            return faces;
        }
        private static Tile[] MakeWallPieces(Texture2D texture, int startX, int topY = 240)
        {
            var pieces = new Tile[25];
            for (int row = 0; row < 5; row++)
                for (int column = 0; column < 5; column++)
                {
                    var tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = Sprite.Create(texture, new Rect(startX + column * 16, topY - row * 16, 16, 16), new Vector2(.5f, .5f), 16);
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
        private bool SouthWallEdge(Vector2Int p) => !ArtFloor(p) && ArtFloor(p + Vector2Int.up);
        private Tile SouthWallAt(Vector2Int p)
        {
            if (southWallPieces == null || ArtFloor(p)) return null;
            var edge = p;
            if (!SouthWallEdge(edge))
            {
                if (SouthWallEdge(p + Vector2Int.left)) edge += Vector2Int.left;
                else if (SouthWallEdge(p + Vector2Int.right)) edge += Vector2Int.right;
                else return null;
            }
            int left = edge.x, right = edge.x;
            while (SouthWallEdge(new Vector2Int(left - 1, p.y))) left--;
            while (SouthWallEdge(new Vector2Int(right + 1, p.y))) right++;
            if (!ArtFloor(new Vector2Int(left - 1, p.y))) left--;
            if (!ArtFloor(new Vector2Int(right + 1, p.y))) right++;
            int column = WallSpanColumn(p.x, left, right, out _, out _);
            return southWallPieces[column];
        }
        private bool VerticalWallEdge(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < Layout.Width * 2 && p.y < Layout.Height * 2
            && !ArtFloor(p) && (ArtFloor(p + Vector2Int.left) || ArtFloor(p + Vector2Int.right));
        private Tile VerticalWallAt(Vector2Int p)
        {
            if (verticalWallPieces == null || ArtFloor(p)) return null;
            // Extend side walls to the north wall's cornice, five tiles above its floor.
            var edge = p;
            int depth = 0;
            while (!VerticalWallEdge(edge) && depth < 5)
            {
                edge += Vector2Int.down;
                depth++;
                if (ArtFloor(edge)) return null;
            }
            if (!VerticalWallEdge(edge)) return null;
            int top = edge.y, bottom = edge.y;
            while (VerticalWallEdge(new Vector2Int(p.x, top + 1))) top++;
            while (VerticalWallEdge(new Vector2Int(p.x, bottom - 1))) bottom--;
            int cornice = top;
            while (cornice < top + 5 && cornice + 1 < Layout.Height * 2 && !ArtFloor(new Vector2Int(p.x, cornice + 1))) cornice++;
            // Reserve the upper corridor's shallow south cap instead of clipping a pillar into it.
            if (SouthWallAt(new Vector2Int(p.x, cornice)) != null) cornice--;
            if (p.y > cornice || cornice < bottom) return null;
            // A south-facing boundary shows only the wall top, including its corner caps.
            // Continue its side as a plain shaft instead of exposing a front-facing pillar.
            if (SouthWallAt(new Vector2Int(p.x, bottom - 1)) != null)
                return p.y == cornice ? roomWallPieces[0] : verticalWallPieces[1 + (cornice - 1 - p.y) % 2];
            int height = cornice - bottom + 1;
            // A lower doorway can leave fewer than five tiles below walkable floor.
            // Fit the complete pillar into the available height; never discard ornament slices.
            if (height < 5) return compactWallPieces[height][cornice - p.y];
            if (height == 5) return verticalWallPieces[3 + cornice - p.y];
            // Use the horizontal capital so the corner shares its transparent top inset.
            if (p.y == cornice) return roomWallPieces[0];
            // Rows 3..7 form one complete pillar. Only the plain shaft (rows 1..2) repeats.
            if (p.y <= bottom + 4) return verticalWallPieces[3 + bottom + 4 - p.y];
            return verticalWallPieces[1 + (cornice - 1 - p.y) % 2];
        }
        // A full north face needs five art cells. Keep the upper floor and its south cap free.
        private int NorthFaceHeight(Vector2Int foot)
        {
            int height = 0;
            for (int depth = 1; depth <= 5; depth++)
            {
                var cell = foot + Vector2Int.up * depth;
                if (ArtFloor(cell) || SouthWallAt(cell) != null) break;
                height++;
            }
            return height;
        }
        // Distribute spans of three or four cells between single pillars, anchored at both ends.
        private int WallColumn(Vector2Int foot, out int segmentStart, out bool shortenedInterior)
        {
            int left = foot.x, right = foot.x;
            while (WallFoot(new Vector2Int(left - 1, foot.y))) left--;
            while (WallFoot(new Vector2Int(right + 1, foot.y))) right++;
            // End pillars share the side-wall columns outside the walkable floor.
            return WallSpanColumn(foot.x, left - 1, right + 1, out segmentStart, out shortenedInterior);
        }
        private static int WallSpanColumn(int x, int left, int right, out int segmentStart, out bool shortenedInterior)
        {
            shortenedInterior = false;
            int distance = right - left;
            if (distance == 0) { segmentStart = left; return 0; }
            int intervals = Mathf.CeilToInt(distance / 4f);
            int shortLength = distance / intervals, longer = distance % intervals;
            int start = left;
            for (int i = 0; i < intervals; i++)
            {
                int end = start + shortLength + (i < longer ? 1 : 0);
                if (x <= end)
                {
                    segmentStart = start;
                    if (x == start) return 0;
                    if (x == end) return end == right ? 4 : 0;
                    int offset = x - start;
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
            // The shallow south cap has a transparent lower half. Continue the side shaft
            // behind that half at inner corners instead of leaving a gap below the cap.
            wallJointMap.SetTile(position, !floor && SouthWallAt(p) != null
                ? VerticalWallEdge(p) && verticalWallPieces != null ? verticalWallPieces[1] : ceilingTile
                : null);
            shadowMap.SetTile(position, shadow);
            TileBase wall = null;
            bool litWindow = false;
            bool torch = false;
            glowMap.SetTile(position, null);
            if (!floor)
            {
                var verticalWall = SouthWallAt(p) ?? VerticalWallAt(p);
                wall = verticalWall != null ? verticalWall : (ceilingTile != null ? ceilingTile : repeatingWallTile);
                // A corridor below a room must not paint its tall face over the room's side wall.
                for (int depth = 1; verticalWall == null && depth <= 5; depth++)
                {
                    var foot = p + Vector2Int.down * depth;
                    if (!ArtFloor(foot)) continue;
                    int faceHeight = NorthFaceHeight(foot);
                    if (depth > faceHeight) break;
                    int wallColumn = WallColumn(foot, out int segmentStart, out bool shortenedInterior);
                    int index = (5 - depth) * 5 + wallColumn;
                    var owner = Construction.At(new Vector2Int(foot.x / 2, foot.y / 2));
                    if (owner?.Kind == ConstructionKind.StorageRoom && storageWallFaces != null)
                    {
                        uint style = ArtHash(segmentStart, foot.y) % (uint)storageWallFaces.Length;
                        wall = storageWallFaces[style][faceHeight][(faceHeight - depth) * 5 + wallColumn];
                        break;
                    }
                    bool corridor = owner != null && !DungeonConstruction.IsRoom(owner.Kind);
                    if (faceHeight < 5 && compactFacePieces != null)
                    {
                        wall = compactFacePieces[faceHeight][(faceHeight - depth) * 5 + wallColumn];
                        break;
                    }
                    // Plain room-wall spans and shortened corridor spans have no window.
                    // Alternate complete corridor wall spans between windows and torches.
                    // A fixture stays at the span center, away from pillars and doorways.
                    bool corridorTorch = corridor && !shortenedInterior && (Mathf.Abs(segmentStart / 5) % 2 == 0);
                    torch = depth == 1 && wallColumn == 2 && (!corridor || shortenedInterior || corridorTorch);
                    litWindow = corridor && !shortenedInterior && !corridorTorch && wallColumn == 2 && depth <= 3;
                    if (litWindow && depth == 1 && windowFloorLight != null)
                        glowMap.SetTile(new Vector3Int(foot.x, foot.y, 0), windowFloorLight);
                    if ((!corridor || shortenedInterior || corridorTorch) && roomWallPieces != null) wall = roomWallPieces[index];
                    else if (corridor && bannerWallPieces != null && ArtHash(segmentStart, foot.y) % 5 == 0) wall = bannerWallPieces[index];
                    else wall = Piece(wallPieces, index, repeatingWallTile);
                    break;
                }
            }
            wallMap.SetTile(position, wall);
            RefreshWallTorch(p, torch);
            if (windowFloorLight == null && litWindow) glowMap.SetTile(position, windowGlow);
        }
        private void RefreshWallTorch(Vector2Int position, bool visible)
        {
            if (wallTorchPrefab == null) return;
            if (wallTorches.TryGetValue(position, out var existing))
            {
                if (visible) return;
                existing.SetActive(false);
                Destroy(existing);
                wallTorches.Remove(position);
            }
            else if (visible)
            {
                var instance = Instantiate(wallTorchPrefab, artRoot);
                // Native 16 PPU artwork, bottom aligned just above the floor.
                instance.transform.localPosition = new Vector3(position.x + .5f, position.y - .5f, 0);
                wallTorches.Add(position, instance);
            }
        }
        private void RefreshArtTile(Vector2Int cell)
        {
            if (stagedPresentation || deferArtRefresh || !UsesArtTiles || floorMap == null) return;
            // Excavation changes carpet boundaries and the five rows of upright wall above it.
            for (int y = -2; y <= 6; y++)
                for (int x = 0; x < Layout.Width * 2; x++) PaintArtPiece(new Vector2Int(x, cell.y * 2 + y));
            // A new opening can move the cap and repeat pattern along an entire side wall.
            for (int x = cell.x * 2 - 1; x <= cell.x * 2 + 2; x++)
                for (int y = 0; y < Layout.Height * 2; y++)
                    if (y < cell.y * 2 - 2 || y > cell.y * 2 + 6) PaintArtPiece(new Vector2Int(x, y));
        }
        private void RefreshAllArtTiles()
        {
            if (stagedPresentation || !UsesArtTiles || floorMap == null) return;
            PaintOccupiedArt();
        }
    }
}

