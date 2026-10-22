using DarkLord.UI;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DarkLord.Editor
{
    // In-place HUD styling; preserves health bindings and existing prefab identities.
    public static class PendantHealthSetup
    {
        public const string Art = "Assets/Sprtie/UI/InGame/PendantHealth";
        public static void Apply(GameHud hud)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Art + "/HPBar-Sheet.png").OfType<Sprite>().ToArray();
            var frameSprite = sprites.FirstOrDefault(sprite => sprite.name == "PendantFrame");
            var rubySprite = sprites.FirstOrDefault(sprite => sprite.name == "PendantGemFill");
            if (frameSprite == null || rubySprite == null) return;
            var panel = hud.transform.Find("Player Status");
            if (panel == null) return;
            const float scale = 4;
            Place((RectTransform)panel, Vector2.up, new Vector2(-56, 0), new Vector2(396, 256));
            foreach (var image in panel.GetComponentsInChildren<UnityEngine.UI.Image>(true)) image.enabled = false;
            foreach (var outline in panel.GetComponentsInChildren<UnityEngine.UI.Outline>(true)) outline.enabled = false;
            var title = panel.Find("Title");
            if (title != null) title.gameObject.SetActive(false);
            var track = panel.Find("Health Track");
            if (track != null) track.gameObject.SetActive(false);
            var frame = Image(panel, "Pendant Frame");
            frame.sprite = frameSprite;
            Place(frame.rectTransform, Vector2.up, Vector2.zero, frameSprite.rect.size * scale);
            frame.type = UnityEngine.UI.Image.Type.Simple;
            var fill = Image(panel, "Pendant Ruby");
            fill.sprite = rubySprite;
            // Both authored cells share a 64x64 coordinate system. Crop only transparent
            // gem padding so Image.fillAmount measures the visible gem, not the sheet.
            var rubyRect = rubySprite.rect;
            Place(fill.rectTransform, Vector2.up,
                new Vector2(rubyRect.x * scale, -(frameSprite.rect.height - rubyRect.yMax) * scale),
                rubyRect.size * scale);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical;
            fill.fillOrigin = (int)UnityEngine.UI.Image.OriginVertical.Bottom;
            fill.fillAmount = 1;
            var text = panel.Find("Health").GetComponent<TMP_Text>();
            Place(text.rectTransform, Vector2.up, new Vector2(206, -102), new Vector2(190, 42));
            text.fontSize = 23;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.alignment = TextAlignmentOptions.Left;
            text.color = new Color32(245, 231, 211, 255);
            text.margin = Vector4.zero;
            text.raycastTarget = false;
            var data = new SerializedObject(hud);
            data.FindProperty("healthFill").objectReferenceValue = fill;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static UnityEngine.UI.Image Image(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)).transform;
                child.SetParent(parent, false);
            }
            var image = child.GetComponent<UnityEngine.UI.Image>();
            image.enabled = true;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
