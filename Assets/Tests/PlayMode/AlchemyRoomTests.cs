#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using DarkLord.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class AlchemyRoomTests
    {
        private Scene scene;
        private float previousTime;
        private bool previousAudio;
        private DungeonGrid grid;
        private CurrencyWallet wallet;
        private WaveDirector waves;
        private AlchemyRoomPanel panel;
        private Vector2Int center;
        [UnitySetUp] public IEnumerator Setup()
        {
            previousTime = Time.timeScale; previousAudio = AudioListener.pause;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            Time.timeScale = 1;
            grid = Find<DungeonGrid>(); wallet = Find<CurrencyWallet>(); waves = Find<WaveDirector>(); panel = Find<AlchemyRoomPanel>();
            var start = new Vector2Int(7, 15);
            foreach (var end in new[] { new Vector2Int(4, 15), new Vector2Int(4, 18), new Vector2Int(11, 18) })
            {
                Assert.AreEqual(ConstructionResult.Success, grid.Construction.TryPlanCorridor(start, end, out var branch));
                Finish(branch); start = end;
            }
            center = new Vector2Int(4, 22);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousTime; AudioListener.pause = previousAudio;
        }
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        private void MoveTo(Vector2Int cell)
        {
            var position = grid.CellCenter(cell); waves.Lord.transform.position = position;
            waves.Lord.GetComponent<Rigidbody2D>().position = position; Physics2D.SyncTransforms();
        }
        private DungeonSection PlanRoom()
        {
            var item = AssetDatabase.LoadAssetAtPath<BuildItemDefinition>("Assets/Data/Construction/AlchemyRoom.asset");
            var expansion = Find<DungeonExpansion>(); expansion.SetExpansionMode(true);
            var construction = Find<ConstructionPanel>(); construction.ShowCategory(BuildCategory.Rooms);
            Assert.IsTrue(construction.GetComponentsInChildren<BuildItemButton>().Any(c => c.Item == item));
            expansion.Select(item); Assert.AreEqual(ConstructionResult.Success, expansion.TryBuild(center));
            expansion.SetExpansionMode(false); return grid.Construction.At(center);
        }
        private void Finish(DungeonSection room)
        {
            for (int pass = 0; pass < room.Cells.Count; pass++) foreach (var cell in room.Cells) grid.Layout.TryExpand(cell);
            Assert.IsTrue(room.IsComplete);
        }
        private AlchemyStation PrepareStation()
        {
            Finish(PlanRoom()); MoveTo(center + Vector2Int.down);
            Assert.IsTrue(waves.Alchemy.TryInteract());
            return grid.AlchemyStationAt(center);
        }
        [UnityTest] public IEnumerator StationIsFixedAndRequiresInteractionThenConvertsAfterTimeEvenWhenAway()
        {
            var room = PlanRoom(); MoveTo(center + Vector2Int.down);
            Assert.IsNull(grid.AlchemyStationAt(center)); Assert.IsFalse(waves.Alchemy.TryInteract());
            Finish(room); yield return null;
            var station = grid.AlchemyStationAt(center); Assert.IsNotNull(station);
            Assert.AreEqual(grid.CellCenter(center), station.transform.position);
            Assert.IsFalse(grid.CanPlaceTrap(center)); Assert.IsFalse(grid.CanPlaceBuilding(center));
            Assert.IsFalse(panel.IsVisible, "Entering the room must not open the window automatically.");
            Assert.IsFalse(waves.Alchemy.TryExchange(CurrencyType.Souls));
            Assert.IsTrue(waves.Alchemy.TryInteract()); yield return null; Assert.IsTrue(panel.IsVisible);
            panel.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "Blood exchange").onClick.Invoke();
            Assert.AreEqual(250, wallet.Gold); Assert.AreEqual(30, wallet.Souls); Assert.IsTrue(station.HasJob);
            Assert.That(station.Remaining, Is.EqualTo(30).Within(.05f));
            Assert.IsFalse(waves.Alchemy.TryExchange(CurrencyType.DarkCrystals)); Assert.AreEqual(5, wallet.DarkCrystals);
            Canvas.ForceUpdateCanvases();
            foreach (var label in panel.transform.Find("Alchemy room exchange").GetComponentsInChildren<TMP_Text>())
            { label.ForceMeshUpdate(); Assert.IsFalse(label.isTextOverflowing, label.name); }
            panel.Close(); MoveTo(grid.Layout.Core); yield return null; Assert.IsFalse(panel.IsVisible);
            station.AdvanceWork(29); Assert.AreEqual(250, wallet.Gold);
            station.AdvanceWork(1); Assert.AreEqual(250, wallet.Gold); Assert.IsTrue(station.IsReady);
            Assert.IsFalse(waves.Alchemy.TryCollect(), "Collection requires interacting nearby.");
            MoveTo(center + Vector2Int.down); Assert.IsTrue(waves.Alchemy.TryInteract()); yield return null;
            Assert.IsTrue(panel.CollectInteractable);
            panel.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "Collect alchemy").onClick.Invoke();
            Assert.AreEqual(300, wallet.Gold); Assert.IsFalse(station.HasJob);
            panel.ExchangeCrystal(); Assert.AreEqual(4, wallet.DarkCrystals); Assert.AreEqual(300, wallet.Gold);
            station.AdvanceWork(59); Assert.AreEqual(300, wallet.Gold);
            station.AdvanceWork(1); Assert.AreEqual(300, wallet.Gold); Assert.IsTrue(waves.Alchemy.TryCollect()); Assert.AreEqual(400, wallet.Gold);
            Assert.IsFalse(waves.Alchemy.TryCollect());
            station.AdvanceWork(60); Assert.AreEqual(400, wallet.Gold, "Completion pays only once.");
        }
        [UnityTest] public IEnumerator CapacityAndMaterialsGateStartAndFinishedOutputWaitsWithoutLoss()
        {
            var station = PrepareStation(); wallet.RestoreAmounts(1951, 10, 1); yield return null;
            Assert.IsFalse(panel.BloodInteractable); Assert.IsFalse(waves.Alchemy.TryExchange(CurrencyType.Souls));
            Assert.AreEqual(10, wallet.Souls); Assert.IsFalse(station.HasJob);
            wallet.RestoreAmounts(1950, 10, 1); Assert.IsTrue(waves.Alchemy.TryExchange(CurrencyType.Souls));
            Assert.AreEqual(0, wallet.Souls); Assert.AreEqual(1950, wallet.Gold);
            wallet.Add(CurrencyType.Gold, 50); station.AdvanceWork(30);
            Assert.IsTrue(station.IsReady); Assert.AreEqual(2000, wallet.Gold);
            Assert.IsFalse(waves.Alchemy.TryExchange(CurrencyType.DarkCrystals)); Assert.AreEqual(1, wallet.DarkCrystals);
            var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(Find<GameSaveSession>().Capture()));
            Find<GameSaveSession>().Restore(saved); Time.timeScale = 1;
            station = grid.AlchemyStationAt(center); Assert.IsTrue(station.IsReady);
            wallet.TrySpend(CurrencyType.Gold, 50); station.AdvanceWork(0);
            Assert.AreEqual(1950, wallet.Gold); Assert.IsTrue(station.IsReady, "Making storage space must not auto-collect.");
            MoveTo(center + Vector2Int.down); Assert.IsTrue(waves.Alchemy.TryInteract()); Assert.IsTrue(waves.Alchemy.TryCollect());
            Assert.AreEqual(2000, wallet.Gold); Assert.IsFalse(station.HasJob);
            wallet.RestoreAmounts(0, 9, 0); MoveTo(center + Vector2Int.down); Assert.IsTrue(waves.Alchemy.TryInteract());
            Assert.IsFalse(waves.Alchemy.TryExchange(CurrencyType.Souls)); Assert.IsFalse(waves.Alchemy.TryExchange(CurrencyType.DarkCrystals));
            Assert.AreEqual(9, wallet.Souls); Assert.AreEqual(0, wallet.Gold);
        }
        [UnityTest] public IEnumerator PauseAndSaveRestorePreserveRemainingTimeAndDoNotDoubleChargeOrPay()
        {
            var station = PrepareStation(); Assert.IsTrue(waves.Alchemy.TryExchange(CurrencyType.DarkCrystals));
            Assert.AreEqual(4, wallet.DarkCrystals);
            float remaining = station.Remaining;
            Time.timeScale = 0; station.AdvanceWork(60); yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(remaining, station.Remaining); Assert.AreEqual(250, wallet.Gold);
            Time.timeScale = 1; station.AdvanceWork(20);
            var session = Find<GameSaveSession>(); var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            Assert.That(saved.dungeon.alchemyJobs.Single().remaining, Is.EqualTo(40).Within(.05f));
            SaveSlotStore.Validate(saved); session.Restore(saved); Time.timeScale = 1;
            station = grid.AlchemyStationAt(center); Assert.IsFalse(waves.Alchemy.IsOpen);
            Assert.IsTrue(station.HasJob); Assert.AreEqual(4, wallet.DarkCrystals);
            station.AdvanceWork(39); Assert.AreEqual(250, wallet.Gold);
            station.AdvanceWork(1); Assert.AreEqual(250, wallet.Gold); Assert.IsTrue(station.IsReady);
            Assert.IsTrue(waves.Alchemy.TryInteract()); Assert.IsTrue(waves.Alchemy.TryCollect()); Assert.AreEqual(350, wallet.Gold);
            saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            Assert.IsEmpty(saved.dungeon.alchemyJobs); session.Restore(saved); Time.timeScale = 1;
            grid.AlchemyStationAt(center).AdvanceWork(60); Assert.AreEqual(350, wallet.Gold);
            saved.dungeon.alchemyJobs = null; session.Restore(saved);
            Assert.IsNotNull(grid.AlchemyStationAt(center)); Assert.IsFalse(grid.AlchemyStationAt(center).HasJob);
        }
        [UnityTest] public IEnumerator StationsWorkIndependentlyAndCannotStartDuringInvasionOrAfterDeath()
        {
            var firstCenter = center; var first = PrepareStation(); Assert.IsTrue(waves.Alchemy.TryExchange(CurrencyType.Souls));
            panel.Close(); center += Vector2Int.right * 6;
            var second = PrepareStation(); Assert.IsTrue(waves.Alchemy.TryExchange(CurrencyType.DarkCrystals));
            Assert.AreNotSame(first, second); first.AdvanceWork(30);
            Assert.IsTrue(first.IsReady); Assert.IsTrue(second.HasJob); Assert.AreEqual(250, wallet.Gold);
            second.AdvanceWork(60); Assert.AreEqual(250, wallet.Gold); Assert.IsTrue(second.IsReady);
            waves.BeginNextWave(); yield return null;
            Assert.IsFalse(waves.Alchemy.TryExchange(CurrencyType.Souls));
            Assert.IsTrue(waves.Alchemy.TryCollect(), "Completed output can be collected during an invasion."); Assert.AreEqual(350, wallet.Gold);
            MoveTo(firstCenter + Vector2Int.down); Assert.IsTrue(waves.Alchemy.TryInteract()); Assert.IsTrue(waves.Alchemy.TryCollect());
            Assert.AreEqual(400, wallet.Gold);
            waves.Lord.Kill(); Assert.IsFalse(waves.Alchemy.TryInteract());
            Assert.AreEqual(30, wallet.Souls); Assert.AreEqual(4, wallet.DarkCrystals);
            Assert.IsNotNull(grid.AlchemyStationAt(firstCenter));
        }
        [UnityTest] public IEnumerator InteractAndEscapeKeysOpenAndCloseWithoutPausing()
        {
            Finish(PlanRoom()); MoveTo(center + Vector2Int.down);
            var original = UnityEngine.InputSystem.InputSystem.settings; var flags = original.hideFlags; original.hideFlags = HideFlags.None;
            var settings = Object.Instantiate(original); settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            UnityEngine.InputSystem.InputSystem.settings = settings;
            var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            try
            {
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F));
                yield return null; yield return null; Assert.IsTrue(panel.IsVisible);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));
                yield return null; yield return null;
                Assert.IsFalse(panel.IsVisible); Assert.AreEqual(GameMenu.ScreenState.Playing, Find<GameMenu>().State); Assert.AreEqual(1, Time.timeScale);
            }
            finally
            {
                UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard); UnityEngine.InputSystem.InputSystem.settings = original;
                original.hideFlags = flags; Object.DestroyImmediate(settings);
            }
        }
        [UnityTest] public IEnumerator JobStateIsCommittedBeforeMaterialEventsAndBadSaveJobsAreRejected()
        {
            var station = PrepareStation(); bool observed = false;
            void Observe(CurrencyType currency, int amount)
            {
                if (currency != CurrencyType.Souls) return;
                Assert.IsTrue(station.HasJob); Assert.AreEqual(30, wallet.Souls); Assert.AreEqual(250, wallet.Gold); observed = true;
            }
            wallet.Changed += Observe;
            Assert.IsTrue(waves.Alchemy.TryExchange(CurrencyType.Souls)); wallet.Changed -= Observe; Assert.IsTrue(observed);
            var saved = Find<GameSaveSession>().Capture();
            saved.dungeon.alchemyJobs[0].remaining = float.NaN;
            Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(saved));
            saved.dungeon.alchemyJobs[0].remaining = 30; saved.dungeon.alchemyJobs[0].source = CurrencyType.Gold;
            Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(saved));
            yield return null;
        }
        [UnityTest] public IEnumerator ReadyMarkerAndUncollectedOutputSurviveReloadUntilManualCollection()
        {
            var station = PrepareStation(); Assert.IsTrue(waves.Alchemy.TryExchange(CurrencyType.Souls));
            station.AdvanceWork(30); panel.Close(); Assert.AreEqual(250, wallet.Gold);
            var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
            var follow = camera.GetComponent<CameraFollow>(); if (follow != null) follow.enabled = false;
            camera.transform.position = station.transform.position + Vector3.back * 10;
            yield return null; yield return null;
            var overlay = Find<AlchemyReadyOverlay>(); Assert.IsTrue(overlay.IsShowing(station),
                $"ready={station.IsReady}, stationActive={station.isActiveAndEnabled}, active={overlay.isActiveAndEnabled}, marker={overlay.IndicatorFor(station)}, overlayCamera={typeof(AlchemyReadyOverlay).GetField("worldCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(overlay)}, canvas={overlay.GetComponentInParent<Canvas>()}, camera={camera.transform.position}, size={camera.orthographicSize}, screen={camera.WorldToScreenPoint(station.transform.position + Vector3.up * 3.1f)}, viewport={camera.pixelRect}");
            var marker = overlay.IndicatorFor(station); var label = marker.GetComponentInChildren<TMP_Text>();
            Assert.AreEqual("수령 가능", label.text); label.ForceMeshUpdate(); Assert.IsFalse(label.isTextOverflowing);
            Assert.IsFalse(label.raycastTarget); Assert.IsFalse(marker.GetComponent<UnityEngine.UI.Image>().raycastTarget);
            var canvas = overlay.GetComponentInParent<Canvas>(); var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Assert.Greater(RectTransformUtility.WorldToScreenPoint(uiCamera, marker.position).y, camera.WorldToScreenPoint(station.transform.position).y);
            var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(Find<GameSaveSession>().Capture()));
            Find<GameSaveSession>().Restore(saved); Time.timeScale = 1; station = grid.AlchemyStationAt(center);
            yield return null;
            Assert.AreEqual(250, wallet.Gold); Assert.IsTrue(station.IsReady); Assert.IsTrue(overlay.IsShowing(station));
            wallet.RestoreAmounts(2000, 30, 5); Assert.IsTrue(waves.Alchemy.TryInteract()); yield return null;
            Assert.IsFalse(panel.CollectInteractable); Assert.IsFalse(waves.Alchemy.TryCollect()); Assert.IsTrue(overlay.IsShowing(station));
            wallet.TrySpend(CurrencyType.Gold, 50); yield return null;
            Assert.AreEqual(1950, wallet.Gold); Assert.IsTrue(panel.CollectInteractable); Assert.IsTrue(station.IsReady);
            bool observed = false;
            void Observe(CurrencyType currency, int amount) { if (currency == CurrencyType.Gold) { Assert.IsFalse(station.HasJob); Assert.AreEqual(2000, amount); observed = true; } }
            wallet.Changed += Observe;
            panel.Collect(); wallet.Changed -= Observe; yield return null;
            Assert.IsTrue(observed); Assert.AreEqual(2000, wallet.Gold); Assert.IsFalse(station.HasJob); Assert.IsFalse(overlay.IsShowing(station));
            panel.Collect(); Assert.AreEqual(2000, wallet.Gold, "Repeated collection cannot duplicate rewards.");
            saved = Find<GameSaveSession>().Capture(); Assert.IsEmpty(saved.dungeon.alchemyJobs);
            Find<GameSaveSession>().Restore(saved); Time.timeScale = 1; yield return null;
            Assert.IsFalse(grid.AlchemyStationAt(center).HasJob); Assert.AreEqual(2000, wallet.Gold);
        }
    }
}
#endif
