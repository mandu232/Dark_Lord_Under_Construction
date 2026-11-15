#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class LateGamePerformanceTests
    {
        [UnityTest]
        public IEnumerator RestingOrbsOfOneKindMergeAndPickupGrantsTheTotal()
        {
            var player = new GameObject("Orb collector"); var root = new GameObject("Drops");
            player.transform.position = Vector3.right * 40;
            try
            {
                var health = player.AddComponent<ActorHealth>(); health.Configure(Faction.Dungeon, 100);
                DeathLoot.Restore(new[]
                {
                    new LootSaveData { position = Vector3.zero, kind = 3, amount = 10 },
                    new LootSaveData { position = new Vector3(.3f, 0), kind = 3, amount = 10 },
                    new LootSaveData { position = new Vector3(0, .3f), kind = 3, amount = 10 },
                    new LootSaveData { position = new Vector3(.1f, .1f), kind = 0, amount = 2 }, // another kind stays apart
                    new LootSaveData { position = new Vector3(8, 0), kind = 3, amount = 4 },     // too far to join
                }, player.transform, root.transform);
                yield return null; yield return null;

                var saved = DeathLoot.Capture(player.transform);
                var experience = saved.Where(d => d.kind == 3).Select(d => d.amount).OrderBy(a => a).ToArray();
                CollectionAssert.AreEqual(new[] { 4, 30 }, experience, "The three close orbs became one orb of 30.");
                Assert.AreEqual(2, saved.Single(d => d.kind == 0).amount);
                Assert.AreEqual(3, root.GetComponentsInChildren<DeathLoot>().Length);

                // A merged orb survives a save round trip with its whole amount.
                DeathLoot.Restore(saved, player.transform, root.transform);
                yield return null; yield return null;
                CollectionAssert.AreEquivalent(saved.Select(d => (d.kind, d.amount)),
                    DeathLoot.Capture(player.transform).Select(d => (d.kind, d.amount)));

                // Picking the merged orb up grants all 30 experience (one level), not one orb's 10.
                var merged = DeathLoot.Capture(player.transform).Single(d => d.kind == 3 && d.amount == 30);
                player.transform.position = merged.position;
                for (int i = 0; i < 10 && health.Level < 2; i++) yield return null;
                Assert.AreEqual(2, health.Level); Assert.AreEqual(0, health.Experience);
                Assert.IsFalse(DeathLoot.Capture(player.transform).Any(d => d.kind == 3 && d.amount == 30));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(player); }
        }

        [UnityTest]
        public IEnumerator ScatteredDropsCollapseIntoFewOrbs()
        {
            var player = new GameObject("Orb collector"); var root = new GameObject("Drops");
            player.transform.position = Vector3.right * 40;
            var random = Random.state;
            try
            {
                var health = player.AddComponent<ActorHealth>(); health.Configure(Faction.Dungeon, 100);
                Random.InitState(91);
                for (int i = 0; i < 30; i++) DeathLoot.Scatter(Vector3.zero, player.transform, root.transform);
                var before = DeathLoot.Capture(player.transform);
                yield return new WaitForSeconds(.8f); // every orb has landed
                var after = DeathLoot.Capture(player.transform);
                Assert.Less(after.Length, before.Length / 2, "Orbs from one spot merge instead of piling up.");
                foreach (int kind in new[] { 0, 1, 2, 3 })
                    Assert.AreEqual(before.Where(d => d.kind == kind).Sum(d => d.amount),
                        after.Where(d => d.kind == kind).Sum(d => d.amount), "Merging never loses or adds loot.");
            }
            finally { Random.state = random; Object.DestroyImmediate(root); Object.DestroyImmediate(player); }
        }

        [UnityTest]
        public IEnumerator OffscreenActorsPauseTheirAnimatorAndResumeWhenSeen()
        {
            var cameraNode = new GameObject("Culling camera") { tag = "MainCamera" };
            var actor = new GameObject("Culled actor");
            var controller = new AnimatorController();
            var untagged = new List<Camera>();
            try
            {
                var camera = cameraNode.AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 5;
                cameraNode.transform.position = new Vector3(0, 0, -10);
                foreach (var other in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if (other != camera && other.CompareTag("MainCamera")) { other.tag = "Untagged"; untagged.Add(other); }
                controller.AddLayer("Base Layer");
                var machine = controller.layers[0].stateMachine;
                machine.AddState("Idle"); machine.AddState("Run");

                var renderer = actor.AddComponent<SpriteRenderer>();
                var animator = actor.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                var view = actor.AddComponent<ActorAnimation>();
                Vector3 visible = camera.transform.position + Vector3.forward * 10;
                actor.transform.position = visible;
                yield return null;
                Assert.IsFalse(view.IsCulled); Assert.IsTrue(animator.enabled);

                actor.transform.position = visible + Vector3.right * (camera.orthographicSize * camera.aspect + ActorAnimation.CullMargin + 3);
                yield return null;
                Assert.IsTrue(view.IsCulled, "An actor outside the view and its margin is culled.");
                Assert.IsFalse(animator.enabled, "The Animator stops evaluating offscreen.");

                // A request made offscreen still turns the sprite at once and plays when seen again.
                view.Present(ActorState.Run, Vector2.left);
                Assert.IsTrue(renderer.flipX);
                Assert.AreEqual(ActorState.Run, view.LastRequested);
                yield return null;
                Assert.IsFalse(animator.enabled);

                actor.transform.position = visible + Vector3.right * (camera.orthographicSize * camera.aspect + ActorAnimation.CullMargin * .5f);
                yield return null;
                Assert.IsFalse(view.IsCulled, "The margin keeps actors at the screen edge animating.");
                Assert.IsTrue(animator.enabled);
                yield return null;
                Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Run"), "The offscreen request plays once visible.");
            }
            finally
            {
                Object.DestroyImmediate(actor); Object.DestroyImmediate(cameraNode); Object.DestroyImmediate(controller);
                foreach (var other in untagged) if (other != null) other.tag = "MainCamera";
            }
        }

        [Test]
        public void SharedPathTreeMatchesSingleSearches()
        {
            var layout = new DungeonLayout(31, 21);
            var construction = new DungeonConstruction(layout);
            var floor = new List<Vector2Int>();
            for (int y = 0; y < layout.Height; y++)
                for (int x = 0; x < layout.Width; x++)
                    if (construction.IsWalkable(new Vector2Int(x, y))) floor.Add(new Vector2Int(x, y));
            var random = new System.Random(5);
            var closed = new HashSet<Vector2Int>(floor.Where(_ => random.NextDouble() < .2));
            System.Predicate<Vector2Int> blocked = closed.Contains;
            var single = new List<Vector2Int>(); var shared = new List<Vector2Int>();
            foreach (var start in floor.Where((_, i) => i % 7 == 0))
                foreach (var room in new DungeonSection[] { null, construction.Sections[0] })
                {
                    var destinations = new HashSet<Vector2Int>(floor.Where((_, i) => i % 3 == 0));
                    construction.BuildPathTree(start, room, blocked, destinations);
                    foreach (var destination in destinations)
                    {
                        bool found = construction.FindPath(start, destination, single, room, blocked);
                        Assert.AreEqual(found, construction.PathTo(destination, shared), $"{start} -> {destination}");
                        CollectionAssert.AreEqual(single, shared, $"{start} -> {destination}");
                    }
                }
        }
    }
}
#endif
