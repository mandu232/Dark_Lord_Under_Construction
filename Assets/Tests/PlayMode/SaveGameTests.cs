#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using DarkLord.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace DarkLord.Tests
{
    [Category("CombatRewards")]
    [Category("OpeningRegression")]
    [Category("MinionRoster")]
    public sealed class SaveGameTests
    {
        private string directory;
        private SaveSlotStore store;
        private Scene scene;
        private float timeScale;
        private bool audioPaused;
        [SetUp] public void Setup()
        {
            directory=Path.Combine(Application.temporaryCachePath,"SaveTests-"+Guid.NewGuid().ToString("N"));
            store=new SaveSlotStore(directory);timeScale=Time.timeScale;audioPaused=AudioListener.pause;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(scene.IsValid() && scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale=timeScale;AudioListener.pause=audioPaused;
            if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
        [UnityTest]
        public IEnumerator SaveNoticeExpiresWhilePausedAndRepeatedSaveRestartsTimer()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            yield return null; yield return null;
            var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameMenu>(true)).Single();
            var status = menu.GetComponentInChildren<PausePanelBindings>(true).transform.Find("Save Status").GetComponent<TMP_Text>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var save = typeof(GameMenu).GetMethod("SaveCurrent", flags);
            typeof(GameMenu).GetField("saveStatusDuration", flags).SetValue(menu, .3f);
            menu.PauseGame();
            Assert.IsTrue((bool)save.Invoke(menu, new object[] { 0, store }));
            Assert.IsNotEmpty(status.text);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsTrue((bool)save.Invoke(menu, new object[] { 0, store }));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsNotEmpty(status.text, "Saving again restarts the notice lifetime.");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsEmpty(status.text);
            Assert.AreEqual(0, Time.timeScale, "The notice must expire without resuming gameplay.");
        }

        private static GameSaveData Data()
        {
            var layout=new DungeonLayout(41,31);
            var construction=new DungeonConstruction(layout);
            var cells=new int[41*31];for(int y=0;y<31;y++)for(int x=0;x<41;x++)cells[y*41+x]=(int)layout.GetCell(new Vector2Int(x,y));
            return new GameSaveData { gold=123,playerHealth=90,playerMaximumHealth=100,coreHealth=200,coreMaximumHealth=200,
                dungeon=new DungeonSaveData {width=41,height=31,cells=cells,sections=construction.Sections.Select(s=>new SectionSaveData {
                    kind=s.Kind,bounds=s.Bounds,complete=s.IsComplete,core=s.IsCore,initial=true,cells=s.Cells.ToArray()}).ToArray(),traps=new TrapSaveData[0],queue=new Vector2Int[0]},
                wave=new WaveSaveData {enemies=new MinionSaveData[0]}};
        }
        [Test] public void ThreeSlotsRemainIndependentAndCorruptPrimaryRecoversBackup()
        {
            var data=Data();for(int slot=0;slot<3;slot++){data.gold=100+slot;store.Write(slot,data);}
            data.gold=200;store.Write(0,data);
            File.WriteAllText(store.PathFor(0),"corrupted");
            Assert.IsTrue(store.TryRead(0,out var recovered,out var message));Assert.AreEqual(100,recovered.gold);Assert.That(message,Does.Contain("복구"));
            Assert.IsTrue(store.TryRead(1,out var second,out _));Assert.AreEqual(101,second.gold);
            Assert.IsTrue(store.TryRead(2,out var third,out _));Assert.AreEqual(102,third.gold);
        }
        [Test] public void OldMinionRecordsDefaultToDefensiveAndInvalidPolicyIsRejected()
        {
            var legacy = JsonUtility.FromJson<MinionSaveData>("{\"health\":100,\"maximumHealth\":100,\"order\":0}");
            Assert.AreEqual(DarkLord.Minions.MinionBehaviour.Defensive, legacy.behaviour);
            var data = Data(); data.guards = new[] { legacy };
            store.Write(0, data);
            legacy.behaviour = (DarkLord.Minions.MinionBehaviour)99;
            Assert.Throws<InvalidDataException>(() => store.Write(0, data));
        }
        [Test] public void InvalidWritePreservesPreviousRecordAndUnknownVersionIsRejected()
        {
            var data=Data();store.Write(0,data);data.version=999;
            Assert.Throws<InvalidDataException>(()=>store.Write(0,data));
            Assert.IsTrue(store.TryRead(0,out var previous,out _));Assert.AreEqual(123,previous.gold);
            Directory.CreateDirectory(directory);File.WriteAllText(store.PathFor(1),JsonUtility.ToJson(data));
            Assert.IsFalse(store.TryRead(1,out _,out _));Assert.IsFalse(store.TryRead(2,out _,out _));
        }
        [UnityTest] public IEnumerator MenuButtonsExposeSaveWarningAndThreeNewGameSlots()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");yield return null;yield return null;
            var menu=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameMenu>(true)).Single();
            var pause=menu.GetComponentInChildren<PausePanelBindings>(true);
            var save=pause.transform.Find("Pause Actions/Save").GetComponent<UnityEngine.UI.Button>();
            Assert.AreEqual("SaveGame",save.onClick.GetPersistentMethodName(0));Assert.AreSame(pause,save.onClick.GetPersistentTarget(0));
            menu.PauseGame();menu.AskQuit();Assert.AreEqual(GameMenu.ScreenState.Quit,menu.State);menu.CancelQuit();
            Assert.AreEqual(GameMenu.ScreenState.Pause,menu.State);
            yield return SceneManager.UnloadSceneAsync(scene);
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/MainMenu.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");yield return null;
            menu=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameMenu>(true)).Single();
            menu.StartGame();Assert.AreEqual(GameMenu.ScreenState.Slots,menu.State);
            var slots=menu.GetComponentInChildren<SaveSlotPanel>();Assert.NotNull(slots);
            for(int slot=1;slot<=3;slot++)Assert.IsTrue(slots.transform.Find("Slot "+slot).GetComponent<UnityEngine.UI.Button>().interactable);
            menu.CancelSlots();Assert.AreEqual(GameMenu.ScreenState.Main,menu.State);
        }        [UnityTest] public IEnumerator ContinueRestoresChosenSlotAfterSceneReload()
        {
            GameSaveSession.BeginNew(2);
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");yield return null;yield return null;
            var roots=scene.GetRootGameObjects();
            var session=roots.SelectMany(r=>r.GetComponentsInChildren<GameSaveSession>(true)).Single();
            var menu=roots.SelectMany(r=>r.GetComponentsInChildren<GameMenu>(true)).Single();menu.PauseGame();
            var wallet=roots.SelectMany(r=>r.GetComponentsInChildren<CurrencyWallet>(true)).Single();wallet.RestoreAmounts(987,65,43);
            Assert.IsTrue(session.Save(2,store),session.Message);
            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.IsTrue(GameSaveSession.BeginContinue(2,out var error,store),error);
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");yield return null;yield return null;
            roots=scene.GetRootGameObjects();session=roots.SelectMany(r=>r.GetComponentsInChildren<GameSaveSession>(true)).Single();
            Assert.IsTrue(session.Ready);Assert.AreEqual(2,GameSaveSession.ActiveSlot);
            var restored=session.Capture();Assert.AreEqual(987,restored.gold);Assert.AreEqual(65,restored.souls);Assert.AreEqual(43,restored.crystals);
        }        [UnityTest] public IEnumerator GameStateRoundTripRestoresConstructionGuardsCombatAndDirtyWarning()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");yield return null;yield return null;
            var roots=scene.GetRootGameObjects();
            T Find<T>() where T:Component=>roots.SelectMany(r=>r.GetComponentsInChildren<T>(true)).Single();
            var session=Find<GameSaveSession>();var menu=Find<GameMenu>();var dungeon=Find<DungeonGrid>();var summoning=Find<MinionSummoning>();var waves=Find<WaveDirector>();var wallet=Find<CurrencyWallet>();
            Assert.IsTrue(session.Ready);Assert.IsTrue(session.HasUnsavedChanges);
            var initial = session.Capture(); initial.options = new NewGameOptions { difficulty = GameDifficulty.Easy }; session.Restore(initial);
            // Current starts already contain four guards; retain one for this focused round trip.
            foreach (var guard in dungeon.Guards.Skip(1).ToArray()) Assert.IsTrue(summoning.RemoveGuard(guard));
            Assert.IsTrue(summoning.BeginRelocation(dungeon.Guards.Single()));
            Assert.IsTrue(summoning.TryPlace(dungeon.Layout.Core + Vector2Int.right * 2 + Vector2Int.up * 2));
            dungeon.Guards.Single().SetBehaviour(DarkLord.Minions.MinionBehaviour.Aggressive);
            var corridor=UnityEditor.AssetDatabase.FindAssets("t:BuildItemDefinition").Select(g=>UnityEditor.AssetDatabase.LoadAssetAtPath<BuildItemDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).First(i=>i.constructionKind==ConstructionKind.Corridor);
            var start=dungeon.Layout.Core+Vector2Int.right*2;var end=start+Vector2Int.right*7;
            Assert.AreEqual(ConstructionResult.Success,dungeon.Construction.TryPlanCorridor(start,end,out var section));dungeon.QueueConstruction(section,corridor.prefab);
            var entrance=dungeon.Layout.Core+Vector2Int.left*2;Assert.IsTrue(dungeon.TryPlaceEntrance(entrance));waves.BeginNextWave();
            menu.PauseGame();Assert.IsTrue(session.Save(0,store),session.Message);Assert.IsFalse(session.HasUnsavedChanges);
            Assert.IsTrue(store.TryRead(0,out var saved,out var readError), readError);string expected=JsonUtility.ToJson(session.Capture());
            wallet.Add(CurrencyType.Gold,456);summoning.ClearSavedGuards();dungeon.RemoveEntrance();
            Assert.IsTrue(session.HasUnsavedChanges);menu.AskQuit();Assert.AreEqual(GameMenu.ScreenState.Quit,menu.State);menu.CancelQuit();
            session.Restore(saved);
            Assert.AreEqual(expected,JsonUtility.ToJson(session.Capture()));Assert.IsFalse(session.HasUnsavedChanges);
            Assert.AreEqual(DarkLord.Minions.MinionBehaviour.Aggressive,dungeon.Guards.Single().Behaviour);
            Assert.AreEqual(1,summoning.GuardCount);Assert.AreEqual(1,dungeon.Construction.PendingCount);Assert.AreEqual(WaveState.Active,waves.State);
            Assert.Greater(waves.RemainingEnemies,0);Assert.IsTrue(dungeon.Layout.HasEntrance);
            yield return null;Assert.IsFalse(session.HasUnsavedChanges,"Paused restore must remain clean.");
        }
    }
}
#endif


