using System.Collections;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("ResourceEconomy")]
    [Category("ResourceEconomyRegression")]
    [Category("CoreTechnologyRegression")]
    [Category("PathGlowStarter")]
    public sealed class ResourceEconomyTests
    {
        private Scene scene;
        private WaveDirector waves;
        private DungeonGrid grid;
        private GameSaveSession session;
        private RunEconomy economy;
        private CurrencyWallet wallet;
        private float previousTime;
        private Keyboard keyboard;
        private InputSettings originalSettings, inputSettings;
        private HideFlags originalFlags;
        private T Find<T>() where T:Component => scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).Single();
        [UnitySetUp] public IEnumerator Setup()
        {
            previousTime=Time.timeScale; Time.timeScale=1;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            waves=Find<WaveDirector>(); grid=Find<DungeonGrid>(); session=Find<GameSaveSession>(); economy=waves.Economy; wallet=Find<CurrencyWallet>();
            waves.ConfigureRun(new NewGameOptions{difficulty=GameDifficulty.Easy});
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            if(originalSettings!=null){InputSystem.settings=originalSettings;originalSettings.hideFlags=originalFlags;}
            if(inputSettings!=null)Object.DestroyImmediate(inputSettings);
            Time.timeScale=previousTime;
        }
        private void Move(Vector3 position){waves.Lord.transform.position=position;waves.Lord.GetComponent<Rigidbody2D>().position=position;Physics2D.SyncTransforms();}
        private void OpenCore(){Move(economy.CorePosition+Vector3.left*1.5f);Assert.IsTrue(economy.TryInteract());Assert.IsTrue(economy.IsCoreOpen);}
        private void Round(int number)
        {
            waves.RestoreSave(new WaveSaveData{number=number,state=WaveState.Preparing,enemies=new MinionSaveData[0]});
            economy.OnRoundStarted(number);
        }
        [UnityTest] public IEnumerator WaveLaunchChargesLivingMinionsOnce()
        {
            int blood=wallet.Souls;Assert.AreEqual(8,economy.NextUpkeep);
            waves.BeginNextWave();Assert.AreEqual(blood-8,wallet.Souls);Assert.AreEqual(1,economy.LastUpkeepWave);
            economy.OnRoundStarted(1);Assert.AreEqual(blood-8,wallet.Souls);
            yield return null;
        }
        [Test] public void ShortageCarriesDebtAndLaterPaymentClearsIt()
        {
            wallet.RestoreAmounts(250,1,5);Round(1);
            Assert.AreEqual(0,wallet.Souls);Assert.AreEqual(7,economy.BloodDebt);
            wallet.Add(CurrencyType.Souls,30);Round(2);
            Assert.AreEqual(15,wallet.Souls);Assert.AreEqual(0,economy.BloodDebt);Assert.AreEqual(15,economy.LastBloodPaid);
        }
        [Test] public void DeadMinionsDoNotPayUpkeep()
        {foreach(var guard in grid.Guards.ToArray())guard.Health.Kill();Assert.AreEqual(0,economy.NextUpkeep);int before=wallet.Souls;Round(1);Assert.AreEqual(before,wallet.Souls);}
        [UnityTest] public IEnumerator CoreUpgradesChangeStatsAndRejectInsufficientFunds()
        {
            OpenCore();int maximum=waves.Lord.MaximumHealth;
            Assert.IsTrue(economy.TryUpgrade(LordUpgrade.Vitality));Assert.AreEqual(maximum+20,waves.Lord.MaximumHealth);Assert.AreEqual(2,wallet.DarkCrystals);
            Assert.IsFalse(economy.TryUpgrade(LordUpgrade.Power));Assert.AreEqual(0,economy.AttackBonus);
            wallet.RestoreAmounts(250,40,50);
            Assert.IsTrue(economy.TryUpgrade(LordUpgrade.Power));Assert.IsTrue(economy.TryUpgrade(LordUpgrade.Speed));
            Assert.AreEqual(5,economy.AttackBonus);Assert.AreEqual(8,economy.MagicBonus);
            economy.Close();yield return null;Assert.That(waves.Lord.GetComponent<TopDownMotor>().SpeedMultiplier,Is.EqualTo(1.05f).Within(.001f));
        }
        [Test] public void CoreRequiresProximityAndEnforcesFiveLevels()
        {
            wallet.RestoreAmounts(250,40,1000);Assert.IsFalse(economy.TryUpgrade(LordUpgrade.Power));OpenCore();
            for(int i=0;i<5;i++)Assert.IsTrue(economy.TryUpgrade(LordUpgrade.Power));
            int balance=wallet.DarkCrystals;Assert.IsFalse(economy.TryUpgrade(LordUpgrade.Power));Assert.AreEqual(balance,wallet.DarkCrystals);
            Move(grid.CellCenter(grid.Layout.Entrance));Assert.IsFalse(economy.TryUpgrade(LordUpgrade.Speed));
        }
        [Test] public void MerchantNeverArrivesAcrossRounds()
        {
            for (int wave = 1; wave <= 10; wave++)
            {
                Round(wave);
                Assert.IsFalse(economy.MerchantPresent);
                Assert.IsFalse(economy.CanBuy(0));
            }
            Assert.IsEmpty(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GoblinMerchantVisual>()));
        }
        [Test] public void LegacyMerchantVisitIsRemovedWithoutChangingResources()
        {
            var saved = economy.Capture(); saved.merchantRemaining = 90; saved.nextMerchantWave = 2;
            saved.offers = new[] { "legacy stock" }; saved.sold = new[] { true };
            int gold = wallet.Gold;
            economy.Restore(saved); economy.Restore(saved);
            Assert.IsFalse(economy.MerchantPresent); Assert.IsFalse(economy.IsMerchantOpen);
            Assert.AreEqual(0, economy.MerchantRemaining); Assert.IsNull(economy.Offer(0));
            Assert.IsFalse(economy.TryBuy(0)); Assert.AreEqual(gold, wallet.Gold);
            Assert.IsEmpty(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GoblinMerchantVisual>()));
        }
        [Test] public void SavePreservesUpgradesWithoutRestoringMerchant()
        {
            OpenCore(); Assert.IsTrue(economy.TryUpgrade(LordUpgrade.Vitality)); economy.Close();
            var saved = session.Capture(); int maximum = waves.Lord.MaximumHealth, gold = wallet.Gold;
            saved.economy.merchantRemaining = 90;
            saved.economy.offers = new[] { "", "", "" }; saved.economy.sold = new bool[4];
            session.Restore(saved); session.Restore(saved);
            Assert.AreEqual(maximum, waves.Lord.MaximumHealth);
            Assert.AreEqual(1, economy.Level(LordUpgrade.Vitality)); Assert.AreEqual(gold, wallet.Gold);
            Assert.IsFalse(economy.MerchantPresent);
            saved.economy = null; session.Restore(saved);
            Assert.AreEqual(0, economy.Level(LordUpgrade.Vitality)); Assert.IsFalse(economy.MerchantPresent);
        }
        [Test] public void InvalidEconomySaveIsRejectedBeforeChangingLiveState()
        {
            var saved=session.Capture();saved.economy.power=6;
            Assert.Throws<System.IO.InvalidDataException>(()=>session.Restore(saved));Assert.AreEqual(0,economy.Level(LordUpgrade.Power));
        }
        [UnityTest] public IEnumerator CorePowerUpgradeIncreasesActualAttackAndNovaDamage()
        {
            OpenCore();Assert.IsTrue(economy.TryUpgrade(LordUpgrade.Power));economy.Close();yield return null;
            var node=new GameObject("Economy damage target",typeof(ActorHealth),typeof(CircleCollider2D));SceneManager.MoveGameObjectToScene(node,scene);
            node.transform.position=waves.Lord.transform.position+Vector3.left*.8f;
            var target=node.GetComponent<ActorHealth>();target.Configure(Faction.Invader,500);Physics2D.SyncTransforms();
            Assert.IsTrue(waves.Combat.TryAttack(Vector2.left));Assert.AreEqual(470,target.CurrentHealth);
            Assert.IsTrue(waves.Combat.TryNova());Assert.AreEqual(420,target.CurrentHealth);
        }
        [UnityTest] public IEnumerator FOpensCoreFacilityAndEscapeClosesWithoutPausing()
        {
            originalSettings=InputSystem.settings;originalFlags=originalSettings.hideFlags;originalSettings.hideFlags=HideFlags.DontUnloadUnusedAsset;
            inputSettings=Object.Instantiate(originalSettings);inputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=inputSettings;keyboard=InputSystem.AddDevice<Keyboard>();
            Move(economy.CorePosition+Vector3.left*1.5f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return null;yield return null;
            Assert.IsTrue(economy.IsCoreOpen,"F must select the nearby core");Assert.IsTrue(Find<DarkLord.UI.EconomyPanel>().IsVisible,"The facility window refreshes in LateUpdate");
            Assert.IsFalse(Find<MinionSummoning>().IsOpen);Assert.IsFalse(waves.Combat.TryNova());
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;
            Assert.IsFalse(economy.IsOpen);Assert.AreEqual(1,Time.timeScale);
        }
    }
}
