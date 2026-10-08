using DarkLord.Actors;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace DarkLord.Dungeon
{
    [DisallowMultipleComponent, RequireComponent(typeof(Grid))]
    public sealed partial class DungeonGrid : MonoBehaviour
    {
        // Sprites remain at 16 PPU: a 32px tile occupies two world units.
        public const float CellSize = 2f;
        public static Vector3 GridCellSize => new Vector3(CellSize, CellSize, 1);
        [SerializeField, Range(15, 101)] private int width = 41;
        [SerializeField, Range(11, 101)] private int height = 31;
        [SerializeField] private Material previewMaterial;
        [SerializeField, Min(0.02f)] private float tileBreakInterval = 0.045f;
        [SerializeField] private GameObject initialRoomPrefab, initialCorridorPrefab, entrancePrefab;
        private GameObject entranceMarker;
        private readonly Dictionary<Vector2Int, GameObject> traps = new Dictionary<Vector2Int, GameObject>();
        private readonly List<Vector2Int> entrancePath = new List<Vector2Int>();
        private Grid grid;
        private Mesh mesh;
        private Color[] colors;
        private BoxCollider2D[,] blockers;
        private GameObject generated;
        private int highlighted = -1;
        private readonly Dictionary<Vector2Int, float> miningMarks = new Dictionary<Vector2Int, float>();
        public DungeonLayout Layout { get; private set; }
        public DungeonConstruction Construction { get; private set; }
        private readonly List<DarkLord.Minions.MinionController> guards = new List<DarkLord.Minions.MinionController>();
        private readonly List<int> areaHighlight = new List<int>();
        private readonly List<Vector2Int> constructionTiles = new List<Vector2Int>();
        private Vector2Int? breakingCell;
        private float nextBreakAt;
        private ParticleSystem rockChips;
        private readonly Dictionary<DungeonSection, GameObject> sectionPrefabs = new Dictionary<DungeonSection, GameObject>();
        public int RemainingConstructionTiles => constructionTiles.Count;

        private void Awake() => Initialize();

        public void Initialize()
        {
            if (Layout != null) return;
            grid = GetComponent<Grid>();
            grid.cellSize = GridCellSize;
            grid.cellGap = Vector3.zero;
            Layout = new DungeonLayout(Mathf.Clamp(width, 15, 101), Mathf.Clamp(height, 11, 101));
            Construction = new DungeonConstruction(Layout);
            generated = new GameObject("Generated dungeon cells");
            generated.layer = gameObject.layer;
            generated.transform.SetParent(transform, false);
            BuildPresentation();
            Layout.CellChanged += OnCellChanged;
            foreach (var section in Construction.Sections)
            {
                var prefab = section.IsCore ? initialRoomPrefab : initialCorridorPrefab;
                if (prefab == null || UsesArtTiles) continue;
                var design = Instantiate(prefab, CellCenter(section.Center), transform.rotation, generated.transform);
                if (!section.IsCore) design.transform.localScale = new Vector3(section.Bounds.width / 5f, 1, 1);
                design.name = "Initial " + prefab.name;
            }
            RefreshEntranceMarker();
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
            if (breakingCell == cell) return new Color(1f, 0.92f, 0.68f);
            if (Layout.GetCell(cell) == DungeonCell.Rock && miningMarks.TryGetValue(cell, out float progress))
                return Color.Lerp(new Color(0.95f, 0.65f, 0.12f), new Color(0.2f, 0.85f, 0.9f), progress);
            if (UsesArtTiles) return Color.clear;
            if (cell == Layout.Core) return new Color(0.60f, 0.28f, 0.78f);
            if (Layout.HasEntrance && cell == Layout.Entrance) return new Color(0.25f, 0.65f, 0.85f);
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
            renderer.sortingOrder = UsesArtTiles ? -31000 : short.MinValue;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            BuildArtTiles();
        }

        private void Paint(int index, Color color)
        {
            if (UsesArtTiles && color.a > 0) color.a = 0.42f;
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
            miningMarks.Remove(cell);
            var section = Construction.At(cell);
            if (section != null && section.IsComplete && sectionPrefabs.TryGetValue(section, out var prefab))
            {
                sectionPrefabs.Remove(section);
                if (prefab != null && !UsesArtTiles)
                {
                    if (section.Kind == ConstructionKind.Corridor)
                    {
                        var corridor = new GameObject("Corridor Design " + section.Center);
                        corridor.transform.SetParent(generated.transform, false);
                        foreach (var part in section.Cells) Instantiate(prefab, CellCenter(part), transform.rotation, corridor.transform);
                    }
                    else
                    {
                        var design = Instantiate(prefab, CellCenter(section.Center), transform.rotation, generated.transform);
                        design.name = prefab.name + " " + section.Center;
                    }
                }
            }
            RefreshArtTile(cell);
            if (section != null && section.IsComplete) foreach (var part in section.Cells) RefreshArtTile(part);
            var collider = blockers[cell.x, cell.y];
            if (collider != null) collider.enabled = false;
            Paint(cell.y * Layout.Width + cell.x, CellColor(cell));
            mesh.colors = colors;
        }

        public void Highlight(Vector2Int? cell, bool allowed)
        {
            Initialize();
            ClearAreaHighlight();
            if (highlighted >= 0) Paint(highlighted, CellColor(new Vector2Int(highlighted % Layout.Width, highlighted / Layout.Width)));
            highlighted = cell.HasValue && Layout.Contains(cell.Value) ? cell.Value.y * Layout.Width + cell.Value.x : -1;
            if (highlighted >= 0) Paint(highlighted, allowed ? new Color(0.30f, 0.85f, 0.55f) : new Color(0.90f, 0.30f, 0.25f));
            mesh.colors = colors;
        }

        public void QueueConstruction(DungeonSection section, GameObject prefab = null)
        {
            Initialize();
            savedDesigns[section] = prefab;
            if (prefab != null) sectionPrefabs[section] = prefab;
            foreach (var cell in section.Cells)
                if (Layout.GetCell(cell) == DungeonCell.Rock && !constructionTiles.Contains(cell))
                {
                    constructionTiles.Add(cell);
                    SetMiningMark(cell, true);
                }
        }

        private void Update()
        {
            if (Time.timeScale == 0 || Layout == null || Time.time < nextBreakAt) return;
            if (breakingCell.HasValue)
            {
                var cell = breakingCell.Value;
                breakingCell = null;
                if (Layout.TryExpand(cell) == ExpansionResult.Success) EmitRockChips(cell);
                constructionTiles.Remove(cell);
                nextBreakAt = Time.time + tileBreakInterval * 0.6f;
                return;
            }
            for (int i = 0; i < constructionTiles.Count; i++)
            {
                var cell = constructionTiles[i];
                if (Layout.GetCell(cell) != DungeonCell.Rock) { constructionTiles.RemoveAt(i--); continue; }
                if (Layout.CanExpand(cell) != ExpansionResult.Success) continue;
                breakingCell = cell;
                Paint(cell.y * Layout.Width + cell.x, CellColor(cell));
                mesh.colors = colors;
                nextBreakAt = Time.time + tileBreakInterval * 0.4f;
                break;
            }
        }

        private void EmitRockChips(Vector2Int cell)
        {
            if (previewMaterial == null) return;
            if (rockChips == null)
            {
                var go = new GameObject("Tile crumble fragments");
                go.transform.SetParent(transform, false);
                rockChips = go.AddComponent<ParticleSystem>();
                rockChips.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = rockChips.main;
                main.loop = false; main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.28f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
                main.startColor = new Color(0.74f, 0.61f, 0.42f);
                main.gravityModifier = 0.3f; main.maxParticles = 256;
                var emission = rockChips.emission; emission.enabled = false;
                var shape = rockChips.shape; shape.enabled = false;
                var size = rockChips.sizeOverLifetime; size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = previewMaterial;
                renderer.sortingOrder = -100;
                rockChips.Play();
            }
            if (!rockChips.isPlaying) rockChips.Play();
            var center = CellCenter(cell);
            for (int i = 0; i < 6; i++)
            {
                var particle = new ParticleSystem.EmitParams
                {
                    position = center + new Vector3(Random.Range(-0.65f, 0.65f), Random.Range(-0.65f, 0.65f), -0.1f),
                    velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(-1f, 2f), 0),
                    rotation = Random.Range(0f, 360f)
                };
                rockChips.Emit(particle, 1);
            }
        }

        public void HighlightArea(RectInt bounds, bool allowed)
        {
            Highlight(null, false);
            foreach (var cell in bounds.allPositionsWithin)
            {
                if (!Layout.Contains(cell)) continue;
                int index = cell.y * Layout.Width + cell.x;
                areaHighlight.Add(index);
                Paint(index, allowed ? new Color(0.30f, 0.85f, 0.55f) : new Color(0.90f, 0.30f, 0.25f));
            }
            mesh.colors = colors;
        }

        public void HighlightCells(System.Collections.Generic.IEnumerable<Vector2Int> cells, bool allowed)
        {
            Highlight(null, false);
            foreach (var cell in cells)
            {
                if (!Layout.Contains(cell)) continue;
                int index = cell.y * Layout.Width + cell.x;
                areaHighlight.Add(index);
                Paint(index, allowed ? new Color(0.30f, 0.85f, 0.55f) : new Color(0.90f, 0.30f, 0.25f));
            }
            mesh.colors = colors;
        }

        private void ClearAreaHighlight()
        {
            foreach (var index in areaHighlight) Paint(index, CellColor(new Vector2Int(index % Layout.Width, index / Layout.Width)));
            areaHighlight.Clear();
        }

        public System.Collections.Generic.IReadOnlyList<DarkLord.Minions.MinionController> Guards => guards;

        public void RegisterGuard(DarkLord.Minions.MinionController guard)
        {
            if (guard == null) return;
            guard.AssignDefenceDungeon(this);
            if (!guards.Contains(guard)) guards.Add(guard);
        }

        public bool HasTrap(Vector2Int cell) => traps.TryGetValue(cell, out var trap) && trap != null && trap.activeSelf;
        private bool EmptyFloor(Vector2Int cell)
        {
            if (!Construction.IsWalkable(cell) || cell == Layout.Core || BuildingAt(cell) != null || HasTrap(cell) || GuardPostAt(cell) != null) return false;
            Physics2D.SyncTransforms();
            var hits = Physics2D.OverlapCircleAll(CellCenter(cell), 0.65f);
            foreach (var hit in hits) if (hit.GetComponentInParent<ActorHealth>() != null) return false;
            return true;
        }
        public bool CanPlaceEntrance(Vector2Int cell)
        {
            Initialize();
            return EmptyFloor(cell) && Construction.FindPath(cell, Layout.Core, entrancePath);
        }
        public bool TryPlaceEntrance(Vector2Int cell)
        {
            if (!isActiveAndEnabled || Time.timeScale == 0 || !CanPlaceEntrance(cell)) return false;
            Layout.SetEntrance(cell); RefreshEntranceMarker(); return true;
        }
        private void RefreshEntranceMarker()
        {
            if (entranceMarker != null) { entranceMarker.SetActive(false); Destroy(entranceMarker); entranceMarker = null; }
            if (Layout.HasEntrance && entrancePrefab != null)
                entranceMarker = Instantiate(entrancePrefab, CellCenter(Layout.Entrance), transform.rotation, generated.transform);
        }
        public bool RemoveEntrance()
        {
            if (!Layout.RemoveEntrance()) return false;
            RefreshEntranceMarker(); return true;
        }
        public bool CanPlaceTrap(Vector2Int cell) => EmptyFloor(cell) && (!Layout.HasEntrance || cell != Layout.Entrance);
        public bool TryPlaceTrap(Vector2Int cell, GameObject prefab)
        {
            if (!isActiveAndEnabled || Time.timeScale == 0 || prefab == null || !CanPlaceTrap(cell)) return false;
            traps[cell] = Instantiate(prefab, CellCenter(cell), transform.rotation, generated.transform);
            savedTrapDesigns[cell] = prefab;
            return true;
        }
        public bool RemoveTrap(Vector2Int cell)
        {
            if (!HasTrap(cell)) return false;
            var trap = traps[cell]; traps.Remove(cell); savedTrapDesigns.Remove(cell); trap.SetActive(false); Destroy(trap); return true;
        }
        public DarkLord.Minions.MinionController GuardAt(Vector2Int cell)
        {
            guards.RemoveAll(g => g == null);
            foreach (var guard in guards)
                if (guard.isActiveAndEnabled && guard.IsAlive && guard.Health.Faction == Faction.Dungeon && WorldToCell(guard.transform.position) == cell)
                    return guard;
            return null;
        }
        public DarkLord.Minions.MinionController GuardPostAt(Vector2Int cell)
        {
            guards.RemoveAll(g => g == null || !g.IsAlive);
            foreach (var guard in guards)
                if (guard.isActiveAndEnabled && guard.Health.Faction == Faction.Dungeon && WorldToCell(guard.GuardPosition) == cell)
                    return guard;
            return null;
        }
        public void UnregisterGuard(DarkLord.Minions.MinionController guard) => guards.Remove(guard);

        public void GetRoomDefenders(DungeonSection room, List<ActorHealth> result)
        {
            result.Clear();
            guards.RemoveAll(g => g == null);
            if (room == null) return;
            foreach (var guard in guards)
                if (guard.isActiveAndEnabled && guard.IsAlive && guard.Health.Faction == Faction.Dungeon &&
                    room.Bounds.Contains(WorldToCell(guard.transform.position))) result.Add(guard.Health);
        }

        private void OnDestroy()
        {
            if (Layout != null) Layout.CellChanged -= OnCellChanged;
            if (mesh != null) Destroy(mesh);
            if (generated != null) Destroy(generated);
            if (rockChips != null) Destroy(rockChips.gameObject);
        }
    }
}

