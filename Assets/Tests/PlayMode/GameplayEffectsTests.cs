using System.Collections;
using System.Linq;
using DarkLord.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class GameplayEffectsTests
    {
        [UnityTest]
        public IEnumerator RepeatedHealingKeepsOneReadableEffectPerActor()
        {
            var scene = SceneManager.CreateScene("Healing overlap validation");
            var owner = new GameObject("Healing target");
            SceneManager.MoveGameObjectToScene(owner, scene);
            float previousScale = Time.timeScale;
            try
            {
                Time.timeScale = 1;
                for (int i = 0; i < 10; i++) GameplayEffects.Show(owner.transform, GameplayEffectKind.Heal, Vector3.zero, follow: true);
                var pool = scene.GetRootGameObjects().Single(g => g.GetComponent<GameplayEffects>() != null);
                Assert.AreEqual(1, pool.GetComponentsInChildren<SpriteRenderer>().Count(r => r.enabled));
                yield return new WaitForSeconds(.7f);
                Assert.IsFalse(pool.GetComponentsInChildren<SpriteRenderer>().Any(r => r.enabled));
                GameplayEffects.Show(owner.transform, GameplayEffectKind.Heal, Vector3.zero, follow: true);
                Assert.AreEqual(1, pool.GetComponentsInChildren<SpriteRenderer>().Count(r => r.enabled));
                Assert.AreEqual(1, pool.GetComponentsInChildren<SpriteRenderer>().Length);
            }
            finally { Time.timeScale = previousScale; }
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator ShieldHoldsItsReadableFrameUntilTheEndingTransition()
        {
            var scene = SceneManager.CreateScene("Shield timing validation");
            var owner = new GameObject("Shield owner");
            SceneManager.MoveGameObjectToScene(owner, scene);
            float previousScale = Time.timeScale;
            try
            {
                Time.timeScale = 1;
                GameplayEffects.Show(owner.transform, GameplayEffectKind.Aegis, Vector3.zero, follow: true, duration: .8f);
                yield return new WaitForSeconds(.25f);
                var renderer = scene.GetRootGameObjects().Single(g => g.GetComponent<GameplayEffects>() != null).GetComponentInChildren<SpriteRenderer>();
                var fullShield = Resources.Load<GameplayEffectLibrary>("GameplayEffects").Find(GameplayEffectKind.Aegis).frames[5];
                Assert.IsTrue(renderer.enabled);
                Assert.AreSame(fullShield, renderer.sprite);
                yield return new WaitForSeconds(.25f);
                Assert.IsTrue(renderer.enabled);
                Assert.AreSame(fullShield, renderer.sprite);
                yield return new WaitForSeconds(.4f);
                Assert.IsFalse(renderer.enabled);
            }
            finally { Time.timeScale = previousScale; }
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator PlaybackIsBoundedPausesAndReusesRenderers()
        {
            var scene = SceneManager.CreateScene("Effect pool validation");
            var owner = new GameObject("Effect owner");
            SceneManager.MoveGameObjectToScene(owner, scene);
            float previousScale = Time.timeScale;
            try
            {
                Time.timeScale = 1;
                for (int i = 0; i < 80; i++) GameplayEffects.Show(owner.transform, GameplayEffectKind.Slash, Vector3.zero);
                var pool = scene.GetRootGameObjects().Single(g => g.GetComponent<GameplayEffects>() != null);
                Assert.AreEqual(48, pool.GetComponentsInChildren<SpriteRenderer>().Length);
                Time.timeScale = 0;
                yield return new WaitForSecondsRealtime(.4f);
                Assert.AreEqual(48, pool.GetComponentsInChildren<SpriteRenderer>().Count(r => r.enabled));
                Time.timeScale = 1;
                yield return new WaitForSeconds(.4f);
                Assert.IsFalse(pool.GetComponentsInChildren<SpriteRenderer>().Any(r => r.enabled));
                GameplayEffects.Show(owner.transform, GameplayEffectKind.Heal, Vector3.zero, follow: true);
                owner.transform.position = new Vector3(3, 4);
                yield return null;
                Assert.AreEqual(new Vector3(3, 3.9f), pool.GetComponentsInChildren<SpriteRenderer>().Single(r => r.enabled).transform.position);
                Assert.AreEqual(48, pool.GetComponentsInChildren<SpriteRenderer>().Length);
                owner.SetActive(false);
                yield return null;
                Assert.IsFalse(pool.GetComponentsInChildren<SpriteRenderer>().Any(r => r.enabled));
            }
            finally { Time.timeScale = previousScale; }
            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.IsFalse(Object.FindObjectsByType<GameplayEffects>(FindObjectsSortMode.None).Any(p => p.gameObject.scene == scene));
        }

        [Test]
        public void AllConfiguredFramesExistAndPlayInSheetOrder()
        {
            var library = Resources.Load<GameplayEffectLibrary>("GameplayEffects");
            Assert.NotNull(library);
            foreach (GameplayEffectKind kind in System.Enum.GetValues(typeof(GameplayEffectKind)))
            {
                var clip = library.Find(kind);
                Assert.NotNull(clip, kind.ToString());
                Assert.Greater(clip.duration, 0);
                Assert.Greater(clip.width, 0);
                Assert.IsNotEmpty(clip.frames);
                for (int i = 0; i < clip.frames.Length; i++)
                {
                    Assert.NotNull(clip.frames[i]);
                    if (i == 0) continue;
                    if (clip.frames[i - 1].texture != clip.frames[i].texture) continue; // Summon opens, then closes on a second sheet.
                    var a = clip.frames[i - 1].rect; var b = clip.frames[i].rect;
                    Assert.IsTrue(a.y > b.y || (a.y == b.y && a.x < b.x), kind.ToString());
                }
            }
        }
    }
}
