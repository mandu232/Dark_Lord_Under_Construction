#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DarkLord.Tests
{
    [Category("CombatRewards")]
    [Category("MinionRoster")]
    public sealed class CombatAugmentTests
    {
        private Scene scene;
        private float previousTime;
        private bool previousAudio;
        private readonly List<GameObject> objects = new List<GameObject>();
        private WaveDirector waves;
        private DungeonGrid dungeon;
        private PlayerCombat combat;
        private static readonly Vector3 Center = new Vector3(900, 900, 0);
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        [UnitySetUp] public IEnumerator Setup()
        {
            previousTime = Time.timeScale; previousAudio = AudioListener.pause;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            Time.timeScale = 1; waves = Find<WaveDirector>(); dungeon = Find<DungeonGrid>(); combat = waves.Combat;
            waves.Lord.transform.position = Center; waves.Lord.GetComponent<Rigidbody2D>().position = Center;
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
            waves.RestoreSave(new WaveSaveData { number = 2, state = WaveState.Active, enemies = Array.Empty<MinionSaveData>(), augments = augments });
            Time.timeScale = 1;
        }
        private ActorHealth Actor(Vector3 offset, bool guard = false, int hp = 200)
        {
            var node = new GameObject("Augment test actor"); objects.Add(node); SceneManager.MoveGameObjectToScene(node, scene); node.transform.position = Center + offset;
            var health = node.AddComponent<ActorHealth>(); health.Configure(guard ? Faction.Dungeon : Faction.Invader, hp);
            node.AddComponent<CircleCollider2D>().radius = .18f; node.AddComponent<TopDownMotor>();
            if (guard) { var minion = node.AddComponent<MinionController>(); dungeon.RegisterGuard(minion); minion.Defend(node.transform.position); }
            return health;
        }
        private void Offer(int[] rewards, int[] selected = null, int wave = 1) => waves.RestoreSave(new WaveSaveData {
            number = wave, state = WaveState.RewardSelection, enemies = Array.Empty<MinionSaveData>(), rewards = rewards, augments = selected });
        [Test] public void NovaRewardsSlowedTargetsAndRefundsAtMostFifteenMana()
        {
            Active(WaveRewardCatalog.BloodNova);
            var slowed = Enumerable.Range(0, 4).Select(i => Actor(new Vector3(.8f + i * .2f, .2f))).ToArray();
            foreach (var target in slowed) target.GetComponent<TopDownMotor>().ApplySlow(.5f, 3);
            slowed[0].gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * .1f;
            var fast = Actor(Vector3.up); var ally = Actor(Vector3.left, true);
            Physics2D.SyncTransforms(); Assert.IsTrue(combat.TryNova());
            Assert.IsTrue(slowed.All(t => t.CurrentHealth == 130)); Assert.AreEqual(155, fast.CurrentHealth);
            Assert.AreEqual(200, ally.CurrentHealth); Assert.AreEqual(85, combat.Mana);
        }
        [UnityTest] public IEnumerator LordMarkIsConsumedByExactlyOneRealMinionAttack()
        {
            Active(WaveRewardCatalog.LordsHunt);
            var target = Actor(Vector3.right); var guard = Actor(Vector3.right * 1.7f, true);
            Physics2D.SyncTransforms(); Assert.IsTrue(combat.TryAttack(Vector2.right)); Assert.AreEqual(175, target.CurrentHealth);
            Assert.IsTrue(target.GetComponent<AugmentStatus>().LordMarked);
            yield return new WaitForSeconds(.32f);
            Assert.AreEqual(147, target.CurrentHealth); Assert.IsFalse(target.GetComponent<AugmentStatus>().LordMarked);
            yield return new WaitForSeconds(.85f);
            Assert.AreEqual(137, target.CurrentHealth, "The second attack gets no consumed mark bonus.");
        }
        [UnityTest] public IEnumerator MinionSlowEnablesNovaAndExpiresWhenAttacksStop()
        {
            Active(WaveRewardCatalog.BindingBrood, WaveRewardCatalog.BloodNova);
            var target = Actor(Vector3.right); var guard = Actor(Vector3.right * 1.7f, true);
            yield return new WaitForSeconds(.32f);
            Assert.AreEqual(.65f, target.GetComponent<TopDownMotor>().SlowMultiplier); Assert.AreEqual(190, target.CurrentHealth);
            Physics2D.SyncTransforms(); Assert.IsTrue(combat.TryNova()); Assert.AreEqual(120, target.CurrentHealth);
            guard.GetComponent<MinionController>().enabled = false;
            yield return new WaitForSeconds(1.4f);
            Assert.AreEqual(1, target.GetComponent<TopDownMotor>().SlowMultiplier);
        }
        [Test] public void ActualTrapBrandsEnemyAndLordConsumesBrandOnlyOnce()
        {
            Active(WaveRewardCatalog.TrapBrand);
            var target = Actor(Vector3.right);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/SpikeTrap.prefab");
            var node = Object.Instantiate(prefab, Center + Vector3.right, Quaternion.identity, dungeon.transform); objects.Add(node);
            Physics2D.SyncTransforms(); Assert.AreEqual(1, node.GetComponent<SpikeTrap>().TryPulse());
            Assert.AreEqual(188, target.CurrentHealth); Assert.IsTrue(target.GetComponent<AugmentStatus>().TrapBranded);
            Assert.IsTrue(combat.TryAttack(Vector2.right)); Assert.AreEqual(143, target.CurrentHealth);
            Assert.IsFalse(target.GetComponent<AugmentStatus>().TrapBranded);
            combat.RestoreSave(new CombatSaveData()); Assert.IsTrue(combat.TryAttack(Vector2.right)); Assert.AreEqual(118, target.CurrentHealth);
        }
        [UnityTest] public IEnumerator WardHealsAndEmpowersNearbyMinionsOnceDespiteExtraHitboxes()
        {
            Active(WaveRewardCatalog.BloodPact);
            var guard = Actor(Vector3.right, true, 100); guard.TakeDamage(50);
            guard.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * .1f;
            var outside = Actor(Vector3.up * 6, true, 100); outside.TakeDamage(50);
            var target = Actor(Vector3.right * 1.7f);
            Physics2D.SyncTransforms(); Assert.IsTrue(combat.TryWard()); Assert.AreEqual(70, guard.CurrentHealth);
            Assert.AreEqual(50, outside.CurrentHealth); Assert.IsTrue(guard.GetComponent<AugmentStatus>().Empowered);
            yield return new WaitForSeconds(.55f); // The setup wound causes the existing 0.2-second hit reaction first.
            Assert.AreEqual(175, target.CurrentHealth); Assert.IsFalse(guard.GetComponent<AugmentStatus>().Empowered);
        }
        [UnityTest] public IEnumerator MinionKillReturnsManaAndReducesNovaCooldown()
        {
            Active(WaveRewardCatalog.Tribute);
            combat.RestoreSave(new CombatSaveData { mana = 30, novaCooldown = 4 });
            Actor(Vector3.right * 4, false, 5); Actor(Vector3.right * 3.3f, true);
            yield return new WaitForSeconds(.32f);
            Assert.That(combat.Mana, Is.InRange(38, 39)); Assert.That(combat.NovaCooldown, Is.InRange(2.5f, 3));
        }
        [Test] public void EffectsAreDisabledDuringPreparationPauseAndWithoutSelection()
        {
            Active(); var target = Actor(Vector3.right); target.GetComponent<TopDownMotor>().ApplySlow(.5f, 4);
            Physics2D.SyncTransforms(); combat.TryNova(); Assert.AreEqual(155, target.CurrentHealth);
            Active(WaveRewardCatalog.LordsHunt, WaveRewardCatalog.TrapBrand, WaveRewardCatalog.BloodNova);
            Time.timeScale = 0;
            Assert.AreEqual(25, waves.Augments.MeleeDamage(target, 25)); Assert.IsFalse(waves.Augments.NovaBonus(target));
            waves.Augments.TrapHit(target); Assert.IsNull(target.GetComponent<AugmentStatus>());
            waves.RestoreSave(new WaveSaveData {number = 1, state = WaveState.Preparing, enemies = Array.Empty<MinionSaveData>(), augments = new[] {5,6,8}});
            Time.timeScale = 1;
            Assert.AreEqual(25, waves.Augments.MeleeDamage(target, 25)); Assert.IsFalse(waves.Augments.NovaBonus(target));
        }
        [Test] public void AllRewardRoundsOfferItemsWithoutAddingCombatAugments()
        {
            var selected = new List<int>();
            for (int wave = 1; wave <= 5; wave++)
            {
                waves.RestoreSave(new WaveSaveData { number = wave, state = WaveState.Active, enemies = Array.Empty<MinionSaveData>(), augments = selected.ToArray() });
                typeof(WaveDirector).GetMethod("CheckWaveCleared", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(waves, null);
                Assert.AreEqual(3, waves.RewardChoices.Distinct().Count()); Assert.AreEqual(0, Time.timeScale);
                Assert.IsTrue(waves.RewardChoices.All(id => id >= 0 && id <= 2 || id == WaveRewardCatalog.RoomItem || id == WaveRewardCatalog.TrapItem));
                var snapshot = JsonUtility.FromJson<WaveSaveData>(JsonUtility.ToJson(waves.CaptureSave()));
                waves.RestoreSave(snapshot); CollectionAssert.AreEqual(snapshot.rewards, waves.RewardChoices);
                int chosen = waves.RewardChoices[0]; Assert.IsTrue(waves.ChooseReward(0)); Assert.IsFalse(waves.ChooseReward(0));
                if (WaveRewardCatalog.IsAugment(chosen)) selected.Add(chosen);
                CollectionAssert.AreEqual(selected.OrderBy(id => id), waves.Augments.Capture());
            }
            Assert.AreEqual(WaveState.Victorious, waves.State);
        }
        [Test] public void SaveRestoresSelectedEffectsAndActorTimersAndRejectsInvalidRecords()
        {
            Active(5, 6, 8, 9);
            var session = Find<GameSaveSession>(); var saved = session.Capture();
            saved.guards = new[] { new MinionSaveData { position = Center + Vector3.left, post = Center, health = 80, maximumHealth = 100,
                augmentStatus = new AugmentStatusSaveData {empowered = 3}} };
            saved.wave.enemies = new[] {new MinionSaveData { position = Center + Vector3.right, health = 100, maximumHealth = 100,
                augmentStatus = new AugmentStatusSaveData {lordMark = 2, trapBrand = 1}}};
            SaveSlotStore.Validate(saved); session.Restore(JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(saved)));
            var restored = session.Capture(); CollectionAssert.AreEqual(new[] {5,6,8,9}, restored.wave.augments);
            Assert.That(restored.wave.enemies[0].augmentStatus.lordMark, Is.EqualTo(2).Within(.001f));
            Assert.That(restored.wave.enemies[0].augmentStatus.trapBrand, Is.EqualTo(1).Within(.001f));
            Assert.That(restored.guards[0].augmentStatus.empowered, Is.EqualTo(3).Within(.001f));
            restored.wave.augments = new[] {5,5}; Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(restored));
            restored.wave.augments = new[] {99}; Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(restored));
            restored.wave.augments = null; restored.wave.enemies[0].augmentStatus.lordMark = float.NaN;
            Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(restored));
            restored.wave.enemies[0].augmentStatus = null; restored.guards[0].augmentStatus = null;
            session.Restore(restored); Assert.IsEmpty(waves.Augments.Capture());
        }
        [UnityTest] public IEnumerator CardsUseUiSpritesFitDescriptionsHoverWhilePausedAndChooseOnlyOnce()
        {
            var skin = Resources.Load<RewardUiSkin>("WaveRewardSkin"); Assert.IsNotNull(skin);
            Assert.That(AssetDatabase.GetAssetPath(skin.cardFrame), Does.StartWith("Assets/Sprtie/UI/"));
            var ui = Find<WaveRewardPanel>();
            foreach (var choices in new[] {new[] {5,6,7}, new[] {8,9,10}})
            {
                Offer(choices); yield return null; Assert.IsTrue(ui.IsVisible); Assert.AreEqual(0, Time.timeScale);
                foreach (var label in ui.transform.Find("Wave reward selection").GetComponentsInChildren<TMP_Text>())
                { label.ForceMeshUpdate(); Assert.IsFalse(label.isTextOverflowing, label.name + ": " + label.text); }
            }
            var button = ui.transform.Find("Wave reward selection").GetComponentsInChildren<UnityEngine.UI.Button>().First();
            Assert.AreSame(button.gameObject, EventSystem.current.currentSelectedGameObject);
            var hits = new List<RaycastResult>();
            var cardRect = button.GetComponent<RectTransform>();
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, cardRect.TransformPoint(cardRect.rect.center)) };
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.AreSame(button, hits.Select(h => h.gameObject.GetComponentInParent<UnityEngine.UI.Button>()).FirstOrDefault(b => b != null));
            Assert.AreSame(skin.cardFrame, button.transform.Find("Card artwork").GetComponent<UnityEngine.UI.Image>().sprite);
            var visual = button.transform.Find("Card artwork").GetComponent<RectTransform>(); float before = visual.anchoredPosition.y;
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.Greater(visual.anchoredPosition.y, before); Assert.AreEqual(0, Time.timeScale);
            button.onClick.Invoke(); button.onClick.Invoke();
            Assert.AreEqual(1, waves.Augments.Capture().Length); Assert.IsFalse(ui.IsVisible); Assert.AreEqual(1, Time.timeScale);
        }
    }
}
#endif
