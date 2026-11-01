using System.Collections;
using DarkLord.Actors;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class PlayerHitStopTests
    {
        private Scene scene;
        private PlayerCombat player;
        private ActorHealth health;
        private float previousScale;

        [SetUp]
        public void Setup()
        {
            previousScale = Time.timeScale; PlayerHitStop.CancelActive(); Time.timeScale = 1;
            scene = SceneManager.CreateScene("Hit stop validation");
            var go = new GameObject("Hit stop player"); SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = new Vector3(1000, 1000);
            health = go.AddComponent<ActorHealth>(); health.Configure(Faction.Dungeon, 1000);
            player = go.AddComponent<PlayerCombat>();
        }

        private ActorHealth Enemy(int index = 0, int hp = 1000)
        {
            var go = new GameObject("Hit stop enemy"); SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = player.transform.position + new Vector3(1 + index % 4 * .3f, index / 4 * .25f);
            go.AddComponent<CircleCollider2D>().radius = .1f;
            var actor = go.AddComponent<ActorHealth>(); actor.Configure(Faction.Invader, hp);
            return actor;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            PlayerHitStop.CancelActive(); Time.timeScale = 1;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousScale;
        }

        [Test]
        public void MissedSpellAndBlockedDamageDoNotStopTime()
        {
            Assert.IsTrue(player.TryNova()); Assert.AreEqual(1, Time.timeScale);
            var enemy = Enemy(); Assert.IsTrue(player.TryWard());
            Assert.IsFalse(health.TakeDamage(20, enemy)); Assert.AreEqual(1, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator RealNovaHitsEveryTargetButProducesOneShortPulse()
        {
            var enemies = new ActorHealth[20]; for (int i = 0; i < enemies.Length; i++) enemies[i] = Enemy(i);
            Physics2D.SyncTransforms();
            Assert.IsTrue(player.TryNova());
            foreach (var enemy in enemies) Assert.AreEqual(955, enemy.CurrentHealth);
            Assert.That(Time.timeScale, Is.EqualTo(.03f).Within(.0001f));
            yield return new WaitForSecondsRealtime(.11f);
            Assert.AreEqual(1, Time.timeScale, "An AoE must not queue a pause for each victim.");
            PlayerHitStop.OnHit(player, enemies[0], PlayerHitStop.ImpactKind.Heavy);
            Assert.AreEqual(1, Time.timeScale, "Rapid hits have a short real-time refractory period.");
        }

        [Test]
        public void PassivePlayerDamageAndNpcCombatDoNotFreezeTheWorld()
        {
            var enemy = Enemy(); enemy.TakeDamage(10, health);
            Assert.AreEqual(1, Time.timeScale);
            var npc = new GameObject("Dungeon minion"); SceneManager.MoveGameObjectToScene(npc, scene);
            var ally = npc.AddComponent<ActorHealth>(); ally.Configure(Faction.Dungeon, 100);
            enemy.TakeDamage(10, ally); Assert.AreEqual(1, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator IncomingDamageRestoresThePreviousSlowMotionSpeed()
        {
            var enemy = Enemy(); Time.timeScale = .5f;
            Assert.IsTrue(health.TakeDamage(20, enemy));
            Assert.That(Time.timeScale, Is.EqualTo(.015f).Within(.0001f));
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(.5f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator PauseDuringImpactIsNeverOverwritten()
        {
            health.TakeDamage(20, Enemy()); Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(0, Time.timeScale);
            Time.timeScale = 1; yield return null; Assert.AreEqual(1, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator ExternalTimeChangeAndDisabledCombatKeepTheirClockOwnership()
        {
            health.TakeDamage(20, Enemy()); Time.timeScale = .65f;
            yield return null; Assert.AreEqual(.65f, Time.timeScale);
            PlayerHitStop.CancelActive(); Time.timeScale = 1;
            yield return new WaitForSecondsRealtime(.16f);
            health.TakeDamage(20, Enemy(1)); Assert.Less(Time.timeScale, .1f);
            player.enabled = false; Assert.AreEqual(1, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator KillingManyTargetsInOneFrameCannotAccumulateFreezeTime()
        {
            var target = Enemy();
            PlayerHitStop.OnHit(player, target, PlayerHitStop.ImpactKind.Light);
            target.Kill();
            for (int i = 0; i < 30; i++) PlayerHitStop.OnHit(player, target, PlayerHitStop.ImpactKind.Heavy);
            Assert.Less(Time.timeScale, .1f);
            yield return new WaitForSecondsRealtime(.11f);
            Assert.AreEqual(1, Time.timeScale);
        }

        [Test]
        public void DisabledHitStopComponentDoesNotTakeOwnershipOfTime()
        {
            player.gameObject.AddComponent<PlayerHitStop>().enabled = false;
            health.TakeDamage(20, Enemy()); Assert.AreEqual(1, Time.timeScale);
        }

        [Test]
        public void KillingBlowRespectsARewardPauseSetByDeathListeners()
        {
            var enemy = Enemy(0, 10); enemy.Died += () => Time.timeScale = 0;
            Physics2D.SyncTransforms(); Assert.IsTrue(player.TryNova());
            Assert.IsFalse(enemy.IsAlive); Assert.AreEqual(0, Time.timeScale);
            PlayerHitStop.CancelActive(); Assert.AreEqual(0, Time.timeScale);
        }
    }
}
