using System;
using System.Collections.Generic;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using DarkLord.Persistence;
using UnityEngine;

namespace DarkLord.Gameplay
{
    [Serializable] public sealed class TutorialSaveData
    {
        public bool active;
        public int dialogueVersion, dialoguePage, dialogueLine;
        public bool dialogueOpen;
        public float elapsed;
        public int spawned;
        public bool collapsed;
        public int trainingVersion;
        public TutorialTrainingStep trainingStep;
        public MinionSaveData[] enemies = Array.Empty<MinionSaveData>();
    }

    // Separate encounter ownership keeps the opening out of wave rewards and upkeep.
    public sealed partial class WaveDirector
    {
        public bool BlocksTutorialInput => Tutorial != null && Tutorial.BlocksPlayerInput;
        public bool InCombat => State == WaveState.Active || RemainingEnemies > 0 || Tutorial != null && Tutorial.Active && !Tutorial.Collapsed;
        public bool CanUseFacilities => !IsFinished && !InCombat && !BlocksTutorialInput;
        public StartingTutorial Tutorial { get; private set; }
        public void BeginTutorial()
        {
            if (Tutorial == null) Tutorial = gameObject.AddComponent<StartingTutorial>();
            Tutorial.Configure(this, dungeon, invaderPrefab);
            Tutorial.Begin();
            Changed?.Invoke();
        }
        public void RestoreTutorial(TutorialSaveData data)
        {
            if (Tutorial == null) Tutorial = gameObject.AddComponent<StartingTutorial>();
            Tutorial.Configure(this, dungeon, invaderPrefab);
            Tutorial.Restore(data);
            Changed?.Invoke();
        }
        internal void FinishTutorial() { ScheduleNextWave(); Changed?.Invoke(); }
    }

    [DefaultExecutionOrder(-1000)]
    public sealed partial class StartingTutorial : MonoBehaviour
    {
        public const float BattleEnds = 38, CollapseAt = 42, FinishAt = 49;
        private WaveDirector waves;
        private DungeonGrid dungeon;
        private MinionController prefab;
        private readonly List<MinionController> enemies = new List<MinionController>();
        private readonly List<ActorHealth> protectedActors = new List<ActorHealth>();
        private int spawned;
        private bool collapsed;
        private int dialoguePage, dialogueLine, dialogueClickFrame = -1;
        public bool DialogueOpen { get; private set; }
        public bool BlocksPlayerInput => Active && DialogueOpen || dialogueClickFrame == Time.frameCount;
        public float Elapsed { get; private set; }
        public bool Active { get; private set; }
        public bool Collapsed => collapsed;
        public bool Cinematic => Active && Elapsed >= BattleEnds && Elapsed < CollapseAt + 2;
        public ActorHealth Rowen => enemies.FirstOrDefault(e => e != null && e.Health.MaximumHealth == 900)?.Health;
        public int LivingEnemies => enemies.Count(e => e != null && e.IsAlive);
        private readonly struct DialogueEntry
        {
            public readonly bool Rowen;
            public readonly string Text;
            public DialogueEntry(bool rowen, string text) { Rowen = rowen; Text = text; }
        }
        private static readonly DialogueEntry[][] Conversations = {
            new[] {
                new DialogueEntry(true, "마왕이여, 오늘 여기서 끝내겠다!"),
                new DialogueEntry(false, "로웬… 감히 내 영역을 짓밟았겠다.\n권속들이여, 저 침입자들을 맞이하라!")
            },
            new[] {
                new DialogueEntry(true, "토벌대여, 전진하라! 저 코어를 부수면 마왕의 지배도 끝이다!"),
                new DialogueEntry(false, "내 코어에는 손대지 못한다. 권속들이여, 이 방을 지켜라!\nWASD 이동 · 좌클릭 공격 · Space 회피 · 하수인을 꾹 눌러 방에 배치")
            },
            new[] {
                new DialogueEntry(true, "더 밀어붙여라! …잠깐, 이 진동은 뭐지?"),
                new DialogueEntry(false, "천장이 버티지 못한다! 권속들이여, 시작방으로 물러서라!"),
                new DialogueEntry(true, "낙석이다! 전원 후퇴하라! 대열을 유지해!"),
                new DialogueEntry(false, "입구 쪽 복도가 무너진다… 이 틈에 추격을 끊을 수 있겠군.")
            },
            new[] {
                new DialogueEntry(true, "길이 막혔군… 마왕, 이것으로 끝났다고 생각하지 마라!"),
                new DialogueEntry(false, "아직 내 던전은 살아 있다. 오늘은 여기까지다, 로웬."),
                new DialogueEntry(true, "기억해라. 다음에는 네 코어까지 반드시 도달하겠다!"),
                new DialogueEntry(false, "숨 돌릴 틈은 벌었다. 남은 복도 끝을 새 입구로 삼자.\n이제 시체를 F로 들어 소환소에 운반하고, 그 힘으로 새 권속을 불러내겠다.")
            },
            new[] { new DialogueEntry(false, "새 권속이 깨어났군! 시체를 회수하고 소환하는 법을 익혔다.\n로웬이 돌아오기 전에 던전을 다시 세우자.") }
        };
        public static int DialogueLineCount(int page) => Conversations[Mathf.Clamp(page, 0, 4)].Length;
        public bool SpeakerIsRowen => Conversations[dialoguePage][dialogueLine].Rowen;
        public string SpeakerName => SpeakerIsRowen ? "로웬 · 토벌대 지휘관" : "마왕 · 플레이어";
        public Sprite SpeakerSprite => (SpeakerIsRowen ? prefab.GetComponentInChildren<SpriteRenderer>() : waves.Lord.GetComponentInChildren<SpriteRenderer>())?.sprite;
        public string Heading => DialogueOpen ? SpeakerName : Elapsed < BattleEnds ? "강적 로웬과 토벌대의 침공" : Elapsed < CollapseAt ? "경고 · 복도가 무너집니다!" : "붕괴 · 추격이 끊겼습니다";
        public string Instruction => Conversations[dialoguePage][dialogueLine].Text;

