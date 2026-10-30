#if UNITY_EDITOR
using System.Collections;
using DarkLord.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DarkLord.Tests
{
    public sealed class MinionInspectionLayoutTests
    {
        [UnityTest]
        public IEnumerator HiddenAuthoredLayoutPreservesCloseButtonAndScrollsStats()
        {
            var host = new GameObject("Inspection layout regression", typeof(RectTransform), typeof(Canvas));
            host.SetActive(false);
            host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/InGame/MinionInspectionPanel.prefab");
                var root = Object.Instantiate(prefab, host.transform, false);
                var panel = root.GetComponent<MinionInspectionPanel>();
                var data = new SerializedObject(panel);
                var window = (GameObject)data.FindProperty("window").objectReferenceValue;
                var stats = (TMP_Text)data.FindProperty("statsText").objectReferenceValue;
                Assert.IsFalse(window.activeSelf, "Reproduce initialization with the authored window hidden.");
                var close = ((UnityEngine.UI.Button)data.FindProperty("closeButton").objectReferenceValue).GetComponent<RectTransform>();
                var authoredPosition = new Vector2(37, 91);
                var authoredSize = new Vector2(284, 64);
                close.anchoredPosition = authoredPosition;
                close.sizeDelta = authoredSize;
                host.SetActive(true);
                yield return null;
                yield return null;
                Assert.AreEqual(authoredPosition, close.anchoredPosition, "Initialization must preserve the authored close-button position.");
                Assert.AreEqual(authoredSize, close.sizeDelta, "Pixel correction must not resize the close button.");
                // Supply deterministic content without requiring a gameplay target.
                panel.enabled = false;
                window.SetActive(true);
                stats.text = "";
                for (int i = 0; i < 40; i++) stats.text += "Inspection stat line " + i + "\n";
                Canvas.ForceUpdateCanvases();
                stats.ForceMeshUpdate();
                var scrolls = root.GetComponentsInChildren<ScrollRect>(true);
                Assert.AreEqual(1, scrolls.Length, "Awake must reuse the inactive prefab viewport instead of nesting another scroll.");
                var scroll = scrolls[0];
                Assert.Greater(scroll.viewport.rect.height, 0);
                Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height);
                Assert.Greater(stats.textInfo.characterCount, 0);
                scroll.verticalNormalizedPosition = 1;
                var top = scroll.content.anchoredPosition;
                scroll.verticalNormalizedPosition = 0;
                Canvas.ForceUpdateCanvases();
                Assert.Greater(Vector2.Distance(top, scroll.content.anchoredPosition), 1, "The overflowing stats must actually scroll.");
            }
            finally { Object.DestroyImmediate(host); }
        }
    }
}
#endif
