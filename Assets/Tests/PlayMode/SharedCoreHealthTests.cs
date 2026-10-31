using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DarkLord.Actors;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("SharedCoreHealth")]
    public sealed class SharedHealthPoolTests
    {
        private GameObject playerObject, coreObject;
        private ActorHealth player, core;
        [SetUp] public void Setup()
        {
            playerObject=new GameObject("Shared health player");coreObject=new GameObject("Shared health core");
            player=playerObject.AddComponent<ActorHealth>();core=coreObject.AddComponent<ActorHealth>();
            player.Configure(Faction.Dungeon,100);core.Configure(Faction.Dungeon,300);core.ShareHealthWith(player);
        }
        [TearDown] public void Cleanup(){UnityEngine.Object.DestroyImmediate(coreObject);UnityEngine.Object.DestroyImmediate(playerObject);}
        [Test] public void EitherTargetDamagesTheSamePoolWithoutDoubleApplying()
        {
            int playerEvents=0,coreEvents=0;player.Damaged+=_=>playerEvents++;core.Damaged+=_=>coreEvents++;
            Assert.IsTrue(core.TakeDamage(20));Assert.AreEqual(80,player.CurrentHealth);Assert.AreEqual(80,core.CurrentHealth);
            Assert.IsTrue(player.TakeDamage(15));Assert.AreEqual(65,player.CurrentHealth);Assert.AreEqual(65,core.CurrentHealth);
            Assert.AreEqual(2,playerEvents);Assert.AreEqual(2,coreEvents);
        }
        [Test] public void HealingAndMaximumChangesThroughEitherBodyAreShared()
        {
            core.TakeDamage(50);Assert.AreEqual(20,player.Heal(20));Assert.AreEqual(70,core.CurrentHealth);
            Assert.AreEqual(30,core.Heal(999));Assert.AreEqual(100,player.CurrentHealth);
            core.RestoreHealth(200,150);Assert.AreEqual(200,player.MaximumHealth);Assert.AreEqual(150,player.CurrentHealth);
            player.RestoreHealth(220,170);Assert.AreEqual(220,core.MaximumHealth);Assert.AreEqual(170,core.CurrentHealth);
        }
        [Test] public void LethalDamageKillsBothExactlyOnceAndHealingCannotResurrect()
        {
            int playerDeaths=0,coreDeaths=0;player.Died+=()=>playerDeaths++;core.Died+=()=>coreDeaths++;
            core.TakeDamage(999);player.Kill();core.Kill();
            Assert.IsFalse(player.IsAlive||core.IsAlive);Assert.AreEqual(1,playerDeaths);Assert.AreEqual(1,coreDeaths);
            Assert.AreEqual(0,player.Heal(100));Assert.AreEqual(0,core.Heal(100));
        }
        [Test] public void ReceivingBodyRetainsItsDamageRules()
        {
            typeof(ActorHealth).GetField("DamageFilter",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,new Func<ActorHealth,bool>(_=>false));
            Assert.IsFalse(player.TakeDamage(20));Assert.AreEqual(100,core.CurrentHealth);
            Assert.IsTrue(core.TakeDamage(20));Assert.AreEqual(80,player.CurrentHealth);
            typeof(ActorHealth).GetField("magicResistance",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(core,50);
            core.TakeDamage(20,null,DamageType.Magic);Assert.AreEqual(70,player.CurrentHealth);
        }
        [Test] public void RelinkingDoesNotDuplicateEventsAndUnrelatedActorsStayIndependent()
        {
            core.ShareHealthWith(player);core.ShareHealthWith(player);int events=0;core.Damaged+=_=>events++;
            player.TakeDamage(5);Assert.AreEqual(1,events);
            var other=new GameObject("Independent health");try{var hp=other.AddComponent<ActorHealth>();hp.Configure(Faction.Dungeon,40);hp.TakeDamage(10);Assert.AreEqual(95,core.CurrentHealth);Assert.AreEqual(30,hp.CurrentHealth);hp.Configure(Faction.Dungeon,0);Assert.AreEqual(1,hp.CurrentHealth);Assert.AreEqual(1,hp.MaximumHealth);Assert.AreEqual(95,core.CurrentHealth);}finally{UnityEngine.Object.DestroyImmediate(other);}
        }
    }

    [Category("SharedCoreHealth")]
    public sealed class SharedCoreHealthSceneTests
    {
        private Scene scene;private WaveDirector waves;private GameSaveSession session;private float previousTime;private bool previousAudio;
        private T Find<T>() where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).Single();
        [UnitySetUp] public IEnumerator Setup()
        {
            previousTime=Time.timeScale;previousAudio=AudioListener.pause;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");yield return null;yield return null;
            Time.timeScale=1;AudioListener.pause=false;waves=Find<WaveDirector>();session=Find<GameSaveSession>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale=previousTime;AudioListener.pause=previousAudio;
        }
        [UnityTest] public IEnumerator HealthAndManaBarsFollowDamageAndManaUse()
        {
            waves.Core.TakeDamage(25);waves.Combat.RestoreSave(new CombatSaveData{mana=40});yield return null;
            var bars=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RectTransform>(true));
            var hp=bars.Single(r=>r.name=="Shared Health Bar").Find("Track/Fill").GetComponent<UnityEngine.UI.Image>();
            var mana=bars.Single(r=>r.name=="Mana Bar").Find("Track/Fill").GetComponent<UnityEngine.UI.Image>();
            Assert.AreEqual(75,waves.Lord.CurrentHealth);Assert.AreEqual(75,waves.Core.CurrentHealth);
            Assert.That(hp.rectTransform.anchorMax.x,Is.EqualTo(.75f).Within(.001f));
            Assert.That(mana.rectTransform.anchorMax.x,Is.InRange(.39f,.43f));
        }
        [UnityTest] public IEnumerator PixelVitalsAndManualWaveButtonMatchDifficulty()
        {
            var button=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.UI.Button>(true)).Single(b=>b.name=="Start Wave Button");
            foreach(var difficulty in new[]{GameDifficulty.Easy,GameDifficulty.Normal,GameDifficulty.Hard,GameDifficulty.Nightmare})
            {
                waves.ConfigureRun(new NewGameOptions{difficulty=difficulty});yield return null;
                Assert.AreEqual(difficulty==GameDifficulty.Easy,button.gameObject.activeSelf,difficulty.ToString());
            }
            Assert.AreEqual("CastleButton_0",button.GetComponent<UnityEngine.UI.Image>().sprite.name);
            Assert.IsFalse(button.transform.Find("Castle Frame").GetComponent<UnityEngine.UI.Image>().enabled);
            foreach(var name in new[]{"Shared Health Bar","Mana Bar"})
            {
                var bar=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RectTransform>(true)).Single(r=>r.name==name);
                Assert.AreEqual("SliderTrack",bar.GetComponent<UnityEngine.UI.Image>().sprite.name);
                var value=bar.GetComponentInChildren<TMPro.TMP_Text>().text;
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(value,@"^\d+ / \d+$"),value);
            }
            waves.ConfigureRun(new NewGameOptions{difficulty=GameDifficulty.Easy});yield return null;
            Assert.IsTrue(button.interactable);button.onClick.Invoke();yield return null;
            Assert.AreEqual(1,waves.CurrentWave);Assert.IsFalse(button.gameObject.activeSelf);
        }
        [Test] public void SaveRoundTripAndLegacyMigrationKeepOnePool()
        {
            waves.Core.TakeDamage(25);var saved=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            Assert.IsTrue(saved.sharedCoreHealth);Assert.AreEqual(saved.playerHealth,saved.coreHealth);
            waves.Lord.Heal(100);session.Restore(saved);Assert.AreEqual(75,waves.Lord.CurrentHealth);Assert.AreEqual(75,waves.Core.CurrentHealth);
            saved.sharedCoreHealth=false;saved.playerMaximumHealth=100;saved.playerHealth=80;saved.coreMaximumHealth=300;saved.coreHealth=150;
            session.Restore(saved);Assert.AreEqual(50,waves.Lord.CurrentHealth);Assert.AreEqual(100,waves.Core.MaximumHealth);
            saved.coreMaximumHealth=400;saved.coreHealth=200;
            session.Restore(saved);Assert.AreEqual(200,waves.Lord.MaximumHealth);Assert.AreEqual(100,waves.Core.CurrentHealth);
            saved.coreHealth=0;session.Restore(saved);Assert.IsFalse(waves.Lord.IsAlive||waves.Core.IsAlive);
        }
        [Test] public void CoreRepairAndUpgradeAffectPlayerHealth()
        {
            waves.Core.TakeDamage(40);Assert.IsTrue(waves.Combat.TryRepairCore());Assert.AreEqual(85,waves.Lord.CurrentHealth);
            Assert.IsTrue(waves.Economy.TryInteract());
            Assert.IsTrue(waves.Combat.TryUpgradeCore());Assert.AreEqual(waves.Lord.MaximumHealth,waves.Core.MaximumHealth);
            Assert.AreEqual(waves.Lord.CurrentHealth,waves.Core.CurrentHealth);Assert.Greater(waves.Lord.MaximumHealth,100);
        }
        [Test] public void DeathThroughCoreEndsTheRunWithBothBodiesDead()
        {
            waves.Core.TakeDamage(9999);Assert.IsFalse(waves.Lord.IsAlive||waves.Core.IsAlive);Assert.AreEqual(WaveState.Defeated,waves.State);
        }
    }
}