        public void Configure(WaveDirector director, DungeonGrid grid, MinionController enemyPrefab)
        { waves = director; dungeon = grid; prefab = enemyPrefab; }
        public void Begin()
        {
            Clear(); Elapsed = 0; spawned = 0; collapsed = false; Active = true;
            dialoguePage = 0; dialogueLine = 0; DialogueOpen = true;
            ProtectDefenders(); BindTrainingEvents();
            SynchronizeDialoguePause();
        }
        public bool NextDialogue()
        {
            if (!Active || !DialogueOpen || MenuOwnsPause || !OwnsDialoguePause && Time.timeScale <= 0) return false;
            dialogueClickFrame = Time.frameCount;
            if (dialogueLine + 1 < DialogueLineCount(dialoguePage)) dialogueLine++;
            else if (dialoguePage == 0) { dialoguePage = 1; dialogueLine = 0; }
            else { DialogueOpen = false; if (dialoguePage == 3) BeginTraining(); else if (dialoguePage == 4) CompleteTutorial(); }
            SynchronizeDialoguePause();
            return true;
        }
        private void ProtectDefenders()
        {
            foreach (var actor in FindObjectsByType<ActorHealth>(FindObjectsSortMode.None))
                if (actor.gameObject.scene == gameObject.scene && actor.Faction == Faction.Dungeon && actor.IsAlive)
                { actor.ScriptedHealthFloor = 1; actor.Damaged += Protect; protectedActors.Add(actor); }
        }
        // The scripted opening teaches defence without consuming the starting army.
        private void Protect(ActorHealth source)
        {
            foreach (var actor in protectedActors)
                if (actor != null && actor.IsAlive && actor.CurrentHealth < actor.MaximumHealth / 2)
                    actor.Heal(actor.MaximumHealth / 2 - actor.CurrentHealth);
            if (waves.Core.CurrentHealth <= waves.Core.MaximumHealth / 2 && Elapsed < BattleEnds)
            { Elapsed = BattleEnds; dialoguePage = 2; dialogueLine = 0; DialogueOpen = true; SynchronizeDialoguePause(); }
        }
        private void Update()
        {
            SynchronizeDialoguePause();
            if (!Active || waves.IsFinished || DialogueOpen || dialogueClickFrame == Time.frameCount) return;
            if (IsTraining)
            {
                if (TrainingStep == TutorialTrainingStep.Pickup || TrainingStep == TutorialTrainingStep.Deliver) EnsureTrainingCorpse();
                return;
            }
            UpdateRetreat();
            float boundary = Elapsed < BattleEnds ? BattleEnds : Elapsed < CollapseAt + 4 ? CollapseAt + 4 : FinishAt;
            Advance(Mathf.Min(Time.deltaTime, boundary - Elapsed));
        }
        public void Advance(float seconds)
        {
            if (!Active || seconds <= 0 || IsTraining) return;
            float previous = Elapsed;
            Elapsed = Mathf.Min(CollapseAt + 4, Elapsed + seconds);
            if (previous < BattleEnds && Elapsed >= BattleEnds) { dialoguePage = 2; dialogueLine = 0; DialogueOpen = true; }
            if (previous < CollapseAt + 4 && Elapsed >= CollapseAt + 4) { dialoguePage = 3; dialogueLine = 0; DialogueOpen = true; }
            while (spawned < 9 && Elapsed >= 4 + spawned * 1.5f && Elapsed < CollapseAt)
            { Spawn(null, spawned == 0); spawned++; }
            if (Elapsed >= CollapseAt && !collapsed) Collapse();
            SynchronizeDialoguePause();
        }
        private void Spawn(MinionSaveData saved, bool boss)
        {
            var actor = Instantiate(prefab, saved != null ? saved.position : dungeon.CellCenter(dungeon.Layout.Entrance) + Vector3.up * ((spawned % 3 - 1) * .4f), Quaternion.identity, transform);
            actor.name = boss ? "강적 로웬" : "로웬의 토벌대 병사";
            actor.Health.Configure(Faction.Invader, boss ? 900 : 65);
            if (boss) actor.Health.ScriptedHealthFloor = 1;
            if (saved != null) actor.Health.RestoreHealth(saved.maximumHealth, saved.health);
            var tactics = actor.GetComponent<InvaderTactics>() ?? actor.gameObject.AddComponent<InvaderTactics>();
            tactics.Configure(boss ? InvaderRole.Charger : spawned % 3 == 0 ? InvaderRole.Shieldbearer : InvaderRole.Warrior);
            if (saved != null)
            {
                tactics.RestoreSave(saved.tactics);
                actor.RestoreAttackCooldown(saved.attackCooldown);
                actor.GetComponent<TopDownMotor>().RestoreSlows(saved.slows);
            }
            if (boss) actor.transform.localScale *= 1.4f;
            actor.Assault(waves.Core, dungeon);
            actor.Health.Died += () => { if (actor != null && !boss) waves.Harvest?.LeaveCorpse(actor.Health); };
            enemies.Add(actor);
        }
        private void Collapse()
        {
            collapsed = true;
            // Rescue actors before changing collision; Rowen retreats, never awards a boss kill.
            var safe = dungeon.CellCenter(dungeon.Layout.Core);
            waves.Lord.GetComponent<DarkLord.Player.MinionSummoning>()?.SetOpen(false);
            foreach (var actor in protectedActors)
                if (actor != null && actor != waves.Core && dungeon.WorldToCell(actor.transform.position).x < dungeon.Layout.Core.x - 3)
                {
                    actor.transform.position = safe;
                    if (actor.TryGetComponent<Rigidbody2D>(out var body)) body.position = safe;
                }
            foreach (var enemy in enemies) if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            enemies.Clear();
            if (waves.Harvest != null) foreach (var corpse in waves.Harvest.Corpses)
                if (corpse != null && dungeon.WorldToCell(corpse.transform.position).x <= dungeon.Layout.Core.x - 5)
                    corpse.transform.position = safe + Vector3.left * 2;
            dungeon.CollapseOpeningCorridor();
            foreach (var actor in protectedActors) if (actor != null) actor.Heal(actor.MaximumHealth);
            waves.Combat?.RefillMana(); EnsureTrainingCorpse();
        }
        public TutorialSaveData Capture() => !Active ? null : new TutorialSaveData {
            active = true, elapsed = Elapsed, spawned = spawned, collapsed = collapsed,
            trainingVersion = 1, trainingStep = TrainingStep, dialogueVersion = 3, dialoguePage = dialoguePage, dialogueLine = dialogueLine, dialogueOpen = DialogueOpen,
            enemies = enemies.Where(e => e != null && e.IsAlive).Select(SaveSnapshots.Actor).ToArray()
        };
        public void Restore(TutorialSaveData data)
        {
            Clear();
            if (data == null || !data.active) return;
            Elapsed = Mathf.Clamp(data.elapsed, 0, FinishAt); spawned = Mathf.Clamp(data.spawned, 0, 9); collapsed = data.collapsed;
            Active = Elapsed < FinishAt;
            dialoguePage = data.dialogueVersion >= 1 ? data.dialoguePage : Elapsed < BattleEnds ? 1 : Elapsed < CollapseAt + 4 ? 2 : 3;
            dialogueLine = data.dialogueVersion >= 2 ? Mathf.Clamp(data.dialogueLine, 0, DialogueLineCount(dialoguePage) - 1) : dialoguePage == 3 ? 3 : dialoguePage == 1 || dialoguePage == 2 ? 1 : 0;
            DialogueOpen = data.dialogueVersion >= 1 && data.dialogueOpen;
            TrainingStep = data.trainingVersion == 1 ? data.trainingStep : TutorialTrainingStep.None;
            if (!Active) return;
            ProtectDefenders(); BindTrainingEvents();
            if (!collapsed && data.enemies != null) foreach (var saved in data.enemies) Spawn(saved, saved.maximumHealth == 900);
            SynchronizeDialoguePause();
        }
        private void ReleaseProtection()
        { foreach (var actor in protectedActors) if (actor != null) { actor.Damaged -= Protect; actor.ScriptedHealthFloor = 0; } protectedActors.Clear(); }
        private void Clear()
        {
            UnbindTrainingEvents(); TrainingStep = TutorialTrainingStep.None;
            ReleaseProtection(); Active = false; DialogueOpen = false; dialogueClickFrame = -1;
            ReleaseDialoguePause();
            foreach (var enemy in enemies) if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            enemies.Clear();
        }
        private void OnDisable() => ReleaseDialoguePause();
        private void OnDestroy() { UnbindTrainingEvents(); ReleaseProtection(); ReleaseDialoguePause(); }
    }
}
