#if UNITY_EDITOR
using System.Collections;
using DarkLord.Actors;
using DarkLord.Minions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class HeroAnimationTests
    {
        private GameObject actor;

        [Test]
        public void AllHeroTransitionsIncludingNestedStatesHaveAnExitTimeOrCondition()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Gameplay/Animations/Hero/Hero.controller");
            foreach (var asset in assets)
            {
                if (!(asset is UnityEditor.Animations.AnimatorState state)) continue;
                foreach (var transition in state.transitions)
                    Assert.That(transition.hasExitTime || transition.conditions.Length > 0, Is.True,
                        state.name + " -> " + transition.destinationState.name);
            }
        }

        [TearDown]
        public void Cleanup()
        {
            if (actor != null) Object.DestroyImmediate(actor);
        }

        private void Spawn()
        {
            actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Gameplay/Prefabs/Enemies/WaveInvader.prefab"));
            actor.transform.position = new Vector3(1000, 1000, 0);
        }

        [UnityTest]
        public IEnumerator ParametersSelectWalkAndTurningDoesNotRestartIt()
        {
            Spawn();
            actor.GetComponent<MinionController>().enabled = false;
            var view = actor.GetComponent<ActorAnimation>();
            var animator = actor.GetComponent<Animator>();
            var renderer = actor.GetComponent<SpriteRenderer>();
            ActorState[] states = { ActorState.Idle, ActorState.Run, ActorState.Attack, ActorState.Hit, ActorState.Die };
            int[] rows = { 672, 576, 288, 96, 0 };
            for (int i = 0; i < states.Length; i++)
            {
                view.Present(states[i], Vector2.left);
                animator.Update(0);
                animator.Update(0);
                Assert.That(animator.GetFloat("Move_Speed"), Is.EqualTo(states[i] == ActorState.Run ? 1 : 0));
                Assert.That(renderer.sprite.rect.y, Is.EqualTo(rows[i]));
                Assert.That(renderer.flipX, Is.True);
                var first = renderer.sprite;
                animator.Update(0.13f);
                Assert.That(renderer.sprite, Is.Not.EqualTo(first), states[i].ToString());
            }
            animator.Update(2);
            Assert.That(renderer.sprite.name, Is.EqualTo("Death_9"));
            Object.DestroyImmediate(actor);
            Spawn();
            actor.GetComponent<MinionController>().enabled = false;
            view = actor.GetComponent<ActorAnimation>();
            animator = actor.GetComponent<Animator>();
            renderer = actor.GetComponent<SpriteRenderer>();
            view.Present(ActorState.Run, Vector2.left);
            animator.Update(0);
            animator.Update(0.2f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Walk"), Is.True);
            float progress = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            view.Present(ActorState.Run, Vector2.right);
            animator.Update(0);
            Assert.That(renderer.flipX, Is.False);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(progress).Within(0.001f));
            Assert.That(renderer.sprite.pixelsPerUnit, Is.EqualTo(16));
            Assert.That(renderer.color, Is.EqualTo(Color.white));
            // A parameter change alone must drive the Animator, without Present/Play.
            animator.SetTrigger("Attack_1");
            animator.Update(0);
            animator.Update(0);
            Assert.That(renderer.sprite.rect.y, Is.EqualTo(288));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeparateTriggersRecoverToLocomotionAndDeathRemainsFinal()
        {
            Spawn();
            actor.GetComponent<MinionController>().enabled = false;
            var animator = actor.GetComponent<Animator>();
            var renderer = actor.GetComponent<SpriteRenderer>();
            Assert.That(animator.HasState(0, Animator.StringToHash("Base Layer.Run")), Is.False);
            foreach (var parameter in animator.parameters) Assert.That(parameter.name, Is.Not.EqualTo("State"));
            string[] triggers = { "Attack_1", "Attack_2", "Block", "Hit" };
            int[] rows = { 288, 192, 384, 96 };
            foreach (float speed in new[] { 0f, 1f })
            {
                animator.SetFloat("Move_Speed", speed);
                for (int i = 0; i < triggers.Length; i++)
                {
                    animator.SetTrigger(triggers[i]);
                    animator.Update(0);
                    animator.Update(0);
                    Assert.That(renderer.sprite.rect.y, Is.EqualTo(rows[i]), triggers[i]);
                    for (int frame = 0; frame < 70; frame++) animator.Update(0.02f);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(speed == 0 ? "Base Layer.Idle" : "Base Layer.Walk"), Is.True, triggers[i]);
                }
            }
            var view = actor.GetComponent<ActorAnimation>();
            view.Present(ActorState.Attack, Vector2.right);
            // Fatal damage can arrive before the Animator consumes the attack trigger.
            view.Present(ActorState.Die, Vector2.right);
            for (int frame = 0; frame < 100; frame++) animator.Update(0.02f);
            Assert.That(renderer.sprite.name, Is.EqualTo("Death_9"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DamageInterruptsWithHitAndDeathHoldsLastFrame()
        {
            Spawn();
            var health = actor.GetComponent<ActorHealth>();
            var controller = actor.GetComponent<MinionController>();
            var animator = actor.GetComponent<Animator>();
            var renderer = actor.GetComponent<SpriteRenderer>();
            health.TakeDamage(1, null);
            animator.Update(0);
            animator.Update(0);
            Assert.That(controller.State, Is.EqualTo(ActorState.Hit));
            Assert.That(renderer.sprite.rect.y, Is.EqualTo(96));
            health.TakeDamage(10000, null);
            animator.Update(0);
            animator.Update(0);
            Assert.That(controller.State, Is.EqualTo(ActorState.Die));
            animator.Update(1.2f);
            Assert.That(renderer.sprite.name, Is.EqualTo("Death_9"));
            Assert.That(actor.GetComponent<Collider2D>().enabled, Is.False);
            yield return null;
        }
    }
}
#endif
