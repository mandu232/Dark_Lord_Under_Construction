using System.Collections;
using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("StartingTutorial")]
    [Category("DialogueInteraction")]
    [Category("CorpseCameraRevision")]
    [Category("OpeningRegression")]
    public sealed class StartingTutorialTests
    {
        private Scene scene;
        private WaveDirector waves;
        private DungeonGrid grid;
        private GameSaveSession session;
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1;
            GameSaveSession.BeginNew(0, new NewGameOptions { difficulty = GameDifficulty.Easy });
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            waves = Find<WaveDirector>(); grid = Find<DungeonGrid>(); session = Find<GameSaveSession>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Time.timeScale = 1; if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene); }
        [UnityTest, Category("TutorialLastDefender")] public IEnumerator LastSurvivingMinionEndsOpeningWithPlayerAboveHalfHealth()
        {
            var guards=grid.Guards.Where(g=>g != null && g.IsAlive).ToArray(); Assert.AreEqual(4,guards.Length);
            Assert.Greater(waves.Lord.CurrentHealth,waves.Lord.MaximumHealth/2);
            guards[0].Health.Kill(); guards[1].Health.Kill();
            Assert.Less(waves.Tutorial.Elapsed,StartingTutorial.BattleEnds,"Two survivors should not end the battle");
            guards[2].Health.Kill();
            Assert.AreEqual(1,grid.Guards.Count(g=>g != null && g.IsAlive));
            Assert.AreEqual(StartingTutorial.BattleEnds,waves.Tutorial.Elapsed);
            Assert.IsTrue(waves.Tutorial.DialogueOpen); Assert.IsTrue(waves.Tutorial.Instruction.Contains("천장"));
            Assert.Greater(waves.Lord.CurrentHealth,waves.Lord.MaximumHealth/2);
            waves.Tutorial.Advance(5); Assert.IsTrue(waves.Tutorial.Collapsed);
            Assert.AreEqual(1,grid.Guards.Count(g=>g != null && g.IsAlive));
            yield return null;
        }
        [UnityTest, Category("TutorialLastDefender")] public IEnumerator HalfHealthStillEndsOpeningWithMultipleMinionsAlive()
        {
            Assert.AreEqual(4,grid.Guards.Count(g=>g != null && g.IsAlive));
            waves.Lord.TakeDamage(waves.Lord.MaximumHealth);
            Assert.GreaterOrEqual(waves.Tutorial.Elapsed,StartingTutorial.BattleEnds);
            Assert.IsTrue(waves.Tutorial.DialogueOpen);
            yield return null;
        }
        [UnityTest] public IEnumerator FreshGameStartsTutorialWithFourGuardsAndBlocksWaves()
        {
            Assert.IsTrue(waves.Tutorial.Active); Assert.AreEqual(4, Find<MinionSummoning>().GuardCount);
            Assert.IsFalse(waves.CanStartWave); waves.BeginNextWave(); Assert.AreEqual(0, waves.CurrentWave);
            waves.Tutorial.Advance(18);
            Assert.AreEqual(9, waves.Tutorial.LivingEnemies); Assert.AreEqual(StartingTutorial.RowenMaximumHealth, waves.Tutorial.Rowen.MaximumHealth);
            Assert.AreEqual(0, waves.RemainingEnemies); yield return null;
        }
        [UnityTest] public IEnumerator RowenBossBarTracksHealthAndLegacySaveMigrates()
        {
            var presentation = Find<DarkLord.UI.StartingTutorialPresentation>();
            var bar = presentation.transform.Find("Rowen boss health bar");
            Assert.IsNotNull(bar); Assert.IsFalse(bar.gameObject.activeSelf);
            // Older scene variants retain this legacy HUD; current scenes use GameHud directly.
            foreach (var waveHud in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DarkLord.UI.WaveHud>(true)))
            {
                Assert.IsTrue(waveHud.isActiveAndEnabled);
                Assert.IsFalse(waveHud.GetComponent<UnityEngine.UI.Image>().enabled);
                Assert.IsFalse(waveHud.transform.Find("Castle Frame").GetComponent<UnityEngine.UI.Image>().enabled);
                Assert.IsFalse(waveHud.transform.Find("Wave").GetComponent<TMPro.TMP_Text>().enabled);
                Assert.IsFalse(waveHud.transform.Find("Status").GetComponent<TMPro.TMP_Text>().enabled);
            }
            Assert.AreEqual("로웬 · 서풍의 용사", waves.Tutorial.SpeakerName);
            TutorialTestActions.DismissDialogue(waves); yield return null;
            waves.Tutorial.Advance(4); yield return null;
            var rowen = waves.Tutorial.Rowen;
            Assert.AreEqual(StartingTutorial.RowenMaximumHealth, rowen.MaximumHealth);
            Assert.IsTrue(bar.gameObject.activeSelf);
            Assert.AreEqual("서풍의 용사 · 로웬", bar.Find("Boss name").GetComponent<TMPro.TMP_Text>().text);
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(rowen.TakeDamage(1000)); yield return null;
            Assert.AreEqual(StartingTutorial.RowenMaximumHealth - 1000, rowen.CurrentHealth);
            var fill = bar.Find("Health track/Health fill").GetComponent<RectTransform>();
            Assert.That(fill.anchorMax.x, Is.EqualTo(.95f).Within(.001f));
            var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            var boss = saved.tutorial.enemies.Single(e => e.tutorialBoss);
            SaveSlotStore.Validate(saved); session.Restore(saved); yield return null;
            Assert.AreEqual(StartingTutorial.RowenMaximumHealth - 1000, waves.Tutorial.Rowen.CurrentHealth);
            Assert.IsTrue(bar.gameObject.activeSelf);
            boss.tutorialBoss = false; boss.maximumHealth = 900; boss.health = 400;
            session.Restore(saved); yield return null;
            Assert.AreEqual(StartingTutorial.RowenMaximumHealth, waves.Tutorial.Rowen.MaximumHealth);
            Assert.AreEqual(StartingTutorial.RowenMaximumHealth - 500, waves.Tutorial.Rowen.CurrentHealth);
            waves.Tutorial.Rowen.TakeDamage(StartingTutorial.RowenMaximumHealth);
            Assert.IsTrue(waves.Tutorial.Rowen.IsAlive, "The story boss must retreat; only one soldier may die.");
            Assert.AreEqual(1, waves.Tutorial.Rowen.CurrentHealth);
            yield return null; Assert.IsTrue(bar.gameObject.activeSelf);
        }
        [UnityTest] public IEnumerator BossEntranceRevealsCurrentHealthAndPausesWithGameplay()
        {
            TutorialTestActions.DismissDialogue(waves); yield return null;
            waves.Tutorial.Advance(4); yield return null;
            var bar = Find<DarkLord.UI.StartingTutorialPresentation>().transform.Find("Rowen boss health bar").GetComponent<RectTransform>();
            var fill = bar.Find("Health track/Health fill").GetComponent<RectTransform>();
            Assert.IsTrue(bar.gameObject.activeInHierarchy);
            Assert.Less(fill.anchorMax.x, .1f);
            yield return new WaitForSeconds(.55f);
            float partial = fill.anchorMax.x;
            Assert.That(partial, Is.InRange(.1f, .8f));
            Assert.IsTrue(waves.Tutorial.Rowen.TakeDamage(2000));
            Time.timeScale = 0; yield return new WaitForSecondsRealtime(.2f);
            float paused = fill.anchorMax.x;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(fill.anchorMax.x, Is.EqualTo(paused).Within(.001f));
            Time.timeScale = 1; yield return new WaitForSeconds(1.4f);
            float actual = (float)waves.Tutorial.Rowen.CurrentHealth / waves.Tutorial.Rowen.MaximumHealth;
            Assert.That(fill.anchorMax.x, Is.EqualTo(actual).Within(.001f));
            Assert.That(bar.anchoredPosition, Is.EqualTo(new Vector2(0, -38)));
            Assert.That(bar.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1).Within(.001f));
            var glint = bar.Find("Health track/Entrance glint").GetComponent<UnityEngine.UI.Image>();
            Assert.That(glint.color.a, Is.EqualTo(0).Within(.001f));
        }
        [UnityTest] public IEnumerator OpeningCasualtiesAreOneEnemyAndAllButOneDefenderIncludingAfterSave()
        {
            TutorialTestActions.DismissDialogue(waves); yield return null;
            waves.Tutorial.Advance(18);
            var army = grid.Guards.ToArray();
            Assert.AreEqual(4, army.Length);
            var soldiers = waves.Tutorial.GetComponentsInChildren<DarkLord.Minions.MinionController>()
                .Where(m => m.Health.Faction == DarkLord.Actors.Faction.Invader).ToArray();
            Assert.AreEqual(9, soldiers.Length);
            foreach (var guard in army) guard.Health.TakeDamage(999999);
            Assert.AreEqual(1, army.Count(g => g.IsAlive), "Only one defender is protected during combat.");
            foreach (var enemy in soldiers) enemy.Health.TakeDamage(999999);
            Assert.AreEqual(1, soldiers.Count(e => !e.IsAlive));
            Assert.IsTrue(waves.Tutorial.Rowen.IsAlive);
            Assert.AreEqual(1, waves.Harvest.Corpses.Count());
            var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            Assert.IsTrue(saved.tutorial.openingEnemyCasualtyResolved);
            session.Restore(saved); yield return null;
            foreach (var enemy in waves.Tutorial.GetComponentsInChildren<DarkLord.Minions.MinionController>())
                if (enemy.Health.Faction == DarkLord.Actors.Faction.Invader) enemy.Health.TakeDamage(999999);
            Assert.AreEqual(8, waves.Tutorial.LivingEnemies);
            Assert.AreEqual(1, waves.Harvest.Corpses.Count());
            waves.Tutorial.Advance(43);
            Assert.AreEqual(1, grid.Guards.Count(g => g.IsAlive));
            Assert.AreEqual(1, waves.Harvest.Corpses.Count());
            Assert.IsNull(Find<DarkLord.UI.StartingTutorialPresentation>().GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Rube companion portrait"));
        }

        [UnityTest] public IEnumerator PassiveOpeningStillLeavesExactlyOneEnemyCorpseAndOneDefender()
        {
            waves.Tutorial.Advance(43);
            Assert.AreEqual(1, grid.Guards.Count(g => g.IsAlive));
            Assert.AreEqual(1, waves.Harvest.Corpses.Count());
            Assert.IsTrue(waves.Tutorial.Capture().openingEnemyCasualtyResolved);
            yield return null;
        }

        [UnityTest] public IEnumerator CollapseChangesTerrainRescuesPlayerAndLeavesUsableEntrance()
        {
            var old = grid.Layout.Entrance;
            waves.Lord.transform.position = grid.CellCenter(old);
            waves.Tutorial.Advance(43);
            Assert.IsTrue(waves.Tutorial.Collapsed); Assert.IsFalse(grid.Layout.IsFloor(old));
            Assert.AreNotEqual(old, grid.Layout.Entrance); Assert.IsTrue(grid.Layout.IsFloor(grid.Layout.Entrance));
            Assert.IsTrue(grid.Construction.FindPath(grid.Layout.Entrance, grid.Layout.Core, new System.Collections.Generic.List<Vector2Int>()));
            Assert.AreEqual(grid.Layout.Core, grid.WorldToCell(waves.Lord.transform.position));
            Physics2D.SyncTransforms();
            Assert.IsTrue(Physics2D.OverlapPointAll(grid.CellCenter(old)).Any(c => c is BoxCollider2D));
            Assert.AreEqual(0, waves.Tutorial.LivingEnemies);
            Assert.IsNull(grid.Construction.At(old), "Collapsed opening must release the previous corridor reservation.");
            Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlanCorridor(grid.Layout.Entrance, old + Vector2Int.right, out var rebuilt));
            for (int pass = 0; pass < rebuilt.Cells.Count; pass++)
                foreach (var cell in rebuilt.Cells) if (!grid.Layout.IsFloor(cell)) grid.Layout.TryExpand(cell);
            Assert.IsTrue(rebuilt.IsComplete);
            Assert.IsTrue(grid.Construction.FindPath(old + Vector2Int.right, grid.Layout.Core, new System.Collections.Generic.List<Vector2Int>()));
            yield return null;
        }
        [UnityTest] public IEnumerator TutorialSaveRoundTripPreservesEncounterAndLegacyDoesNotRestart()
        {
            waves.Tutorial.Advance(18); var saved = session.Capture();
            SaveSlotStore.Validate(saved); var health = waves.Tutorial.Rowen.CurrentHealth;
            saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(saved)); session.Restore(saved);
            Assert.IsTrue(waves.Tutorial.Active); Assert.AreEqual(9, waves.Tutorial.LivingEnemies);
            Assert.AreEqual(health, waves.Tutorial.Rowen.CurrentHealth);
            saved.tutorial = null; session.Restore(saved);
            Assert.IsFalse(waves.Tutorial.Active); Assert.AreEqual(0, waves.Tutorial.LivingEnemies); yield return null;
        }
        [UnityTest] public IEnumerator CompletedOpeningSavesCollapsedMapAndStartsRealFirstWave()
        {
            var blood = Find<CurrencyWallet>().GetAmount(CurrencyType.Souls);
            yield return TutorialTestActions.Complete(waves); Assert.IsFalse(waves.Tutorial.Active);
            var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture())); SaveSlotStore.Validate(saved); session.Restore(saved);
            Assert.IsFalse(waves.Tutorial.Active); Assert.IsTrue(waves.CanStartWave);
            Assert.AreEqual(blood, Find<CurrencyWallet>().GetAmount(CurrencyType.Souls));
            waves.BeginNextWave(); Assert.AreEqual(1, waves.CurrentWave); yield return null;
        }
        [UnityTest] public IEnumerator PauseAndAutomaticTimerWaitForTutorial()
        {
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Normal });
            float timer = waves.NextWaveRemaining, elapsed = waves.Tutorial.Elapsed;
            Time.timeScale = 0; yield return null; yield return null;
            Assert.AreEqual(elapsed, waves.Tutorial.Elapsed); Assert.AreEqual(timer, waves.NextWaveRemaining);
            Time.timeScale = 1; yield return null; yield return null;
            Assert.AreEqual(timer, waves.NextWaveRemaining);
            yield return TutorialTestActions.Complete(waves); Assert.GreaterOrEqual(waves.NextWaveRemaining, 120); Assert.LessOrEqual(waves.NextWaveRemaining, 300);
        }
        [UnityTest] public IEnumerator OpeningProtectsDefendersFromFatalDamageAndReleasesProtection()
        {
            waves.Lord.TakeDamage(99999); Assert.IsTrue(waves.Lord.IsAlive);
            waves.Core.TakeDamage(99999); Assert.IsTrue(waves.Core.IsAlive);
            Assert.GreaterOrEqual(waves.Tutorial.Elapsed, StartingTutorial.BattleEnds);
            yield return TutorialTestActions.Complete(waves); waves.Lord.TakeDamage(99999); Assert.IsFalse(waves.Lord.IsAlive); yield return null;
        }
        [UnityTest] public IEnumerator CollapseCameraPansWithoutZoomAndDialogueIsAtBottom()
        {
            var camera = Find<Camera>();
            foreach (var component in camera.GetComponents<Behaviour>()) if (component.GetType().Name == "PixelPerfectCamera") component.enabled = false;
            camera.orthographicSize = 8;
            yield return null;
            var original = camera.transform.position;
            waves.Tutorial.Advance(StartingTutorial.BattleEnds - waves.Tutorial.Elapsed);
            Time.timeScale = 0; yield return null; yield return null;
            Assert.Less(Vector3.Distance(original, camera.transform.position), .2f);
            waves.Tutorial.Advance(1); yield return null;
            var midpoint = camera.transform.position;
            Assert.Greater(Vector3.Distance(original, midpoint), 1);
            Assert.AreEqual(8, camera.orthographicSize);
            var panel = Find<DarkLord.UI.StartingTutorialPresentation>().transform.Find("Opening tutorial").GetComponent<RectTransform>();
            Assert.AreEqual(0, panel.anchorMin.y); Assert.Greater(panel.anchoredPosition.y, 0);
            waves.Tutorial.Advance(7); yield return null;
            Assert.AreEqual(8, camera.orthographicSize);
            Assert.IsTrue(camera.GetComponent<CameraFollow>().enabled);
        }
        [UnityTest] public IEnumerator SaveDuringCollapseDoesNotSpawnArmyOrReopenOldEntrance()
        {
            waves.Tutorial.Advance(43); var saved = session.Capture(); SaveSlotStore.Validate(saved);
            var entrance = grid.Layout.Entrance; session.Restore(saved);
            Assert.IsTrue(waves.Tutorial.Collapsed); Assert.AreEqual(0, waves.Tutorial.LivingEnemies);
            Assert.AreEqual(entrance, grid.Layout.Entrance); Assert.IsFalse(waves.CanStartWave);
            saved.tutorial.elapsed = float.NaN;
            Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(saved));
            yield return null;
        }
    }
}
