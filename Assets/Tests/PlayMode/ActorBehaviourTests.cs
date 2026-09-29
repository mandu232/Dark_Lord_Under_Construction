using System.Collections;
using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Minions;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DarkLord.Tests
{
    public sealed class ActorBehaviourTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private Keyboard testKeyboard;
        private InputSettings originalInputSettings;
        private InputSettings testInputSettings;
        private HideFlags originalSettingsFlags;

        private GameObject Actor(Vector2 position, Faction faction, bool minion = false)
        {
            var go = new GameObject("Test actor");
            objects.Add(go);
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().radius = 0.24f;
            go.AddComponent<ActorHealth>().Configure(faction, 100);
            go.AddComponent<TopDownMotor>();
            if (minion) go.AddComponent<MinionController>();
            return go;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
            objects.Clear();
            if (testKeyboard != null) InputSystem.RemoveDevice(testKeyboard);
            testKeyboard = null;
            if (originalInputSettings != null)
            {
                InputSystem.settings = originalInputSettings;
                originalInputSettings.hideFlags = originalSettingsFlags;
            }
            if (testInputSettings != null) Object.DestroyImmediate(testInputSettings);
            originalInputSettings = null;
            testInputSettings = null;
            Time.timeScale = 1;
        }

        [UnityTest]
        public IEnumerator PlayerUsesProjectMoveActionAndStopsWhenReleased()
        {
            Assert.IsNotNull(InputSystem.actions, "Project-wide input action asset must be assigned.");
            // Synthetic keyboard input must not depend on which editor tab has focus.
            originalInputSettings = InputSystem.settings;
            // Input System 1.20 destroys HideAndDontSave settings when replacing them.
            // Retain the original until teardown can restore it for the next test.
            originalSettingsFlags = originalInputSettings.hideFlags;
            originalInputSettings.hideFlags = HideFlags.None;
            testInputSettings = Object.Instantiate(originalInputSettings);
            testInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            testInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = testInputSettings;
            var player = Actor(Vector2.zero, Faction.Dungeon);
            player.AddComponent<PlayerMovement>();
            testKeyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.W, Key.D));
            InputSystem.Update();
            yield return new WaitForSeconds(0.15f);
            Assert.That(player.transform.position.x, Is.GreaterThan(0.1f));
            Assert.That(player.transform.position.y, Is.GreaterThan(0.1f));
            Assert.That(player.GetComponent<TopDownMotor>().Velocity.magnitude, Is.EqualTo(3).Within(0.01f));
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            InputSystem.Update();
            yield return new WaitForSeconds(0.1f);
            Assert.That(player.GetComponent<TopDownMotor>().Velocity.magnitude, Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator DiagonalMovementIsNormalizedAndDisableStops()
        {
            var motor = Actor(Vector2.zero, Faction.Dungeon).GetComponent<TopDownMotor>();
            motor.Move(Vector2.one);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(motor.MoveSpeed).Within(0.01f));
            motor.enabled = false;
            Assert.That(motor.Velocity, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void FriendlyFireIsIgnoredAndDeathHappensOnce()
        {
            var a = Actor(Vector2.zero, Faction.Dungeon).GetComponent<ActorHealth>();
            var ally = Actor(Vector2.right, Faction.Dungeon).GetComponent<ActorHealth>();
            int deaths = 0;
            a.Died += () => deaths++;
            Assert.IsFalse(a.TakeDamage(10, ally));
            Assert.That(a.CurrentHealth, Is.EqualTo(100));
            a.TakeDamage(100);
            a.TakeDamage(100);
            Assert.That(deaths, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MovementCannotPassThroughWall()
        {
            var motor = Actor(Vector2.zero, Faction.Dungeon).GetComponent<TopDownMotor>();
            var wall = new GameObject("Test wall");
            objects.Add(wall);
            wall.transform.position = Vector2.right;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 4);
            motor.Move(Vector2.right);
            yield return new WaitForSeconds(0.5f);
            Assert.That(motor.transform.position.x, Is.LessThan(0.55f));
            Assert.That(motor.transform.position.x, Is.GreaterThan(0.3f));
        }

        [UnityTest]
        public IEnumerator TargetLeavingRangeBeforeImpactTakesNoDamage()
        {
            var guard = Actor(Vector2.zero, Faction.Dungeon, true).GetComponent<MinionController>();
            var enemy = Actor(new Vector2(0.7f, 0), Faction.Invader).GetComponent<ActorHealth>();
            yield return new WaitForSeconds(0.05f);
            Assert.That(guard.State, Is.EqualTo(ActorState.Attack));
            enemy.transform.position = new Vector2(4, 0);
            yield return new WaitForSeconds(0.25f);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator GuardAttacksEnemyWithoutAnimationClips()
        {
            var guard = Actor(Vector2.zero, Faction.Dungeon, true).GetComponent<MinionController>();
            var enemy = Actor(new Vector2(0.7f, 0), Faction.Invader).GetComponent<ActorHealth>();
            yield return new WaitForSeconds(0.4f);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(90));
            Assert.That(guard.State, Is.EqualTo(ActorState.Attack));
        }

        [UnityTest]
        public IEnumerator HitInterruptsPendingAttackAndDeathStopsCommands()
        {
            var guard = Actor(Vector2.zero, Faction.Dungeon, true).GetComponent<MinionController>();
            var enemy = Actor(new Vector2(0.7f, 0), Faction.Invader).GetComponent<ActorHealth>();
            yield return null;
            guard.GetComponent<ActorHealth>().TakeDamage(1, enemy);
            Assert.That(guard.State, Is.EqualTo(ActorState.Hit));
            yield return new WaitForSeconds(0.3f);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(100));
            guard.GetComponent<ActorHealth>().TakeDamage(100, enemy);
            Assert.That(guard.State, Is.EqualTo(ActorState.Die));
            guard.Defend(Vector2.one * 10);
            guard.Rally(enemy.transform, Vector2.zero);
            yield return new WaitForFixedUpdate();
            Assert.That(guard.GetComponent<TopDownMotor>().Velocity, Is.EqualTo(Vector2.zero));
            Assert.IsFalse(guard.GetComponent<Collider2D>().enabled);
        }

        [UnityTest]
        public IEnumerator RallyAndReturnPreserveDefencePost()
        {
            var guard = Actor(Vector2.zero, Faction.Dungeon, true).GetComponent<MinionController>();
            var leader = Actor(new Vector2(2, 0), Faction.Dungeon);
            guard.Rally(leader.transform, new Vector2(-0.7f, 0));
            yield return new WaitForSeconds(0.65f);
            Assert.That(guard.transform.position.x, Is.GreaterThan(0.7f));
            guard.ReturnToPost();
            yield return new WaitForSeconds(0.65f);
            Assert.That(Vector2.Distance(guard.transform.position, Vector2.zero), Is.LessThan(0.2f));
            Assert.That(guard.Order, Is.EqualTo(MinionOrder.Defend));
        }

        [UnityTest]
        public IEnumerator InvaderIgnoresNearbyDefendersAndPursuesDistantCore()
        {
            var invader = Actor(Vector2.zero, Faction.Invader, true).GetComponent<MinionController>();
            var core = Actor(new Vector2(12, 0), Faction.Dungeon).GetComponent<ActorHealth>();
            var player = Actor(new Vector2(0, 0.7f), Faction.Dungeon).GetComponent<ActorHealth>();
            invader.Assault(core);
            yield return new WaitForSeconds(0.25f);
            Assert.That(invader.Target, Is.SameAs(core));
            Assert.That(invader.transform.position.x, Is.GreaterThan(0.1f));
            Assert.That(player.CurrentHealth, Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator InvaderRetaliatesAgainstLatestAttackerThenReturnsToCore()
        {
            var invader = Actor(Vector2.zero, Faction.Invader, true).GetComponent<MinionController>();
            var core = Actor(new Vector2(12, 0), Faction.Dungeon).GetComponent<ActorHealth>();
            var player = Actor(new Vector2(0, 1), Faction.Dungeon).GetComponent<ActorHealth>();
            var minion = Actor(new Vector2(0, -1), Faction.Dungeon).GetComponent<ActorHealth>();
            invader.Assault(core);
            invader.GetComponent<ActorHealth>().TakeDamage(1, player);
            Assert.That(invader.Target, Is.SameAs(player), "Retaliation must work outside the core's defence radius.");
            invader.GetComponent<ActorHealth>().TakeDamage(1, minion);
            Assert.That(invader.Target, Is.SameAs(minion));
            minion.TakeDamage(100);
            yield return new WaitForSeconds(0.3f);
            Assert.That(invader.Target, Is.SameAs(core));
        }

        [UnityTest]
        public IEnumerator InvaderReturnsToCoreWhenAttackerIsDisabledOrDestroyed()
        {
            var invader = Actor(Vector2.zero, Faction.Invader, true).GetComponent<MinionController>();
            var core = Actor(new Vector2(12, 0), Faction.Dungeon).GetComponent<ActorHealth>();
            var attacker = Actor(Vector2.up, Faction.Dungeon).GetComponent<ActorHealth>();
            invader.Assault(core);
            invader.GetComponent<ActorHealth>().TakeDamage(1, attacker);
            attacker.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.3f);
            Assert.That(invader.Target, Is.SameAs(core));
            attacker.gameObject.SetActive(true);
            invader.GetComponent<ActorHealth>().TakeDamage(1, attacker);
            Object.Destroy(attacker.gameObject);
            yield return new WaitForSeconds(0.3f);
            Assert.That(invader.Target, Is.SameAs(core));
        }

        [UnityTest]
        public IEnumerator InvaderDamagesCoreAndDoesNotAcquireBystandersAfterCoreDies()
        {
            var invader = Actor(Vector2.zero, Faction.Invader, true).GetComponent<MinionController>();
            var core = Actor(new Vector2(0.7f, 0), Faction.Dungeon).GetComponent<ActorHealth>();
            var player = Actor(new Vector2(0, 0.7f), Faction.Dungeon).GetComponent<ActorHealth>();
            invader.Assault(core);
            yield return new WaitForSeconds(0.4f);
            Assert.That(core.CurrentHealth, Is.EqualTo(90));
            Assert.That(player.CurrentHealth, Is.EqualTo(100));
            core.TakeDamage(100);
            yield return new WaitForSeconds(0.4f);
            Assert.That(invader.Target, Is.Null);
            Assert.That(invader.GetComponent<TopDownMotor>().Velocity, Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator GuardDoesNotChaseOutsideAssignedArea()
        {
            var guard = Actor(Vector2.zero, Faction.Dungeon, true).GetComponent<MinionController>();
            var enemy = Actor(new Vector2(3, 0), Faction.Invader);
            yield return new WaitForSeconds(0.25f);
            Assert.IsNotNull(guard.Target);
            enemy.transform.position = new Vector2(7, 0);
            yield return new WaitForSeconds(0.4f);
            Assert.IsNull(guard.Target);
            Assert.That(guard.transform.position.x, Is.LessThan(1));
        }
    }
}
