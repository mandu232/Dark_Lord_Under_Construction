#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Persistence;
using DarkLord.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DarkLord.Tests
{
    public sealed class InvaderRoleTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private Scene scene;
        private float previousTime;
        private bool previousAudio;
        private static readonly Vector3 Center = new Vector3(850, 850);
        [SetUp] public void Setup() { previousTime = Time.timeScale; previousAudio = AudioListener.pause; Time.timeScale = 1; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var node in objects) if (node != null) Object.DestroyImmediate(node);
            objects.Clear();
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousTime; AudioListener.pause = previousAudio;
        }
        private ActorHealth Actor(Vector3 offset, Faction team = Faction.Invader)
        {
            var node = new GameObject("Role test actor"); objects.Add(node); node.transform.position = Center + offset;
            var health = node.AddComponent<ActorHealth>(); health.Configure(team, 100);
            node.AddComponent<CircleCollider2D>().radius = .2f;
            return health;
        }
        private InvaderTactics Enemy(InvaderRole role)
        {
            var health = Actor(Vector3.zero); health.gameObject.AddComponent<MinionController>();
            var tactics = health.gameObject.AddComponent<InvaderTactics>();
            tactics.RestoreSave(new InvaderTacticsSaveData { role = role });
            return tactics;
        }
        [Test] public void FiveWavesIntroduceDifferentProblemsAndRespectScaledCounts()
        {
            int[][] expected = { new[] {3,0,0,0}, new[] {3,0,0,2}, new[] {6,0,1,0}, new[] {6,2,1,0}, new[] {3,3,2,3} };
            for (int wave = 1; wave <= 5; wave++)
            {
                var formation = WaveFormation.Create(wave, wave * 2 + 1);
                for (int role = 0; role < 4; role++) Assert.AreEqual(expected[wave - 1][role], formation.Count(r => (int)r == role));
                foreach (GameDifficulty difficulty in System.Enum.GetValues(typeof(GameDifficulty)))
                {
                    int count = new NewGameOptions { difficulty = difficulty }.EnemyCount(wave * 2 + 1);
                    Assert.AreEqual(count, WaveFormation.Create(wave, count).Length);
                }
            }
            Assert.AreEqual(1, WaveFormation.Create(5, 0).Length);
        }
        [Test] public void ShieldReducesOnlyFrontalPhysicalDamageAndUnsubscribesOnDisable()
        {
            var shield = Enemy(InvaderRole.Shieldbearer); var health = shield.GetComponent<ActorHealth>();
            var source = Actor(Vector3.right, Faction.Dungeon); shield.GetComponent<TopDownMotor>().Face(Vector2.right);
            health.TakeDamage(20, source); Assert.AreEqual(94, health.CurrentHealth);
            health.TakeDamage(20, source, DamageType.Magic); Assert.AreEqual(74, health.CurrentHealth);
            source.transform.position = Center + Vector3.left;
            health.TakeDamage(20, source); Assert.AreEqual(54, health.CurrentHealth);
            source.transform.position = Center + Vector3.up;
            health.TakeDamage(20, source); Assert.AreEqual(34, health.CurrentHealth);
            shield.enabled = false; source.transform.position = Center + Vector3.right;
            health.TakeDamage(20, source); Assert.AreEqual(14, health.CurrentHealth);
        }
        [Test] public void HealerCastsHealsAllyOnceAndCannotHealEnemyOrSelf()
        {
            var healer = Enemy(InvaderRole.Healer); var ally = Actor(Vector3.right * 2); ally.TakeDamage(40);
            var hostile = Actor(Vector3.up * 2, Faction.Dungeon); hostile.TakeDamage(60);
            healer.GetComponent<ActorHealth>().TakeDamage(30);
            Physics2D.SyncTransforms();
            Assert.IsTrue(healer.TryAct(null)); Assert.AreEqual(InvaderAction.Healing, healer.Action); Assert.AreEqual(60, ally.CurrentHealth);
            var saved = JsonUtility.FromJson<InvaderTacticsSaveData>(JsonUtility.ToJson(healer.CaptureSave()));
            saved.remaining = 0; healer.RestoreSave(saved); healer.TryAct(null);
            Assert.AreEqual(78, ally.CurrentHealth); Assert.AreEqual(40, hostile.CurrentHealth);
            Assert.AreEqual(70, healer.GetComponent<ActorHealth>().CurrentHealth);
            Assert.IsFalse(healer.TryAct(null)); Assert.AreEqual(78, ally.CurrentHealth);
        }
        [Test] public void HealingInterruptRangeWallsAndPauseAreRespected()
        {
            var healer = Enemy(InvaderRole.Healer); var ally = Actor(Vector3.right * 2); ally.TakeDamage(50);
            Physics2D.SyncTransforms(); Assert.IsTrue(healer.TryAct(null));
            healer.GetComponent<ActorHealth>().TakeDamage(1); Assert.AreEqual(InvaderAction.Ready, healer.Action);
            Assert.AreEqual(50, ally.CurrentHealth);
            healer.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Healer });
            Time.timeScale = 0; Assert.IsFalse(healer.TryAct(null)); Time.timeScale = 1;
            var wall = new GameObject("Healing wall"); objects.Add(wall); wall.layer = 8; wall.transform.position = Center + Vector3.right;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 3);
            var data = new UnityEditor.SerializedObject(healer.GetComponent<MinionController>());
            data.FindProperty("obstacleLayers").intValue = 1 << 8; data.ApplyModifiedPropertiesWithoutUndo();
            Physics2D.SyncTransforms(); Assert.IsFalse(healer.TryAct(null));
            Object.DestroyImmediate(wall); ally.transform.position = Center + Vector3.right * 8;
            healer.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Healer });
            Physics2D.SyncTransforms(); Assert.IsFalse(healer.TryAct(null));
        }
        [Test] public void ChargeLocksDirectionHitsOnceAndHasRecovery()
        {
            var charger = Enemy(InvaderRole.Charger); var victim = Actor(Vector3.right * 3, Faction.Dungeon);
            Physics2D.SyncTransforms(); Assert.IsTrue(charger.TryAct(victim));
            Assert.AreEqual(InvaderAction.WindingUp, charger.Action); Assert.AreEqual(100, victim.CurrentHealth);
            var saved = charger.CaptureSave(); saved.remaining = 0; charger.RestoreSave(saved); charger.TryAct(victim);
            Assert.AreEqual(InvaderAction.Charging, charger.Action);
            var motor = charger.GetComponent<TopDownMotor>(); motor.ApplySlow(.5f, 2);
            Assert.That(motor.EffectiveMoveSpeed, Is.EqualTo(motor.MoveSpeed * 2.6f * .5f).Within(.01f));
            victim.transform.position = Center + Vector3.right * .8f;
            victim.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * .2f;
            Physics2D.SyncTransforms(); charger.TryAct(victim);
            Assert.AreEqual(78, victim.CurrentHealth); Assert.AreEqual(InvaderAction.Recovering, charger.Action);
            charger.TryAct(victim); Assert.AreEqual(78, victim.CurrentHealth);
            Assert.AreEqual(1, motor.SpeedMultiplier);
        }
        [UnityTest] public IEnumerator ChargeMovesInRealPhysicsAndPauseFreezesWarning()
        {
            var charger = Enemy(InvaderRole.Charger); var victim = Actor(Vector3.right * 5, Faction.Dungeon);
            charger.GetComponent<MinionController>().Assault(victim);
            yield return null;
            Assert.AreEqual(InvaderAction.WindingUp, charger.Action);
            Time.timeScale = 0; var before = charger.CaptureSave(); var position = charger.transform.position;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(charger.CaptureSave().remaining, Is.EqualTo(before.remaining).Within(.02f));
            Assert.AreEqual(position, charger.transform.position);
            Time.timeScale = 1;
            yield return new WaitForSeconds(1.55f);
            Assert.Greater(charger.transform.position.x, Center.x + 2);
            Assert.Less(victim.CurrentHealth, 100);
        }
        [Test] public void ChargeCanHitColliderlessCoreObjective()
        {
            var charger = Enemy(InvaderRole.Charger); var core = Actor(Vector3.right * 3, Faction.Dungeon);
            Object.DestroyImmediate(core.GetComponent<Collider2D>());
            charger.TryAct(core); var saved = charger.CaptureSave(); saved.remaining = 0;
            charger.RestoreSave(saved); charger.TryAct(core);
            core.transform.position = Center + Vector3.right * .8f;
            charger.TryAct(core);
            Assert.AreEqual(78, core.CurrentHealth);
        }
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        [UnityTest] public IEnumerator HealerCompletesCastThroughControllerAndChargeCannotCrossTerrain()
        {
            var healer = Enemy(InvaderRole.Healer); var ally = Actor(Vector3.up * 2); ally.TakeDamage(50);
            var victim = Actor(Vector3.right * 5, Faction.Dungeon);
            healer.GetComponent<MinionController>().Assault(victim);
            yield return new WaitForSeconds(1);
            Assert.AreEqual(68, ally.CurrentHealth);
            healer.gameObject.SetActive(false); ally.gameObject.SetActive(false);
            var charger = Enemy(InvaderRole.Charger); charger.GetComponent<MinionController>().Assault(victim);
            yield return null; Assert.AreEqual(InvaderAction.WindingUp, charger.Action);
            var wall = new GameObject("Charge blocking wall"); objects.Add(wall); wall.transform.position = Center + Vector3.right * 2.5f;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.3f, 4);
            yield return new WaitForSeconds(1.5f);
            Assert.Less(charger.transform.position.x, Center.x + 2.5f);
            Assert.AreEqual(100, victim.CurrentHealth);
        }
        [UnityTest] public IEnumerator ForecastMatchesSpawnQueueAcrossAllWavesAndSaves()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            var waves = Find<WaveDirector>(); var hud = Find<WaveHud>();
            for (int wave = 1; wave <= 5; wave++)
            {
                Time.timeScale = 1;
                waves.RestoreSave(new WaveSaveData { number = wave - 1, state = WaveState.Preparing, enemies = new MinionSaveData[0] });
                waves.ConfigureRun(new NewGameOptions { difficulty = wave % 2 == 0 ? GameDifficulty.Hard : GameDifficulty.Normal });
                var formation = waves.NextFormation;
                var label = hud.GetComponentInParent<Canvas>().transform.Find("Next wave formation/Formation and counters").GetComponent<TMP_Text>();
                Assert.IsTrue(label.gameObject.activeInHierarchy);
                Assert.That(label.text, Does.Contain(WaveFormation.Summary(formation)));
                label.ForceMeshUpdate(); Assert.IsFalse(label.isTextOverflowing, label.text);
                waves.BeginNextWave(); Time.timeScale = 0;
                var saved = JsonUtility.FromJson<WaveSaveData>(JsonUtility.ToJson(waves.CaptureSave()));
                CollectionAssert.AreEqual(formation, saved.enemies.Select(e => e.tactics.role).Concat(saved.pendingRoles).ToArray());
                Assert.IsFalse(label.gameObject.activeInHierarchy);
                waves.RestoreSave(saved);
                CollectionAssert.AreEqual(saved.pendingRoles, waves.CaptureSave().pendingRoles);
                Assert.AreEqual(saved.enemies[0].tactics.role, waves.CaptureSave().enemies[0].tactics.role);
            }
            // A version-1 save lacking tactics and queued role fields restores ordinary warriors.
            waves.RestoreSave(new WaveSaveData { number = 2, state = WaveState.Active, pending = 2,
                enemies = new[] {new MinionSaveData {position = Center, health = 40, maximumHealth = 55}} });
            Assert.AreEqual(InvaderRole.Warrior, waves.CaptureSave().enemies[0].tactics.role);
            CollectionAssert.AreEqual(new[] {InvaderRole.Warrior, InvaderRole.Warrior}, waves.CaptureSave().pendingRoles);
        }
        [UnityTest] public IEnumerator ResumedWaveSpawnsEntireAdvertisedFormationAndValidatesSavedRoles()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            var waves = Find<WaveDirector>(); Time.timeScale = 1;
            waves.RestoreSave(new WaveSaveData { number = 4, state = WaveState.Preparing, enemies = new MinionSaveData[0] });
            var expected = waves.NextFormation;
            var data = new UnityEditor.SerializedObject(waves); data.FindProperty("spawnInterval").floatValue = .05f; data.ApplyModifiedPropertiesWithoutUndo();
            waves.BeginNextWave(); var midway = JsonUtility.FromJson<WaveSaveData>(JsonUtility.ToJson(waves.CaptureSave()));
            waves.RestoreSave(midway);
            yield return new WaitForSeconds(.7f);
            var saved = Find<GameSaveSession>().Capture();
            Assert.AreEqual(0, saved.wave.pending);
            CollectionAssert.AreEqual(expected, saved.wave.enemies.Select(e => e.tactics.role).ToArray());
            SaveSlotStore.Validate(saved);
            saved.wave.enemies[0].tactics.role = (InvaderRole)99;
            Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(saved));
            saved.wave.enemies[0].tactics.role = InvaderRole.Shieldbearer;
            saved.wave.pendingRoles = new[] {InvaderRole.Charger};
            Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(saved));
        }
    }
}
#endif
