#if UNITY_EDITOR
using System.Collections;
using DarkLord.Actors;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class PlayerDirectionalAnimationTests
    {
        private GameObject actor;

        [TearDown]
        public void Cleanup()
        {
            if (actor != null) Object.DestroyImmediate(actor);
        }

        [UnityTest]
        public IEnumerator EightDirectionsSelectCorrectWalkAndIdleSprites()
        {
            actor = new GameObject("Directional animation test");
            var sprite = actor.AddComponent<SpriteRenderer>();
            var animator = actor.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Animations/Player/PlayerDirectional.controller");
            Assert.IsNotNull(animator.runtimeAnimatorController);
            var view = actor.AddComponent<ActorAnimation>();
            var config = new SerializedObject(view);
            config.FindProperty("useExistingSpeedParameter").boolValue = true;
            config.FindProperty("useDirectionalParameters").boolValue = true;
            config.ApplyModifiedPropertiesWithoutUndo();
            Vector2[] directions = { Vector2.down, new Vector2(1,-1), Vector2.right,
                Vector2.one, Vector2.up, new Vector2(-1,1), Vector2.left, -Vector2.one };
            string[] sheets = { "Front", "Front Side", "Sidet", "Back Side", "Back", "Back Side", "Sidet", "Front Side" };
            int[] walkRows = { 0, 1, 2, 3, 4, 3, 2, 1 };
            for (int i = 0; i < directions.Length; i++)
            {
                // Consecutive Run calls exercise turning without changing locomotion state.
                view.Present(ActorState.Run, directions[i]);
                animator.Update(0.1f);
                animator.Update(0.1f);
                Assert.That(animator.GetFloat("Speed"), Is.EqualTo(1));
                Assert.IsNotNull(sprite.sprite);
                Assert.That(sprite.sprite.texture.name, Is.EqualTo("Walk"));
                Assert.That(sprite.sprite.rect.y, Is.EqualTo(256 - walkRows[i] * 64));
                Assert.That(sprite.sprite.pixelsPerUnit, Is.EqualTo(16));
                Assert.That(sprite.flipX, Is.EqualTo(directions[i].x < 0));
            }
            for (int i = 0; i < directions.Length; i++)
            {
                view.Present(ActorState.Idle, directions[i]);
                animator.Update(0.1f);
                animator.Update(0.1f);
                Assert.That(animator.GetFloat("Speed"), Is.EqualTo(0));
                Assert.That(sprite.sprite.texture.name, Is.EqualTo("Idle Collection " + sheets[i]));
                Assert.That(sprite.flipX, Is.EqualTo(directions[i].x < 0));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator StoryExpressionsSurviveLocomotionAndReturnOrCancelOnDeath()
        {
            actor = new GameObject("Story expression test");
            var sprite = actor.AddComponent<SpriteRenderer>();
            var animator = actor.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Animations/Player/PlayerDirectional.controller");
            var view = actor.AddComponent<ActorAnimation>();
            var config = new SerializedObject(view);
            config.FindProperty("useExistingSpeedParameter").boolValue = true;
            config.FindProperty("useDirectionalParameters").boolValue = true;
            config.ApplyModifiedPropertiesWithoutUndo();
            foreach (PlayerExpression expression in System.Enum.GetValues(typeof(PlayerExpression)))
            {
                foreach (var facing in new[] { Vector2.down, Vector2.left })
                {
                    view.Present(ActorState.Idle, facing);
                    animator.Update(.01f);
                    Assert.IsTrue(view.PlayExpression(expression, 0));
                    view.Present(ActorState.Run, Vector2.up);
                    animator.Update(.01f);
                    Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Expression_" + expression));
                    Assert.That(sprite.sprite.texture.name, Is.EqualTo("Idle Collection " + (facing.x < 0 ? "Sidet" : "Front")));
                    Assert.That(sprite.flipX, Is.EqualTo(facing.x < 0));
                    view.StopExpression();
                    view.Present(ActorState.Run, facing);
                    animator.Update(.1f); animator.Update(.1f);
                    Assert.IsFalse(view.IsExpressionPlaying);
                    Assert.IsFalse(animator.GetCurrentAnimatorStateInfo(0).IsName("Expression_" + expression));
                }
            }
            Assert.IsTrue(view.PlayExpression(PlayerExpression.Accept, .02f));
            yield return new WaitForSeconds(.05f);
            Assert.IsFalse(view.IsExpressionPlaying);
            Assert.IsTrue(view.PlayExpression(PlayerExpression.Crying, 0));
            view.Present(ActorState.Die, Vector2.down);
            Assert.IsFalse(view.IsExpressionPlaying);
            Assert.IsFalse(view.PlayExpression(PlayerExpression.Accept));
        }

        [Test]
        public void StoppingKeepsLastFacingAndAnalogDirectionsSnapToEightSectors()
        {
            actor = new GameObject("Facing test");
            var motor = actor.AddComponent<TopDownMotor>();
            motor.Move(new Vector2(-1, 1));
            Vector2 facing = motor.Facing;
            motor.Move(Vector2.zero);
            Assert.That(motor.Facing, Is.EqualTo(facing));
            Assert.That(ActorAnimation.QuantizeFacing(new Vector2(0.1f, 1)), Is.EqualTo(Vector2.up));
            Assert.That(ActorAnimation.QuantizeFacing(new Vector2(-0.8f, -1)), Is.EqualTo(-Vector2.one));
        }
    }
}
#endif
