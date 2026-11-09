using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Gameplay;
using UnityEngine;

namespace DarkLord.Dungeon
{
    public sealed partial class DungeonGrid
    {
        private const float CoreFootY = -.75f;
        private const float CorePixelsPerUnit = 16f;
        // The plinth's ground outline in source pixels, with Y flipped from the sprite sheet.
        private static readonly Vector2[] CoreFootprint =
        {
            new Vector2(-23, 16) / CorePixelsPerUnit, new Vector2(23, 16) / CorePixelsPerUnit,
            new Vector2(32, 9) / CorePixelsPerUnit, new Vector2(32, 4) / CorePixelsPerUnit,
            new Vector2(25, 0) / CorePixelsPerUnit, new Vector2(-25, 0) / CorePixelsPerUnit,
            new Vector2(-32, 4) / CorePixelsPerUnit, new Vector2(-32, 9) / CorePixelsPerUnit
        };
        private ActorHealth coreActor;
        private WaveDirector coreDirector;
        private readonly List<Vector2Int> corePath = new List<Vector2Int>();
        public Vector2Int CoreCell { get { Initialize(); return Layout.ActiveCore; } }
        public DungeonSection CoreRoom => Construction.RoomAt(CoreCell);
        public bool CanMoveCoreNow => coreActor != null && coreActor.IsAlive && coreDirector != null &&
            coreDirector.State == WaveState.Preparing && coreDirector.CanUseFacilities &&
            !(coreDirector.Tutorial != null && coreDirector.Tutorial.Active) && Time.timeScale > 0;

        public void BindCore(ActorHealth actor, WaveDirector director)
        {
            Initialize();
            coreActor = actor;
            coreDirector = director;
            if (coreActor == null) return;
            coreActor.transform.position = CellCenter(CoreCell);
            var footprint = coreActor.GetComponent<PolygonCollider2D>();
            if (footprint == null) footprint = coreActor.gameObject.AddComponent<PolygonCollider2D>();
            footprint.offset = new Vector2(0, CoreFootY);
            footprint.pathCount = 1;
            footprint.SetPath(0, CoreFootprint);
            footprint.isTrigger = false;
            coreActor.ContactCollider = footprint;
            if (coreActor.transform.Find("Dungeon core artwork") == null)
            {
                var prefab = Resources.Load<GameObject>("DungeonCoreVisual");
                if (prefab != null)
                {
                    var visual = Instantiate(prefab, coreActor.transform, false);
                    visual.name = "Dungeon core artwork";
                    CoreVisual = BuildCoreVisual(visual);
                }
            }
        }

        public DungeonCoreVisual CoreVisual { get; private set; }

        // The animated sheet replaces the 1-tile sprite, so the old sprite's emission and shadow copies go.
        private static DungeonCoreVisual BuildCoreVisual(GameObject visual)
        {
            var body = visual.transform.Find("Intact");
            if (body == null) return null;
            body.localPosition = new Vector3(0, CoreFootY, 0);
            foreach (var name in new[] { "Crimson emission", "Crimson source aura", "Projected core shadow", "Foot contact shadow" })
            {
                var part = visual.transform.Find(name) ?? body.Find(name);
                if (part != null) part.gameObject.SetActive(false);
            }
            var light = visual.transform.Find("Crimson floor light") ?? body.Find("Crimson floor light");
            if (light != null) { light.localPosition = Vector3.zero; light.localScale = new Vector3(4, 4, 1); }
            // Sort against actors by where the dais meets the floor: actor roots stand 13 pixels above their feet.
            visual.GetComponent<FacilityDepthSorting>()?.SetDepthOffset(CoreFootY + 13f / CorePixelsPerUnit);
            var core = visual.AddComponent<DungeonCoreVisual>();
            core.Configure(body.GetComponent<SpriteRenderer>());
            return core;
        }

        public bool CanRelocateCore(Vector2Int cell)
        {
            if (!CanMoveCoreNow || Construction.RoomAt(cell) == null || !EmptyFloor(cell) ||
                Layout.HasEntrance && cell == Layout.Entrance) return false;
            return Construction.FindPath(Layout.HasEntrance ? Layout.Entrance : CoreCell, cell, corePath);
        }

        public bool TryRelocateCore(Vector2Int cell)
        {
            if (!CanRelocateCore(cell)) return false;
            Layout.MoveCore(cell);
            coreActor.transform.position = CellCenter(cell);
            Physics2D.SyncTransforms();
            return true;
        }

        private void RestoreCorePosition()
        {
            if (coreActor != null) coreActor.transform.position = CellCenter(CoreCell);
        }
    }
}
