using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace DarkLord.Dungeon
{
    [DisallowMultipleComponent, RequireComponent(typeof(Grid))]
    public sealed class DungeonGrid : MonoBehaviour
    {
        // Sprites remain at 16 PPU: a 32px tile occupies two world units.
        public const float CellSize = 2f;
        public static Vector3 GridCellSize => new Vector3(CellSize, CellSize, 1);
        [SerializeField, Range(15, 101)] private int width = 41;
        [SerializeField, Range(11, 101)] private int height = 31;
        [SerializeField] private Material previewMaterial;
        private Grid grid;
        private Mesh mesh;
        private Color[] colors;
        private BoxCollider2D[,] blockers;
        private GameObject generated;
        private int highlighted = -1;
        private readonly Dictionary<Vector2Int, float> miningMarks = new Dictionary<Vector2Int, float>();
        public DungeonLayout Layout { get; private set; }

        private void Awake() => Initialize();

        public void Initialize()
        {
            if (Layout != null) return;
            grid = GetComponent<Grid>();
            grid.cellSize = GridCellSize;
            grid.cellGap = Vector3.zero;
            Layout = new DungeonLayout(Mathf.Clamp(width, 15, 101), Mathf.Clamp(height, 11, 101));
            generated = new GameObject("Generated dungeon cells");
            generated.layer = gameObject.layer;
            generated.transform.SetParent(transform, false);
            BuildPresentation();
            Layout.CellChanged += OnCellChanged;
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            Initialize();
            Vector3Int cell = grid.WorldToCell(world);
            return new Vector2Int(cell.x, cell.y);
        }

        public Vector3 CellCenter(Vector2Int cell)
        {
            Initialize();
            return grid.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        }

        public ExpansionResult CanExpand(Vector2Int cell, Vector3 workerPosition)
        {
            Initialize();
            var result = Layout.CanExpand(cell);
            if (result != ExpansionResult.Success) return result;
            var worker = WorldToCell(workerPosition);
            // Work from the neighboring floor, never diagonally or through another rock.
            if (!Layout.IsFloor(worker) || Mathf.Abs(worker.x - cell.x) + Mathf.Abs(worker.y - cell.y) != 1)
                return ExpansionResult.OutOfReach;
            return ExpansionResult.Success;
        }

        public ExpansionResult TryExpand(Vector2Int cell, Vector3 workerPosition)
        {
            if (!isActiveAndEnabled) return ExpansionResult.Unavailable;
            var result = CanExpand(cell, workerPosition);
            return result == ExpansionResult.Success ? Layout.TryExpand(cell) : result;
        }

        private Color CellColor(Vector2Int cell)
        {
            if (Layout.GetCell(cell) == DungeonCell.Rock && miningMarks.TryGetValue(cell, out float progress))
                return Color.Lerp(new Color(0.95f, 0.65f, 0.12f), new Color(0.2f, 0.85f, 0.9f), progress);
            if (cell == Layout.Core) return new Color(0.60f, 0.28f, 0.78f);
            if (cell == Layout.Entrance) return new Color(0.25f, 0.65f, 0.85f);
            switch (Layout.GetCell(cell))
            {
                case DungeonCell.Floor: return new Color(0.30f, 0.35f, 0.39f);
                case DungeonCell.Bedrock: return new Color(0.09f, 0.11f, 0.15f);
                default: return new Color(0.48f, 0.42f, 0.34f);
            }
        }

        private void BuildPresentation()
        {
            int count = Layout.Width * Layout.Height;
            var vertices = new Vector3[count * 4];
            colors = new Color[count * 4];
            var triangles = new int[count * 6];
            blockers = new BoxCollider2D[Layout.Width, Layout.Height];
            for (int y = 0; y < Layout.Height; y++)
                for (int x = 0; x < Layout.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    int index = y * Layout.Width + x;
                    int v = index * 4;
                    // A small gap draws the grid without a texture or sprite asset.
                    float left = x * CellSize + 0.025f, bottom = y * CellSize + 0.025f;
                    float right = (x + 1) * CellSize - 0.025f, top = (y + 1) * CellSize - 0.025f;
                    vertices[v] = new Vector3(left, bottom, 0);
                    vertices[v + 1] = new Vector3(left, top, 0);
                    vertices[v + 2] = new Vector3(right, top, 0);
                    vertices[v + 3] = new Vector3(right, bottom, 0);
                    Paint(index, CellColor(cell));
                    int t = index * 6;
                    triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                    triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
                    if (!Layout.IsFloor(cell))
                    {
                        var collider = generated.AddComponent<BoxCollider2D>();
                        collider.offset = new Vector2((x + 0.5f) * CellSize, (y + 0.5f) * CellSize);
                        collider.size = Vector2.one * CellSize;
                        blockers[x, y] = collider;
                    }
                }
            mesh = new Mesh { name = "Dungeon placeholder grid", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors;
            mesh.RecalculateBounds();
            mesh.MarkDynamic();
            generated.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = generated.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = previewMaterial;
            renderer.sortingOrder = short.MinValue;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void Paint(int index, Color color)
        {
            for (int i = 0; i < 4; i++) colors[index * 4 + i] = color;
        }

        public void SetMiningMark(Vector2Int cell, bool queued, float progress = 0)
        {
            if (mesh == null || !Layout.Contains(cell)) return;
            progress = Mathf.Round(Mathf.Clamp01(progress) * 20) / 20;
            if (queued && miningMarks.TryGetValue(cell, out float oldProgress) && oldProgress == progress) return;
            if (queued) miningMarks[cell] = progress;
            else miningMarks.Remove(cell);
            Paint(cell.y * Layout.Width + cell.x, CellColor(cell));
            mesh.colors = colors;
        }

        private void OnCellChanged(Vector2Int cell)
        {
            var collider = blockers[cell.x, cell.y];
            if (collider != null) collider.enabled = false;
            Paint(cell.y * Layout.Width + cell.x, CellColor(cell));
            mesh.colors = colors;
        }

        public void Highlight(Vector2Int? cell, bool allowed)
        {
            Initialize();
            if (highlighted >= 0) Paint(highlighted, CellColor(new Vector2Int(highlighted % Layout.Width, highlighted / Layout.Width)));
            highlighted = cell.HasValue && Layout.Contains(cell.Value) ? cell.Value.y * Layout.Width + cell.Value.x : -1;
            if (highlighted >= 0) Paint(highlighted, allowed ? new Color(0.30f, 0.85f, 0.55f) : new Color(0.90f, 0.30f, 0.25f));
            mesh.colors = colors;
        }

        private void OnDestroy()
        {
            if (Layout != null) Layout.CellChanged -= OnCellChanged;
            if (mesh != null) Destroy(mesh);
            if (generated != null) Destroy(generated);
        }
    }
}
