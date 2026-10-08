using System;
using System.IO;
using UnityEngine;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Minions;

namespace DarkLord.Persistence
{
    [Serializable] public sealed class GameSaveData
    {
        public int version = 1;
        public string savedAtUtc = "";
        public float playedSeconds;
        public int gold, souls, crystals;
        public Vector3 playerPosition;
        public int playerHealth, playerMaximumHealth, coreHealth, coreMaximumHealth;
        public DungeonSaveData dungeon;
        public MinionSaveData[] guards = new MinionSaveData[0];
        public WaveSaveData wave;
    }
    [Serializable] public sealed class DungeonSaveData
    {
        public int width, height, expandedCells;
        public int[] cells;
        public bool hasEntrance;
        public Vector2Int entrance;
        public SectionSaveData[] sections;
        public TrapSaveData[] traps;
        public Vector2Int[] queue;
        public BuildingSaveData[] buildings;
    }
    [Serializable] public sealed class SectionSaveData
    {
        public ConstructionKind kind;
        public RectInt bounds;
        public bool complete, core, initial;
        public string designId;
        public Vector2Int[] cells;
    }
    [Serializable] public sealed class BuildingSaveData
    {
        public Vector2Int cell;
        public string designId;
        public int health, maximumHealth, lastPaidRound;
    }
    [Serializable] public sealed class TrapSaveData { public Vector2Int cell; public string designId; }
    [Serializable] public sealed class MinionSaveData
    {
        public Vector3 position;
        public Vector2 post;
        public int health, maximumHealth;
        public MinionOrder order;
        public MinionBehaviour behaviour;
    }
    [Serializable] public sealed class WaveSaveData
    {
        public int number, pending;
        public WaveState state;
        public MinionSaveData[] enemies;
    }

    // Files contain data and stable build-item IDs, never scene object references.
    public sealed class SaveSlotStore
    {
        private readonly string directory;
        public SaveSlotStore(string directory) { this.directory = directory; }
        public static SaveSlotStore Default => new SaveSlotStore(Path.Combine(Application.persistentDataPath, "Saves"));
        public string PathFor(int slot)
        {
            if (slot < 0 || slot >= 3) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(directory, "slot" + (slot + 1) + ".json");
        }
        public bool Exists(int slot) => File.Exists(PathFor(slot)) || File.Exists(PathFor(slot) + ".bak");
        public bool TryRead(int slot, out GameSaveData data, out string error)
        {
            data = null; error = "";
            var path = PathFor(slot);
            foreach (var candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    var parsed = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(candidate));
                    Validate(parsed); data = parsed;
                    error = candidate == path ? "" : "이전 정상 저장본으로 복구했습니다.";
                    return true;
                }
                catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is ArgumentException || exception is UnauthorizedAccessException)
                { error = "저장 기록을 읽을 수 없습니다. " + exception.Message; }
            }
            if (error.Length == 0) error = "빈 저장 공간입니다.";
            return false;
        }
        public void Write(int slot, GameSaveData data)
        {
            Validate(data);
            Directory.CreateDirectory(directory);
            string path = PathFor(slot), temporary = path + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                { writer.Write(JsonUtility.ToJson(data, true)); writer.Flush(); stream.Flush(true); }
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        public static void Validate(GameSaveData data)
        {
            if (data == null || data.version != 1) throw new InvalidDataException("지원하지 않는 저장 형식입니다.");
            var d = data.dungeon;
            if (d == null || d.width < 15 || d.width > 101 || d.height < 11 || d.height > 101 ||
                d.cells == null || d.cells.Length != d.width * d.height || d.sections == null || d.traps == null || d.queue == null ||
                data.guards == null || data.wave == null || data.wave.enemies == null)
                throw new InvalidDataException("저장 데이터가 불완전합니다.");
            if (data.gold < 0 || data.souls < 0 || data.crystals < 0 || data.playerMaximumHealth < 1 || data.coreMaximumHealth < 1 ||
                data.playerHealth < 0 || data.playerHealth > data.playerMaximumHealth || data.coreHealth < 0 || data.coreHealth > data.coreMaximumHealth ||
                data.wave.number < 0 || data.wave.pending < 0 || !Enum.IsDefined(typeof(WaveState), data.wave.state) || !Finite(data.playerPosition) ||
                float.IsNaN(data.playedSeconds) || float.IsInfinity(data.playedSeconds) || data.playedSeconds < 0)
                throw new InvalidDataException("저장 수치가 올바르지 않습니다.");
            foreach (int cell in d.cells) if (cell < 0 || cell > 2) throw new InvalidDataException("잘못된 타일입니다.");
            foreach (var section in d.sections)
            {
                if (section == null || section.cells == null || section.cells.Length == 0 || !Enum.IsDefined(typeof(ConstructionKind), section.kind))
                    throw new InvalidDataException("잘못된 건설 구역입니다.");
                foreach (var cell in section.cells) CheckCell(d, cell);
            }
            foreach (var trap in d.traps) { if (trap == null) throw new InvalidDataException("잘못된 함정입니다."); CheckCell(d, trap.cell); }
            if (d.buildings != null)
            {
                var seen = new System.Collections.Generic.HashSet<Vector2Int>();
                foreach (var building in d.buildings)
                {
                    if (building == null || string.IsNullOrEmpty(building.designId) || building.maximumHealth <= 0 || building.health < 0 ||
                        building.health > building.maximumHealth || building.lastPaidRound < 0 || building.lastPaidRound > data.wave.number || !seen.Add(building.cell))
                        throw new InvalidDataException("잘못된 건물 기록입니다.");
                    CheckCell(d, building.cell);
                }
            }
            foreach (var cell in d.queue) CheckCell(d, cell);
            if (d.hasEntrance) CheckCell(d, d.entrance);
            foreach (var actor in data.guards) CheckActor(actor);
            foreach (var actor in data.wave.enemies) CheckActor(actor);
        }
        private static void CheckCell(DungeonSaveData d, Vector2Int p)
        { if (p.x < 0 || p.y < 0 || p.x >= d.width || p.y >= d.height) throw new InvalidDataException("맵 밖의 좌표입니다."); }
        private static bool Finite(Vector3 p) => !float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.x) && !float.IsInfinity(p.y) && !float.IsInfinity(p.z);
        private static void CheckActor(MinionSaveData actor)
        {
            if (actor == null || actor.health <= 0 || actor.maximumHealth < actor.health || !Finite(actor.position) || !Finite(actor.post) ||
                !Enum.IsDefined(typeof(MinionOrder), actor.order) || !Enum.IsDefined(typeof(MinionBehaviour), actor.behaviour)) throw new InvalidDataException("잘못된 캐릭터 기록입니다.");
        }
    }
}


