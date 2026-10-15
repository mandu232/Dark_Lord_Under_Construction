#if UNITY_EDITOR
using System.Collections;
using DarkLord.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class PixelSliderTests
    {
        private GameObject root;
        private float previousTimeScale;
        [SetUp] public void Setup() => previousTimeScale = Time.timeScale;
        [TearDown] public void Cleanup() { if (root != null) Object.DestroyImmediate(root); Time.timeScale = previousTimeScale; }

        [UnityTest] public IEnumerator SettingsSlidersUseMatchingPixelsAndRotateWithoutChangingValue()
        {
            root = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/SettingsPanel.prefab"));
            root.SetActive(true);
            yield return null;
            var sliders = root.GetComponentsInChildren<UnityEngine.UI.Slider>(true);
            Assert.AreEqual(4, sliders.Length);
            float pixels = 1080f / 242f;
            foreach (var slider in sliders)
            {
                var track = slider.transform.Find("Track").GetComponent<UnityEngine.UI.Image>();
                var visual = slider.handleRect.Find("Handle Visual").GetComponent<UnityEngine.UI.Image>();
                Assert.AreEqual("Assets/Sprtie/UI/Slider/SliderTrack.png", UnityEditor.AssetDatabase.GetAssetPath(track.sprite));
                Assert.AreEqual("Assets/Sprtie/UI/Slider/SliderHander.png", UnityEditor.AssetDatabase.GetAssetPath(visual.sprite));
                Assert.AreEqual(pixels, track.rectTransform.rect.height / track.sprite.rect.height, .001f);
                Assert.AreEqual(pixels, visual.rectTransform.rect.width / visual.sprite.rect.width, .001f);
                Assert.AreEqual(pixels, visual.rectTransform.rect.height / visual.sprite.rect.height, .001f);
                Assert.AreEqual(slider.handleRect.rect.height, slider.handleRect.rect.width, .001f);
                var fill = slider.fillRect.GetComponent<UnityEngine.UI.Image>();
                Assert.AreEqual("Assets/Sprtie/UI/Slider/SliderTrack_Bar.png", UnityEditor.AssetDatabase.GetAssetPath(fill.sprite));
                Assert.IsTrue(fill.gameObject.activeInHierarchy);
                Assert.AreEqual(UnityEngine.UI.Image.Type.Sliced, fill.type);
                Assert.AreEqual(track.pixelsPerUnitMultiplier, fill.pixelsPerUnitMultiplier, .001f);
                Assert.AreEqual(pixels, ((RectTransform)fill.rectTransform.parent).rect.height / fill.sprite.rect.height, .001f);
                foreach (float percent in new[] { 0f, .25f, .5f, 1f })
                {
                    slider.SetValueWithoutNotify(percent);
                    Assert.AreEqual(percent, fill.rectTransform.anchorMax.x, .001f);
                }
            }
            var first = sliders[0];
            var handle = (RectTransform)first.handleRect.Find("Handle Visual");
            var behaviour = first.GetComponent<SliderHandleOrientation>();
            first.SetValueWithoutNotify(.37f);
            var before = first.handleRect.anchoredPosition;
            var select = new BaseEventData(EventSystem.current);
            ExecuteEvents.Execute(first.gameObject, select, ExecuteEvents.selectHandler);
            Assert.AreEqual(0, handle.localEulerAngles.z, .01f);
            Time.timeScale = 0;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            behaviour.OnPointerDown(pointer); behaviour.OnBeginDrag(pointer);
            Assert.AreEqual(0, handle.localEulerAngles.z, .01f, "Press must begin a transition, not snap.");
            yield return new WaitForSecondsRealtime(.05f);
            Assert.Greater(handle.localEulerAngles.z, 0);
            Assert.Less(handle.localEulerAngles.z, 90);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(90, handle.localEulerAngles.z, .01f);
            behaviour.OnPointerUp(pointer); behaviour.OnEndDrag(pointer);
            Assert.AreEqual(90, handle.localEulerAngles.z, .01f, "Release must begin a smooth return.");
            yield return new WaitForSecondsRealtime(.05f);
            Assert.Greater(handle.localEulerAngles.z, 0);
            Assert.Less(handle.localEulerAngles.z, 90);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(0, handle.localEulerAngles.z, .01f);
            ExecuteEvents.Execute(first.gameObject, select, ExecuteEvents.selectHandler);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(0, handle.localEulerAngles.z, .01f, "Keeping keyboard focus must not rotate the handle.");
            Assert.AreEqual(.37f, first.value, .001f);
            Assert.AreEqual(before, first.handleRect.anchoredPosition);
            behaviour.OnPointerDown(pointer);
            first.gameObject.SetActive(false);
            Assert.AreEqual(0, handle.localEulerAngles.z, .01f);
            first.gameObject.SetActive(true);
            Assert.AreEqual(0, handle.localEulerAngles.z, .01f);
        }
    }
}
#endif
