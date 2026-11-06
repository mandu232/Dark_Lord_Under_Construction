using System.Collections.Generic;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Minions;
using UnityEngine;

namespace DarkLord.Player
{
    public sealed partial class MinionSummoning
    {
        private readonly List<CorpseWorker> workers = new List<CorpseWorker>();

        public int WorkerCount => workers.Count(worker => worker != null && worker.IsAlive);
        public int WorkerCapacity => dungeon?.Construction == null ? 0 :
            dungeon.Construction.Sections.Count(room => room.IsComplete && DungeonConstruction.IsRoom(room.Kind));

        public bool WorkerRecruitmentAvailable => GetComponent<CorpseHarvest>()?.WorkersCanCollect == true;

        public bool TrySummonWorker()
        {
            if (!IsRecruiting || !IsOpen || !CanHandleMinions || IsRelocating || !WorkerRecruitmentAvailable) return false;
            var station = recruitmentStation;
            if (station == null || !station.isActiveAndEnabled || station.IsRuined || station.SummonStacks < 1 ||
                Vector2.Distance(transform.position, station.transform.position) > 3f) return false;
            if (WorkerCount >= WorkerCapacity)
            {
                message = "일꾼 한도에 도달했습니다. 방을 완성하면 한도가 1 늘어납니다.";
                return false;
            }
            var definition = RecruitDefinitionFor(MinionRole.BloodSlime);
            var room = dungeon.Construction.RoomAt(station.Cell);
            if (definition == null || room == null || !FindSpawnNear(station.Cell, room, out var cell))
            {
                message = "소환소 옆에 일꾼을 소환할 공간이 없습니다.";
                return false;
            }
            if (!station.TrySpendSummonStack()) return false;
            var worker = CreateWorker(definition, dungeon.CellCenter(cell), CorpseWorker.MaximumHealth);
            GameplayEffects.Show(worker.transform, GameplayEffectKind.Summon, worker.transform.position);
            SetOpen(false);
            message = "시체 수거 슬라임을 소환했습니다.";
            return true;
        }

        private CorpseWorker CreateWorker(MinionController definition, Vector3 position, int currentHealth)
        {
            workers.RemoveAll(worker => worker == null || !worker.IsAlive);
            var actor = Instantiate(definition, position, Quaternion.identity, dungeon.transform);
            actor.enabled = false;
            actor.name = "시체 수거 슬라임";
            var visual = actor.GetComponentInChildren<SpriteRenderer>();
            if (visual != null) visual.color = new Color(.6f, .9f, 1f);
            var worker = actor.gameObject.AddComponent<CorpseWorker>();
            worker.Configure(this, GetComponent<CorpseHarvest>(), currentHealth);
            workers.Add(worker);
            return worker;
        }

        internal bool TryWorkerShelter(CorpseWorker worker, out Vector2Int cell)
        {
            cell = default;
            var room = dungeon.CoreRoom;
            if (room == null) return false;
            int rank = 0;
            foreach (var other in workers)
            {
                if (other == worker) break;
                if (other != null && other.IsAlive) rank++;
            }
            int count = 0;
            for (int i = room.Cells.Count - 1; i >= 0; i--)
            {
                var candidate = room.Cells[i];
                if (candidate == dungeon.CoreCell || dungeon.BuildingAt(candidate) != null ||
                    dungeon.HasTrap(candidate) || !dungeon.Construction.IsWalkable(candidate)) continue;
                if (count++ == 0) cell = candidate;
                if (rank-- == 0) { cell = candidate; return true; }
            }
            return count > 0;
        }

        internal void GetRoomWorkers(DungeonSection room, List<ActorHealth> result)
        {
            foreach (var worker in workers)
                if (worker != null && worker.isActiveAndEnabled && worker.IsAlive &&
                    dungeon.Construction.RoomAt(dungeon.WorldToCell(worker.transform.position)) == room)
                    result.Add(worker.Health);
        }

        internal CorpseWorkerSaveData[] CaptureWorkers() => workers.Where(worker => worker != null && worker.IsAlive)
            .Select(worker => new CorpseWorkerSaveData { position = worker.transform.position, health = worker.Health.CurrentHealth }).ToArray();

        internal void RestoreWorkers(CorpseWorkerSaveData[] saved)
        {
            foreach (var worker in workers)
                if (worker != null) { worker.gameObject.SetActive(false); Destroy(worker.gameObject); }
            workers.Clear();
            var definition = RecruitDefinitionFor(MinionRole.BloodSlime);
            if (saved == null || definition == null) return;
            foreach (var entry in saved)
            {
                var position = dungeon.Construction.IsWalkable(dungeon.WorldToCell(entry.position))
                    ? entry.position : dungeon.CellCenter(dungeon.CoreCell);
                CreateWorker(definition, position, entry.health);
            }
        }
    }
}
