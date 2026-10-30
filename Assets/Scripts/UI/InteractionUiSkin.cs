using UnityEngine;

namespace DarkLord.UI
{
    public sealed class InteractionUiSkin : ScriptableObject
    {
        public Sprite frame, normalButton, activeButton;
        public GameObject windowTemplate;

        public static void Apply(GameObject window)
        {
            if (window == null) return;
            var skin = Resources.Load<InteractionUiSkin>("InteractionUiSkin");
            if (skin == null) return;
            var background = window.GetComponent<UnityEngine.UI.Image>();
            if (background != null) { background.sprite = null; background.color = Color.clear; }
            var authored = window.transform.Find("Castle Frame");
            if (authored == null || authored.GetComponent<UnityEngine.UI.Image>() == null ||
                authored.GetComponent<UnityEngine.UI.Image>().sprite == null)
            {
                if (authored != null) authored.gameObject.SetActive(false);
                var source = skin.windowTemplate != null ? skin.windowTemplate.transform.Find("Castle Frame") : null;
                if (source != null)
                {
                    var existing = window.transform.Find("Interaction Frame");
                    if (existing == null)
                    {
                        var copy = Instantiate(source.gameObject, window.transform, false);
                        copy.name = "Interaction Frame"; copy.transform.SetAsFirstSibling();
                        copy.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                    }
                }
            }
            if (background != null) background.raycastTarget = true;
            foreach (var button in window.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                ApplyButton(button);
        }

        public static void ApplyInspection(GameObject window)
        {
            var skin = Resources.Load<InteractionUiSkin>("InteractionUiSkin");
            if (window == null || skin == null || skin.windowTemplate == null) return;
            CopyRect((RectTransform)skin.windowTemplate.transform, (RectTransform)window.transform);
            foreach (string name in new[] { "Title", "Stats", "Health" })
            {
                var source = skin.windowTemplate.transform.Find(name);
                var target = window.transform.Find(name);
                if (source == null || target == null) continue;
                CopyRect((RectTransform)source, (RectTransform)target);
                var sourceText = source.GetComponent<TMPro.TMP_Text>();
                var targetText = target.GetComponent<TMPro.TMP_Text>();
                if (sourceText != null && targetText != null) CopyTextStyle(sourceText, targetText);
                if (name == "Health")
                {
                    foreach (string path in new[] { "Track", "Track/Inset" })
                    {
                        var a = source.Find(path); var b = target.Find(path);
                        if (a != null && b != null) CopyRect((RectTransform)a, (RectTransform)b);
                    }
                }
                if (name == "Health")
                    foreach (var text in target.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    {
                        var reference = source.Find(text.name);
                        if (reference == null) continue;
                        CopyRect((RectTransform)reference, text.rectTransform);
                        CopyTextStyle(reference.GetComponent<TMPro.TMP_Text>(), text);
                    }
            }
            var divider = window.transform.Find("Title Divider");
            if (divider != null) divider.gameObject.SetActive(false);
            Apply(window);
        }

        public static void ApplyFacility(GameObject window)
        {
            var skin = Resources.Load<InteractionUiSkin>("InteractionUiSkin");
            if (skin != null && skin.windowTemplate != null)
                CopyRect((RectTransform)skin.windowTemplate.transform, (RectTransform)window.transform);
            Apply(window);
        }

        public static void CopyRect(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin; target.anchorMax = source.anchorMax;
            target.pivot = source.pivot; target.sizeDelta = source.sizeDelta;
            target.anchoredPosition = source.anchoredPosition;
        }

        private static void CopyTextStyle(TMPro.TMP_Text source, TMPro.TMP_Text target)
        {
            target.font = source.font; target.fontSize = source.fontSize;
            target.fontStyle = source.fontStyle; target.color = source.color;
        }

        public static void ApplyButton(UnityEngine.UI.Button button)
        {
            var skin = Resources.Load<InteractionUiSkin>("InteractionUiSkin");
            if (skin == null || button.image == null) return;
            var image = button.image;
            HidePreviousFrame(button.transform);
            image.sprite = skin.normalButton; image.overrideSprite = null;
            image.type = UnityEngine.UI.Image.Type.Sliced; image.color = Color.white;
            image.raycastTarget = true; button.targetGraphic = image;
            button.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
            button.spriteState = new UnityEngine.UI.SpriteState
            {
                highlightedSprite = skin.activeButton, selectedSprite = skin.activeButton,
                pressedSprite = skin.activeButton, disabledSprite = skin.normalButton
            };
        }

        private static void HidePreviousFrame(Transform owner)
        {
            var previous = owner.Find("Castle Frame");
            if (previous != null) previous.gameObject.SetActive(false);
        }
    }
}
