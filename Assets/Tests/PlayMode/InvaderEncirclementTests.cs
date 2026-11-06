using System.Collections;
using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class InvaderEncirclementTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private float previousTime;
        [SetUp] public void Setup() { previousTime = Time.timeScale; Time.timeScale = 1; }
        [TearDown] public void Cleanup()
        {
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear(); Time.timeScale = previousTime;
        }
        private GameObject Node(string name)
        {
            var node = new GameObject(name); objects.Add(node); return node;
        }
        private ActorHealth Actor(Vector2 position, Faction faction)
        {
            var node = Node(faction.ToString()); node.transform.position = position;
            var body = node.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true;
            node.AddComponent<CircleCollider2D>().radius = .2f;
            var health = node.AddComponent<ActorHealth>(); health.Configure(faction, 10000);
            return health;
        }
        [Test] public void WallAdjacentPlayerKeepsAllApproachesInsideRoom()
        {
            var grid = Node("Room").AddComponent<DungeonGrid>();
            var room = grid.Construction.RoomAt(grid.Layout.Core);
            Vector2 target = grid.CellCenter(room.Bounds.min) - Vector3.one * .5f;
            for (int rank = 0; rank < 12; rank++)
            {
                Assert.IsTrue(InvaderEncirclement.TryApproach(grid, room, grid.CellCenter(room.Center), target,
                    .85f, rank, 12, out var point));
                Assert.AreSame(room, grid.Construction.RoomAt(grid.WorldToCell(point)));
                Assert.IsTrue(grid.Construction.IsWalkable(grid.WorldToCell(point)));
            }
        }
        [UnityTest] public IEnumerator RoomInvadersSurroundPlayerAndDealDamage()
        {
            var grid = Node("Room").AddComponent<DungeonGrid>();
            Vector2 center = grid.CellCenter(grid.Layout.Core);
            var player = Actor(center, Faction.Dungeon);
            player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var core = Actor(center + Vector2.right * 5, Faction.Dungeon);
            core.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var invaders = new List<MinionController>();
            for (int i = 0; i < 4; i++)
            {
                var health = Actor(center + new Vector2(-4, (i - 1.5f) * .2f), Faction.Invader);
                var invader = health.gameObject.AddComponent<MinionController>();
                invader.Assault(core, grid, player); invaders.Add(invader);
            }
            yield return new WaitForSeconds(7);
            var quadrants = new HashSet<int>();
            foreach (var invader in invaders)
            {
                Assert.AreSame(player, invader.Target);
                Vector2 delta = (Vector2)invader.transform.position - center;
                float angle = Mathf.Repeat(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + 45, 360);
                quadrants.Add(Mathf.FloorToInt(angle / 90));
                Assert.AreSame(grid.CoreRoom, grid.Construction.RoomAt(grid.WorldToCell(invader.transform.position)));
                Assert.Less(delta.magnitude, 1.2f, "Attackers must close in after flanking");
            }
            Assert.AreEqual(4, quadrants.Count, "Four attackers must occupy different sides");
            Assert.Less(player.CurrentHealth, player.MaximumHealth);
            int healthBeforeMoving = player.CurrentHealth;
            Vector2 moved = center + new Vector2(1.5f, 1.5f);
            player.GetComponent<Rigidbody2D>().position = moved;
            player.transform.position = moved;
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(5);
            foreach (var invader in invaders)
            {
                Assert.AreSame(player, invader.Target);
                Assert.Less(Vector2.Distance(invader.transform.position, moved), 1.2f,
                    "The encirclement must follow a moving player");
            }
            Assert.Less(player.CurrentHealth, healthBeforeMoving);
        }

        [UnityTest] public IEnumerator CrowdedInvadersKeepSpacingWhileSurroundingPlayer()
        {
            var grid = Node("Crowded room").AddComponent<DungeonGrid>();
            Vector2 center = grid.CellCenter(grid.Layout.Core);
            var player = Actor(center, Faction.Dungeon);
            player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var core = Actor(center + Vector2.right * 5, Faction.Dungeon);
            core.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var invaders = new List<MinionController>();
            for (int i = 0; i < 12; i++)
            {
                var health = Actor(center + new Vector2(-4, (i - 5.5f) * .08f), Faction.Invader);
                var invader = health.gameObject.AddComponent<MinionController>();
                invader.Assault(core, grid, player); invaders.Add(invader);
            }
            yield return new WaitForSeconds(2);
            float deadline = Time.time + 8;
            while (Time.time < deadline)
            {
                for (int i = 0; i < invaders.Count; i++) for (int j = i + 1; j < invaders.Count; j++)
                    Assert.GreaterOrEqual(Vector2.Distance(invaders[i].transform.position, invaders[j].transform.position), .48f,
                        "Bodies must not overlap during flanking or attack steps");
                yield return new WaitForFixedUpdate();
            }
            Assert.Less(player.CurrentHealth, player.MaximumHealth, "Spacing must still permit attacks");
        }
    }
}
