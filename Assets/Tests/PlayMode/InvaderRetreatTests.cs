#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("StarterDungeon")]
    public sealed class InvaderRetreatTests
    {
        private const int TerrainLayer = 8;
        private readonly List<GameObject> objects = new List<GameObject>();

        [UnityTest]
        public IEnumerator PartyThatLosesHalfItsMembersRunsOutThroughTheRift()
        {
            var grid = Grid();
            var entrance = grid.Layout.Entrance;
            var objective = Actor(grid, grid.Layout.Core, Faction.Dungeon, 100000);
            var party = new InvaderParty();
            int retreats = 0;
            party.Retreated += _ => retreats++;
            var members = new List<MinionController>();
            for (int i = 0; i < 3; i++)
            {
                var invader = Invader(Actor(grid, entrance + new Vector2Int(4 + i, i - 1), Faction.Invader, 1000));
                invader.Assault(objective, grid);
                party.Add(invader);
                members.Add(invader);
            }
            yield return null;
            Assert.IsFalse(party.Retreating);

            members[0].Health.Kill();
            yield return new WaitForSeconds(.4f);
            Assert.IsFalse(party.Retreating, "One fallen of three is not yet a rout.");

            members[1].Health.Kill();
            var survivor = members[2];
            bool escaped = false;
            survivor.Escaped += () => escaped = true;
            yield return new WaitForSeconds(.4f);
            Assert.IsTrue(party.Retreating, "Half the party down: the rest run.");
            Assert.IsTrue(survivor.IsRetreating);
            Assert.AreEqual(1, retreats);

            float deadline = Time.time + 10;
            while (!escaped && Time.time < deadline) yield return null;
            if (!escaped) Assert.Fail($"The survivor never reached the rift; it is at {grid.WorldToCell(survivor.transform.position)}, the rift at {entrance}.");
            yield return null;
            Assert.IsTrue(survivor == null, "An escaped invader leaves the dungeon.");
            Assert.AreEqual(1, retreats);
        }

        [UnityTest]
        public IEnumerator BadlyWoundedPartyRetreatsAndHeroPartyNeverDoes()
        {
            var grid = Grid();
            var objective = Actor(grid, grid.Layout.Core, Faction.Dungeon, 100000);
            var party = new InvaderParty();
            var members = new List<MinionController>();
            for (int i = 0; i < 2; i++)
            {
                var invader = Invader(Actor(grid, grid.Layout.Entrance + new Vector2Int(3, i), Faction.Invader, 1000));
                invader.Assault(objective, grid);
                party.Add(invader);
                members.Add(invader);
            }
            yield return null;
            foreach (var member in members) member.Health.RestoreHealth(1000, 200);
            yield return new WaitForSeconds(.4f);
            Assert.IsTrue(party.Retreating, "Worn down to a fifth of its strength, the party breaks.");

            var heroParty = new InvaderParty();
            var hero = Invader(Actor(grid, grid.Layout.Entrance + new Vector2Int(5, 0), Faction.Invader, 1000));
            var tactics = hero.gameObject.AddComponent<InvaderTactics>();
            tactics.SetRace(RegionCatalog.ForDay(1));
            tactics.Configure(InvaderRole.Warrior);
            tactics.SetHero(true);
            var follower = Invader(Actor(grid, grid.Layout.Entrance + new Vector2Int(5, 1), Faction.Invader, 1000));
            foreach (var member in new[] { hero, follower }) { member.Assault(objective, grid); heroParty.Add(member); }
            yield return null;
            follower.Health.Kill();
            hero.Health.RestoreHealth(1000, 50);
            yield return new WaitForSeconds(.5f);
            Assert.IsFalse(heroParty.Retreating, "A champion fights to the end.");
            Assert.IsFalse(hero.IsRetreating);
        }

        [UnityTest]
        public IEnumerator InvadersMarchAtTheirOwnPaceAndBoltWhenTheyBreak()
        {
            var grid = Grid();
            var objective = Actor(grid, grid.Layout.Core, Faction.Dungeon, 100000);
            var party = new InvaderParty();
            var members = new List<MinionController>();
            for (int i = 0; i < 2; i++)
            {
                var invader = Invader(Actor(grid, grid.Layout.Entrance + new Vector2Int(3, i), Faction.Invader, 1000));
                invader.Assault(objective, grid);
                party.Add(invader);
                members.Add(invader);
            }
            yield return new WaitForSeconds(.5f);
            var leader = party.Leader;
            float slowest = float.PositiveInfinity, fastest = 0;
            for (float end = Time.time + 1.5f; Time.time < end; )
            {
                Assert.LessOrEqual(leader.GaitPace, 1.01f, "A column on the march walks, it does not run.");
                slowest = Mathf.Min(slowest, leader.GaitPace);
                fastest = Mathf.Max(fastest, leader.GaitPace);
                yield return null;
            }
            Assert.GreaterOrEqual(fastest, .7f, "Marching, though warily, is still a brisk walk.");
            Assert.Greater(fastest - slowest, .02f, "Its step surges and lags instead of ticking like clockwork.");

            party.Retreat();
            Assert.IsTrue(members.TrueForAll(m => m.IsHesitating), "Breaking, each falters for a moment first.");
            yield return new WaitForSeconds(1f);
            foreach (var member in members)
            {
                Assert.IsFalse(member.IsHesitating);
                Assert.Greater(member.GaitPace, 1.05f, "Routed, it runs for its life.");
            }
        }

        [UnityTest]
        public IEnumerator RoutedInvaderStrikesAtAPursuerOnItsHeels()
        {
            var grid = Grid();
            var objective = Actor(grid, grid.Layout.Core, Faction.Dungeon, 100000);
            var party = new InvaderParty();
            var runner = Invader(Actor(grid, grid.Layout.Entrance + new Vector2Int(4, 0), Faction.Invader, 1000));
            var friend = Invader(Actor(grid, grid.Layout.Entrance + new Vector2Int(4, 1), Faction.Invader, 1000));
            foreach (var member in new[] { runner, friend }) { member.Assault(objective, grid); party.Add(member); }
            yield return null;
            party.Retreat();
            // A defender keeps pace right behind it all the way out.
            var chaser = Actor(grid, grid.Layout.Entrance + new Vector2Int(5, 0), Faction.Dungeon, 100000);
            int before = chaser.CurrentHealth;
            float deadline = Time.time + 3;
            while (Time.time < deadline && chaser.CurrentHealth == before && runner != null)
            {
                Vector2 heels = (Vector2)runner.transform.position + Vector2.right * .6f;
                chaser.GetComponent<Rigidbody2D>().position = heels;
                chaser.transform.position = heels;
                yield return null;
            }
            Assert.Less(chaser.CurrentHealth, before, "Run down, it turns and swings at the pursuer.");
            Assert.IsTrue(runner == null || runner.IsRetreating, "Then it keeps running.");
        }

        [Test]
        public void RetreatAddsOneOrTwoInvadersToTheNextWaveOncePerWave()
        {
            var node = Track(new GameObject("Retreat bonus director"));
            node.SetActive(false);
            var waves = node.AddComponent<WaveDirector>();
            var random = Random.state;
            try
            {
                Random.InitState(3);
                waves.OnPartyRetreated(2);
                int first = waves.RetreatBonus;
                Assert.That(first, Is.InRange(1, WaveDirector.MaxRetreatBonus));
                waves.OnPartyRetreated(2);
                Assert.AreEqual(first, waves.RetreatBonus, "A second party of the same wave adds nothing more.");
                waves.OnPartyRetreated(3);
                Assert.That(waves.RetreatBonus - first, Is.InRange(1, WaveDirector.MaxRetreatBonus));
            }
            finally { Random.state = random; }

            var region = RegionCatalog.ForDay(5);
            var parties = WaveFormation.CreateParties(5, 2, region);
            var before = parties.Select(p => p.ToArray()).ToArray();
            var reinforced = WaveFormation.AddReinforcements(parties, 5, 2, region);
            Assert.AreEqual(before.Sum(p => p.Length) + 2, reinforced.Sum(p => p.Length));
            Assert.AreEqual(before[0].Length + 2, reinforced[0].Length, "Reinforcements join the first party.");
            CollectionAssert.AreEqual(before[0], reinforced[0].Skip(2), "They march ahead of its own members.");
            CollectionAssert.AreEqual(before[1], reinforced[1], "A boss in the last party still enters last.");
        }

        private DungeonGrid Grid()
        {
            var go = Track(new GameObject("Retreat test grid"));
            go.layer = TerrainLayer;
            go.transform.position = new Vector3(400, 400, 0);
            var grid = go.AddComponent<DungeonGrid>();
            Physics2D.SyncTransforms();
            return grid;
        }

        private ActorHealth Actor(DungeonGrid grid, Vector2Int cell, Faction faction, int health)
        {
            var go = Track(new GameObject("Retreat test actor"));
            go.transform.position = grid.CellCenter(cell);
            var actor = go.AddComponent<ActorHealth>();
            actor.Configure(faction, health);
            go.AddComponent<CircleCollider2D>().radius = .24f;
            go.AddComponent<TopDownMotor>();
            return actor;
        }

        private static MinionController Invader(ActorHealth actor)
        {
            var invader = actor.gameObject.AddComponent<MinionController>();
            typeof(MinionController).GetField("obstacleLayers", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(invader, (LayerMask)(1 << TerrainLayer));
            return invader;
        }

        private GameObject Track(GameObject go) { objects.Add(go); return go; }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1;
            yield return null;
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }
    }
}
#endif
