using System;
using System.Collections;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Persistence;
using DarkLord.Player;
using DarkLord.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("MinionRoomFlow")]
    public sealed class MinionRoomFlowTests
    {
        private Scene scene;
        private WaveDirector waves;
        private DungeonGrid grid;
        private MinionSummoning summoning;
        private ResourceBuilding station;
        private Vector2Int source, destination;
        private float previousTime;
        private bool previousAudio;
        private InputSettings originalSettings, inputSettings;
        private HideFlags originalFlags;
        private Mouse mouse;
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        private BuildItemDefinition Definition(string name) => Resources.FindObjectsOfTypeAll<BuildItemDefinition>().First(i => i.name == name);
        [UnitySetUp] public IEnumerator Setup()
        {
            previousTime = Time.timeScale; previousAudio = AudioListener.pause; Time.timeScale = 1; AudioListener.pause = false;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            waves = Find<WaveDirector>(); grid = Find<DungeonGrid>(); summoning = Find<MinionSummoning>();
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Easy });
            var session = Find<GameSaveSession>(); var saved = session.Capture();
            source = grid.Layout.Core + new Vector2Int(-9, 4); destination = grid.Layout.Core + new Vector2Int(9, 7);
            foreach (var center in new[] { source, destination })
            {
                var bounds = DungeonConstruction.Footprint(ConstructionKind.Room, center);
                var list = new System.Collections.Generic.List<Vector2Int>(); foreach (var cell in bounds.allPositionsWithin) list.Add(cell);
                var cells = list.ToArray();
                foreach (var cell in cells) saved.dungeon.cells[cell.y * saved.dungeon.width + cell.x] = (int)DungeonCell.Floor;
                saved.dungeon.sections = saved.dungeon.sections.Concat(new[] { new SectionSaveData { kind = ConstructionKind.Room, bounds = bounds, cells = cells, complete = true, initial = true } }).ToArray();
            }
            session.Restore(saved); Time.timeScale = 1;
            var item = Definition("MinionRecruitment"); grid.Inventory.Grant(item); Assert.IsTrue(grid.TryPlaceBuildingItem(source, item));
            station = grid.BuildingAt(source);
            MoveLord(grid.CellCenter(source + Vector2Int.down));
            Physics2D.SyncTransforms(); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (originalSettings != null) { InputSystem.settings = originalSettings; originalSettings.hideFlags = originalFlags; }
            if (inputSettings != null) UnityEngine.Object.DestroyImmediate(inputSettings);
            Time.timeScale = previousTime; AudioListener.pause = previousAudio;
        }
        private void MoveLord(Vector3 position) { waves.Lord.transform.position = position; waves.Lord.GetComponent<Rigidbody2D>().position = position; }
        private MinionController Summon(MinionRole role = MinionRole.BloodSlime)
        {
            Assert.IsTrue(summoning.TryRecruit()); Assert.IsTrue(summoning.TrySummon(role), summoning.Message);
            return grid.Guards.Last();
        }
        [TestCase(MinionRole.BloodSlime)] [TestCase(MinionRole.BloodArcher)] [TestCase(MinionRole.BloodSupport)]
        public void RecruitmentInstantlyCreatesMinionNextToStationAndChargesOnce(MinionRole role)
        {
            float before = waves.Combat.Mana;
            var guard = Summon(role);
            Assert.AreEqual(role, guard.Role); Assert.AreEqual(1, summoning.GuardCount);
            Assert.That(Vector2.Distance(guard.transform.position, station.transform.position), Is.InRange(1.5f, 4.1f));
            Assert.AreSame(grid.Construction.RoomAt(source), guard.AssignedRoom);
            Assert.AreEqual(before - 20, waves.Combat.Mana);
            Assert.IsFalse(summoning.IsPlacing); Assert.IsFalse(summoning.IsOpen);
            Assert.IsFalse(summoning.TryPlace(destination)); Assert.AreEqual(before - 20, waves.Combat.Mana);
        }
        [Test] public void FullSourceRoomRejectsSummonWithoutSpendingMana()
        {
            Summon(); Summon(); float before = waves.Combat.Mana;
            Assert.IsTrue(summoning.TryRecruit()); Assert.IsFalse(summoning.TrySummon(MinionRole.BloodSlime));
            Assert.AreEqual(before, waves.Combat.Mana); Assert.AreEqual(2, summoning.GuardCount);
        }
        [Test] public void DropAnywhereInDisconnectedRoomFindsEmptySpotAndTeleportsWithoutCost()
        {
            var guard = Summon(); float mana = waves.Combat.Mana;
            guard.Health.TakeDamage(7); guard.RestoreAttackCooldown(.6f);
            int hitPoints = guard.Health.CurrentHealth; float cooldown = guard.AttackCooldownRemaining;
            Assert.IsFalse(grid.Construction.FindPath(source, destination, new System.Collections.Generic.List<Vector2Int>()));
            var item = Definition("GoldMint"); grid.Inventory.Grant(item); Assert.IsTrue(grid.TryPlaceBuildingItem(destination, item));
            Assert.IsTrue(summoning.BeginRelocation(guard, true)); Assert.IsTrue(guard.IsHeld);
            Assert.IsTrue(summoning.TryPlace(destination), summoning.Message);
            Assert.AreSame(grid.Construction.RoomAt(destination), guard.AssignedRoom);
            Assert.AreNotEqual(destination, grid.WorldToCell(guard.transform.position));
            Assert.AreEqual((Vector2)guard.transform.position, guard.GuardPosition);
            Assert.IsFalse(guard.IsHeld); Assert.IsFalse(summoning.IsOpen); Assert.AreEqual(mana, waves.Combat.Mana);
            Assert.AreEqual(hitPoints, guard.Health.CurrentHealth); Assert.That(guard.AttackCooldownRemaining, Is.EqualTo(cooldown).Within(.001f));
        }
        [Test] public void InvalidOrFullRoomDropKeepsMinionAndCancelReleasesIt()
        {
            var guard = Summon(); Vector3 original = guard.transform.position;
            Assert.IsTrue(summoning.BeginRelocation(guard, true));
            Assert.IsFalse(summoning.TryPlace(Vector2Int.zero)); Assert.IsTrue(guard.IsHeld);
            summoning.SetOpen(false); Assert.IsFalse(guard.IsHeld); Assert.AreEqual(original, guard.transform.position);
            for (int i = 0; i < 2; i++)
            {
                var other = UnityEngine.Object.Instantiate(summoning.RecruitDefinition, grid.CellCenter(destination + Vector2Int.right * i), Quaternion.identity);
                SceneManager.MoveGameObjectToScene(other.gameObject, scene); other.Defend(other.transform.position); grid.RegisterGuard(other);
            }
            Assert.IsTrue(summoning.BeginRelocation(guard, true)); Assert.IsFalse(summoning.TryPlace(destination));
            Assert.AreEqual(original, guard.transform.position); summoning.SetOpen(false);
        }
        [UnityTest] public IEnumerator GuardsRoamOnlyTheirRoomAndKeepAssignmentAfterSaveRestore()
        {
            var guard = Summon(); var room = guard.AssignedRoom; Vector3 initial = guard.transform.position; float maxDistance = 0;
            for (int i = 0; i < 18; i++)
            {
                yield return new WaitForSeconds(.1f);
                maxDistance = Mathf.Max(maxDistance, Vector2.Distance(initial, guard.transform.position));
                Assert.IsTrue(room.Bounds.Contains(grid.WorldToCell(guard.transform.position)));
            }
            Assert.Greater(maxDistance, .3f);
            var session = Find<GameSaveSession>(); var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            session.Restore(saved); guard = grid.Guards.Single();
            Assert.AreSame(grid.Construction.RoomAt(source), guard.AssignedRoom);
            Assert.AreEqual(saved.guards[0].post, guard.GuardPosition);
        }
        [UnityTest] public IEnumerator GuardDetectsWholeRoomAttacksEntrantsAndDoesNotChaseOutside()
        {
            var guard = Summon(); var core = grid.Layout.Core;
            guard.Relocate(grid.CellCenter(core + new Vector2Int(3, -3)));
            var enemyNode = new GameObject("Room entrant"); SceneManager.MoveGameObjectToScene(enemyNode, scene);
            var enemy = enemyNode.AddComponent<ActorHealth>(); enemy.Configure(Faction.Invader, 1000);
            enemyNode.AddComponent<CircleCollider2D>().radius = .2f;
            enemy.transform.position = grid.CellCenter(core + new Vector2Int(-3, 3)); Physics2D.SyncTransforms();
            Assert.Greater(Vector2.Distance(guard.transform.position, enemy.transform.position), guard.DetectionRange);
            yield return new WaitForSeconds(.35f); Assert.AreSame(enemy, guard.Target);
            enemy.transform.position = guard.transform.position + Vector3.left * .45f; Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.8f); Assert.Less(enemy.CurrentHealth, 1000);
            enemy.transform.position = grid.CellCenter(core + Vector2Int.left * 4); Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.8f);
            Assert.IsNull(guard.Target); Assert.IsTrue(guard.AssignedRoom.Bounds.Contains(grid.WorldToCell(guard.transform.position)));
        }
        [UnityTest] public IEnumerator HeldMinionStopsAndInspectionHasNoBehaviourOrRelocateButtons()
        {
            var guard = Summon(); yield return null; yield return null;
            var panel = Find<MinionInspectionPanel>(); panel.Show(guard); Assert.IsTrue(panel.IsOpen);
            Assert.IsFalse(panel.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b => b.name == "Aggressive" || b.name == "Defensive" || b.name == "Relocate"));
            Assert.IsTrue(summoning.BeginRelocation(guard, true)); Vector3 position = guard.transform.position;
            yield return new WaitForSeconds(.6f); Assert.That(Vector2.Distance(position, guard.transform.position), Is.LessThan(.01f));
            Assert.IsNull(guard.Target); summoning.SetOpen(false); Assert.IsFalse(guard.IsHeld);
        }
        private MinionController AddDestinationGuard(int offset)
        {
            var guard = UnityEngine.Object.Instantiate(summoning.RecruitDefinition, grid.CellCenter(destination + Vector2Int.right * offset), Quaternion.identity);
            SceneManager.MoveGameObjectToScene(guard.gameObject, scene); guard.Defend(guard.transform.position); grid.RegisterGuard(guard); return guard;
        }
        private void Overview()
        {
            var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
            camera.GetComponents<Behaviour>().First(b => b.GetType().Name == "PixelPerfectCamera").enabled = false;
            camera.GetComponent<CameraFollow>().enabled = false;
            camera.transform.position = Vector3.Lerp(grid.CellCenter(source), grid.CellCenter(destination), .5f) + Vector3.back * 10;
            camera.orthographicSize = 23; camera.ResetWorldToCameraMatrix(); camera.ResetProjectionMatrix();
        }
        [UnityTest, Category("RelocationColors")] public IEnumerator HoldingShowsAllRoomColorsAndCountsAndRefreshesAfterDeath()
        {
            var held = Summon(); Summon(); var first = AddDestinationGuard(0); AddDestinationGuard(1);
            Overview(); Assert.IsTrue(summoning.BeginRelocation(held, true));
            yield return null; yield return null;
            var overlay = Find<RoomCapacityOverlay>(); var from = grid.Construction.RoomAt(source); var to = grid.Construction.RoomAt(destination); var core = grid.Construction.RoomAt(grid.Layout.Core);
            Assert.IsTrue(overlay.IsVisible);
            Assert.AreEqual("1 / 2", overlay.LabelFor(from).CountText, "Held guard is excluded from its old room during the drag.");
            Assert.IsTrue(overlay.LabelFor(from).PlacementAllowed); Assert.Greater(overlay.TintFor(from).color.g, overlay.TintFor(from).color.r);
            Assert.AreEqual("2 / 2", overlay.LabelFor(to).CountText); Assert.IsFalse(overlay.LabelFor(to).PlacementAllowed);
            Assert.Greater(overlay.TintFor(to).color.r, overlay.TintFor(to).color.g);
            Assert.AreEqual("0 / 4", overlay.LabelFor(core).CountText); Assert.IsTrue(overlay.LabelFor(core).PlacementAllowed);
            foreach (var room in new[] { from, to, core })
            { Assert.IsTrue(overlay.TintFor(room).gameObject.activeSelf); Assert.IsFalse(overlay.TintFor(room).raycastTarget); Assert.Less(overlay.TintFor(room).color.a, .3f); }
            first.Health.Kill(); yield return new WaitForSecondsRealtime(.15f);
            Assert.AreEqual("1 / 2", overlay.LabelFor(to).CountText); Assert.IsTrue(overlay.LabelFor(to).PlacementAllowed);
            Assert.IsTrue(summoning.CanPlace(destination, out _));
            Assert.IsTrue(summoning.TryPlace(destination)); yield return null;
            Assert.IsFalse(overlay.IsVisible);
            foreach (var room in new[] { from, to, core })
            { Assert.IsFalse(overlay.TintFor(room).gameObject.activeSelf); Assert.IsFalse(overlay.LabelFor(room).gameObject.activeSelf); }
        }
        [UnityTest, Category("RelocationColors")] public IEnumerator CapacityBonusAndDestructionUpdateColorsAndCancelHidesEverything()
        {
            var held = Summon(); AddDestinationGuard(0); AddDestinationGuard(1);
            var item = Definition("MinionQuarters"); var facilityCell = destination + new Vector2Int(-2, -2);
            grid.Inventory.Grant(item); Assert.IsTrue(grid.TryPlaceBuildingItem(facilityCell, item));
            Overview(); Assert.IsTrue(summoning.BeginRelocation(held, true)); yield return null; yield return null;
            var overlay = Find<RoomCapacityOverlay>(); var room = grid.Construction.RoomAt(destination);
            Assert.AreEqual("2 / 4", overlay.LabelFor(room).CountText); Assert.IsTrue(overlay.LabelFor(room).PlacementAllowed);
            grid.BuildingAt(facilityCell).Health.Kill(); yield return new WaitForSecondsRealtime(.15f);
            Assert.AreEqual("2 / 2", overlay.LabelFor(room).CountText); Assert.IsFalse(overlay.LabelFor(room).PlacementAllowed);
            Assert.IsFalse(summoning.CanPlace(destination, out _));
            summoning.SetOpen(false); yield return null;
            Assert.IsFalse(overlay.TintFor(room).gameObject.activeSelf); Assert.IsFalse(overlay.LabelFor(room).gameObject.activeSelf);
        }
        [UnityTest] public IEnumerator WholeRewardCardLiftsWhilePausedWithoutAcquireButton()
        {
            waves.RestoreSave(new WaveSaveData { number=1, state=WaveState.Active, enemies=Array.Empty<MinionSaveData>() });
            typeof(WaveDirector).GetMethod("CheckWaveCleared", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(waves, null);
            yield return null;
            var view = Find<WaveRewardPanel>(); var card = view.transform.Find("Wave reward selection/Item acquisition/Reward 1");
            var visual = card.Find("Card artwork").GetComponent<RectTransform>(); var hit = card.GetComponent<RectTransform>();
            Vector2 origin = visual.anchoredPosition, hitOrigin = hit.anchoredPosition;
            Assert.IsNull(visual.Find("Selection plate")); Assert.IsFalse(card.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("획득하기")));
            ExecuteEvents.Execute(card.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(.2f); Assert.Greater(visual.anchoredPosition.y, origin.y + 5); Assert.AreEqual(hitOrigin, hit.anchoredPosition);
            ExecuteEvents.Execute(card.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
            yield return new WaitForSecondsRealtime(.2f); Assert.That(Vector2.Distance(origin, visual.anchoredPosition), Is.LessThan(.01f));
            Assert.AreEqual(0, Time.timeScale); card.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.AreNotEqual(WaveState.RewardSelection, waves.State);
        }
        [UnityTest] public IEnumerator ActualLongPressAndRoomDropUsesMouseReleaseAndBlocksLordAttack()
        {
            var guard = Summon();
            originalSettings = InputSystem.settings; originalFlags = originalSettings.hideFlags; originalSettings.hideFlags = HideFlags.DontUnloadUnusedAsset;
            inputSettings = UnityEngine.Object.Instantiate(originalSettings); inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; InputSystem.settings = inputSettings;
            mouse = InputSystem.AddDevice<Mouse>();
            var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
            camera.GetComponents<Behaviour>().First(b => b.GetType().Name == "PixelPerfectCamera").enabled = false; camera.GetComponent<CameraFollow>().enabled = false;
            camera.transform.position = Vector3.Lerp(grid.CellCenter(source), grid.CellCenter(destination), .5f) + Vector3.back * 10; camera.orthographicSize = 20;
            camera.ResetWorldToCameraMatrix(); camera.ResetProjectionMatrix();
            var sprite = guard.GetComponentInChildren<SpriteRenderer>();
            Vector2 pointer = camera.WorldToScreenPoint(sprite.bounds.center);
            InputSystem.QueueStateEvent(mouse, new MouseState { position=pointer }); yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position=pointer, buttons=1 }); yield return null;
            Assert.IsTrue(summoning.BlocksGameplayInput); Assert.IsFalse(waves.Combat.IsAttacking);
            yield return new WaitForSecondsRealtime(.36f); Assert.IsTrue(summoning.IsRelocating); Assert.IsTrue(guard.IsHeld);
            Vector2 drop = camera.WorldToScreenPoint(grid.CellCenter(destination));
            InputSystem.QueueStateEvent(mouse, new MouseState { position=drop, buttons=1 }); yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position=drop }); yield return null; yield return null;
            Assert.IsFalse(summoning.IsRelocating); Assert.AreSame(grid.Construction.RoomAt(destination), guard.AssignedRoom);
        }

        [UnityTest] public IEnumerator InvasionCancelsHeldMinionAndRestoresPlacementAfterCombat()
        {
            var guard = Summon();
            var originalPosition = guard.GuardPosition;
            Assert.IsTrue(summoning.BeginRelocation(guard, true));
            Assert.IsTrue(guard.IsHeld);
            waves.RestoreSave(new WaveSaveData { number = 1, state = WaveState.Active, pending = 1, enemies = Array.Empty<MinionSaveData>() });
            Assert.IsTrue(waves.InCombat);
            Assert.IsFalse(summoning.TryPlace(destination));
            yield return null;
            Assert.IsFalse(summoning.IsRelocating);
            Assert.IsFalse(guard.IsHeld);
            Assert.AreEqual(originalPosition, guard.GuardPosition);
            Assert.IsFalse(summoning.BeginRelocation(guard));
            waves.RestoreSave(new WaveSaveData { number = 1, state = WaveState.Preparing, enemies = Array.Empty<MinionSaveData>() });
            yield return null;
            Assert.IsFalse(waves.InCombat);
            Assert.IsTrue(summoning.BeginRelocation(guard));
            Assert.IsTrue(summoning.TryPlace(destination));
            Assert.AreSame(grid.Construction.RoomAt(destination), guard.AssignedRoom);
        }
    }
}
