#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Persistence;
using DarkLord.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DarkLord.Tests
{
    public sealed class InvaderRoleTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private Scene scene;
        private float previousTime;
        private bool previousAudio;
        private static readonly Vector3 Center = new Vector3(850, 850);
        [SetUp] public void Setup() { previousTime = Time.timeScale; previousAudio = AudioListener.pause; Time.timeScale = 1; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var node in objects) if (node != null) Object.DestroyImmediate(node);
            objects.Clear();
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousTime; AudioListener.pause = previousAudio;
        }
        private ActorHealth Actor(Vector3 offset, Faction team = Faction.Invader)
        {
            var node = new GameObject("Role test actor"); objects.Add(node); node.transform.position = Center + offset;
            var health = node.AddComponent<ActorHealth>(); health.Configure(team, 100);
            node.AddComponent<CircleCollider2D>().radius = .2f;
            return health;
        }
        private InvaderTactics Enemy(InvaderRole role)
        {
            var health = Actor(Vector3.zero); health.gameObject.AddComponent<MinionController>();
            var tactics = health.gameObject.AddComponent<InvaderTactics>();
            tactics.RestoreSave(new InvaderTacticsSaveData { role = role });
            return tactics;
        }
        [Test] public void RowenRoarsOnceAtHalfHealthThenStrikesFarTargetsWithJudgement()
        {
            var hero = Enemy(InvaderRole.Charger); hero.SetHero(true);
            var far = Actor(Vector3.right * 9, Faction.Dungeon);
            Physics2D.SyncTransforms();
            hero.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Charger, cooldown = 0 });
            Assert.IsTrue(hero.TryAct(far));
            Assert.AreEqual(InvaderAction.Judging, hero.Action, "A target beyond charge range is struck from afar");
            var health = hero.GetComponent<ActorHealth>(); health.RestoreHealth(100, 50);
            hero.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Charger, cooldown = 0 });
            Assert.IsTrue(hero.TryAct(far));
            Assert.AreEqual(InvaderAction.Roaring, hero.Action); Assert.IsTrue(hero.PhaseTwo); Assert.IsTrue(hero.HeroArmored);
            hero.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Charger, action = InvaderAction.Judging, phaseTwo = true, marks = new Vector2[] { far.transform.position } });
            Assert.IsTrue(hero.TryAct(far));
            Assert.AreEqual(75, far.CurrentHealth);
            Assert.AreEqual(InvaderAction.Recovering, hero.Action); Assert.IsFalse(hero.HeroArmored, "The recovery is the opening to punish him");
        }
        [Test] public void RegionalBossSignatureHitsOverlappingMarksOnceAndHrimsIceSlows()
        {
            var boss = Enemy(InvaderRole.Charger); boss.SetRace(RegionCatalog.At(4)); boss.SetHero(true);
            var far = Actor(Vector3.right * 4, Faction.Dungeon);
            var motor = far.gameObject.AddComponent<TopDownMotor>();
            Physics2D.SyncTransforms();
            Vector2 point = far.transform.position;
            boss.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Charger, action = InvaderAction.Signature, marks = new[] { point, point + Vector2.right * .5f } });
            Assert.IsTrue(boss.TryAct(far));
            Assert.AreEqual(78, far.CurrentHealth, "Two overlapping marks strike once");
            Assert.Less(motor.SlowMultiplier, 1f, "Frost marks slow what they hit");
            Assert.AreEqual(InvaderAction.Recovering, boss.Action);
        }
        [Test] public void ShieldbearerBashesAdjacentTargetAfterTelegraph()
        {
            var shield = Enemy(InvaderRole.Shieldbearer);
            var near = Actor(Vector3.right, Faction.Dungeon);
            Physics2D.SyncTransforms();
            shield.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Shieldbearer, cooldown = 0 });
            Assert.IsTrue(shield.TryAct(near));
            Assert.AreEqual(InvaderAction.ShieldBash, shield.Action); Assert.AreEqual(100, near.CurrentHealth);
            shield.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Shieldbearer, action = InvaderAction.ShieldBash, direction = Vector2.right });
            Assert.IsTrue(shield.TryAct(near));
            Assert.AreEqual(90, near.CurrentHealth);
        }

        [Test] public void PaladinBlocksAndBashesWhileClericHealsItsWoundedAlly()
        {
            var paladin = Enemy(InvaderRole.Paladin);
            var guard = Actor(Vector3.right, Faction.Dungeon);
            var paladinHealth = paladin.GetComponent<ActorHealth>();
            paladin.GetComponent<TopDownMotor>().Face(Vector2.right);
            paladinHealth.TakeDamage(20, guard);
            Assert.AreEqual(94, paladinHealth.CurrentHealth);
            Physics2D.SyncTransforms();
            Assert.IsTrue(paladin.TryAct(guard));
            Assert.AreEqual(InvaderAction.ShieldBash, paladin.Action);
            var bash = paladin.CaptureSave();
            bash.remaining = 0;
            paladin.RestoreSave(bash);
            Assert.IsTrue(paladin.TryAct(guard));
            Assert.AreEqual(90, guard.CurrentHealth);

            var cleric = Enemy(InvaderRole.Cleric);
            var wounded = Actor(Vector3.up * 3);
            wounded.Configure(Faction.Invader, 250);
            wounded.TakeDamage(80);
            Physics2D.SyncTransforms();
            Assert.IsTrue(cleric.TryAct(null));
            Assert.AreEqual(InvaderAction.Healing, cleric.Action);
            var heal = cleric.CaptureSave();
            heal.remaining = 0;
            cleric.RestoreSave(heal);
            Assert.IsTrue(cleric.TryAct(null));
            Assert.AreEqual(200, wounded.CurrentHealth);
        }
        [Test] public void RowenCleaveHitsAllEnemiesInFrontAndKeepsChargeAtRange()
        {
            var hero = Enemy(InvaderRole.Charger); hero.SetHero(true);
            var front = Actor(Vector3.right, Faction.Dungeon);
            var flank = Actor(new Vector3(.3f, 1.5f), Faction.Dungeon);
            var behind = Actor(Vector3.left, Faction.Dungeon);
            Physics2D.SyncTransforms();
            hero.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Charger, cooldown = 0 });
            Assert.IsTrue(hero.TryAct(front));
            Assert.AreEqual(InvaderAction.HeroCleave, hero.Action);
            hero.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Charger, action = InvaderAction.HeroCleave, direction = Vector2.right });
            Assert.IsTrue(hero.TryAct(front));
            Assert.AreEqual(75, front.CurrentHealth); Assert.AreEqual(75, flank.CurrentHealth);
            Assert.AreEqual(100, behind.CurrentHealth);
            hero.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Charger, cooldown = 0 });
            front.transform.position = Center + Vector3.right * 4;
            Physics2D.SyncTransforms();
            Assert.IsTrue(hero.TryAct(front));
            Assert.AreEqual(InvaderAction.WindingUp, hero.Action);
        }

        [UnityTest] public IEnumerator RowenCleaveAndChargeKnockTargetsBack()
        {
            var hero = Enemy(InvaderRole.Charger); hero.SetHero(true);
            var target = Actor(Vector3.right, Faction.Dungeon);
            var motor = target.gameObject.AddComponent<TopDownMotor>();
            Physics2D.SyncTransforms(); yield return null;
            hero.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Charger, action = InvaderAction.HeroCleave, direction = Vector2.right });
            var start = target.transform.position;
            Assert.IsTrue(hero.TryAct(target));
            yield return new WaitForSeconds(.16f);
            Assert.Greater(target.transform.position.x - start.x, .5f, "Cleave must push the target away");
            motor.ClearKnockback(); motor.Stop();
            motor.GetComponent<Rigidbody2D>().position = Center + Vector3.right;
            target.transform.position = Center + Vector3.right; Physics2D.SyncTransforms();
            hero.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Charger, action = InvaderAction.Charging, remaining = 1, direction = Vector2.right });
            start = target.transform.position;
            Assert.IsTrue(hero.TryAct(target));
            yield return new WaitForSeconds(.16f);
            Assert.Greater(target.transform.position.x - start.x, .5f, "Charge must push the target away");
        }

        [UnityTest] public IEnumerator RowenChargePushesMultipleMinionsAndContinues()
        {
            var hero = Enemy(InvaderRole.Charger); hero.SetHero(true);
            var one = Actor(Vector3.right * .7f, Faction.Dungeon); one.gameObject.AddComponent<MinionController>();
            var two = Actor(Vector3.right * 1.3f, Faction.Dungeon); two.gameObject.AddComponent<MinionController>();
            // Stop defender AI from chasing while retaining the body-blocking component.
            one.GetComponent<MinionController>().SetHeld(true); two.GetComponent<MinionController>().SetHeld(true);
            one.GetComponent<TopDownMotor>().IgnoreActorBlocking = true;
            two.GetComponent<TopDownMotor>().IgnoreActorBlocking = true;
            hero.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Charger, action = InvaderAction.Charging, remaining = .35f, direction = Vector2.right });
            Physics2D.SyncTransforms();
            Vector3 start = hero.transform.position;
            float until = Time.time + .3f;
            while (Time.time < until) { hero.TryAct(one); yield return null; }
            Assert.Greater(hero.transform.position.x - start.x, .5f, "Charge continues through bodies");
            Assert.AreEqual(78, one.CurrentHealth, "Each target is hit once");
            Assert.AreEqual(78, two.CurrentHealth, "Both targets are struck");
            Assert.Greater(one.transform.position.x, Center.x + .7f);
            Assert.Greater(two.transform.position.x, Center.x + 1.3f);
        }

        [Test] public void RowenDangerAreasMatchAttackDirectionAndDisappearAfterAction()
        {
            var hero = Enemy(InvaderRole.Charger); hero.SetHero(true);
            hero.transform.localScale = Vector3.one * 1.4f;
            var target = Actor(Vector3.right, Faction.Dungeon);
            Physics2D.SyncTransforms();
            hero.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Charger, cooldown = 0 });
            Assert.IsTrue(hero.TryAct(target));
            var warning = hero.transform.Find("Rowen danger area").GetComponent<SpriteRenderer>();
            Assert.IsTrue(warning.enabled);
            Assert.That(warning.color.r, Is.GreaterThan(warning.color.g));
            Assert.That(warning.bounds.size.x, Is.EqualTo(5f).Within(.02f));
            Assert.That(warning.bounds.size.y, Is.EqualTo(5f).Within(.02f));
            var texture = warning.sprite.texture;
            Assert.Less(texture.GetPixel(25, 32).a, .01f, "Cleave does not cover the rear");
            Assert.Greater(texture.GetPixel(90, 32).a, .1f, "Cleave covers the front");
            hero.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Charger, action = InvaderAction.WindingUp, remaining = .75f, direction = Vector2.right });
            Assert.IsTrue(warning.enabled);
            float travel = hero.GetComponent<TopDownMotor>().MoveSpeed * 2.6f * .35f;
            Assert.That(warning.bounds.size.x, Is.EqualTo(travel + 2.3f).Within(.02f));
            Assert.That(warning.bounds.size.y, Is.EqualTo(2.3f).Within(.02f));
            Assert.That(warning.bounds.center.x - hero.transform.position.x, Is.EqualTo(travel / 2).Within(.02f));
            hero.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Charger, action = InvaderAction.Recovering, remaining = .2f });
            Assert.IsFalse(warning.enabled, "Danger area ends after windup");
            hero.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Charger, action = InvaderAction.WindingUp, remaining = .3f, direction = Vector2.up });
            Assert.That(warning.bounds.size.y, Is.EqualTo(travel + 2.3f).Within(.02f));
            hero.enabled = false;
            Assert.IsFalse(warning.enabled, "Disabling the actor clears the warning");
        }

        [Test] public void FiveWavesIntroduceDifferentProblemsAndRespectScaledCounts()
        {
            int[][] expected = { new[] {3,0,0,0,0}, new[] {4,0,0,1,0}, new[] {5,0,1,1,0}, new[] {5,2,1,1,0}, new[] {6,2,1,1,1} };
            for (int wave = 1; wave <= 5; wave++)
            {
                var formation = WaveFormation.Create(wave, wave * 2 + 1);
                for (int role = 0; role < expected[wave - 1].Length; role++)
                    Assert.AreEqual(expected[wave - 1][role], formation.Count(r => (int)r == role));
                foreach (GameDifficulty difficulty in System.Enum.GetValues(typeof(GameDifficulty)))
                {
                    int count = new NewGameOptions { difficulty = difficulty }.EnemyCount(wave * 2 + 1);
                    Assert.AreEqual(count, WaveFormation.Create(wave, count).Length);
                }
            }
            Assert.AreEqual(1, WaveFormation.Create(5, 0).Length);
        }
        [UnityTest] public IEnumerator ArcherAimsAtRangeReleasesWithItsSpriteAndHitsAfterTravel()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/WaveInvader.prefab");
            var node = Object.Instantiate(prefab, Center, Quaternion.identity); objects.Add(node);
            var tactics = node.GetComponent<InvaderTactics>() ?? node.AddComponent<InvaderTactics>();
            var archer = node.GetComponent<MinionController>();
            var body = node.GetComponent<SpriteRenderer>();
            archer.Health.Configure(Faction.Invader, 100);
            tactics.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Archer });
            var victim = Actor(Vector3.right * 5, Faction.Dungeon);
            var ally = Actor(Vector3.right * 2);
            archer.Assault(victim);
            Physics2D.SyncTransforms();

            for (float limit = Time.time + 2; body.sprite.name != "archer_9" && Time.time < limit;)
                yield return null;
            Assert.AreEqual(InvaderAction.Aiming, tactics.Action);
            Assert.AreEqual("archer_9", body.sprite.name);
            Assert.That(Vector3.Distance(node.transform.position, Center), Is.LessThan(.1f));
            Assert.AreEqual(100, victim.CurrentHealth);

            for (float limit = Time.time + 1; body.sprite.name != "archer_10" && Time.time < limit;)
                yield return null;
            Assert.AreEqual("archer_10", body.sprite.name);
            Assert.AreEqual(InvaderAction.Recovering, tactics.Action);
            Assert.IsNotNull(Object.FindAnyObjectByType<InvaderArrow>());
            Assert.AreEqual(100, victim.CurrentHealth);
            yield return new WaitForSeconds(.48f);
            Assert.AreEqual(88, victim.CurrentHealth);
            Assert.AreEqual(100, ally.CurrentHealth);

            var wall = new GameObject("Arrow blocking wall") { layer = 8 };
            objects.Add(wall); wall.transform.position = Center + Vector3.right * 3;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.3f, 2);
            Physics2D.SyncTransforms();
            // A wall placed after aiming still intercepts a released arrow.
            tactics.RestoreSave(new InvaderTacticsSaveData {
                role = InvaderRole.Archer, action = InvaderAction.Aiming, direction = Vector2.right, cooldown = 2
            });
            yield return new WaitForSeconds(.55f);
            Assert.AreEqual(88, victim.CurrentHealth);
            Assert.IsNull(Object.FindAnyObjectByType<InvaderArrow>());
        }

        [UnityTest] public IEnumerator ArcherBacksAwayFromAClosingPlayerThenStopsAndReaims()
        {
            var bow = Enemy(InvaderRole.Archer);
            var archer = bow.GetComponent<MinionController>();
            var player = Actor(Vector3.right * 5f, Faction.Dungeon);
            player.gameObject.AddComponent<DarkLord.Player.PlayerCombat>().enabled = false;
            archer.Assault(player);
            yield return null;
            Assert.AreEqual(InvaderAction.Aiming, bow.Action);

            player.GetComponent<Rigidbody2D>().position = Center + Vector3.right * 1.2f;
            player.transform.position = Center + Vector3.right * 1.2f;
            Physics2D.SyncTransforms();
            yield return null;
            yield return null;
            Assert.AreEqual(InvaderAction.Ready, bow.Action, "Close pressure cancels the first aim");
            Assert.AreEqual(ActorState.Run, archer.State);

            for (float end = Time.time + 3; bow.Action != InvaderAction.Aiming && Time.time < end;)
            {
                Assert.IsNull(Object.FindAnyObjectByType<InvaderArrow>(), "No arrows while retreating");
                yield return null;
            }
            Assert.AreEqual(InvaderAction.Aiming, bow.Action);
            Assert.GreaterOrEqual(Vector2.Distance(archer.transform.position, player.transform.position), 3.2f);
            var position = archer.transform.position;
            yield return new WaitForSeconds(.2f);
            Assert.That(Vector2.Distance(position, archer.transform.position), Is.LessThan(.05f));
            yield return new WaitForSeconds(.8f);
            Assert.AreEqual(88, player.CurrentHealth);
        }

        [UnityTest] public IEnumerator ArcherCanShootWhenItsRetreatIsBlockedByTerrain()
        {
            var bow = Enemy(InvaderRole.Archer);
            var archer = bow.GetComponent<MinionController>();
            var player = Actor(Vector3.right * 1.25f, Faction.Dungeon);
            player.gameObject.AddComponent<DarkLord.Player.PlayerCombat>().enabled = false;
            var wall = new GameObject("Wall behind archer") { layer = 8 };
            objects.Add(wall);
            wall.transform.position = Center + Vector3.left * .5f;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.3f, 4f);
            var data = new UnityEditor.SerializedObject(archer);
            data.FindProperty("obstacleLayers").intValue = 1 << 8;
            data.ApplyModifiedPropertiesWithoutUndo();
            Physics2D.SyncTransforms();
            archer.Assault(player);
            yield return new WaitForSeconds(1);
            Assert.AreEqual(88, player.CurrentHealth);
            Assert.That(Vector2.Distance(Center, archer.transform.position), Is.LessThan(.05f));
        }
        [Test] public void ShieldReducesOnlyFrontalPhysicalDamageAndUnsubscribesOnDisable()
        {
            var shield = Enemy(InvaderRole.Shieldbearer); var health = shield.GetComponent<ActorHealth>();
            var source = Actor(Vector3.right, Faction.Dungeon); shield.GetComponent<TopDownMotor>().Face(Vector2.right);
            health.TakeDamage(20, source); Assert.AreEqual(94, health.CurrentHealth);
            health.TakeDamage(20, source, DamageType.Magic); Assert.AreEqual(74, health.CurrentHealth);
            source.transform.position = Center + Vector3.left;
            health.TakeDamage(20, source); Assert.AreEqual(54, health.CurrentHealth);
            source.transform.position = Center + Vector3.up;
            health.TakeDamage(20, source); Assert.AreEqual(34, health.CurrentHealth);
            shield.enabled = false; source.transform.position = Center + Vector3.right;
            health.TakeDamage(20, source); Assert.AreEqual(14, health.CurrentHealth);
        }
        [Test] public void HealerCastsHealsAllyOnceAndCannotHealEnemyOrSelf()
        {
            var healer = Enemy(InvaderRole.Healer); var ally = Actor(Vector3.right * 2); ally.TakeDamage(40);
            var hostile = Actor(Vector3.up * 2, Faction.Dungeon); hostile.TakeDamage(60);
            healer.GetComponent<ActorHealth>().TakeDamage(30);
            Physics2D.SyncTransforms();
            Assert.IsTrue(healer.TryAct(null)); Assert.AreEqual(InvaderAction.Healing, healer.Action); Assert.AreEqual(60, ally.CurrentHealth);
            var saved = JsonUtility.FromJson<InvaderTacticsSaveData>(JsonUtility.ToJson(healer.CaptureSave()));
            saved.remaining = 0; healer.RestoreSave(saved); healer.TryAct(null);
            Assert.AreEqual(78, ally.CurrentHealth); Assert.AreEqual(40, hostile.CurrentHealth);
            Assert.AreEqual(70, healer.GetComponent<ActorHealth>().CurrentHealth);
            Assert.IsFalse(healer.TryAct(null)); Assert.AreEqual(78, ally.CurrentHealth);
        }
        [Test] public void HealingInterruptRangeWallsAndPauseAreRespected()
        {
            var healer = Enemy(InvaderRole.Healer); var ally = Actor(Vector3.right * 2); ally.TakeDamage(50);
            Physics2D.SyncTransforms(); Assert.IsTrue(healer.TryAct(null));
            healer.GetComponent<ActorHealth>().TakeDamage(1); Assert.AreEqual(InvaderAction.Ready, healer.Action);
            Assert.AreEqual(50, ally.CurrentHealth);
            healer.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Healer });
            Time.timeScale = 0; Assert.IsFalse(healer.TryAct(null)); Time.timeScale = 1;
            var wall = new GameObject("Healing wall"); objects.Add(wall); wall.layer = 8; wall.transform.position = Center + Vector3.right;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 3);
            var data = new UnityEditor.SerializedObject(healer.GetComponent<MinionController>());
            data.FindProperty("obstacleLayers").intValue = 1 << 8; data.ApplyModifiedPropertiesWithoutUndo();
            Physics2D.SyncTransforms(); Assert.IsFalse(healer.TryAct(null));
            Object.DestroyImmediate(wall); ally.transform.position = Center + Vector3.right * 8;
            healer.RestoreSave(new InvaderTacticsSaveData { role = InvaderRole.Healer });
            Physics2D.SyncTransforms(); Assert.IsFalse(healer.TryAct(null));
        }
        [Test] public void ChargeLocksDirectionHitsOnceAndHasRecovery()
        {
            var charger = Enemy(InvaderRole.Charger); var victim = Actor(Vector3.right * 3, Faction.Dungeon);
            Physics2D.SyncTransforms(); Assert.IsTrue(charger.TryAct(victim));
            Assert.AreEqual(InvaderAction.WindingUp, charger.Action); Assert.AreEqual(100, victim.CurrentHealth);
            var saved = charger.CaptureSave(); saved.remaining = 0; charger.RestoreSave(saved); charger.TryAct(victim);
            Assert.AreEqual(InvaderAction.Charging, charger.Action);
            var motor = charger.GetComponent<TopDownMotor>(); motor.ApplySlow(.5f, 2);
            Assert.That(motor.EffectiveMoveSpeed, Is.EqualTo(motor.MoveSpeed * 2.6f * .5f).Within(.01f));
            victim.transform.position = Center + Vector3.right * .8f;
            victim.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * .2f;
            Physics2D.SyncTransforms(); charger.TryAct(victim);
            Assert.AreEqual(78, victim.CurrentHealth); Assert.AreEqual(InvaderAction.Recovering, charger.Action);
            charger.TryAct(victim); Assert.AreEqual(78, victim.CurrentHealth);
            Assert.AreEqual(1, motor.SpeedMultiplier);
        }
        [UnityTest] public IEnumerator ChargeMovesInRealPhysicsAndPauseFreezesWarning()
        {
            var charger = Enemy(InvaderRole.Charger); var victim = Actor(Vector3.right * 5, Faction.Dungeon);
            charger.GetComponent<MinionController>().Assault(victim);
            yield return null;
            Assert.AreEqual(InvaderAction.WindingUp, charger.Action);
            Time.timeScale = 0; var before = charger.CaptureSave(); var position = charger.transform.position;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(charger.CaptureSave().remaining, Is.EqualTo(before.remaining).Within(.02f));
            Assert.AreEqual(position, charger.transform.position);
            Time.timeScale = 1;
            yield return new WaitForSeconds(1.55f);
            Assert.Greater(charger.transform.position.x, Center.x + 2);
            Assert.Less(victim.CurrentHealth, 100);
        }
        [Test] public void ChargeCanHitColliderlessCoreObjective()
        {
            var charger = Enemy(InvaderRole.Charger); var core = Actor(Vector3.right * 3, Faction.Dungeon);
            Object.DestroyImmediate(core.GetComponent<Collider2D>());
            charger.TryAct(core); var saved = charger.CaptureSave(); saved.remaining = 0;
            charger.RestoreSave(saved); charger.TryAct(core);
            core.transform.position = Center + Vector3.right * .8f;
            charger.TryAct(core);
            Assert.AreEqual(78, core.CurrentHealth);
        }
        private T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        [UnityTest] public IEnumerator HealerCompletesCastThroughControllerAndChargeCannotCrossTerrain()
        {
            var healer = Enemy(InvaderRole.Healer); var ally = Actor(Vector3.up * 2); ally.TakeDamage(50);
            var victim = Actor(Vector3.right * 5, Faction.Dungeon);
            healer.GetComponent<MinionController>().Assault(victim);
            yield return new WaitForSeconds(1);
            Assert.AreEqual(68, ally.CurrentHealth);
            healer.gameObject.SetActive(false); ally.gameObject.SetActive(false);
            var charger = Enemy(InvaderRole.Charger); charger.GetComponent<MinionController>().Assault(victim);
            yield return null; Assert.AreEqual(InvaderAction.WindingUp, charger.Action);
            var wall = new GameObject("Charge blocking wall"); objects.Add(wall); wall.transform.position = Center + Vector3.right * 2.5f;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.3f, 4);
            yield return new WaitForSeconds(1.5f);
            Assert.Less(charger.transform.position.x, Center.x + 2.5f);
            Assert.AreEqual(100, victim.CurrentHealth);
        }
        [UnityTest] public IEnumerator ForecastMatchesSpawnQueueAcrossAllWavesAndSaves()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            var waves = Find<WaveDirector>(); var hud = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaveHud>(true)).SingleOrDefault();
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Easy });
            for (int wave = 1; wave <= 5; wave++)
            {
                Time.timeScale = 1;
                waves.RestoreSave(new WaveSaveData { number = wave - 1, state = WaveState.Preparing, enemies = new MinionSaveData[0] });
                waves.ConfigureRun(new NewGameOptions { difficulty = wave % 2 == 0 ? GameDifficulty.Hard : GameDifficulty.Normal });
                var formation = waves.NextFormation;
                TMP_Text label = null;
                if (hud != null)
                {
                    label = hud.GetComponentInParent<Canvas>().transform.Find("Next wave formation/Formation and counters").GetComponent<TMP_Text>();
                    Assert.IsTrue(label.gameObject.activeInHierarchy);
                    Assert.That(label.text, Does.Contain(WaveFormation.Summary(formation)));
                    label.ForceMeshUpdate(); Assert.IsFalse(label.isTextOverflowing, label.text);
                }
                var arrival = waves.CaptureSave(); arrival.schedule.remaining = 0; waves.RestoreSave(arrival);
                yield return null; yield return null; Time.timeScale = 0;
                var saved = JsonUtility.FromJson<WaveSaveData>(JsonUtility.ToJson(waves.CaptureSave()));
                CollectionAssert.AreEqual(formation, saved.enemies.Select(e => e.tactics.role).Concat(saved.pendingRoles).ToArray());
                if (label != null) Assert.IsFalse(label.gameObject.activeInHierarchy);
                waves.RestoreSave(saved);
                CollectionAssert.AreEqual(saved.pendingRoles, waves.CaptureSave().pendingRoles);
                Assert.AreEqual(saved.enemies[0].tactics.role, waves.CaptureSave().enemies[0].tactics.role);
            }
            // A version-1 save lacking tactics and queued role fields restores ordinary warriors.
            waves.RestoreSave(new WaveSaveData { number = 2, state = WaveState.Active, pending = 2,
                enemies = new[] {new MinionSaveData {position = Center, health = 40, maximumHealth = 55}} });
            Assert.AreEqual(InvaderRole.Warrior, waves.CaptureSave().enemies[0].tactics.role);
            CollectionAssert.AreEqual(new[] {InvaderRole.Warrior, InvaderRole.Warrior}, waves.CaptureSave().pendingRoles);
        }
        [UnityTest] public IEnumerator ResumedWaveSpawnsEntireAdvertisedFormationAndValidatesSavedRoles()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            var waves = Find<WaveDirector>(); Time.timeScale = 1;
            waves.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Easy });
            waves.RestoreSave(new WaveSaveData { number = RegionCatalog.DaysPerRegion - 1, state = WaveState.Preparing, enemies = new MinionSaveData[0] });
            var expected = waves.NextFormation;
            var data = new UnityEditor.SerializedObject(waves); data.FindProperty("spawnInterval").floatValue = .05f; data.ApplyModifiedPropertiesWithoutUndo();
            waves.BeginNextWave(); var midway = JsonUtility.FromJson<WaveSaveData>(JsonUtility.ToJson(waves.CaptureSave()));
            waves.RestoreSave(midway);
            var session = Find<GameSaveSession>();
            for (float end = Time.time + 5; session.Capture().wave.pending > 0 && Time.time < end;) yield return null;
            var saved = session.Capture();
            Assert.AreEqual(0, saved.wave.pending);
            CollectionAssert.AreEqual(expected, saved.wave.enemies.Select(e => e.tactics.role).ToArray());
            Assert.AreEqual(1, saved.wave.enemies.Count(e => e.tactics.role == InvaderRole.Archer));
            Assert.AreEqual(1, saved.wave.enemies.Count(e => e.tactics.hero), "Rowen arrives exactly once, last");
            Assert.IsTrue(saved.wave.enemies[saved.wave.enemies.Length - 1].tactics.hero);
            SaveSlotStore.Validate(saved);
            saved.wave.enemies[0].tactics.role = (InvaderRole)99;
            Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(saved));
            saved.wave.enemies[0].tactics.role = InvaderRole.Shieldbearer;
            saved.wave.pendingRoles = new[] {InvaderRole.Charger};
            Assert.Throws<System.IO.InvalidDataException>(() => SaveSlotStore.Validate(saved));
        }
    }
}
#endif

