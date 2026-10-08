using UnityEngine;
using UnityEngine.Tilemaps;
namespace DarkLord.Dungeon
{
    public sealed partial class DungeonGrid
    {
        [Header("Dungeon artwork")]
        [SerializeField] private TileBase roomFloorTile, corridorCarpetTile, repeatingWallTile;
        [SerializeField] private TileBase[] stonePieces, carpetPieces, wallPieces;
        private Tilemap floorMap, carpetMap, wallMap;
        private Transform artRoot;
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
            carpetMap = ArtLayer("Continuous corridor carpet", -31999);
            wallMap = ArtLayer("North wall faces and brown wall tops", -31998);
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
        private bool BackedCarpet(Vector2Int p) => ArtFloor(p) ? ArtCarpet(p) : ArtCarpet(p + Vector2Int.down) || ArtCarpet(p + Vector2Int.down * 2);
        private static TileBase Piece(TileBase[] pieces, int index, TileBase fallback) => pieces != null && index < pieces.Length && pieces[index] != null ? pieces[index] : fallback;
        private void PaintArtPiece(Vector2Int p)
        {
            if (p.x < 0 || p.y < 0 || p.x >= Layout.Width * 2 || p.y >= Layout.Height * 2) return;
            var position = new Vector3Int(p.x, p.y, 0);
            bool floor = ArtFloor(p);
            floorMap.SetTile(position, BackedFloor(p) ? Piece(stonePieces, (2 - p.y % 3) * 3 + p.x % 3, roomFloorTile) : null);
            // A nine-piece carpet only uses its gold edges at the boundary of the connected carpet.
            int column = !BackedCarpet(p + Vector2Int.left) ? 0 : !BackedCarpet(p + Vector2Int.right) ? 2 : 1;
            int row = !BackedCarpet(p + Vector2Int.up) ? 0 : !BackedCarpet(p + Vector2Int.down) ? 2 : 1;
            carpetMap.SetTile(position, BackedCarpet(p) ? Piece(carpetPieces, row * 3 + column, corridorCarpetTile) : null);
            TileBase wall = null;
            if (!floor)
            {
                wall = Piece(wallPieces, 2, repeatingWallTile);
                // Upright faces exist only above open floor. Side/bottom walls remain solid top faces.
                for (int depth = 1; depth <= 5; depth++)
                {
                    var foot = p + Vector2Int.down * depth;
                    if (!ArtFloor(foot)) continue;
                    int wallColumn = !ArtFloor(foot + Vector2Int.left) ? 0 : !ArtFloor(foot + Vector2Int.right) ? 4 : 1 + p.x % 3;
                    wall = Piece(wallPieces, (5 - depth) * 5 + wallColumn, repeatingWallTile);
                    break;
                }
            }
            wallMap.SetTile(position, wall);
        }
        private void RefreshArtTile(Vector2Int cell)
        {
            if (!UsesArtTiles || floorMap == null) return;
            // Excavation changes carpet boundaries and the five rows of upright wall above it.
            for (int y = -1; y <= 6; y++)
                for (int x = -1; x <= 2; x++) PaintArtPiece(cell * 2 + new Vector2Int(x, y));
        }
    }
}

