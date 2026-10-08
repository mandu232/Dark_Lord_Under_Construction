using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DarkLord.UI
{
    public sealed class InspectionHealthBar : MonoBehaviour
    {
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Image fill;
        public string Label => valueText.text;
        public float Ratio { get; private set; }
        public void SetHealth(int current, int maximum)
        {
            Ratio = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0;
            valueText.text = $"체력  {current} / {maximum}";
            var rect = fill.rectTransform;
            rect.anchorMax = new Vector2(Ratio, 1);
            fill.enabled = Ratio > 0;
            fill.color = Ratio <= .3f ? new Color32(235, 83, 79, 255) : Ratio <= .6f ? new Color32(235, 185, 81, 255) : new Color32(92, 204, 133, 255);
        }
    }
}
