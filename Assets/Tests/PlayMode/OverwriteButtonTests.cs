#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class OverwriteButtonTests
    {
        private GameObject root, eventRoot;
        private GameObject previousSelection;

        [TearDown] public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (eventRoot != null) Object.DestroyImmediate(eventRoot);
            else if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
        }

        [UnityTest] public IEnumerator FocusedOverwriteButtonsRespondToPointerAndReturnToNormal()
        {
            if (EventSystem.current == null) eventRoot = new GameObject("Test EventSystem", typeof(EventSystem));
            previousSelection = EventSystem.current.currentSelectedGameObject;
            root = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Shared/SaveSlotPanel.prefab"));
            root.SetActive(true);
            var panel = root.transform.Find("Overwrite Confirmation");
            panel.gameObject.SetActive(true);
            yield return null;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            foreach (var button in panel.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                Assert.AreSame(button.image.sprite, button.image.overrideSprite, "Keyboard focus alone uses normal art.");
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
                Assert.AreSame(button.spriteState.highlightedSprite, button.image.overrideSprite, "Focused button must still highlight on hover.");
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                Assert.AreSame(button.spriteState.pressedSprite, button.image.overrideSprite);
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                bool clicked = false;
                button.onClick.AddListener(() => clicked = true);
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                Assert.IsTrue(clicked);
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerExitHandler);
                Assert.AreSame(button.image.sprite, button.image.overrideSprite, "Pointer exit restores normal art while keeping focus.");
            }
        }
    }
}
#endif
