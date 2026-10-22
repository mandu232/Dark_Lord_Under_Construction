using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DarkLord.UI
{
    public sealed class RoomCapacityLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image skullIcon;
        public string CountText => countText.text;
        public bool PlacementAllowed { get; private set; }
        public void SetPlacementAvailability(bool allowed)
        {
            PlacementAllowed = allowed;
            countText.color = allowed ? new Color32(121, 255, 164, 255) : new Color32(255, 133, 125, 255);
        }
        public void SetCount(int count, int capacity, Sprite icon)
        {
            countText.text = $"{count} / {capacity}";
            var background = GetComponent<Image>();
            if (background != null) background.color = new Color(.035f, .025f, .045f, .94f);
            skullIcon.enabled = icon != null;
            countText.rectTransform.offsetMin = new Vector2(icon != null ? 44 : 8, countText.rectTransform.offsetMin.y);
            countText.color = count >= capacity ? new Color32(255, 171, 154, 255) : new Color32(245, 237, 224, 255);
            skullIcon.sprite = icon;
            skullIcon.color = icon != null ? Color.white : new Color(1, 1, 1, 0.12f);
        }
    }
}
