using System.Collections;
using DarkLord.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DarkLord.Persistence;

namespace DarkLord.Tests
{
    public sealed class OpeningStoryTests
    {
        [UnityTest]
        public IEnumerator OpeningAdvancesWhilePausedAndCompletesOnce()
        {
            var owner = new GameObject("Story Test");
            float previousTime = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                int completed = 0;
                var story = OpeningStory.Play(owner.transform, TMP_Settings.defaultFontAsset, () => completed++);
                Assert.IsNotNull(story);
                yield return null;
                var label = story.transform.Find("Bottom Box/Subtitle").GetComponent<TMP_Text>();
                var triangle = story.transform.Find("Bottom Box/Advance Triangle").GetComponent<RectTransform>();
                Assert.IsTrue(story.IsTyping);
                Assert.IsFalse(triangle.gameObject.activeSelf);
                int initiallyVisible = label.maxVisibleCharacters;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.Greater(label.maxVisibleCharacters, initiallyVisible, "Typing should continue while paused.");
                Assert.IsTrue(story.IsTyping);
                var firstImage = story.GetComponentInChildren<RawImage>().texture;
                string firstSubtitle = label.text;
                story.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
                Assert.IsFalse(story.IsTyping);
                Assert.AreEqual(firstImage, story.GetComponentInChildren<RawImage>().texture, "A click during typing reveals text without changing the cut.");
                Assert.IsTrue(triangle.gameObject.activeSelf);
                float initialY = triangle.anchoredPosition.y;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.AreNotEqual(initialY, triangle.anchoredPosition.y, "The downward triangle should bob while paused.");
                Assert.IsNotNull(triangle.GetComponent<CanvasRenderer>());
                var mesh = triangle.GetComponent<CanvasRenderer>().GetMesh();
                Assert.IsNotNull(mesh);
                Assert.AreEqual(3, mesh.vertexCount, "The advance indicator should render a triangle.");
                for (int i = 1; i <= 5; i++)
                {
                    Assert.IsNotNull(story.GetComponentInChildren<RawImage>().texture);
                    if (story.IsTyping) story.Next();
                    if (i == 2)
                    {
                        story.Previous();
                        yield return WaitForTransition(story);
                        Assert.AreEqual(firstSubtitle, label.text);
                        story.Next(); // Finish typing after going back.
                        story.Next();
                        yield return WaitForTransition(story);
                        story.Next(); // Finish typing on the second cut.
                    }
                    story.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
                    if (i < 5)
                    {
                        Assert.IsTrue(story.IsTransitioning);
                        Assert.IsFalse(triangle.gameObject.activeSelf);
                        var burning = story.transform.Find("Art Area/Burning Page").GetComponent<RawImage>();
                        Assert.IsNotNull(burning.texture);
                        Assert.IsTrue(burning.material.shader.isSupported);
                        int circleCount = Mathf.RoundToInt(burning.material.GetFloat("_BurnCount"));
                        Assert.That(circleCount, Is.InRange(2, 3));
                        var firstCenter = burning.material.GetVector("_BurnCenter1");
                        var secondCenter = burning.material.GetVector("_BurnCenter2");
                        Assert.Greater(Vector2.Distance(firstCenter, secondCenter), .15f, "Burn circles should ignite at separate points.");
                        Color ember = burning.material.GetColor("_EmberColor");
                        Assert.Greater(ember.r, .9f);
                        Assert.Less(ember.g, .2f, "Scarlet fire should not have a yellow core.");
                        var currentImage = story.GetComponentInChildren<RawImage>().texture;
                        story.Next(); // Repeated clicks during a burn must not skip a cut.
                        Assert.AreEqual(currentImage, story.GetComponentInChildren<RawImage>().texture);
                    }
                    yield return WaitForTransition(story);
                }
                story.Finish();
                Assert.AreEqual(1, completed);
                Assert.AreEqual(0, Time.timeScale);
                yield return null;
                Assert.IsTrue(story == null);
                var skipped = OpeningStory.Play(owner.transform, TMP_Settings.defaultFontAsset, () => completed++);
                skipped.Finish(); skipped.Finish();
                Assert.AreEqual(2, completed);
                yield return null;
                Assert.IsTrue(skipped == null);
            }
            finally
            {
                Time.timeScale = previousTime;
                Object.DestroyImmediate(owner);
            }
        }

        [UnityTest]
        public IEnumerator StorySceneShowsLowerSubtitlesAndLastClickLoadsGameplay()
        {
            Assert.IsTrue(Application.CanStreamedLevelBeLoaded(StorySceneController.ScenePath));
            GameSaveSession.BeginNew(2);
            yield return SceneManager.LoadSceneAsync(StorySceneController.ScenePath, LoadSceneMode.Single);
            yield return null;
            var story = Object.FindFirstObjectByType<OpeningStory>();
            Assert.IsNotNull(story);
            Assert.IsNull(story.transform.Find("Top Box"));
            Assert.IsNull(story.transform.Find("Top Box/Progress"));
            Assert.IsNull(story.transform.Find("Bottom Box/Click Hint"));
            Assert.AreEqual(Vector2.zero, story.transform.Find("Art Area").GetComponent<RectTransform>().anchorMin);
            Assert.AreEqual(Vector2.one, story.transform.Find("Art Area").GetComponent<RectTransform>().anchorMax);
            var lower = story.transform.Find("Bottom Box").GetComponent<RectTransform>();
            Assert.AreEqual(Vector2.zero, lower.anchorMin);
            Assert.AreEqual(new Vector2(1, .22f), lower.anchorMax);
            var subtitle = lower.Find("Subtitle").GetComponent<TMP_Text>();
            Assert.IsTrue(subtitle.text.Contains("흑관성"));
            var events = Object.FindFirstObjectByType<EventSystem>();
            Assert.IsNotNull(events);
            var pointer = new PointerEventData(events) { position = new Vector2(Screen.width * .5f, Screen.height * .1f) };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            events.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, "Lower subtitle box should accept a screen click.");
            for (int i = 0; i < 4; i++)
            {
                if (story.IsTyping) ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                string previousSubtitle = subtitle.text;
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                Assert.IsTrue(story.IsTransitioning);
                Assert.AreEqual("", subtitle.text);
                yield return WaitForTransition(story);
                Assert.AreNotEqual(previousSubtitle, subtitle.text);
                Assert.IsTrue(story.IsTyping);
                if (i == 2)
                {
                    Assert.IsTrue(subtitle.text.Contains("릴리아와 루브"));
                    Assert.IsFalse(subtitle.text.Contains("코어를 회수"));
                }
            }
            if (story.IsTyping) ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(lower.Find("Advance Triangle").gameObject.activeSelf);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            float deadline = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().path != "Assets/Scenes/SampleScene.unity" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.AreEqual("Assets/Scenes/SampleScene.unity", SceneManager.GetActiveScene().path);
            Assert.AreEqual(2, GameSaveSession.ActiveSlot);
            Assert.AreEqual(1, Time.timeScale, "Gameplay must start with its clock running.");
        }

        private static IEnumerator WaitForTransition(OpeningStory story)
        {
            float deadline = Time.realtimeSinceStartup + 5;
            while (story != null && story.IsTransitioning && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (story != null) Assert.IsFalse(story.IsTransitioning, "Burn transition should finish even while paused.");
        }
    }
}
