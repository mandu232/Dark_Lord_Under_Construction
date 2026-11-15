#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("StarterDungeon")]
    public sealed class InvaderPartyTacticsTests
    {
        private const int TerrainLayer = 8;
        private readonly List<GameObject> objects = new List<GameObject>();

        [UnityTest]
        public IEnumerator WoundedPartyFallsBackOutOfTheRoomRestsAndReturns()
        {
            var grid = Grid();
            var core = grid.Layout.Core;
            var objective = Actor(grid, core, Faction.Dungeon, 100000);
            var party = new InvaderParty();
            var members = new List<MinionController>();
            foreach (var offset in new[] { new Vector2Int(-2, 0), new Vector2Int(-2, 1) })
            {
                var invader = Invader(Actor(grid, core + offset, Faction.Invader, 1000));
                invader.Assault(objective, grid);
                party.Add(invader);
                members.Add(invader);
            }
            yield return null;
            foreach (var member in members) member.Health.RestoreHealth(1000, 400);
            int fellBack = 0, resumed = 0;
            party.FellBack += _ => fellBack++;
            party.Resumed += _ => resumed++;

            Assert.IsTrue(party.TryFallBack());
            Assert.AreEqual(PartyPlan.FallBack, party.Plan);
            Assert.AreEqual(1, fellBack);
            var coreRoom = grid.Construction.RoomAt(core);
            Assert.AreNotSame(coreRoom, grid.Construction.RoomAt(party.FallBackCell), "The rest point lies outside the room it was fighting in.");
            Assert.IsTrue(members.TrueForAll(m => m.IsFallingBack));

            float deadline = Time.time + 8;
            while (Time.time < deadline && !members.TrueForAll(m => Vector2.Distance(m.transform.position, party.FallBackPoint) <= InvaderParty.FallBackArrival))
                yield return null;
            Assert.IsTrue(members.TrueForAll(m => Vector2.Distance(m.transform.position, party.FallBackPoint) <= InvaderParty.FallBackArrival),
                "The whole party gathers at the rest point.");

            deadline = Time.time + InvaderParty.MaxFallBackDuration + 2;
            while (Time.time < deadline && party.FallingBack) yield return null;
            Assert.AreEqual(PartyPlan.Advance, party.Plan, "After resting the party goes back in.");
            Assert.AreEqual(1, resumed);
            Assert.IsTrue(members.TrueForAll(m => m.Health.CurrentHealth > 400), "Resting healed the party.");
            Assert.IsTrue(members.TrueForAll(m => !m.IsFallingBack && !m.IsRetreating));
        }

        [UnityTest]
        public IEnumerator PartiesAvoidRoomsAnotherLivePartyClaimed()
        {
            var grid = Grid();
            var objective = Actor(grid, grid.Layout.Core, Faction.Dungeon, 100000);
            var room = grid.Construction.RoomAt(grid.Layout.Core);
            var first = new InvaderParty();
            var second = new InvaderParty();
            var scout = Invader(Actor(grid, grid.Layout.Entrance + new Vector2Int(2, 0), Faction.Invader, 1000));
            scout.Assault(objective, grid);
            first.Add(scout);
            yield return null;

            first.Claim(room);
            Assert.IsTrue(InvaderParty.ClaimedByOther(second, room), "A room another party is heading for counts against it.");
            Assert.IsFalse(InvaderParty.ClaimedByOther(first, room), "A party's own claim never counts against it.");
            scout.Health.Kill();
            yield return null;
            Assert.IsFalse(InvaderParty.ClaimedByOther(second, room), "A wiped-out party holds no claim.");
        }

        private DungeonGrid Grid()
        {
            var go = Track(new GameObject("Party tactics grid"));
            go.layer = TerrainLayer;
            go.transform.position = new Vector3(400, 400, 0);
            var grid = go.AddComponent<DungeonGrid>();
            Physics2D.SyncTransforms();
            return grid;
        }

        private ActorHealth Actor(DungeonGrid grid, Vector2Int cell, Faction faction, int health)
        {
            var go = Track(new GameObject("Party tactics actor"));
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
