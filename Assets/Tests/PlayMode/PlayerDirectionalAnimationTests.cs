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
            int[] rows = { 0, 1, 2, 3, 4, 3, 2, 1 };
            for (int i = 0; i < directions.Length; i++)
            {
                // Consecutive Run calls exercise turning without changing locomotion state.
                view.Present(ActorState.Run, directions[i]);
                animator.Update(0.1f);
                animator.Update(0.1f);
                Assert.That(animator.GetFloat("Speed"), Is.EqualTo(1));
                Assert.IsNotNull(sprite.sprite);
                Assert.That(sprite.sprite.texture.name, Is.EqualTo("PlayerWalkSheet"));
                Assert.That(sprite.sprite.rect.y, Is.EqualTo(256 - rows[i] * 64));
                Assert.That(sprite.flipX, Is.EqualTo(directions[i].x < 0));
            }
            for (int i = 0; i < directions.Length; i++)
            {
                view.Present(ActorState.Idle, directions[i]);
                animator.Update(0.1f);
                animator.Update(0.1f);
                Assert.That(animator.GetFloat("Speed"), Is.EqualTo(0));
                Assert.That(sprite.sprite.texture.name, Is.EqualTo("PlayerIdleDefaultSheet"));
                Assert.That(sprite.sprite.rect.y, Is.EqualTo(256 - rows[i] * 64));
                Assert.That(sprite.flipX, Is.EqualTo(directions[i].x < 0));
            }
            yield return null;
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
