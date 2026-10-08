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
        public void SetCount(int count, int capacity, Sprite icon)
        {
            countText.text = $"{count} / {capacity}";
            countText.color = count >= capacity ? new Color32(255, 171, 154, 255) : new Color32(245, 237, 224, 255);
            skullIcon.sprite = icon;
            skullIcon.color = icon != null ? Color.white : new Color(1, 1, 1, 0.12f);
        }
    }
}
