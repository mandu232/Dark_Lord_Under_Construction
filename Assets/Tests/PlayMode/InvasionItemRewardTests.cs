using System;
using System.Collections;
using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("InvasionItems")]
    public sealed class InvasionItemRewardTests
    {
        private Scene scene;
        private WaveDirector waves;
        private float previousTime;
        private bool previousAudio;
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        [UnitySetUp] public IEnumerator Setup()
        {
            previousTime = Time.timeScale; previousAudio = AudioListener.pause;
            Time.timeScale = 1; AudioListener.pause = false;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            waves = Find<WaveDirector>(); Time.timeScale = 1;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousTime; AudioListener.pause = previousAudio;
        }
        private void ClearWave(int number = 1)
        {
            waves.RestoreSave(new WaveSaveData { number = number, state = WaveState.Active, enemies = Array.Empty<MinionSaveData>() });
            typeof(WaveDirector).GetMethod("CheckWaveCleared", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(waves, null);
        }
        [Test] public void RewardOffersConcreteRoomFacilityAndSupplyWithoutNewAugments()
        {
            ClearWave();
            CollectionAssert.AreEquivalent(new[] { WaveRewardCatalog.RoomItem, WaveRewardCatalog.FacilityItem }, waves.RewardChoices.Where(id => id >= WaveRewardCatalog.RoomItem));
            Assert.IsTrue(waves.RewardChoices.All(id => !WaveRewardCatalog.IsAugment(id)));
            for (int i = 0; i < 3; i++)
            {
                int id = waves.RewardChoices[i]; var item = waves.RewardItem(i);
                if (id == WaveRewardCatalog.RoomItem || id == WaveRewardCatalog.FacilityItem)
                {
                    Assert.IsNotNull(item); Assert.IsNotNull(item.icon);
                    Assert.AreEqual(id == WaveRewardCatalog.RoomItem ? BuildCategory.Rooms : BuildCategory.Buildings, item.category);
                }
                else { Assert.That(id, Is.InRange(0, 2)); Assert.IsNull(item); }
            }
            Assert.IsEmpty(waves.Augments.Capture());
        }
        [Test] public void ExactDisplayedItemAndChoicesSurviveReloadAndCanOnlyBeClaimedOnce()
        {
            ClearWave();
            int index = waves.RewardChoices.ToList().IndexOf(WaveRewardCatalog.FacilityItem);
            var shown = waves.RewardItem(index); var grid = Find<DungeonGrid>();
            int before = grid.Inventory.Count(shown);
            var session = Find<GameSaveSession>();
            var save = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            SaveSlotStore.Validate(save); session.Restore(save);
            Assert.AreSame(shown, waves.RewardItem(index)); CollectionAssert.AreEqual(save.wave.rewards, waves.RewardChoices);
            Assert.IsTrue(waves.ChooseReward(index)); Assert.AreEqual(before + 1, grid.Inventory.Count(shown));
            Assert.IsFalse(waves.ChooseReward(index)); Assert.IsEmpty(waves.Augments.Capture());
            session.Restore(JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture())));
            Assert.AreEqual(before + 1, grid.Inventory.Count(shown));
        }
        [UnityTest] public IEnumerator ItemCardsUseRealIconsFitAndClaimThroughTheirButtons()
        {
            ClearWave(); yield return null;
            var view = Find<WaveRewardPanel>(); Assert.IsTrue(view.IsVisible);
            var root = view.transform.Find("Wave reward selection");
            var texts = root.GetComponentsInChildren<TMP_Text>();
            Assert.IsFalse(texts.Any(t => t.text.Contains("증강")));
            Assert.IsTrue(texts.Any(t => t.text.Contains("전리품")));
            Canvas.ForceUpdateCanvases();
            foreach (var label in texts) { label.ForceMeshUpdate(); Assert.IsFalse(label.isTextOverflowing, label.name + ": " + label.text); }
            int index = waves.RewardChoices.ToList().IndexOf(WaveRewardCatalog.RoomItem);
            var item = waves.RewardItem(index); int before = Find<DungeonGrid>().Inventory.Count(item);
            var card = root.Find("Item acquisition/Reward " + (index + 1));
            Assert.AreEqual(item.icon, card.Find("Card artwork/Emblem").GetComponent<UnityEngine.UI.Image>().sprite);
            card.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.AreEqual(before + 1, Find<DungeonGrid>().Inventory.Count(item));
        }
        [UnityTest] public IEnumerator StartAlertShowsOnEachActualArrivalButNotOnSaveRestore()
        {
            var alert = Find<InvasionAlert>();
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Easy });
            Assert.IsFalse(alert.IsVisible); waves.BeginNextWave();
            yield return new WaitForSeconds(.35f);
            Assert.AreEqual(1, alert.PresentedWave); Assert.IsTrue(alert.IsVisible);
            var effects = alert.transform.Find("Invasion warning effects");
            Assert.IsTrue(effects.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("침공 개시")));
            Assert.AreEqual(3, effects.GetComponentsInChildren<InvasionRingGraphic>(true).Length);
            foreach (var ring in effects.GetComponentsInChildren<InvasionRingGraphic>(true)) Assert.IsNotNull(ring.GetComponent<CanvasRenderer>());
            Assert.IsTrue(effects.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(g => !g.raycastTarget));
            var save = waves.CaptureSave(); waves.RestoreSave(save);
            Assert.AreEqual(1, alert.PresentedWave);
            Time.timeScale = 0; yield return new WaitForSecondsRealtime(.1f); Assert.IsTrue(alert.IsVisible); Time.timeScale = 1;
            // An overlapping automatic arrival refreshes the same bounded presentation.
            waves.ConfigureRun(new NewGameOptions());
            save = waves.CaptureSave(); save.schedule.remaining = .02f; waves.RestoreSave(save);
            yield return new WaitForSeconds(.12f);
            Assert.AreEqual(2, alert.PresentedWave); Assert.IsTrue(alert.IsVisible);
            Assert.AreEqual(1, alert.transform.Cast<Transform>().Count(t => t.name == "Invasion warning effects"));
        }
        [Test] public void UnknownRewardItemIsRejectedBeforeMutatingLiveInventory()
        {
            ClearWave(); var session = Find<GameSaveSession>(); var save = session.Capture();
            int index = Array.IndexOf(save.wave.rewards, WaveRewardCatalog.RoomItem);
            save.wave.rewardItemIds[index] = "missing reward asset";
            string before = JsonUtility.ToJson(Find<DungeonGrid>().Inventory.Capture());
            Assert.Throws<InvalidOperationException>(() => session.Restore(save));
            Assert.AreEqual(before, JsonUtility.ToJson(Find<DungeonGrid>().Inventory.Capture()));
        }
    }
}
