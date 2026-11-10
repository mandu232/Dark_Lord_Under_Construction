#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Persistence;
using DarkLord.Player;
using DarkLord.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DarkLord.Tests
{
    [Category("MinionRoster")]
    public sealed class MinionRosterTests
    {
        private Scene scene;
        private float previousTime;
        private bool previousAudio;
        private WaveDirector waves;
        private DungeonGrid dungeon;
        private MinionSummoning summoning;
        private readonly List<GameObject> objects = new List<GameObject>();
        private static readonly Vector3 Center = new Vector3(1000, 1000, 0);
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        [UnitySetUp] public IEnumerator Setup()
        {
            previousTime = Time.timeScale; previousAudio = AudioListener.pause;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            Time.timeScale = 1; waves = Find<WaveDirector>(); dungeon = Find<DungeonGrid>(); summoning = Find<MinionSummoning>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var obj in objects) if (obj != null) Object.DestroyImmediate(obj);
            objects.Clear();
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousTime; AudioListener.pause = previousAudio;
        }
        private void Active(params int[] augments)
        {
            waves.RestoreSave(new WaveSaveData {number = 2, state = WaveState.Active, enemies = Array.Empty<MinionSaveData>(), augments = augments});
            waves.Lord.transform.position = Center + Vector3.down * 3; waves.Lord.GetComponent<Rigidbody2D>().position = waves.Lord.transform.position;
            Time.timeScale = 1;
        }
        private MinionController Minion(MinionRole role, Vector3 offset)
        {
            var prefab = summoning.RecruitDefinitionFor(role); Assert.IsNotNull(prefab);
            var actor = Object.Instantiate(prefab, Center + offset, Quaternion.identity); objects.Add(actor.gameObject);
            SceneManager.MoveGameObjectToScene(actor.gameObject, scene); dungeon.RegisterGuard(actor); actor.Defend(actor.transform.position); return actor;
        }
        private ActorHealth Enemy(Vector3 offset, int hp = 200)
        {
            var node = new GameObject("Roster enemy"); objects.Add(node); SceneManager.MoveGameObjectToScene(node, scene); node.transform.position = Center + offset;
            var health = node.AddComponent<ActorHealth>(); health.Configure(Faction.Invader, hp); node.AddComponent<CircleCollider2D>().radius = .2f; node.AddComponent<TopDownMotor>(); return health;
        }
        [UnityTest] public IEnumerator WraithShootsFromBehindFrontlineAndCannotShootThroughWalls()
        {
            Active(); var tank = Minion(MinionRole.BloodSlime, Vector3.left * 2); var wraith = Minion(MinionRole.BloodWraith, Vector3.zero);
            var enemy = Enemy(Vector3.right * 4); Physics2D.SyncTransforms();
            Assert.AreEqual(100, tank.Health.MaximumHealth); Assert.AreEqual(45, wraith.Health.MaximumHealth);
            Assert.AreEqual(5.5f, wraith.AttackRange); Assert.AreEqual(DamageType.Magic, wraith.AttackType);
            yield return new WaitForSeconds(.45f);
            Assert.AreEqual(182, enemy.CurrentHealth); Assert.That(Vector3.Distance(wraith.transform.position, Center), Is.LessThan(.1f));
            Assert.IsNull(tank.Target, "The short-range frontline cannot acquire this distant enemy.");
            var wall = new GameObject("Ranged wall"); objects.Add(wall); SceneManager.MoveGameObjectToScene(wall, scene);
            wall.layer = 8; wall.transform.position = Center + Vector3.right * 2; wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 4);
            Physics2D.SyncTransforms(); yield return new WaitForSeconds(1.6f);
            Assert.AreEqual(182, enemy.CurrentHealth, "Terrain blocks additional shots.");
        }
        [UnityTest] public IEnumerator WraithConsumesLordMarkAppliesBroodSlowAndPaysTributeOnKill()
        {
            Active(6,7,10); waves.Lord.transform.position = Center; waves.Lord.GetComponent<Rigidbody2D>().position = Center;
            Minion(MinionRole.BloodWraith, Vector3.left * 3); var enemy = Enemy(Vector3.right);
            Physics2D.SyncTransforms(); Assert.IsTrue(waves.Combat.TryAttack(Vector2.right));
            yield return new WaitForSeconds(.45f);
            Assert.AreEqual(139, enemy.CurrentHealth); Assert.IsFalse(enemy.GetComponent<AugmentStatus>().LordMarked);
            Assert.AreEqual(.65f, enemy.GetComponent<TopDownMotor>().SlowMultiplier);
            enemy.RestoreHealth(200, 5); waves.Combat.RestoreSave(new CombatSaveData {mana = 30, novaCooldown = 4});
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(enemy.IsAlive); Assert.GreaterOrEqual(waves.Combat.Mana, 38); Assert.Less(waves.Combat.NovaCooldown, 3);
        }
        [UnityTest] public IEnumerator SupportPrioritizesWoundedFrontlineAndPactEmpowersItsNextAttack()
        {
            Active(9); var tank = Minion(MinionRole.BloodSlime, Vector3.zero); tank.Health.TakeDamage(50);
            var support = Minion(MinionRole.VampireHealer, Vector3.right * 2); support.Health.TakeDamage(20);
            yield return new WaitForSeconds(.95f);
            Assert.AreEqual(64, tank.Health.CurrentHealth); Assert.AreEqual(50, support.Health.CurrentHealth, "Support never heals itself.");
            Assert.IsTrue(tank.GetComponent<AugmentStatus>().Empowered);
            var enemy = Enemy(Vector3.right * .7f); Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.5f);
            Assert.LessOrEqual(enemy.CurrentHealth, 175); Assert.IsFalse(tank.GetComponent<AugmentStatus>().Empowered);
        }
        [UnityTest] public IEnumerator SupportRespectsWallsPauseAndDamageInterruptsHealing()
        {
            Active(); var tank = Minion(MinionRole.BloodSlime, Vector3.zero); tank.Health.TakeDamage(40);
            var support = Minion(MinionRole.VampireHealer, Vector3.right * 2); var ability = support.GetComponent<MinionSupport>();
            var wall = new GameObject("Healing wall"); objects.Add(wall); SceneManager.MoveGameObjectToScene(wall, scene);
            wall.layer = 8; wall.transform.position = Center + Vector3.right; wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 4);
            Physics2D.SyncTransforms(); yield return new WaitForSeconds(.8f); Assert.AreEqual(60, tank.Health.CurrentHealth);
            Object.DestroyImmediate(wall); yield return new WaitForSeconds(.25f); Assert.IsTrue(ability.IsCasting);
            Time.timeScale = 0; float remaining = ability.CaptureSave().remaining;
            yield return new WaitForSecondsRealtime(.2f); Assert.AreEqual(remaining, ability.CaptureSave().remaining); Assert.AreEqual(60, tank.Health.CurrentHealth);
            Time.timeScale = 1; support.Health.TakeDamage(1); Assert.IsFalse(ability.IsCasting);
            yield return new WaitForSeconds(.65f); Assert.AreEqual(60, tank.Health.CurrentHealth);
        }
        [UnityTest] public IEnumerator WebSpiderSlowsWhatItHits()
        {
            Active(); Minion(MinionRole.WebSpider, Vector3.zero); var enemy = Enemy(Vector3.right * 2); Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.6f);
            Assert.Less(enemy.CurrentHealth, 200); Assert.AreEqual(.55f, enemy.GetComponent<TopDownMotor>().SlowMultiplier);
        }
        [Test] public void WraithUsesOnlyMatchingFacilityTraits()
        {
            Active(); var room = dungeon.Construction.Sections.Single(s => s.Kind == ConstructionKind.Room);
            var wallet = Find<CurrencyWallet>(); wallet.SetStartingAmounts(1000, 0, 0);
            string[] facilities = {"ArcaneFocus", "AcidVat", "MeleeTotem"}; var offsets = new[] {Vector2Int.up, Vector2Int.down, Vector2Int.left};
            for (int i = 0; i < 3; i++) Assert.IsTrue(dungeon.TryPlaceBuilding(room.Center + offsets[i], AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/" + facilities[i] + ".prefab"), wallet));
            foreach (var role in new[] {MinionRole.BloodWraith, MinionRole.VampireHealer})
            {
                var actor = Minion(role, Vector3.zero); actor.Relocate(dungeon.CellCenter(room.Center));
                actor.GetRoomBonuses(out int attack, out _, out _); Assert.AreEqual(30, attack); Assert.AreEqual(MinionTrait.Magic, actor.Traits);
                if (role == MinionRole.VampireHealer) Assert.AreEqual(18, actor.GetComponent<MinionSupport>().HealAmount);
            }
        }
        [Test] public void RetiredRolesLoadAsReplacementsAndUnknownRolesAreRejected()
        {
            var session = Find<GameSaveSession>(); var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            var first = dungeon.Layout.Core + new Vector2Int(1, 1); var second = dungeon.Layout.Core + new Vector2Int(-1, 1);
            saved.guards = new[] {
                new MinionSaveData {position = dungeon.CellCenter(first), post = dungeon.CellCenter(first), health = 40, maximumHealth = 60, role = MinionRole.BloodSupport,
                    support = new MinionSupportSaveData {casting = true, remaining = .2f, cooldown = 2, targetPosition = dungeon.CellCenter(second)}},
                new MinionSaveData {position = dungeon.CellCenter(second), post = dungeon.CellCenter(second), health = 50, maximumHealth = 65, role = MinionRole.BladeSlime} };
            SaveSlotStore.Validate(saved); session.Restore(saved); Time.timeScale = 1;
            CollectionAssert.AreEquivalent(new[] {MinionRole.VampireHealer, MinionRole.VampireBat}, dungeon.Guards.Select(g => g.Role));
            var healer = dungeon.Guards.Single(g => g.Role == MinionRole.VampireHealer); Assert.AreEqual(40, healer.Health.CurrentHealth); Assert.IsTrue(healer.GetComponent<MinionSupport>().IsCasting);
            var again = session.Capture(); again.guards[0].role = (MinionRole)99;
            Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(again));
            var legacy = JsonUtility.FromJson<MinionSaveData>("{\"health\":100,\"maximumHealth\":100}");
            Assert.AreEqual(MinionRole.BloodSlime, legacy.role);
        }
        [UnityTest] public IEnumerator VampireBatHealsOnEveryHit()
        {
            Active(); var bat = Minion(MinionRole.VampireBat, Vector3.zero); bat.Health.TakeDamage(20);
            var enemy = Enemy(Vector3.right * .5f); Physics2D.SyncTransforms(); int wounded = bat.Health.CurrentHealth;
            yield return new WaitForSeconds(.5f);
            Assert.Less(enemy.CurrentHealth, 200); Assert.Greater(bat.Health.CurrentHealth, wounded);
        }
        [Test] public void EveryRoleHasItsOwnDefinitionAndLook()
        {
            foreach (var role in MinionRoster.Summonable)
            {
                var definition = summoning.RecruitDefinitionFor(role); Assert.IsNotNull(definition, role.ToString());
                Assert.AreEqual(role, definition.Role);
                if (role != MinionRole.BloodSlime) Assert.IsNotNull(definition.GetComponentInChildren<MinionSkin>(), role.ToString());
                var summon = Object.Instantiate(definition, Center, Quaternion.identity);
                objects.Add(summon.gameObject);
                // The summon card reads the portrait before the first LateUpdate.
                var texture = summon.GetComponentInChildren<SpriteRenderer>().sprite.texture;
                Assert.AreEqual(role + "Sheet", texture.name, role.ToString());
                Assert.AreEqual(TextureFormat.RGBA32, texture.format, role.ToString());
            }
            Assert.IsNull(summoning.RecruitDefinitionFor(MinionRole.BloodArcher), "Retired roles can no longer be summoned.");
        }
        [Test] public void SelectionCancelCostsNothingAndInvalidRoleCannotEnterPlacement()
        {
            Assert.IsTrue(summoning.TryRecruit()); Assert.IsFalse(summoning.TrySummon((MinionRole)99));
            float mana = waves.Combat.Mana; Assert.IsTrue(summoning.TrySummon(MinionRole.BloodWraith));
            summoning.SetOpen(false); Assert.AreEqual(mana, waves.Combat.Mana); Assert.IsFalse(summoning.IsPlacing); Assert.AreEqual(0, summoning.GuardCount);
        }
        [Test] public void OldStartingFacilityMovesWithoutLosingDamageOrOverwritingPostsAndOtherFacilities()
        {
            var session = Find<GameSaveSession>(); var saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(session.Capture()));
            var room = dungeon.Construction.Sections.Single(s => s.Kind == ConstructionKind.Room);
            var original = saved.dungeon.buildings.Single(); original.cell = room.Center; original.health = 0;
            saved.dungeon.buildings = new[] {original, new BuildingSaveData {cell = room.Center + Vector2Int.up, designId = original.designId, health = 90, maximumHealth = 180}};
            var post = dungeon.Layout.Core + new Vector2Int(1, 1);
            saved.guards = new[] {new MinionSaveData {position = dungeon.CellCenter(post), post = dungeon.CellCenter(post), health = 100, maximumHealth = 100}};
            session.Restore(saved); Time.timeScale = 1;
            var buildings = dungeon.GetComponentsInChildren<ResourceBuilding>().Where(b => b.gameObject.activeInHierarchy).ToArray();
            Assert.AreEqual(2, buildings.Length);
            var main = buildings.Single(b => dungeon.Construction.RoomAt(b.Cell).IsCore);
            Assert.AreEqual(0, main.Health.CurrentHealth); Assert.AreNotEqual(post, main.Cell); Assert.AreNotEqual(dungeon.Layout.Core, main.Cell);
            Assert.IsNotNull(dungeon.BuildingAt(room.Center + Vector2Int.up));
            saved = session.Capture(); session.Restore(saved); Assert.AreEqual(2, session.Capture().dungeon.buildings.Length);
            saved.dungeon.buildings = Array.Empty<BuildingSaveData>(); session.Restore(saved); Assert.IsEmpty(session.Capture().dungeon.buildings);
        }
    }
}
#endif
