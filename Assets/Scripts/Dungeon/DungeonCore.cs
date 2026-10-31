using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Gameplay;
using UnityEngine;

namespace DarkLord.Dungeon
{
    public sealed partial class DungeonGrid
    {
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
            var footprint = coreActor.GetComponent<BoxCollider2D>();
            if (footprint == null) footprint = coreActor.gameObject.AddComponent<BoxCollider2D>();
            footprint.offset = new Vector2(0, .5625f);
            footprint.size = new Vector2(1.25f, .5f);
            footprint.isTrigger = false;
            if (coreActor.transform.Find("Dungeon core artwork") == null)
            {
                var prefab = Resources.Load<GameObject>("DungeonCoreVisual");
                if (prefab != null)
                {
                    var visual = Instantiate(prefab, coreActor.transform, false);
                    visual.name = "Dungeon core artwork";
                }
            }
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
