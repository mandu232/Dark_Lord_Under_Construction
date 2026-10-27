using System.Collections;
using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Actors;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Persistence;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("CorpseRecruitment")]
    [Category("DialogueInteraction")]
    [Category("CorpseCameraRevision")]
    [Category("ResourceEconomyRegression")]
    [Category("CoreTechnologyRegression")]
    [Category("PathGlowStarter")]
    public sealed class CorpseRecruitmentTests
    {
        private Scene scene;
        private WaveDirector waves;
        private DungeonGrid grid;
        private GameSaveSession session;
        private ResourceBuilding station;
        private MinionSummoning summoning;
        private float previousTime;
        private int freshGuardCount;
        private bool freshGuardsAreSlimes;
        private InputSettings originalSettings, inputSettings;
        private HideFlags originalFlags;
        private Keyboard keyboard;
        private Mouse mouse;
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        [UnitySetUp] public IEnumerator Setup()
        {
            previousTime = Time.timeScale; Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            waves = Find<WaveDirector>(); grid = Find<DungeonGrid>(); session = Find<GameSaveSession>(); summoning = Find<MinionSummoning>();
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Easy });
            station = grid.RecruitmentFacilities.Single();
            freshGuardCount=grid.Guards.Count();freshGuardsAreSlimes=grid.Guards.All(g=>g.Role==MinionRole.BloodSlime);
            // Reserve space for recruitment scenarios after recording the actual fresh-run roster.
            foreach(var guard in grid.Guards.Skip(1).ToArray())summoning.RemoveGuard(guard);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (originalSettings != null) { InputSystem.settings = originalSettings; originalSettings.hideFlags = originalFlags; }
            if (inputSettings != null) Object.DestroyImmediate(inputSettings);
            Time.timeScale = previousTime;
        }
        private void MoveLord(Vector3 position)
        { waves.Lord.transform.position = position; waves.Lord.GetComponent<Rigidbody2D>().position = position; Physics2D.SyncTransforms(); }
        private void EnableInput()
        {
            originalSettings = InputSystem.settings; originalFlags = originalSettings.hideFlags; originalSettings.hideFlags = HideFlags.DontUnloadUnusedAsset;
            inputSettings = Object.Instantiate(originalSettings); inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = inputSettings;
        }
        private InvaderCorpse Remains(Vector3 position, bool carried = false)
        {
            waves.Harvest.Restore(new[] { new CorpseSaveData { position = position, role = InvaderRole.Warrior, carried = carried } });
            return waves.Harvest.Corpses.Single();
        }
        [Test] public void FreshRunHasCoreRecruitmentAndFourFreeSlimesWithoutRegrantingRemovedGuards()
        {
            Assert.AreSame(grid.Construction.RoomAt(grid.Layout.Core), grid.Construction.RoomAt(station.Cell));
            Assert.AreEqual(MinionRole.BloodSlime, grid.Guards.Single().Role);
            Assert.AreEqual(4,freshGuardCount);Assert.IsTrue(freshGuardsAreSlimes);
            Assert.AreEqual(100, waves.Combat.Mana);
            grid.EnsureStartingRecruitment(); summoning.GrantStartingSlime();
            Assert.AreEqual(1, grid.RecruitmentFacilities.Count()); Assert.AreEqual(1, grid.Guards.Count());
        }
        [UnityTest] public IEnumerator CarryFollowsPlayerAndAbsorbsOnceWithDust()
        {
            MoveLord(grid.CellCenter(grid.Layout.Core + new Vector2Int(-2, -2)));
            var corpse = Remains(waves.Lord.transform.position);
            Assert.IsTrue(waves.Harvest.TryInteract()); Assert.AreSame(corpse, waves.Harvest.Carried);
            MoveLord(station.transform.position + Vector3.down * 1.2f); yield return null;
            Assert.IsNull(waves.Harvest.Carried); Assert.AreEqual(1, station.SummonStacks);
            Assert.AreEqual(0, waves.Harvest.Corpses.Count); Assert.IsTrue(corpse.Consumed);
            Assert.IsNotNull(corpse.GetComponent<CorpseAbsorptionEffect>());
            Assert.IsFalse(waves.Harvest.TryDeliver(corpse, station));
            yield return new WaitForSeconds(1); Assert.IsTrue(corpse == null); Assert.AreEqual(1, station.SummonStacks);
        }

        [UnityTest] public IEnumerator CarryingCorpseSlowsWalkingAndRestoresSpeedAfterDropAndReload()
        {
            EnableInput(); keyboard = InputSystem.AddDevice<Keyboard>();
            var motor = waves.Lord.GetComponent<TopDownMotor>();
            var animator = waves.Lord.GetComponent<Animator>();
            var safe = grid.CellCenter(grid.Layout.Core + new Vector2Int(-2, -2));
            MoveLord(safe);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(2.475f).Within(.1f));
            Assert.That(animator.GetFloat("WalkRate"), Is.EqualTo(1f).Within(.08f));
            MoveLord(safe);
            var corpse = Remains(safe);
            Assert.IsTrue(waves.Harvest.TryPickup(corpse));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreSame(corpse, waves.Harvest.Carried);
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(2.475f * .65f).Within(.1f));
            Assert.That(animator.GetFloat("WalkRate"), Is.EqualTo(.65f).Within(.08f));
            var saved = session.Capture();
            waves.Harvest.DropCarried(); yield return new WaitForSecondsRealtime(.15f);
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(2.475f).Within(.1f));
            session.Restore(saved); MoveLord(safe);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsNotNull(waves.Harvest.Carried);
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(2.475f * .65f).Within(.1f));
            motor.ApplySlow(.5f, 2f);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(2.475f * .65f * .5f).Within(.1f));
            waves.Harvest.enabled = false; yield return new WaitForSecondsRealtime(.15f);
            Assert.IsNull(waves.Harvest.Carried);
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(2.475f * .5f).Within(.1f));
            waves.Harvest.enabled = true;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        }
        [UnityTest] public IEnumerator WalkingBoostDoesNotIncreaseDodgeSpeed()
        {
            var motor = waves.Lord.GetComponent<TopDownMotor>();
            yield return null;
            Assert.That(motor.EffectiveMoveSpeed, Is.EqualTo(2.475f).Within(.01f));
            Assert.IsTrue(waves.Combat.TryDodge(Vector2.right));
            Assert.That(motor.EffectiveMoveSpeed, Is.EqualTo(2.25f * 2.5f).Within(.01f));
        }

        [UnityTest] public IEnumerator MeleeUsesDirectionalSlashAndReturnsToLocomotionWithoutShadowingTheEffect()
        {
            var view = waves.Lord.GetComponent<ActorAnimation>();
            var animator = waves.Lord.GetComponent<Animator>();
            var sprite = waves.Lord.GetComponent<SpriteRenderer>();
            var shadow = waves.Lord.GetComponentInChildren<SpriteProjectedShadow>();
            Vector2[] directions = { Vector2.down, new Vector2(1,-1), Vector2.right, Vector2.one,
                Vector2.up, new Vector2(-1,1), Vector2.left, -Vector2.one };
            int[] rows = { 0, 1, 2, 3, 4, 3, 2, 1 };
            for (int i = 0; i < directions.Length; i++)
            {
                Assert.IsTrue(waves.Combat.TryAttack(directions[i]), $"attack {i}");
                yield return null; yield return null;
                Assert.IsTrue(waves.Combat.IsMeleeAttacking);
                Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"));
                Assert.AreEqual("Slash_1", sprite.sprite.texture.name);
                Assert.AreEqual(256 - rows[i] * 64, sprite.sprite.rect.y, $"direction {i}");
                Assert.AreEqual(directions[i].x < 0, sprite.flipX);
                shadow.Refresh();
                Assert.AreNotEqual("Slash_1", shadow.GetComponent<SpriteRenderer>().sprite.texture.name);
                yield return new WaitForSecondsRealtime(.48f);
                Assert.IsFalse(waves.Combat.IsAttacking);
                Assert.IsFalse(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"));
                Assert.IsFalse(view.IsExpressionPlaying);
                yield return new WaitForSecondsRealtime(.1f);
            }
            Assert.IsTrue(waves.Combat.TryNova());
            yield return null; yield return null;
            Assert.IsTrue(waves.Combat.IsAttacking);
            Assert.IsFalse(waves.Combat.IsMeleeAttacking);
            Assert.AreNotEqual("Slash_1", sprite.sprite.texture.name);
            yield return new WaitForSecondsRealtime(.35f);
            Assert.IsTrue(waves.Combat.TryAttack(Vector2.right));
            yield return null; yield return null;
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"));
            Assert.IsTrue(waves.Combat.TryDodge(Vector2.right));
            yield return null; yield return null;
            Assert.IsFalse(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"));
        }
        [Test] public void RandomSummonSpendsOneStackWithoutManaAndEmptyStackFails()
        {
            MoveLord(station.transform.position + Vector3.down * 2);
            waves.Combat.RestoreSave(new CombatSaveData { mana = 0 }); station.TryAddSummonStack();
            Assert.IsTrue(summoning.TryRecruit()); Assert.IsTrue(summoning.TrySummonRandom(), summoning.Message);
            Assert.AreEqual(0, station.SummonStacks); Assert.AreEqual(0, waves.Combat.Mana);
            Assert.AreEqual(2, grid.Guards.Count()); Assert.That((int)grid.Guards.Last().Role, Is.InRange(0, 2));
            Assert.LessOrEqual(Vector2.Distance(grid.Guards.Last().transform.position, station.transform.position), 4.1f);
            Assert.IsTrue(summoning.TryRecruit()); Assert.IsFalse(summoning.TrySummonRandom()); Assert.AreEqual(2, grid.Guards.Count());
        }
        [Test] public void FullRoomPreservesStack()
        {
            MoveLord(station.transform.position + Vector3.down * 2);
            station.RestoreSummonStacks(20);
            int capacity = summoning.RoomCapacity(grid.Construction.RoomAt(station.Cell));
            for (int i = 1; i < capacity; i++) { Assert.IsTrue(summoning.TryRecruit()); Assert.IsTrue(summoning.TrySummonRandom(), summoning.Message); }
            int stacks = station.SummonStacks;
            Assert.IsTrue(summoning.TryRecruit()); Assert.IsFalse(summoning.TrySummonRandom()); Assert.AreEqual(stacks, station.SummonStacks);
        }
        [Test] public void SaveRestoresCarriedBodyAndStationStacksAndLegacyDefaults()
        {
            MoveLord(grid.CellCenter(grid.Layout.Core + new Vector2Int(-2, -2)));
            Remains(waves.Lord.transform.position, true); station.RestoreSummonStacks(7);
            var saved = session.Capture(); session.Restore(saved);
            Assert.AreEqual(7, grid.RecruitmentFacilities.Single().SummonStacks);
            Assert.AreEqual(1, waves.Harvest.Corpses.Count); Assert.IsNotNull(waves.Harvest.Carried); Assert.AreEqual(1, grid.Guards.Count());
            saved.corpses = null; foreach (var building in saved.dungeon.buildings) building.summonStacks = 0;
            session.Restore(saved); Assert.AreEqual(0, waves.Harvest.Corpses.Count); Assert.AreEqual(0, grid.RecruitmentFacilities.Single().SummonStacks);
        }
        [Test] public void TrapNeedsOwnedItemAndOnlySuccessfulPlacementConsumesIt()
        {
            var trap = Resources.FindObjectsOfTypeAll<BuildItemDefinition>().First(i => i.category == BuildCategory.Traps && i.available);
            var cell = grid.Construction.RoomAt(grid.Layout.Core).Cells.First(c => grid.CanPlaceTrap(c));
            Assert.IsFalse(grid.Inventory.CanUse(trap)); Assert.IsFalse(grid.TryPlaceTrapItem(cell, trap));
            grid.Inventory.Grant(trap); Assert.IsFalse(grid.TryPlaceTrapItem(grid.Layout.Core, trap)); Assert.IsTrue(grid.Inventory.CanUse(trap));
            Assert.IsTrue(grid.TryPlaceTrapItem(cell, trap)); Assert.IsFalse(grid.Inventory.CanUse(trap)); Assert.IsTrue(grid.HasTrap(cell));
        }
        [UnityTest] public IEnumerator RealInvaderDeathLeavesPersistentNonBlockingBody()
        {
            waves.BeginNextWave(); yield return null; yield return null;
            var enemy = waves.GetComponentsInChildren<MinionController>().First(m => m.Health.Faction == Faction.Invader);
            enemy.Health.TakeDamage(99999);
            Assert.AreEqual(1, waves.Harvest.Corpses.Count);
            var corpse = waves.Harvest.Corpses.Single();
            Assert.IsNull(corpse.GetComponent<ActorHealth>()); Assert.IsNull(corpse.GetComponent<Collider2D>());
            yield return new WaitForSeconds(1.7f);
            Assert.IsTrue(enemy == null); Assert.IsNotNull(corpse); Assert.AreEqual(1, session.Capture().corpses.Length);
        }
        [Test] public void DestroyedFacilityRejectsBodyAndKeepsStack()
        {
            var corpse = Remains(station.transform.position);
            station.RestoreSummonStacks(3); station.Health.TakeDamage(99999);
            Assert.IsTrue(station.IsRuined); Assert.IsFalse(waves.Harvest.TryDeliver(corpse, station));
            Assert.IsFalse(corpse.Consumed); Assert.AreEqual(1, waves.Harvest.Corpses.Count); Assert.AreEqual(3, station.SummonStacks);
        }
        [UnityTest] public IEnumerator FKeyCarriesBodyBeforeOpeningRecruitmentAndAllowsWalking()
        {
            EnableInput(); keyboard = InputSystem.AddDevice<Keyboard>();
            MoveLord(station.transform.position + Vector3.down * 2.8f);
            var corpse = Remains(waves.Lord.transform.position);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); yield return null;
            Assert.AreSame(corpse, waves.Harvest.Carried); Assert.IsFalse(summoning.IsOpen);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A)); yield return null; yield return new WaitForSeconds(.15f);
            Assert.AreSame(corpse, waves.Harvest.Carried); Assert.IsFalse(waves.Harvest.BlocksGameplayInput);
            Assert.Less(Vector2.Distance(corpse.transform.position, waves.Lord.transform.position), .7f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); yield return null;
            Assert.IsNull(waves.Harvest.Carried); Assert.IsFalse(summoning.IsOpen);
        }
        [UnityTest] public IEnumerator ClickingCorpseAttacksWithoutDraggingOrConsumingIt()
        {
            EnableInput(); mouse = InputSystem.AddDevice<Mouse>();
            var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
            camera.GetComponents<Behaviour>().First(b => b.GetType().Name == "PixelPerfectCamera").enabled = false;
            camera.GetComponent<CameraFollow>().enabled = false; camera.orthographicSize = 14;
            camera.transform.position = grid.CellCenter(grid.Layout.Core) + Vector3.back * 10;
            camera.ResetWorldToCameraMatrix(); camera.ResetProjectionMatrix();
            var corpse = Remains(grid.CellCenter(grid.Layout.Core + new Vector2Int(-2, -2)));
            Vector2 pointer = camera.WorldToScreenPoint(corpse.Visual.bounds.center);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer }); yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer, buttons = 1 }); yield return null;
            Assert.IsFalse(waves.Harvest.IsDragging); Assert.IsTrue(waves.Combat.IsAttacking);
            Assert.IsFalse(waves.Harvest.BlocksGameplayInput);
            pointer = camera.WorldToScreenPoint(station.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer, buttons = 1 }); yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer }); yield return null;
            Assert.IsFalse(waves.Harvest.IsDragging); Assert.AreEqual(0, station.SummonStacks); Assert.IsFalse(corpse.Consumed);
        }
        [UnityTest] public IEnumerator CorpseUsesLastDeathSpriteAndKeepsPoseAfterSave()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/WaveInvader.prefab");
            var actor = Object.Instantiate(prefab).GetComponent<MinionController>();
            actor.Health.Configure(Faction.Invader, 50); actor.Health.Kill();
            var corpse = waves.Harvest.LeaveCorpse(actor.Health);
            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Enemies/SwordSoldier/SwordSoldierDie.anim");
            var binding = UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            var expected = (Sprite)UnityEditor.AnimationUtility.GetObjectReferenceCurve(clip, binding).Last().value;
            Assert.AreSame(expected, corpse.Visual.sprite);
            Assert.Less(Quaternion.Angle(Quaternion.identity, corpse.transform.rotation), .001f);
            waves.Harvest.Restore(waves.Harvest.Capture());
            Assert.AreSame(expected, waves.Harvest.Corpses.Single().Visual.sprite);
            yield return null;
        }
        [UnityTest] public IEnumerator NearbyHintAndCarryReceiverHighlightFollowInteractionState()
        {
            MoveLord(grid.CellCenter(grid.Layout.Core + new Vector2Int(-2, -2)));
            Remains(waves.Lord.transform.position);
            var hud = Find<DarkLord.UI.CorpseHarvestHud>();
            var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
            camera.GetComponents<Behaviour>().First(b => b.GetType().Name == "PixelPerfectCamera").enabled = false;
            camera.GetComponent<CameraFollow>().enabled = false;
            camera.transform.position = grid.CellCenter(grid.Layout.Core) + Vector3.back * 10;
            camera.orthographicSize = 14; camera.ResetWorldToCameraMatrix(); camera.ResetProjectionMatrix();
            yield return null;
            var outline = hud.transform.Find("Nearby corpse arrow").gameObject;
            var hint = hud.transform.Find("Nearby corpse F hint").gameObject;
            var receiver = hud.transform.Find("Available corpse receiver").gameObject;
            Assert.IsTrue(outline.activeSelf); Assert.IsTrue(hint.activeSelf); Assert.IsFalse(receiver.activeSelf);
            Assert.IsTrue(waves.Harvest.TryInteract()); yield return null;
            Assert.IsFalse(outline.activeSelf); Assert.IsFalse(hint.activeSelf); Assert.IsTrue(receiver.activeSelf);
            var arrow = receiver.GetComponent<DarkLord.UI.TutorialGuideGraphic>();
            Assert.IsNotNull(arrow); Assert.IsFalse(arrow.raycastTarget); Assert.Greater(arrow.color.g, arrow.color.r);
            Assert.IsEmpty(receiver.GetComponentsInChildren<UnityEngine.UI.Image>());
            station.Health.TakeDamage(99999); yield return null; Assert.IsFalse(receiver.activeSelf);
            Time.timeScale = 0; yield return null; Assert.IsFalse(outline.activeSelf); Assert.IsFalse(hint.activeSelf);
        }
        [UnityTest] public IEnumerator SuccessfulRandomSummonShowsCorrectPortraitNameAndExpires()
        {
            MoveLord(station.transform.position + Vector3.down * 2);
            var presentation = Find<DarkLord.UI.SummonPresentation>();
            Assert.IsFalse(presentation.IsVisible);
            Assert.IsTrue(summoning.TryRecruit()); Assert.IsFalse(summoning.TrySummonRandom());
            Assert.IsFalse(presentation.IsVisible);
            station.TryAddSummonStack(); Assert.IsTrue(summoning.TrySummonRandom()); yield return null;
            var minion = grid.Guards.Last();
            Assert.IsTrue(presentation.IsVisible); Assert.AreEqual(minion.DisplayName, presentation.PresentedName);
            Assert.IsNotNull(presentation.PresentedSprite);
            Assert.IsTrue(presentation.transform.Find("Random summon presentation/Summon magic circle").gameObject.activeSelf);
            Assert.IsTrue(presentation.transform.Find("Random summon presentation").GetComponentsInChildren<UnityEngine.UI.Graphic>().All(g => !g.raycastTarget));
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(presentation.IsVisible);
            Assert.IsFalse(presentation.transform.Find("Random summon presentation/Summon magic circle").gameObject.activeSelf);
            yield return new WaitForSeconds(1.8f); Assert.IsFalse(presentation.IsVisible);
        }
    }
}
