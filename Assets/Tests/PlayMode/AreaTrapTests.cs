#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class AreaTrapTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private Scene scene;
        private float previousTime;
        private bool previousAudio;
        private static readonly Vector3 Center = new Vector3(700, 700, 0);
        [SetUp] public void Setup() { previousTime = Time.timeScale; previousAudio = AudioListener.pause; Time.timeScale = 1; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var obj in objects) if (obj != null) Object.DestroyImmediate(obj);
            objects.Clear();
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousTime; AudioListener.pause = previousAudio;
        }
        private SpikeTrap Trap(string key, float interval = .25f)
        {
            var obj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/" + key + ".prefab"), Center, Quaternion.identity);
            objects.Add(obj);
            var trap = obj.GetComponent<SpikeTrap>();
            var data = new SerializedObject(trap); data.FindProperty("cooldown").floatValue = interval; data.ApplyModifiedPropertiesWithoutUndo();
            return trap;
        }
        private ActorHealth Actor(Vector3 position, Faction faction = Faction.Invader, bool motor = false)
        {
            var obj = new GameObject("Trap test actor"); objects.Add(obj); obj.transform.position = position;
            var health = obj.AddComponent<ActorHealth>(); health.Configure(faction, 100);
            obj.AddComponent<CircleCollider2D>().radius = .12f;
            if (motor) obj.AddComponent<TopDownMotor>();
            return health;
        }
        [UnityTest] public IEnumerator OneClockHitsEveryOccupantAndNewArrivalsWait()
        {
            var trap = Trap("SpikeTrap");
            var first = Actor(Center);
            var extra = new GameObject("Second hitbox"); extra.transform.SetParent(first.transform, false); extra.AddComponent<BoxCollider2D>().size = Vector2.one * .2f;
            var ally = Actor(Center, Faction.Dungeon);
            var outside = Actor(Center + Vector3.right * 3);
            var crowd = Enumerable.Range(0, 24).Select(_ => Actor(Center)).ToArray();
            Physics2D.SyncTransforms();
            Assert.AreEqual(25, trap.TryPulse());
            Assert.AreEqual(88, first.CurrentHealth, "Multiple colliders never multiply damage.");
            Assert.IsTrue(crowd.All(a => a.CurrentHealth == 88), "The overlap buffer must include the whole crowd.");
            Assert.AreEqual(100, ally.CurrentHealth); Assert.AreEqual(100, outside.CurrentHealth);
            var late = Actor(Center); Physics2D.SyncTransforms();
            Assert.AreEqual(0, trap.TryPulse()); Assert.AreEqual(100, late.CurrentHealth);
            first.transform.position += Vector3.right * 3; Physics2D.SyncTransforms();
            first.transform.position = Center; Physics2D.SyncTransforms();
            Assert.AreEqual(0, trap.TryPulse(), "Leaving and re-entering cannot reset the trap clock.");
            yield return new WaitForSeconds(.28f);
            Assert.AreEqual(76, first.CurrentHealth); Assert.AreEqual(88, late.CurrentHealth);
        }
        [Test] public void EachTrapHasItsOwnClockAndPausedTrapsCannotPulse()
        {
            var first = Trap("SpikeTrap", .25f); var second = Trap("SpikeTrap", .5f); var enemy = Actor(Center);
            Physics2D.SyncTransforms();
            Assert.AreEqual(4, first.AttackSpeed); Assert.AreEqual(2, second.AttackSpeed);
            Assert.AreEqual(1, first.TryPulse()); Assert.AreEqual(1, second.TryPulse()); Assert.AreEqual(76, enemy.CurrentHealth);
            Time.timeScale = 0; first.RestoreCooldown(0); Assert.AreEqual(0, first.TryPulse()); Assert.AreEqual(76, enemy.CurrentHealth);
        }
        [UnityTest] public IEnumerator StickyPulseSlowsAllEnemiesAndBothMovementModesThenExpires()
        {
            var trap = Trap("StickyTrap");
            var data = new SerializedObject(trap); data.FindProperty("slowDuration").floatValue = .3f; data.ApplyModifiedPropertiesWithoutUndo();
            var first = Actor(Center, Faction.Invader, true); var second = Actor(Center + Vector3.right * .3f, Faction.Invader, true);
            var ally = Actor(Center, Faction.Dungeon, true);
            Physics2D.SyncTransforms(); Assert.AreEqual(2, trap.TryPulse());
            trap.enabled = false; // Applied debuffs linger for their duration after leaving/removing a trap.
            var motor = first.GetComponent<TopDownMotor>(); var other = second.GetComponent<TopDownMotor>();
            Assert.AreEqual(.5f, motor.SlowMultiplier); Assert.AreEqual(.5f, other.SlowMultiplier);
            Assert.AreEqual(1, ally.GetComponent<TopDownMotor>().SlowMultiplier);
            Assert.AreEqual(100, first.CurrentHealth); Assert.AreEqual(100, second.CurrentHealth);
            ally.gameObject.SetActive(false); // Test movement without the opposing actor's body blocking the path.
            motor.Move(Vector2.right);
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(motor.MoveSpeed * .5f).Within(.02f));
            var target = new GameObject("Approach destination"); objects.Add(target); target.transform.position = Center + Vector3.up * 10;
            motor.Approach(target.transform, .1f);
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(motor.MoveSpeed * .5f).Within(.02f));
            Time.timeScale = 0; yield return new WaitForSecondsRealtime(.35f); Assert.AreEqual(.5f, motor.SlowMultiplier);
            Time.timeScale = 1; yield return new WaitForSeconds(.32f); Assert.AreEqual(1, motor.SlowMultiplier);
        }
        [UnityTest] public IEnumerator StrongestSlowWinsWithoutWeakHitsExtendingItAndDisableClearsEffects()
        {
            var motor = Actor(Center, Faction.Invader, true).GetComponent<TopDownMotor>();
            motor.ApplySlow(.5f, .6f); motor.ApplySlow(.25f, .15f); motor.ApplySlow(.5f, .6f);
            Assert.AreEqual(.25f, motor.SlowMultiplier);
            yield return new WaitForSeconds(.18f); Assert.AreEqual(.5f, motor.SlowMultiplier);
            motor.gameObject.SetActive(false); motor.gameObject.SetActive(true); Assert.AreEqual(1, motor.SlowMultiplier);
        }
        [UnityTest] public IEnumerator CatalogAndSaveRestoreKeepTrapClockAndEnemySlows()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null; Time.timeScale = 1;
            T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
            var grid = Find<DungeonGrid>(); var session = Find<GameSaveSession>(); var wave = Find<WaveDirector>();
            var panel = Find<ConstructionPanel>(); var expansion = Find<DungeonExpansion>();
            expansion.SetExpansionMode(true); panel.ShowCategory(BuildCategory.Traps);
            var card = panel.GetComponentsInChildren<BuildItemButton>().Single(c => c.Item.name == "StickyTrap");
            Assert.IsTrue(grid.TryPlaceTrap(grid.Layout.Core + Vector2Int.up, card.Item.prefab));
            expansion.SetExpansionMode(false);
            var trap = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SpikeTrap>()).Single();
            trap.RestoreCooldown(.8f); wave.BeginNextWave();
            var enemy = wave.GetComponentInChildren<DarkLord.Minions.MinionController>(); Assert.IsNotNull(enemy);
            enemy.GetComponent<TopDownMotor>().ApplySlow(.5f, 1.5f);
            var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            SaveSlotStore.Validate(saved); Assert.That(saved.dungeon.traps.Single().cooldownRemaining, Is.EqualTo(.8f).Within(.02f));
            saved.dungeon.traps[0].cooldownRemaining = float.NaN;
            Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(saved));
            saved.dungeon.traps[0].cooldownRemaining = .8f;
            saved.wave.enemies[0].slows[0].remaining = float.PositiveInfinity;
            Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(saved));
            saved.wave.enemies[0].slows[0].remaining = 1.5f;
            session.Restore(saved);
            var restored = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SpikeTrap>()).Single(t => t.gameObject.activeInHierarchy);
            Assert.That(restored.CooldownRemaining, Is.EqualTo(.8f).Within(.02f));
            Assert.AreEqual(.5f, wave.GetComponentsInChildren<DarkLord.Minions.MinionController>().Single(e => e.gameObject.activeInHierarchy).GetComponent<TopDownMotor>().SlowMultiplier);
            saved.dungeon.traps[0].cooldownRemaining = 0; saved.wave.enemies[0].slows = null;
            session.Restore(saved); // Legacy saves omit these optional fields.
            Assert.AreEqual(1, wave.GetComponentsInChildren<DarkLord.Minions.MinionController>().Single(e => e.gameObject.activeInHierarchy).GetComponent<TopDownMotor>().SlowMultiplier);
        }
    }
}
#endif
