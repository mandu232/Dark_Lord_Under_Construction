using System.Collections;
using System.Linq;
using System.Reflection;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Actors;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace DarkLord.Tests
{
    public sealed class WallLootPlacementTests
    {
        [Test] public void SmallDropsPreserveExpectedExperienceAndSplitCurrency()
        {
            var player = new GameObject("Drop collector"); var root = new GameObject("Drops");
            var random = Random.state;
            try
            {
                player.AddComponent<ActorHealth>(); Random.InitState(173);
                for (int i = 0; i < 60; i++) DeathLoot.Scatter(Vector3.zero, player.transform, root.transform);
                var saved = DeathLoot.Capture(player.transform);
                Assert.That(saved.Where(d => d.kind != 1).All(d => d.amount == 1));
                Assert.That(saved.Count(d => d.kind == 0), Is.InRange(120, 240));
                Assert.That(saved.Count(d => d.kind == 3), Is.InRange(75, 225));
                Assert.That(saved.Count(d => d.kind == 3) % 5, Is.Zero);
            }
            finally { Random.state = random; Object.DestroyImmediate(root); Object.DestroyImmediate(player); }
        }
        [UnityTest] public IEnumerator BounceRestoresArtAndNeverMovesFacilityRoot()
        {
            var root = new GameObject("Facility"); var visual = new GameObject("Intact");
            visual.transform.SetParent(root.transform); visual.transform.localPosition = Vector3.up * .625f;
            Vector3 position = visual.transform.localPosition, scale = visual.transform.localScale;
            try
            {
                var bounce = visual.AddComponent<FacilityPlacementBounce>(); bounce.Play();
                yield return new WaitForSeconds(.1f);
                Assert.AreEqual(Vector3.zero, root.transform.position);
                Assert.AreNotEqual(scale, visual.transform.localScale);
                yield return new WaitForSeconds(.6f);
                Assert.AreEqual(position, visual.transform.localPosition); Assert.AreEqual(scale, visual.transform.localScale);
                bounce.Play(); visual.SetActive(false);
                Assert.AreEqual(position, visual.transform.localPosition); Assert.AreEqual(scale, visual.transform.localScale);
            }
            finally { Object.DestroyImmediate(root); }
        }
#if UNITY_EDITOR
        [TestCase(1, 1)] [TestCase(2, 3)] [TestCase(3, 5)]
        public void ParallelCorridorsReserveUpperCapAndFitNorthWall(int gap, int faceHeight)
        {
            var node = new GameObject("Wall clearance test"); node.SetActive(false);
            try
            {
                var grid = node.AddComponent<DungeonGrid>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                void Set(string field, object value) => typeof(DungeonGrid).GetField(field, flags).SetValue(grid, value);
                Tile Load(string name) => AssetDatabase.LoadAssetAtPath<Tile>("Assets/Sprite/Dungeon/Tiles/" + name + ".asset");
                Set("roomFloorTile", Load("StoneFloor")); Set("corridorCarpetTile", Load("CorridorCarpet")); Set("repeatingWallTile", Load("DungeonWall"));
                Set("wallPieces", new TileBase[] { Load("Wall_0_0") });
                grid.Initialize();
                var cells = (DungeonCell[,])typeof(DungeonLayout).GetField("cells", flags).GetValue(grid.Layout);
                for (int y = 1; y < grid.Layout.Height - 1; y++)
                    for (int x = 1; x < grid.Layout.Width - 1; x++) cells[x,y] = DungeonCell.Rock;
                for (int x = 3; x <= 9; x++) { cells[x,3] = DungeonCell.Floor; cells[x,4 + gap] = DungeonCell.Floor; }
                typeof(DungeonGrid).GetMethod("RefreshAllArtTiles", flags).Invoke(grid, null);
                int height = (int)typeof(DungeonGrid).GetMethod("NorthFaceHeight", flags).Invoke(grid, new object[] { new Vector2Int(12,7) });
                Assert.AreEqual(faceHeight, height);
                var map = (Tilemap)typeof(DungeonGrid).GetField("wallMap", flags).GetValue(grid);
                for (int y = 8; y < 8 + faceHeight; y++)
                {
                    var tile = map.GetTile<Tile>(new Vector3Int(12,y,0)); Assert.IsNotNull(tile);
                    float renderedHeight = tile.sprite.bounds.size.y * tile.transform.lossyScale.y;
                    Assert.That(renderedHeight, Is.EqualTo(1).Within(.001f), "Each wall slice stays within its own cell");
                }
                Assert.IsNull(map.GetTile(new Vector3Int(12, (4 + gap) * 2, 0)), "Upper corridor floor stays clear");
            }
            finally { Object.DestroyImmediate(node); }
        }
#endif
    }
}
