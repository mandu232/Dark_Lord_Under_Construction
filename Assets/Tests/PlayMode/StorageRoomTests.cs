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
    [Category("ResourceStorage")]
    [Category("StarterDungeon")]
    [Category("MinionRoster")]
    public sealed class StorageRoomTests
    {
        private Scene scene;
        private float previousTime;
        private bool previousAudio;

        [UnitySetUp] public IEnumerator Setup()
        {
            previousTime = Time.timeScale; previousAudio = AudioListener.pause;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            yield return null; yield return null;
            Time.timeScale = 1;
            Find<WaveDirector>().ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Easy });
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousTime; AudioListener.pause = previousAudio;
        }
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        private static void Finish(DungeonGrid dungeon, DungeonSection room)
        {
            for (int pass = 0; pass < room.Cells.Count; pass++)
                foreach (var cell in room.Cells) dungeon.Layout.TryExpand(cell);
            Assert.IsTrue(room.IsComplete);
        }

        [UnityTest] public IEnumerator RecruitmentUsesFAndEscapeWithoutSpendingOrPausing()
        {
            var dungeon = Find<DungeonGrid>(); var summoning = Find<MinionSummoning>();
            var room = dungeon.Construction.Sections.Single(s => s.IsCore);
            // Stand nearer the facility than the core, which also owns F interaction.
            summoning.transform.position = dungeon.CellCenter(room.Center + new Vector2Int(2, 1));
            summoning.GetComponent<Rigidbody2D>().position = summoning.transform.position;
            Physics2D.SyncTransforms();
            var original = UnityEngine.InputSystem.InputSystem.settings; var flags = original.hideFlags; original.hideFlags = HideFlags.DontUnloadUnusedAsset;
            var settings = Object.Instantiate(original); settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            UnityEngine.InputSystem.InputSystem.settings = settings;
            var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            try
            {
                float mana = summoning.GetComponent<PlayerCombat>().Mana;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.G));
                yield return null; yield return null; Assert.IsFalse(summoning.IsOpen);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F));
                yield return null; yield return null;
                var panel = Find<RecruitmentPanel>(); Assert.IsTrue(panel.IsVisible); Assert.IsFalse(summoning.IsPlacing);
                Assert.IsFalse(Find<AlchemyRoomPanel>().IsVisible);
                Assert.AreEqual(mana, summoning.GetComponent<PlayerCombat>().Mana);
                foreach (var label in panel.transform.Find("Recruitment selection").GetComponentsInChildren<TMP_Text>())
                { label.ForceMeshUpdate(); Assert.IsFalse(label.isTextOverflowing, label.name); }
                Assert.IsFalse(panel.RandomSummonButton.interactable, "No corpse stacks means random recruitment is unavailable.");
                Assert.IsFalse(summoning.IsPlacing);
                Assert.AreEqual(mana, summoning.GetComponent<PlayerCombat>().Mana);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));
                yield return null; yield return null;
                Assert.IsFalse(summoning.IsOpen); Assert.IsFalse(panel.IsVisible);
                Assert.AreEqual(GameMenu.ScreenState.Playing, Find<GameMenu>().State); Assert.AreEqual(1, Time.timeScale);
                Assert.AreEqual(mana, summoning.GetComponent<PlayerCombat>().Mana);
            }
            finally
            {
                UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard); UnityEngine.InputSystem.InputSystem.settings = original;
                original.hideFlags = flags; Object.DestroyImmediate(settings);
            }
        }

        [UnityTest] public IEnumerator StartingRoomHasFreeRecruitmentFacilityAndRestoreNeverDuplicatesIt()
        {
            var dungeon = Find<DungeonGrid>(); var wallet = Find<CurrencyWallet>();
            var session = Find<GameSaveSession>(); var summoning = Find<MinionSummoning>();
            var room = dungeon.Construction.Sections.Single(s => s.IsCore);
            var stationCell = room.Center + new Vector2Int(1, 1);
            var building = dungeon.BuildingAt(stationCell);
            Assert.IsNotNull(building);
            Assert.IsTrue(building.IsRecruitmentFacility);
            Assert.IsFalse(building.IsRuined);
            Assert.AreEqual(1, dungeon.GetComponentsInChildren<ResourceBuilding>().Count());
            Assert.AreEqual(250, wallet.Gold, "The starting facility costs no gold.");
            Assert.IsFalse(dungeon.CanPlaceBuilding(stationCell));
            // Stand nearer the facility than the core, which also owns F interaction.
            summoning.transform.position = dungeon.CellCenter(room.Center + new Vector2Int(2, 1));
            Physics2D.SyncTransforms();
            Assert.IsTrue(summoning.TryRecruit(), summoning.Message);
            Assert.IsFalse(summoning.IsPlacing, "Interaction only opens the selection window.");
            yield return null;
            var panel = Find<RecruitmentPanel>();
            Assert.IsTrue(panel.IsVisible);
            Assert.IsFalse(panel.RandomSummonButton.interactable);
            Assert.AreEqual(4, dungeon.Guards.Count(), "Fresh runs grant four guards.");
            summoning.RemoveGuard(dungeon.Guards.Last());
            Assert.IsTrue(building.TryAddSummonStack());
            Assert.IsTrue(building.TryAddSummonStack());
            float mana = summoning.GetComponent<PlayerCombat>().Mana;
            yield return null;
            Assert.IsTrue(panel.RandomSummonButton.interactable);
            panel.RandomSummonButton.onClick.Invoke();
            Assert.IsFalse(summoning.IsOpen, "Summoning creates the guard immediately and closes the window.");
            Assert.IsFalse(summoning.IsPlacing);
            Assert.AreEqual(4, dungeon.Guards.Count());
            Assert.AreEqual(1, building.SummonStacks, "Exactly one corpse stack is charged.");
            Assert.AreEqual(mana, summoning.GetComponent<PlayerCombat>().Mana);
            building.Health.TakeDamage(30);
            int health = building.Health.CurrentHealth;
            var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            Assert.AreEqual(1, saved.dungeon.buildings.Length);
            session.Restore(saved); Time.timeScale = 1;
            yield return null;
            building = dungeon.BuildingAt(stationCell);
            Assert.IsTrue(building.IsRecruitmentFacility);
            Assert.AreEqual(health, building.Health.CurrentHealth);
            Assert.AreEqual(1, building.SummonStacks);
            Assert.AreEqual(4, dungeon.Guards.Count());
            Assert.AreEqual(1, dungeon.GetComponentsInChildren<ResourceBuilding>().Count());
            session.Restore(saved); Time.timeScale = 1;
            yield return null;
            Assert.AreEqual(1, dungeon.GetComponentsInChildren<ResourceBuilding>().Count());
            saved.dungeon.buildings = new BuildingSaveData[0];
            session.Restore(saved); Time.timeScale = 1;
            yield return null;
            Assert.IsNull(dungeon.BuildingAt(stationCell), "Missing facilities are never respawned on restore.");
            Assert.AreEqual(0, dungeon.GetComponentsInChildren<ResourceBuilding>().Count());
        }

        [UnityTest] public IEnumerator ConstructionStacksOnlyCompletedWarehousesAndRestoresCapsAndHud()
        {
            var wallet = Find<CurrencyWallet>(); var dungeon = Find<DungeonGrid>();
            var expansion = Find<DungeonExpansion>(); var session = Find<GameSaveSession>();
            var view = Find<ConstructionPanel>(); var summoning = Find<MinionSummoning>();
            Assert.AreEqual(0, dungeon.Construction.Sections.Count(s => s.Kind == ConstructionKind.StorageRoom));
            Assert.AreEqual(0, dungeon.Construction.Sections.Count(s => s.Kind == ConstructionKind.Room));
            Assert.AreEqual(1, dungeon.Construction.Sections.Count(s => !DungeonConstruction.IsRoom(s.Kind)));
            Assert.AreEqual(1000, wallet.GetCapacity(CurrencyType.Gold));
            Assert.AreEqual(200, wallet.GetCapacity(CurrencyType.Souls));
            Assert.AreEqual(50, wallet.GetCapacity(CurrencyType.DarkCrystals));
            var item = AssetDatabase.LoadAssetAtPath<BuildItemDefinition>("Assets/Data/Construction/StorageRoom.asset");
            Assert.IsNotNull(item);
            Assert.AreEqual(new Vector2Int(5, 5), DungeonConstruction.Footprint(item.constructionKind, Vector2Int.zero).size);
            dungeon.Inventory.Grant(item); dungeon.Inventory.Grant(item);
            expansion.SetExpansionMode(true); view.ShowCategory(BuildCategory.Rooms);
            Assert.IsTrue(view.GetComponentsInChildren<BuildItemButton>().Any(c => c.Item == item));
            expansion.Select(item);
            foreach (var end in new[] { new Vector2Int(25, 15), new Vector2Int(25, 18), new Vector2Int(35, 18) })
            {
                var start = end == new Vector2Int(25, 15) ? dungeon.Layout.Core + Vector2Int.right * 3 :
                    end == new Vector2Int(25, 18) ? new Vector2Int(25, 15) : new Vector2Int(25, 18);
                Assert.AreEqual(ConstructionResult.Success, dungeon.Construction.TryPlanCorridor(start, end, out var branch));
                Finish(dungeon, branch);
            }
            var firstCell = new Vector2Int(28, 22);
            Assert.AreEqual(ConstructionResult.Success, expansion.TryBuild(firstCell));
            var first = dungeon.Construction.At(firstCell);
            Assert.AreEqual(1000, wallet.GetCapacity(CurrencyType.Gold), "Queued rooms provide no capacity.");
            Assert.AreEqual(250, wallet.Gold, "Building storage never grants resources.");
            Finish(dungeon, first);
            Assert.AreEqual(2000, wallet.GetCapacity(CurrencyType.Gold));
            Assert.AreEqual(400, wallet.GetCapacity(CurrencyType.Souls));
            Assert.AreEqual(100, wallet.GetCapacity(CurrencyType.DarkCrystals));
            Assert.AreSame(first, dungeon.Construction.RoomAt(firstCell));
            Assert.AreEqual(2, summoning.RoomCapacity(first), "Storage remains a small room for placement.");
            var secondCell = firstCell + Vector2Int.right * 6;
            Assert.AreEqual(ConstructionResult.Success, expansion.TryBuild(secondCell));
            var pendingSave = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            SaveSlotStore.Validate(pendingSave); session.Restore(pendingSave); Time.timeScale = 1;
            Assert.AreEqual(2000, wallet.GetCapacity(CurrencyType.Gold));
            Assert.IsFalse(dungeon.Construction.At(secondCell).IsComplete);
            Finish(dungeon, dungeon.Construction.At(secondCell));
            Assert.AreEqual(3000, wallet.GetCapacity(CurrencyType.Gold));
            Assert.AreEqual(600, wallet.GetCapacity(CurrencyType.Souls));
            Assert.AreEqual(150, wallet.GetCapacity(CurrencyType.DarkCrystals));
            wallet.Add(CurrencyType.Gold, int.MaxValue);
            wallet.Add(CurrencyType.Souls, int.MaxValue);
            wallet.Add(CurrencyType.DarkCrystals, int.MaxValue);
            var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            session.Restore(saved); Time.timeScale = 1;
            Assert.AreEqual(3000, wallet.Gold); Assert.AreEqual(600, wallet.Souls); Assert.AreEqual(150, wallet.DarkCrystals);
            yield return null;
            var hudData = new SerializedObject(Find<GameHud>());
            string[] fields = { "goldText", "soulsText", "crystalsText" };
            string[] balances = { "3,000", "600", "150" };
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < fields.Length; i++)
            {
                var label = (TMP_Text)hudData.FindProperty(fields[i]).objectReferenceValue;
                Assert.That(label.text, Does.Contain(balances[i]).And.Contain("/ " + balances[i]));
                label.ForceMeshUpdate();
                Assert.IsFalse(label.isTextOverflowing, fields[i] + " should fit its HUD bounds.");
            }
            // Restoring an older snapshot must remove all storage bonuses immediately.
            var legacy = saved;
            legacy.dungeon.sections = legacy.dungeon.sections.Where(s => s.kind != ConstructionKind.StorageRoom).ToArray();
            legacy.dungeon.queue = new Vector2Int[0];
            session.Restore(legacy); Time.timeScale = 1;
            Assert.AreEqual(1000, wallet.GetCapacity(CurrencyType.Gold));
            Assert.AreEqual(3000, wallet.Gold, "Already saved resources survive a lower cap.");
            wallet.Add(CurrencyType.Gold, 1); Assert.AreEqual(3000, wallet.Gold);
        }
    }
}
#endif
