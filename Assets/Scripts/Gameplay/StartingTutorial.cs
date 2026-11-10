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
        public bool openingCasualtiesResolved;
        public bool openingEnemyCasualtyResolved;
        public int trainingVersion;
        public TutorialTrainingStep trainingStep;
        public MinionSaveData[] enemies = Array.Empty<MinionSaveData>();
    }

    // Separate encounter ownership keeps the opening out of wave rewards and upkeep.
    public sealed partial class WaveDirector
    {
        public bool BlocksTutorialInput => IsRelocating || Tutorial != null && Tutorial.BlocksPlayerInput;
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
        internal void FinishTutorial()
        {
            openingInvasionScheduled = CurrentWave == 0;
            if (openingInvasionScheduled) nextWaveRemaining = 10f;
            else ScheduleNextWave();
            Changed?.Invoke();
        }
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
        private bool casualtiesResolved;
        private bool enemyCasualtyResolved;
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
                new DialogueEntry(true, "주인 행세를 하는군.\n마왕을 쓰러뜨린 건 나다. 그 코어를 내놔.", expression: PlayerExpression.Blinking),
                new DialogueEntry(false, "행세가 아니라, 이제 내가 이 성의 주인이야.\n여긴 우리 집이야. 당장 나가.", rowenMood: TutorialPortraitMood.Determined, expression: PlayerExpression.Decline)
            },
            new[] {
                new DialogueEntry(true, "좋게 말할 때 내놓았으면 좋았을 텐데. 길을 열어라!\n코어는 바로 안쪽이다. 저 흡혈귀부터 쓰러뜨려!", rowenMood: TutorialPortraitMood.Determined, expression: PlayerExpression.Down),
                new DialogueEntry(false, "다들 코어 곁을 지켜!\n놈들이 들어오는 길은 내가 막을게.", rowenMood: TutorialPortraitMood.Determined, expression: PlayerExpression.Accept)
            },
            new[] {
                new DialogueEntry(true, "조금만 더 밀어붙여! 코어가 바로 앞이다!\n…잠깐, 천장이 무너지고 있다!", liliaMood: TutorialPortraitMood.Alarmed, rowenMood: TutorialPortraitMood.Alarmed, expression: PlayerExpression.Down),
                new DialogueEntry(false, "지지대가 버티질 못해… 모두 코어 방으로 물러나!\n다친 애들도 데려와!", liliaMood: TutorialPortraitMood.Alarmed, rowenMood: TutorialPortraitMood.Alarmed, expression: PlayerExpression.Decline),
                new DialogueEntry(true, "무너진다! 전원 후퇴!\n짐은 버려! 목숨부터 챙겨라!", liliaMood: TutorialPortraitMood.Alarmed, rowenMood: TutorialPortraitMood.Alarmed, expression: PlayerExpression.Down),
                new DialogueEntry(false, "놈들이 물러나고 있어. 지금이야, 안쪽으로!\n벽에서 떨어져. 곧 무너질 거야!", false, TutorialPortraitMood.Alarmed, expression: PlayerExpression.Blinking)
            },
            new[] {
                new DialogueEntry(true, "젠장, 잔해가 길을 막았군.\n맨손으론 어림없겠어.", liliaMood: TutorialPortraitMood.Neutral, rowenMood: TutorialPortraitMood.Aftermath, expression: PlayerExpression.Blinking),
                new DialogueEntry(false, "남의 집을 이 꼴로 만들어 놓고,\n아직도 가져갈 게 남았어? 다시는 오지 마.", rowenMood: TutorialPortraitMood.Aftermath, expression: PlayerExpression.Decline),
                new DialogueEntry(true, "이번엔 물러나 주지. 장비를 챙겨 다시 오겠다.\n그때는 반드시 코어를 가져간다.", rowenMood: TutorialPortraitMood.Determined, expression: PlayerExpression.Decline),
                new DialogueEntry(false, "…루브, 너만 남았구나. 더는 아무도 잃지 않을 거야.\n저 병사의 시체를 소환소로 옮기자.\n성을 지키려면 일손부터 늘려야 해.", false, TutorialPortraitMood.Aftermath, expression: PlayerExpression.Down)
            },
            new[] { new DialogueEntry(false, "좋아, 새 복도도 뚫었고 균열도 옮겼어.\n…발소리가 또 들려. 이번엔 여기서 막자, 루브!", false, TutorialPortraitMood.Aftermath, expression: PlayerExpression.Accept) }
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
            Clear(); Elapsed = 0; spawned = 0; collapsed = false; casualtiesResolved = false; enemyCasualtyResolved = false; Active = true;
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
            var survivor = dungeon.Guards.Where(m => m != null && m.IsAlive)
                .OrderBy(m => (m.GuardPosition - (Vector2)dungeon.CellCenter(dungeon.Layout.Core)).sqrMagnitude)
                .ThenBy(m => m.GuardPosition.y).ThenBy(m => m.GuardPosition.x).FirstOrDefault();
            foreach (var actor in FindObjectsByType<ActorHealth>(FindObjectsSortMode.None))
                if (actor.gameObject.scene == gameObject.scene && actor.Faction == Faction.Dungeon && actor.IsAlive)
                {
                    // Only the survivor and non-minion story actors are rescued during combat.
                    actor.ScriptedHealthFloor = actor.GetComponent<MinionController>() == null || actor == survivor?.Health ? 1 : 0;
                    actor.Damaged += Protect; actor.Died += CheckOpeningBattleEnd; protectedActors.Add(actor);
                }
        }
        // Keep the scripted battle playable; the collapse resolves the opening army's losses.
        private void Protect(ActorHealth source)
        {
            foreach (var actor in protectedActors)
                if (actor != null && actor.ScriptedHealthFloor > 0 && actor.IsAlive && actor.CurrentHealth < actor.MaximumHealth / 2)
                    actor.Heal(actor.MaximumHealth / 2 - actor.CurrentHealth);
            CheckOpeningBattleEnd();
        }
        private void CheckOpeningBattleEnd()
        {
            if (!Active || waves.IsFinished || collapsed || Elapsed >= BattleEnds) return;
            bool lastDefender = dungeon.Guards.Count(m => m != null && m.IsAlive && m.Health.Faction == Faction.Dungeon) <= 1;
            if (waves.Core.CurrentHealth <= waves.Core.MaximumHealth / 2 || lastDefender)
            { Elapsed = BattleEnds; dialoguePage = 2; dialogueLine = 0; DialogueOpen = true; SynchronizeDialoguePause(); }
        }
        private void Update()
        {
            SynchronizeDialoguePause();
            if (!Active || waves.IsFinished || DialogueOpen || dialogueClickFrame == Time.frameCount) return;
            if (IsTraining)
            {
                if (TrainingStep == TutorialTrainingStep.Pickup || TrainingStep == TutorialTrainingStep.Deliver) EnsureTrainingCorpse();
                if (TrainingStep == TutorialTrainingStep.Corridor || TrainingStep == TutorialTrainingStep.Entrance) UpdateExpansionTraining();
                return;
            }
            CheckOpeningBattleEnd();
            if (DialogueOpen) return;
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
            while (!collapsed && spawned < 9 && Elapsed >= 4 + spawned * 1.5f)
            { Spawn(null, spawned == 0); spawned++; }
            if (!collapsed && Elapsed >= BattleEnds)
            { ResolveEnemyCasualty(); ResolveOpeningCasualties(); }
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
            actor.Health.ScriptedHealthFloor = boss || enemyCasualtyResolved ? 1 : 0;
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
            actor.Health.Died += () =>
            {
                if (actor == null || boss) return;
                // The tutorial always leaves exactly one corpse, even if two soldiers fall in the same frame.
                bool first = !enemyCasualtyResolved && (waves.Harvest == null || waves.Harvest.Corpses.All(c => c == null || c.Consumed));
                enemyCasualtyResolved = true;
                foreach (var survivor in enemies)
                    if (survivor != null && survivor.IsAlive) survivor.Health.ScriptedHealthFloor = 1;
                if (first) waves.Harvest?.LeaveCorpse(actor.Health, true);
            };
            enemies.Add(actor);
        }
        private void ResolveEnemyCasualty()
        {
            if (enemyCasualtyResolved) return;
            // A passive player still gets exactly one real casualty for corpse training.
            var victim = enemies.FirstOrDefault(e => e != null && e.IsAlive && e.Health != rowen);
            if (victim != null) victim.Health.Kill();
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
            ResolveOpeningCasualties();
            foreach (var actor in protectedActors) if (actor != null) actor.Heal(actor.MaximumHealth);
            waves.Combat?.RefillMana(); EnsureTrainingCorpse();
        }
        private void ResolveOpeningCasualties()
        {
            if (casualtiesResolved) return;
            casualtiesResolved = true;
            var slimes = protectedActors.Where(a => a != null && a.IsAlive)
                .Select(a => a.GetComponent<MinionController>())
                .Where(m => m != null)
                .OrderBy(m => (m.GuardPosition - (Vector2)dungeon.CellCenter(dungeon.Layout.Core)).sqrMagnitude)
                .ThenBy(m => m.GuardPosition.y).ThenBy(m => m.GuardPosition.x).ToArray();
            // Kill uses the normal death/animation/unregistration path and bypasses tutorial HP floors.
            for (int i = 1; i < slimes.Length; i++) slimes[i].Health.Kill();
        }
        public TutorialSaveData Capture() => !Active ? null : new TutorialSaveData {
            active = true, elapsed = Elapsed, spawned = spawned, collapsed = collapsed, openingCasualtiesResolved = casualtiesResolved,
            openingEnemyCasualtyResolved = enemyCasualtyResolved,
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
            casualtiesResolved = data.openingCasualtiesResolved;
            enemyCasualtyResolved = data.openingEnemyCasualtyResolved || data.collapsed ||
                data.enemies != null && data.spawned > data.enemies.Length;
            Active = Elapsed < FinishAt;
            dialoguePage = data.dialogueVersion >= 1 ? data.dialoguePage : Elapsed < BattleEnds ? 1 : Elapsed < CollapseAt + 4 ? 2 : 3;
            dialogueLine = data.dialogueVersion >= 2 ? Mathf.Clamp(data.dialogueLine, 0, DialogueLineCount(dialoguePage) - 1) : dialoguePage == 3 ? 3 : dialoguePage == 1 || dialoguePage == 2 ? 1 : 0;
            DialogueOpen = data.dialogueVersion >= 1 && data.dialogueOpen;
            TrainingStep = data.trainingVersion == 1 ? data.trainingStep : TutorialTrainingStep.None;
            if (!Active) return;
            ProtectDefenders(); BindTrainingEvents();
            if (collapsed && !casualtiesResolved)
            {
                // Migrate old recovery saves, without killing recruits in already-advanced training saves.
                if (TrainingStep == TutorialTrainingStep.None || TrainingStep == TutorialTrainingStep.Pickup ||
                    TrainingStep == TutorialTrainingStep.Deliver || TrainingStep == TutorialTrainingStep.Summon) ResolveOpeningCasualties();
                else casualtiesResolved = true;
            }
            if (!collapsed && data.enemies != null) foreach (var saved in data.enemies)
                Spawn(saved, saved.tutorialBoss || saved.maximumHealth == 900 || saved.maximumHealth == RowenMaximumHealth);
            SynchronizeDialoguePause();
        }
        private void ReleaseProtection()
        { foreach (var actor in protectedActors) if (actor != null) { actor.Damaged -= Protect; actor.Died -= CheckOpeningBattleEnd; actor.ScriptedHealthFloor = 0; } protectedActors.Clear(); }
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
