using System.Collections;
using System.Linq;
using DarkLord.Gameplay;
using DarkLord.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class DefeatPresentationTests
    {
        [UnityTest] public IEnumerator CoreTurnsItsGemThenCollapsesIntoTheEndCardWhenHealthRunsOut()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); yield return null; yield return null;
            T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).First();
            var waves = Find<WaveDirector>();
            var body = waves.Core.transform.Find("Dungeon core artwork/Intact").GetComponent<SpriteRenderer>();
            yield return new WaitForSeconds(1);
            var first = body.sprite;
            Assert.AreEqual(96 / 16f, first.rect.width / first.pixelsPerUnit, "The core spans about two tiles (and the art's glow margin).");
            yield return new WaitForSeconds(1);
            Assert.AreNotEqual(first, body.sprite, "The gem turns");
            Assert.Greater(Vector2.Distance(waves.Lord.transform.position, waves.Core.transform.position), 1.5f, "The lord starts beside the dais, not on it");
            Assert.IsNotEmpty(Physics2D.OverlapPointAll(waves.Core.transform.position).Where(c => c.transform == waves.Core.transform).ToArray(), "The body blocks the dais");

            var defeat = Find<DefeatPresentation>();
            Assert.IsFalse(defeat.Playing);
            waves.Core.TakeDamage(99999);
            yield return null; yield return null;
            Assert.AreEqual(WaveState.Defeated, waves.State);
            Assert.IsTrue(defeat.Playing);
            yield return new WaitForSeconds(7);
            Assert.AreEqual(160, body.sprite.rect.height, "The collapse sheet is holding its wreck frame");
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}

