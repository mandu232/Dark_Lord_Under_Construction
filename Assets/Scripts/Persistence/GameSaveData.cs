using System;
using System.IO;
using UnityEngine;
using DarkLord.Actors;
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
        public int playerLevel = 1, playerExperience;
        public LootSaveData[] loot;
        public bool sharedCoreHealth;
        public DungeonSaveData dungeon;
        public MinionSaveData[] guards = new MinionSaveData[0];
        // Missing in older saves. Minions waiting to return after the current fight.
        public MinionSaveData[] fallenGuards;
        public CorpseWorkerSaveData[] workers;
        public WaveSaveData wave;
        public TutorialSaveData tutorial;
        public CorpseSaveData[] corpses;
        // Missing in legacy saves: null is the original, normal ruleset.
        public NewGameOptions options;
        public DarkLord.Player.CombatSaveData combat;
        public DarkLord.Player.EconomySaveData economy;
    }
    [Serializable] public sealed class CorpseSaveData
    {
        public Vector3 position;
        public InvaderRole role;
        public bool carried;
    }
    [Serializable] public sealed class DungeonSaveData
    {
        public int width, height, expandedCells;
        public bool hasMapOrigin;
        public Vector3 mapOrigin;
        public Vector2Int initialCore;
        public int[] cells;
        public bool hasEntrance;
        public bool hasCoreCell;
        public Vector2Int coreCell;
        public Vector2Int entrance;
        public SectionSaveData[] sections;
        public TrapSaveData[] traps;
        public Vector2Int[] queue;
        public BuildingSaveData[] buildings;
        public AlchemyJobSaveData[] alchemyJobs;
        public ConstructionItemStack[] constructionItems;
        public int lastConstructionRewardDay;
    }
    [Serializable] public sealed class AlchemyJobSaveData
    {
        public Vector2Int cell;
        public DarkLord.Player.CurrencyType source;
        public float remaining;
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
        public int summonStacks;
        public bool tutorialCapacityUnlocked;
    }
    [Serializable] public sealed class TrapSaveData { public Vector2Int cell; public string designId; public float cooldownRemaining; }
    [Serializable] public sealed class MovementSlowSaveData { public float multiplier, remaining; }
    [Serializable] public sealed class EnemyPoiseSaveData { public int hits; public float recoveryRemaining, immunityRemaining; }
    [Serializable] public sealed class MinionSaveData
    {
        public Vector3 position;
        public Vector2 post;
        public int health, maximumHealth;
        public int level = 1, experience;
        public int tier = 1;
        public MinionOrder order;
        public MinionBehaviour behaviour;
        public MovementSlowSaveData[] slows;
        public InvaderTacticsSaveData tactics;
        public MinionRole role;
        public float attackCooldown;
        public int invasionWave;
        public MinionSupportSaveData support;
        public AugmentStatusSaveData augmentStatus;
        public EnemyPoiseSaveData poise;
        public bool tutorialBoss;
    }
    [Serializable] public sealed class WaveSaveData
    {
        public int number, pending;
        public bool minionRecoveryDisabled;
        public int tributeCharges;
        public float thornCooldown;
        public WaveState state;
        public int[] rewards;
        public string[] rewardItemIds;
        public MinionSaveData[] enemies;
        public InvaderRole[] pendingRoles;
        public int[] augments;
        public WaveScheduleSaveData schedule;
    }

    [Serializable] public sealed class WaveScheduleSaveData
    {
        public bool openingInvasionScheduled;
        public float remaining;
        public int rewardWave;
        public int[] pendingWaves, completedWaves, rewardQueue;
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
            if (data.options != null && !data.options.IsValid) throw new InvalidDataException("게임 설정이 올바르지 않습니다.");
            if (data.sharedCoreHealth && (data.playerHealth != data.coreHealth || data.playerMaximumHealth != data.coreMaximumHealth))
                throw new InvalidDataException("공유 체력 정보가 일치하지 않습니다.");
            if (data.combat != null)
            {
                var c = data.combat;
                if (!InRange(c.mana, 100) || c.coreUpgradeLevel < 0 || c.coreUpgradeLevel > 3 ||
                    !InRange(c.attackCooldown, .75f) || !InRange(c.novaCooldown, 6) || !InRange(c.wardCooldown, 12) || !InRange(c.dodgeCooldown, 3) || !InRange(c.hasteRemaining,6) || !InRange(c.wardRemaining,3))
                    throw new InvalidDataException("전투 상태가 올바르지 않습니다.");
            }
            var d = data.dungeon;
            if (d != null && d.hasMapOrigin && (!Finite(d.mapOrigin) || d.initialCore.x < 1 || d.initialCore.y < 1 ||
                d.initialCore.x >= d.width - 1 || d.initialCore.y >= d.height - 1))
                throw new InvalidDataException("저장된 맵 원점이 올바르지 않습니다.");
            if (d == null || d.width < 15 || d.width > DarkLord.Dungeon.DungeonGrid.MaxMapSize || d.height < 11 || d.height > DarkLord.Dungeon.DungeonGrid.MaxMapSize ||
                d.cells == null || d.cells.Length != d.width * d.height || d.sections == null || d.traps == null || d.queue == null ||
                data.guards == null || data.wave == null || data.wave.enemies == null)
                throw new InvalidDataException("저장 데이터가 불완전합니다.");
            if (data.gold < 0 || data.souls < 0 || data.crystals < 0 || data.playerMaximumHealth < 1 || data.coreMaximumHealth < 1 ||
                data.playerHealth < 0 || data.playerHealth > data.playerMaximumHealth || data.coreHealth < 0 || data.coreHealth > data.coreMaximumHealth ||
                data.wave.number < 0 || data.wave.pending < 0 || !Enum.IsDefined(typeof(WaveState), data.wave.state) || !Finite(data.playerPosition) ||
                float.IsNaN(data.playedSeconds) || float.IsInfinity(data.playedSeconds) || data.playedSeconds < 0)
                throw new InvalidDataException("저장 수치가 올바르지 않습니다.");
            if (data.wave.state == WaveState.RewardSelection)
            {
                var rewards = data.wave.rewards;
                if (rewards == null || rewards.Length != 3 || data.wave.schedule == null && (data.wave.pending != 0 || data.wave.enemies.Length != 0) ||
                    data.playerHealth == 0 || data.coreHealth == 0)
                    throw new InvalidDataException("보상 선택 상태가 올바르지 않습니다.");
                var unique = new System.Collections.Generic.HashSet<int>();
                foreach (int reward in rewards)
                    if (!WaveRewardCatalog.IsValid(reward) || !unique.Add(reward)) throw new InvalidDataException("보상 후보가 올바르지 않습니다.");
            }
            if (data.wave.rewardItemIds != null && data.wave.rewardItemIds.Length > 0)
            {
                if (data.wave.rewards == null || data.wave.rewardItemIds.Length != data.wave.rewards.Length)
                    throw new InvalidDataException("보상 아이템 목록이 올바르지 않습니다.");
                for (int i = 0; i < data.wave.rewardItemIds.Length; i++)
                    if (!string.IsNullOrEmpty(data.wave.rewardItemIds[i]) && data.wave.rewards[i] != WaveRewardCatalog.RoomItem && data.wave.rewards[i] != WaveRewardCatalog.FacilityItem && data.wave.rewards[i] != WaveRewardCatalog.TrapItem)
                        throw new InvalidDataException("보상 아이템 종류가 올바르지 않습니다.");
            }
            if (!InRange(data.wave.thornCooldown,2)) throw new InvalidDataException("반격 재사용 시간이 올바르지 않습니다.");
            if (data.wave.tributeCharges < 0 || data.wave.tributeCharges > 5) throw new InvalidDataException("과충전 수치가 올바르지 않습니다.");
            if (data.wave.augments != null)
            {
                var selected = new System.Collections.Generic.HashSet<int>();
                foreach (int id in data.wave.augments)
                    if (!WaveRewardCatalog.IsAugment(id) || !selected.Add(id)) throw new InvalidDataException("획득한 증강이 올바르지 않습니다.");
                if (data.wave.state == WaveState.RewardSelection)
                    foreach (int id in data.wave.rewards) if (selected.Contains(id)) throw new InvalidDataException("이미 획득한 증강입니다.");
            }
            if (d.lastConstructionRewardDay < 0 || d.lastConstructionRewardDay > data.wave.number)
                throw new InvalidDataException("건설 보상 날짜가 올바르지 않습니다.");
            if (d.constructionItems != null)
            {
                var ids = new System.Collections.Generic.HashSet<string>();
                foreach (var stack in d.constructionItems)
                    if (stack == null || string.IsNullOrEmpty(stack.id) || stack.count <= 0 || !ids.Add(stack.id))
                        throw new InvalidDataException("건설 아이템 수량이 올바르지 않습니다.");
            }
            if (data.economy != null)
            {
                var e = data.economy;
                DarkLord.Player.TechCatalog.Validate(e.technology);
                if (e.technology != null && (e.vitality != e.technology.ranks[0] || e.power != e.technology.ranks[1] || e.speed != e.technology.ranks[2] ||
                    data.combat != null && data.combat.coreUpgradeLevel != e.technology.ranks[(int)DarkLord.Player.TechId.CoreVitality]))
                    throw new InvalidDataException("강화 단계와 테크트리 기록이 일치하지 않습니다.");
                if (data.combat != null && data.combat.hasteRemaining > 0 && (e.technology == null || e.technology.ranks[(int)DarkLord.Player.TechId.Haste] == 0))
                    throw new InvalidDataException("해금되지 않은 가속 효과입니다.");
                if (e.vitality < 0 || e.vitality > 5 || e.power < 0 || e.power > 5 || e.speed < 0 || e.speed > 5 || e.bloodDebt < 0 ||
                    e.lastBloodPaid < 0 || e.lastUpkeepWave < 0 || e.lastUpkeepWave > data.wave.number || e.nextMerchantWave < 1 ||
                    !InRange(e.merchantRemaining, 90) || !Finite(e.merchantPosition) || e.offers == null || e.sold == null ||
                    e.merchantRemaining > 0 && (e.offers.Length != 3 || e.sold.Length != 4))
                    throw new InvalidDataException("자원 운영 또는 코어 강화 기록이 올바르지 않습니다.");
            }
            if (data.corpses != null)
            {
                int carried = 0;
                foreach (var corpse in data.corpses)
                    if (corpse == null || !Finite(corpse.position) || !Enum.IsDefined(typeof(InvaderRole), corpse.role) || corpse.carried && ++carried > 1)
                        throw new InvalidDataException("시체 운반 정보가 올바르지 않습니다.");
            }
            foreach (int cell in d.cells) if (cell < 0 || cell > 2) throw new InvalidDataException("잘못된 타일입니다.");
            foreach (var section in d.sections)
            {
                if (section == null || section.cells == null || section.cells.Length == 0 || !Enum.IsDefined(typeof(ConstructionKind), section.kind))
                    throw new InvalidDataException("잘못된 건설 구역입니다.");
                foreach (var cell in section.cells) CheckCell(d, cell);
            }
            foreach (var trap in d.traps)
            {
                if (trap == null || !InRange(trap.cooldownRemaining, 3600)) throw new InvalidDataException("잘못된 함정입니다.");
                CheckCell(d, trap.cell);
            }
            if (d.buildings != null)
            {
                var seen = new System.Collections.Generic.HashSet<Vector2Int>();
                foreach (var building in d.buildings)
                {
                    if (building == null || string.IsNullOrEmpty(building.designId) || building.maximumHealth <= 0 || building.health < 0 ||
                        building.health > building.maximumHealth || building.summonStacks < 0 || building.lastPaidRound < 0 || building.lastPaidRound > data.wave.number || !seen.Add(building.cell))
                        throw new InvalidDataException("잘못된 건물 기록입니다.");
                    CheckCell(d, building.cell);
                }
            }
            foreach (var cell in d.queue) CheckCell(d, cell);
            if (d.alchemyJobs != null)
            {
                var occupied = new System.Collections.Generic.HashSet<Vector2Int>();
                foreach (var job in d.alchemyJobs)
                {
                    if (job == null || (job.source != DarkLord.Player.CurrencyType.Souls && job.source != DarkLord.Player.CurrencyType.DarkCrystals) ||
                        !InRange(job.remaining, DarkLord.Player.AlchemyExchange.DurationFor(job.source)) || !occupied.Add(job.cell))
                        throw new InvalidDataException("잘못된 연성 기록입니다.");
                    CheckCell(d, job.cell);
                    bool found = false;
                    foreach (var room in d.sections)
                        if (room.complete && room.kind == ConstructionKind.AlchemyRoom &&
                            job.cell == new Vector2Int(room.bounds.xMin + room.bounds.width / 2, room.bounds.yMin + room.bounds.height / 2)) found = true;
                    if (!found) throw new InvalidDataException("연금술실을 찾을 수 없습니다.");
                }
            }
            if (d.hasEntrance) CheckCell(d, d.entrance);
            if (d.hasCoreCell)
            {
                CheckCell(d, d.coreCell);
                bool room = false;
                foreach (var section in d.sections)
                {
                    if (section.complete && DungeonConstruction.IsRoom(section.kind) && System.Array.IndexOf(section.cells, d.coreCell) >= 0) room = true;
                    if (section.complete && section.kind == ConstructionKind.AlchemyRoom && d.coreCell == new Vector2Int(section.bounds.xMin + section.bounds.width / 2, section.bounds.yMin + section.bounds.height / 2))
                        throw new InvalidDataException("연성 시설 위에 코어를 배치할 수 없습니다.");
                }
                if (!room || d.cells[d.coreCell.y * d.width + d.coreCell.x] != (int)DungeonCell.Floor ||
                    d.hasEntrance && d.coreCell == d.entrance ||
                    System.Array.Exists(d.traps, t => t.cell == d.coreCell) ||
                    d.buildings != null && System.Array.Exists(d.buildings, b => b.cell == d.coreCell) ||
                    d.alchemyJobs != null && System.Array.Exists(d.alchemyJobs, j => j.cell == d.coreCell))
                    throw new InvalidDataException("코어 위치가 올바르지 않습니다.");
            }
            foreach (var actor in data.guards) CheckActor(actor);
            if (data.fallenGuards != null) foreach (var actor in data.fallenGuards) CheckActor(actor);
            if (data.workers != null)
                foreach (var worker in data.workers)
                    if (worker == null || !Finite(worker.position) || worker.health < 1 || worker.health > CorpseWorker.MaximumHealth)
                        throw new InvalidDataException("Invalid corpse worker.");
            foreach (var actor in data.wave.enemies) CheckActor(actor);
            if (data.tutorial != null && data.tutorial.active)
            {
                var tutorial = data.tutorial;
                if (tutorial.dialogueVersion < 0 || tutorial.dialogueVersion > 3 || tutorial.dialoguePage < 0 || tutorial.dialoguePage > 4)
                    throw new InvalidDataException("튜토리얼 대사 기록이 올바르지 않습니다.");
                if (tutorial.dialogueVersion >= 2 && (tutorial.dialogueLine < 0 || tutorial.dialogueLine >= StartingTutorial.DialogueLineCount(tutorial.dialoguePage)))
                    throw new InvalidDataException("튜토리얼 대화 순서가 올바르지 않습니다.");
                if (tutorial.trainingVersion < 0 || tutorial.trainingVersion > 1 || !Enum.IsDefined(typeof(TutorialTrainingStep), tutorial.trainingStep) ||
                    tutorial.trainingStep != TutorialTrainingStep.None && !tutorial.collapsed)
                    throw new InvalidDataException("시체 소환 훈련 기록이 올바르지 않습니다.");
                if (!InRange(tutorial.elapsed, StartingTutorial.FinishAt) || tutorial.spawned < 0 || tutorial.spawned > 9 ||
                    tutorial.enemies == null || tutorial.enemies.Length > tutorial.spawned ||
                    tutorial.collapsed != (tutorial.elapsed >= StartingTutorial.CollapseAt) || tutorial.collapsed && tutorial.enemies.Length != 0 ||
                    data.wave.number != 0 || data.wave.state != WaveState.Preparing)
                    throw new InvalidDataException("시작 튜토리얼 기록이 올바르지 않습니다.");
                foreach (var actor in tutorial.enemies) CheckActor(actor);
            }
            if (data.wave.schedule != null)
            {
                var schedule = data.wave.schedule;
                if (!InRange(schedule.remaining, 300) || schedule.rewardWave < 0 || schedule.rewardWave > data.wave.number ||
                    schedule.pendingWaves == null || schedule.pendingWaves.Length != data.wave.pending || schedule.completedWaves == null || schedule.rewardQueue == null)
                    throw new InvalidDataException("침공 일정이 올바르지 않습니다.");
                var completed = new System.Collections.Generic.HashSet<int>();
                foreach (int n in schedule.completedWaves)
                    if (n < 1 || n > data.wave.number || !completed.Add(n)) throw new InvalidDataException("침공 완료 기록이 올바르지 않습니다.");
                var rewards = new System.Collections.Generic.HashSet<int>();
                foreach (int n in schedule.rewardQueue)
                    if (!completed.Contains(n) || !rewards.Add(n) || data.wave.state == WaveState.RewardSelection && n == schedule.rewardWave)
                        throw new InvalidDataException("침공 보상 대기열이 올바르지 않습니다.");
                if (data.wave.state == WaveState.RewardSelection && !completed.Contains(schedule.rewardWave))
                    throw new InvalidDataException("침공 보상 번호가 올바르지 않습니다.");
                foreach (int n in schedule.pendingWaves)
                    if (n < 1 || n > data.wave.number || completed.Contains(n)) throw new InvalidDataException("침공 소환 대기열이 올바르지 않습니다.");
                foreach (var actor in data.wave.enemies)
                    if (actor.invasionWave < 1 || actor.invasionWave > data.wave.number || completed.Contains(actor.invasionWave))
                        throw new InvalidDataException("침입자의 웨이브가 올바르지 않습니다.");
            }
            if (data.wave.pendingRoles != null)
            {
                if (data.wave.pendingRoles.Length != data.wave.pending) throw new InvalidDataException("침입 편성이 올바르지 않습니다.");
                foreach (var role in data.wave.pendingRoles)
                    if (!Enum.IsDefined(typeof(InvaderRole), role)) throw new InvalidDataException("잘못된 용사 병과입니다.");
            }
        }
        private static void CheckCell(DungeonSaveData d, Vector2Int p)
        { if (p.x < 0 || p.y < 0 || p.x >= d.width || p.y >= d.height) throw new InvalidDataException("맵 밖의 좌표입니다."); }
        private static bool Finite(Vector3 p) => !float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.x) && !float.IsInfinity(p.y) && !float.IsInfinity(p.z);
        private static bool InRange(float value, float maximum) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= maximum;
        private static void CheckActor(MinionSaveData actor)
        {
            if (actor == null || actor.health <= 0 || actor.maximumHealth < actor.health || !Finite(actor.position) || !Finite(actor.post) ||
                !Enum.IsDefined(typeof(MinionOrder), actor.order) || !Enum.IsDefined(typeof(MinionBehaviour), actor.behaviour)) throw new InvalidDataException("잘못된 캐릭터 기록입니다.");
            if (actor.slows != null) foreach (var slow in actor.slows)
                if (slow == null || !InRange(slow.multiplier, 1) || !InRange(slow.remaining, 3600))
                    throw new InvalidDataException("잘못된 둔화 기록입니다.");
            var t = actor.tactics;
            if (!Enum.IsDefined(typeof(MinionRole), actor.role) || !InRange(actor.attackCooldown, 10))
                throw new InvalidDataException("하수인 병과 기록이 올바르지 않습니다.");
            var poise = actor.poise;
            if (poise != null && (poise.hits < 0 || poise.hits > 2 || !InRange(poise.recoveryRemaining, 2f) ||
                !InRange(poise.immunityRemaining, 1.5f) || poise.hits > 0 && poise.immunityRemaining > 0))
                throw new InvalidDataException("적 강인도 기록이 올바르지 않습니다.");
            var support = actor.support;
            if (support != null && ((actor.role != MinionRole.BloodSupport && (support.casting || support.remaining > 0 || support.cooldown > 0 || support.targetPosition != Vector2.zero)) || !InRange(support.remaining, .55f) || !InRange(support.cooldown, 3.5f) ||
                !Finite(support.targetPosition) || !support.casting && support.remaining > 0))
                throw new InvalidDataException("하수인 치유 기록이 올바르지 않습니다.");
            var status = actor.augmentStatus;
            if (status != null && (!InRange(status.lordMark, 6) || !InRange(status.trapBrand, 8) || !InRange(status.empowered, 5) || !InRange(status.broodRemaining,4) || status.broodHits < 0 || status.broodHits > 2))
                throw new InvalidDataException("증강 효과 시간이 올바르지 않습니다.");
            if (t != null && (!Enum.IsDefined(typeof(InvaderRole), t.role) || !Enum.IsDefined(typeof(InvaderAction), t.action) ||
                !InRange(t.remaining, 1) || !InRange(t.cooldown, 6) || !Finite(t.direction) || !Finite(t.healPosition) ||
                (t.action == InvaderAction.Healing && t.role != InvaderRole.Healer) ||
                ((t.action == InvaderAction.WindingUp || t.action == InvaderAction.Charging || t.action == InvaderAction.Recovering) &&
                    (t.role != InvaderRole.Charger || t.direction.sqrMagnitude < .5f))))
                throw new InvalidDataException("용사 행동 기록이 올바르지 않습니다.");
        }
    }
}


