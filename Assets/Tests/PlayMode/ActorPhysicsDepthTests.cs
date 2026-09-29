using System.Collections;
using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class ActorPhysicsDepthTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        private TopDownMotor Actor(Vector2 position)
        {
            var go = new GameObject("Physics depth test actor");
            objects.Add(go);
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().radius = 0.24f;
            go.AddComponent<ActorHealth>();
            return go.AddComponent<TopDownMotor>();
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
            objects.Clear();
        }

        [UnityTest]
        public IEnumerator HostileActorsBlockWithoutPushingAndCanRetreat()
        {
            var moving = Actor(new Vector2(1000, 1000));
            moving.GetComponent<ActorHealth>().Configure(Faction.Invader, 100);
            var defender = Actor(new Vector2(1001, 1000));
            Vector3 origin = defender.transform.position;
            moving.Move(Vector2.right);
            yield return new WaitForSeconds(0.7f);
            Assert.That(Vector3.Distance(defender.transform.position, origin), Is.LessThan(0.01f));
            Assert.That(defender.transform.position.x - moving.transform.position.x, Is.GreaterThanOrEqualTo(0.50f));
            Assert.That(moving.BlockedBy, Is.EqualTo(defender.GetComponent<ActorHealth>()));
            moving.Move(Vector2.left);
            yield return new WaitForSeconds(0.2f);
            Assert.That(defender.transform.position.x - moving.transform.position.x, Is.GreaterThan(0.9f));
        }

        [UnityTest]
        public IEnumerator CombatantsStopAtAttackRangeAndDealDamageWithoutContact()
        {
            var invader = Actor(new Vector2(1000, 1000));
            var guard = Actor(new Vector2(1002, 1000));
            var enemyHealth = invader.GetComponent<ActorHealth>();
            enemyHealth.Configure(Faction.Invader, 1000);
            var guardHealth = guard.GetComponent<ActorHealth>();
            guardHealth.Configure(Faction.Dungeon, 1000);
            var enemyAI = invader.gameObject.AddComponent<MinionController>();
            guard.gameObject.AddComponent<MinionController>();
            enemyAI.Assault(guardHealth);
            float deadline = Time.time + 2;
            while (Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(Vector2.Distance(invader.transform.position, guard.transform.position), Is.GreaterThan(0.6f));
            }
            // A hit cancels the opponent's windup, so simultaneous attacks need not both land.
            Assert.That(guardHealth.CurrentHealth + enemyHealth.CurrentHealth, Is.LessThan(2000));
            Assert.That(Vector2.Distance(invader.transform.position, guard.transform.position), Is.InRange(0.7f, 0.86f));
            Assert.That(invader.Velocity.magnitude, Is.LessThan(0.01f));
            Assert.That(guard.Velocity.magnitude, Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator AssaultAttacksBlockingDefenderInsteadOfPassingThrough()
        {
            var invader = Actor(new Vector2(1000, 1000));
            invader.GetComponent<ActorHealth>().Configure(Faction.Invader, 1000);
            var blocker = Actor(new Vector2(1001.5f, 1000));
            var core = new GameObject("Blocked assault test core");
            objects.Add(core);
            core.transform.position = new Vector3(1005, 1000);
            var objective = core.AddComponent<ActorHealth>();
            var ai = invader.gameObject.AddComponent<MinionController>();
            ai.Assault(objective);
            yield return new WaitForSeconds(1.2f);
            Assert.That(ai.Target, Is.EqualTo(blocker.GetComponent<ActorHealth>()));
            Assert.That(blocker.GetComponent<ActorHealth>().CurrentHealth, Is.LessThan(100));
            Assert.That(invader.transform.position.x, Is.LessThan(blocker.transform.position.x - 0.5f));
            Assert.That(objective.CurrentHealth, Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator MovingActorDoesNotPushStationaryActorButStillHitsWalls()
        {
            var moving = Actor(new Vector2(1000, 1000));
            var stationary = Actor(new Vector2(1000.55f, 1000));
            Vector3 original = stationary.transform.position;
            var wall = new GameObject("Test solid wall");
            objects.Add(wall);
            wall.transform.position = new Vector3(1002, 1000);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.3f, 3);
            Physics2D.SyncTransforms();
            moving.Move(Vector2.right);
            yield return new WaitForSeconds(0.8f);
            Assert.That(Vector3.Distance(stationary.transform.position, original), Is.LessThan(0.01f));
            Assert.That(moving.transform.position.x, Is.GreaterThan(1001));
            Assert.That(moving.transform.position.x, Is.LessThan(1001.8f));
            Assert.That(Physics2D.OverlapPoint(stationary.transform.position), Is.EqualTo(stationary.GetComponent<Collider2D>()), "Actor colliders must remain visible to targeting/placement queries.");
        }

        [UnityTest]
        public IEnumerator SpawnAndReactivationKeepActorContactsDisabled()
        {
            var first = Actor(new Vector2(1000, 1000));
            yield return null;
            var second = Actor(new Vector2(1000, 1000));
            var a = first.GetComponent<Collider2D>();
            var b = second.GetComponent<Collider2D>();
            Assert.IsTrue(Physics2D.GetIgnoreCollision(a, b));
            second.gameObject.SetActive(false);
            second.gameObject.SetActive(true);
            yield return null;
            Assert.IsTrue(Physics2D.GetIgnoreCollision(a, b));
            Assert.That(Vector3.Distance(first.transform.position, second.transform.position), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator LowerActorRendersInFrontAndSwapsOrderWhenCrossing()
        {
            var above = Actor(new Vector2(100, 10));
            var below = Actor(new Vector2(100, 9));
            yield return null;
            var back = above.GetComponent<SortingGroup>();
            var front = below.GetComponent<SortingGroup>();
            Assert.That(front.sortingOrder, Is.GreaterThan(back.sortingOrder));
            above.GetComponent<Rigidbody2D>().position = new Vector2(100, 8);
            // Let the interpolated Rigidbody publish its new render position.
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(back.sortingOrder, Is.GreaterThan(front.sortingOrder));
            Assert.That(front.sortingOrder, Is.GreaterThan(short.MinValue));
        }
    }
}
