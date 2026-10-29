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
    public enum TutorialPortraitMood { Neutral, Determined, Alarmed, Aftermath }
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
        public const int RowenMaximumHealth = 20000;
        private WaveDirector waves;
        private DungeonGrid dungeon;
        private MinionController prefab;
        private readonly List<MinionController> enemies = new List<MinionController>();
        private readonly List<ActorHealth> protectedActors = new List<ActorHealth>();
        private int spawned;
        private bool collapsed;
        private ActorHealth rowen;
        private int dialoguePage, dialogueLine, dialogueClickFrame = -1;
        public bool DialogueOpen { get; private set; }
        public bool BlocksPlayerInput => Active && DialogueOpen || dialogueClickFrame == Time.frameCount;
        public float Elapsed { get; private set; }
        public bool Active { get; private set; }
        public bool Collapsed => collapsed;
        public bool Cinematic => Active && Elapsed >= BattleEnds && Elapsed < CollapseAt + 2;
        public ActorHealth Rowen => rowen;
        public int LivingEnemies => enemies.Count(e => e != null && e.IsAlive);
        private readonly struct DialogueEntry
        {
            public readonly bool Rowen;
            public readonly string Text;
            public readonly bool RowenPresent;
            public readonly TutorialPortraitMood LiliaMood, RowenMood;
            public readonly PlayerExpression? Expression;
            public DialogueEntry(bool rowen, string text, bool rowenPresent = true,
                TutorialPortraitMood liliaMood = TutorialPortraitMood.Determined, TutorialPortraitMood rowenMood = TutorialPortraitMood.Neutral, PlayerExpression? expression = null)
            { Rowen = rowen; Text = text; RowenPresent = rowenPresent; LiliaMood = liliaMood; RowenMood = rowenMood; Expression = expression; }
        }
        private static readonly DialogueEntry[][] Conversations = {
            new[] {
                new DialogueEntry(true, "도면까지 펴 놨군. 여기가 네 성이라도 된 줄 알아?\n마왕을 쓰러뜨린 건 나야. 그 코어는 내 몫이고.", expression: PlayerExpression.Blinking),
                new DialogueEntry(false, "네가 부수고 간 곳에서 우린 아직 살아.\n코어도 이 성도 원래 우리 것이었어.", rowenMood: TutorialPortraitMood.Determined, expression: PlayerExpression.Decline)
            },
            new[] {
                new DialogueEntry(true, "말로 해선 안 되겠군. 앞줄, 길을 열어!\n코어는 건드리지 마. 저 녀석부터 치운다.", rowenMood: TutorialPortraitMood.Determined, expression: PlayerExpression.Down),
                new DialogueEntry(false, "다들 방 안쪽을 지켜. 통로는 내가 막을 테니.\nWASD 이동 · 좌클릭 공격 · Space 회피 · 하수인을 꾹 눌러 방에 배치", rowenMood: TutorialPortraitMood.Determined, expression: PlayerExpression.Accept)
            },
            new[] {
                new DialogueEntry(true, "조금만 더 밀어! 코어가 바로 앞이다!\n…잠깐. 천장에 금이 가는데?", liliaMood: TutorialPortraitMood.Alarmed, rowenMood: TutorialPortraitMood.Alarmed, expression: PlayerExpression.Down),
                new DialogueEntry(false, "지지대가 벌어졌어… 모두 시작방으로 물러나!\n다친 녀석 두고 오지 말고!", liliaMood: TutorialPortraitMood.Alarmed, rowenMood: TutorialPortraitMood.Alarmed, expression: PlayerExpression.Decline),
                new DialogueEntry(true, "무너진다! 전부 뒤로!\n짐 버려! 거기 깔리면 코어고 뭐고 끝이야!", liliaMood: TutorialPortraitMood.Alarmed, rowenMood: TutorialPortraitMood.Alarmed, expression: PlayerExpression.Down),
                new DialogueEntry(false, "…발소리가 멀어졌어. 우리도 더 안쪽으로 가자.\n벽엔 기대지 마. 저 균열도 벌어지고 있으니까.", false, TutorialPortraitMood.Alarmed, expression: PlayerExpression.Blinking)
            },
            new[] {
                new DialogueEntry(true, "젠장, 또 길이 막혔군. 이걸 치우다간 날 새겠어.\n철수한다. 도구부터 다시 챙겨 와.", liliaMood: TutorialPortraitMood.Neutral, rowenMood: TutorialPortraitMood.Aftermath, expression: PlayerExpression.Blinking),
                new DialogueEntry(false, "그만 포기하고 돌아가지?\n남의 집을 이 꼴로 만들어 놓고, 뭘 더 가져가겠다고.", rowenMood: TutorialPortraitMood.Aftermath, expression: PlayerExpression.Decline),
                new DialogueEntry(true, "잠깐 맡겨 두는 거야. 네 것이 된 줄 알지 마.\n내 코어는 내가 도로 가져갈 테니까.", rowenMood: TutorialPortraitMood.Determined, expression: PlayerExpression.Decline),
                new DialogueEntry(false, "…이제 갔네. 남의 집 부숴 놓고 끝까지 자기 거라네.\n루브, 도면 다시 펴 줘. 남은 복도 끝을 입구로 쓰자.\n소환소도 손봐야겠어. 시체는 F로 옮기면 돼. 일손이 필요하니까.", false, TutorialPortraitMood.Aftermath, expression: PlayerExpression.Down)
            },
            new[] { new DialogueEntry(false, "깨어났어? 이쪽으로 와. 거긴 아직 위험해.\n루브 혼자 맡던 일도 이제 나눌 수 있겠네.\n…그럼, 아까 그 도면부터 마저 보자. 입구는 다시 짜야겠어.", false, TutorialPortraitMood.Aftermath, expression: PlayerExpression.Accept) }
        };
        public static int DialogueLineCount(int page) => Conversations[Mathf.Clamp(page, 0, 4)].Length;
        public bool SpeakerIsRowen => Conversations[dialoguePage][dialogueLine].Rowen;
        public bool RowenInConversation => Conversations[dialoguePage][dialogueLine].RowenPresent;
        public TutorialPortraitMood LiliaMood => Conversations[dialoguePage][dialogueLine].LiliaMood;
        public TutorialPortraitMood RowenMood => Conversations[dialoguePage][dialogueLine].RowenMood;
        public string SpeakerName => SpeakerIsRowen ? "로웬 · 서풍의 용사" : "릴리아 · 흑관성의 새 주인";
        public Sprite SpeakerSprite => (SpeakerIsRowen ? prefab.GetComponentInChildren<SpriteRenderer>() : waves.Lord.GetComponentInChildren<SpriteRenderer>())?.sprite;
        public string Heading => DialogueOpen ? SpeakerName : Elapsed < BattleEnds ? "서풍의 용사 로웬과 토벌대의 침공" : Elapsed < CollapseAt ? "경고 · 복도가 무너집니다!" : "붕괴 · 추격이 끊겼습니다";
        public PlayerExpression? DialogueExpression => Conversations[dialoguePage][dialogueLine].Expression;
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
            actor.name = boss ? "서풍의 용사 로웬" : "로웬의 토벌대 병사";
            actor.Health.Configure(Faction.Invader, boss ? RowenMaximumHealth : 65);
            if (saved != null)
            {
                int maximum = boss && saved.maximumHealth == 900 ? RowenMaximumHealth : saved.maximumHealth;
                int health = boss && saved.maximumHealth == 900 ? maximum - (saved.maximumHealth - saved.health) : saved.health;
                actor.Health.RestoreHealth(maximum, health);
            }
            if (boss) rowen = actor.Health;
            var tactics = actor.GetComponent<InvaderTactics>() ?? actor.gameObject.AddComponent<InvaderTactics>();
            tactics.Configure(boss ? InvaderRole.Charger : spawned % 3 == 0 ? InvaderRole.Shieldbearer : InvaderRole.Warrior);
            if (saved != null)
            {
                tactics.RestoreSave(saved.tactics);
                actor.RestoreAttackCooldown(saved.attackCooldown);
                actor.RestorePoise(saved.poise);
                actor.GetComponent<TopDownMotor>().RestoreSlows(saved.slows);
            }
            tactics.SetHero(boss);
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
            enemies = enemies.Where(e => e != null && e.IsAlive).Select(e => {
                var saved = SaveSnapshots.Actor(e); saved.tutorialBoss = e.Health == rowen; return saved;
            }).ToArray()
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
            if (!collapsed && data.enemies != null) foreach (var saved in data.enemies)
                Spawn(saved, saved.tutorialBoss || saved.maximumHealth == 900 || saved.maximumHealth == RowenMaximumHealth);
            SynchronizeDialoguePause();
        }
        private void ReleaseProtection()
        { foreach (var actor in protectedActors) if (actor != null) { actor.Damaged -= Protect; actor.ScriptedHealthFloor = 0; } protectedActors.Clear(); }
        private void Clear()
        {
            ReleasePlayerExpression();
            UnbindTrainingEvents(); TrainingStep = TutorialTrainingStep.None;
            ReleaseProtection(); Active = false; DialogueOpen = false; dialogueClickFrame = -1;
            ReleaseDialoguePause();
            foreach (var enemy in enemies) if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            enemies.Clear();
            rowen = null;
        }
        private void OnDisable() { ReleasePlayerExpression(); ReleaseDialoguePause(); }
        private void OnDestroy() { ReleasePlayerExpression(); UnbindTrainingEvents(); ReleaseProtection(); ReleaseDialoguePause(); }
    }
}
